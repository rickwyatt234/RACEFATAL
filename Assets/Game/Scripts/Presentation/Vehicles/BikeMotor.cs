using RaceFatal.Presentation.Tracks;
using RaceFatal.Vehicles;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(TrackSurfaceProbe))]
    public class BikeMotor : MonoBehaviour
    {
#region Serialized Fields
        [Header("References")]
        [SerializeField]
        private Transform centerOfMass;
        [Header("Magnetic Surface Lock")] [Tooltip("Desired distance between the bike root and the track surface.")] [Min(0.01f)] [SerializeField] private float desiredSurfaceDistance =
            0.5f;
        [Tooltip("How strongly the bike corrects surface-distance error. Higher values make the magnetic suspension stiffer.")] [Min(0f)] [SerializeField] private float surfaceSpringStrength =
            90f;
        [Tooltip("Damps movement toward or away from the track. This is especially important at crests.")] [Min(0f)] [SerializeField] private float surfaceSpringDamping =
            16f;
        [Tooltip("Maximum acceleration the magnetic suspension may apply in either direction.")] [Min(0f)] [SerializeField] private float maximumSurfaceCorrection =
            140f;
        [Tooltip("When a surface is detected, outward velocity above this value is suppressed.")] [Min(0f)] [SerializeField] private float maximumSurfaceSeparationSpeed =
            1.5f;
        [Tooltip("The separation-speed limiter only operates while the track is within this distance.")] [Min(0.1f)] [SerializeField] private float surfaceLockDistance =
            2f;
#endregion
#region Surface Loss
        [Header("Surface Loss")]
        [Tooltip("How long a newly spawned bike may wait for its first surface before airborne gravity begins.")]
        [Min(0f)]
        [SerializeField]
        private float initialSurfaceGraceTime = 0.5f;
        [Tooltip("How long a bike may temporarily lose the track before being considered airborne.")] [Min(0f)] [SerializeField] private float surfaceLossGraceTime =
            0.2f;
        [Tooltip("Light magnetic pull retained during the surface loss grace period. Helps cross collider seams.")] [Min(0f)] [SerializeField] private float graceAdhesionAcceleration =
            25f;
        [Tooltip("Gravity used after the bike has genuinely become airborne.")] [Min(0f)] [SerializeField] private float airGravityAcceleration = 12f;
#endregion
#region Surface Alignment
        [Header("Surface Alignment")]
        [Tooltip("How quickly the bike's up axis aligns with the track surface normal.")]
        [Min(0f)]
        [SerializeField]
        private float alignmentSpeed = 14f;
        [Tooltip("Largest road-normal change transported in one physics step. Larger discontinuities are treated as a landing.")]
        [Range(1f, 89f)] [SerializeField] private float maximumTransportAngle = 45f;
        [Header("Rotation Stability")] [Tooltip("Prevents physics, collisions, and impacts from rotating the bike. Intentional steering still works through BikeMotor.")] [SerializeField] private bool lockPhysicsRotation =
            true;
#endregion
#region Steering
        [Header("Steering")]
        [Tooltip("Input response rate per second before the chassis steering response multiplier.")]
        [Min(0.1f)]
        [SerializeField]
        private float steeringResponse = 20f;
        [Tooltip("Maximum steering rate at low speed, in degrees per second.")] [Min(0f)] [SerializeField] private float lowSpeedTurnRate = 120f;
        [Tooltip("Maximum steering rate near top speed.")] [Min(0f)] [SerializeField] private float highSpeedTurnRate = 35f;
        [Tooltip("How quickly lateral sliding is removed.")] [Min(0f)] [SerializeField] private float lateralGrip = 5f;
        [Tooltip("Prevents lateral-grip corrections from producing huge destabilizing forces.")] [Min(0f)] [SerializeField] private float maximumLateralGripAcceleration =
            35f;
#endregion
#region Braking
        [Header("Braking")]
        [Min(0f)]
        [SerializeField]
        private float brakeDeceleration = 30f;
#endregion
#region Overspeed
        [Header("Overspeed")]
        [Min(0f)]
        [SerializeField]
        private float overspeedCorrection = 3f;
#endregion
#region Collision Stability
        [Header("Collision Stability")]
        [Tooltip("Limits the velocity PhysX may use when resolving overlapping colliders. Lower values reduce catapult-like bike collisions.")]
        [Min(0.1f)]
        [SerializeField]
        private float maximumDepenetrationVelocity = 8f;
        [Header("Wall Collision Recovery")] [Tooltip("How long after a wall rebound the bike strongly resists separating from the track surface.")] [Min(0f)] [SerializeField] private float wallRecoveryDuration =
            0.15f;
        [Tooltip("Maximum velocity away from the track allowed during wall recovery. Keep this near zero to prevent wall climbing.")] [Min(0f)] [SerializeField] private float wallMaximumSurfaceSeparationSpeed =
            0.1f;
#endregion
#region Runtime Debug
        [Header("Runtime Debug")]
        [SerializeField]
        private float debugThrottle;
        [SerializeField] private float debugBrake;
        [SerializeField] private float debugSteering;
        [SerializeField] private float debugSpeed;
        [SerializeField] private float debugForwardSpeed;
        [SerializeField] private float debugSurfaceDistance;
        [SerializeField] private float debugSurfaceNormalSpeed;
        [SerializeField] private float debugSurfaceCorrection;
        [SerializeField] private bool debugHasSurface;
        [SerializeField] private float debugTimeWithoutSurface;
        [Header("Collision Debug")] [SerializeField] private float debugLastCollisionRebound;
        [SerializeField] private bool debugWallRecoveryActive;
        [SerializeField] private float debugWallRecoveryTimer;
        [SerializeField] private float debugWallOutwardSpeed;
        [Header("Diagnostics")] [SerializeField] private bool logMissingSurface = true;
#endregion
#region Runtime
        private RaceFatal.Presentation.Racing.RacerViewController racer;
        private float Perk(RaceFatal.Career.RacerPerkEffect effect) => racer?.Participant?.Perks.Bonus(effect) ?? 0f;
        private int racerMask;
        private Rigidbody body;
        private TrackSurfaceProbe surfaceProbe;
        private BikePerformance performance;
        private float throttleInput;
        private float brakeInput;
        private float steeringInput;
        private float targetSteeringInput;
        private float speedMultiplier = 1f;
        private float accelerationMultiplier = 1f;
        private float handlingMultiplier = 1f;
        private float timeWithoutSurface;
        private float wallRecoveryTimer;
        private bool missingSurfaceWarningIssued;
        private bool hasAcquiredSurface;
        private bool surfaceAttached;
        private Vector3 previousSurfaceNormal;
        private float curvatureNormalSpeed;
        private Quaternion surfaceRotation;
        private Vector3 surfaceForward;
        public float ThrottleInput => throttleInput;
        public float BrakeInput => brakeInput;
        public float SteeringInput => steeringInput;
        public float LeanResponseMultiplier => performance?.LeanResponseMultiplier ?? 1f;
        public bool HasSurface => surfaceProbe != null && surfaceProbe.HasSurface;

        public float SpeedMetersPerSecond
        {
            get
            {
                if (body == null)
                    return 0f;
                return body.linearVelocity.magnitude;
            }
        }

        public float NormalizedSpeed
        {
            get
            {
                if (performance == null)
                    return 0f;
                float topSpeed = Mathf.Max(0.1f, performance.TopSpeedMetersPerSecond);
                return Mathf.Clamp01(SpeedMetersPerSecond / topSpeed);
            }
        }

        public float SpeedFeetPerSecond => SpeedMetersPerSecond * 3.28084f;
        public float SpeedKph => SpeedMetersPerSecond * 3.6f;
        public float SpeedMPH => SpeedMetersPerSecond * 2.23694f;

#endregion
#region Unity
        private void Awake()
        {
            racer = GetComponent<RaceFatal.Presentation.Racing.RacerViewController>();
            racerMask = LayerMask.GetMask("Racer");
            body = GetComponent<Rigidbody>();
            surfaceProbe = GetComponent<TrackSurfaceProbe>();
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.maxDepenetrationVelocity = maximumDepenetrationVelocity;
            if (centerOfMass != null)
            {
                body.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
            }
        }

        private void OnEnable()
        {
            ResetSurfaceTracking();
            wallRecoveryTimer = 0f;
            missingSurfaceWarningIssued = false;
            debugWallRecoveryActive = false;
            debugWallRecoveryTimer = 0f;
            debugWallOutwardSpeed = 0f;
        }

        private void FixedUpdate()
        {
            if (performance == null)
                return;
            surfaceRotation = body.rotation;
            surfaceForward = surfaceRotation * Vector3.forward;
            steeringInput = Mathf.Lerp(steeringInput, targetSteeringInput, 1f - Mathf.Exp(-steeringResponse * performance.SteeringResponseMultiplier * Time.fixedDeltaTime));
            UpdateWallRecoveryTimer();
            SuppressPhysicsRotation();
            debugSpeed = SpeedMetersPerSecond;
            bool hasSurface = surfaceProbe.Sample();
            debugHasSurface = hasSurface;
            if (hasSurface)
            {
                bool continuousAttachment = surfaceAttached && timeWithoutSurface <= surfaceLossGraceTime;
                timeWithoutSurface = 0f;
                debugTimeWithoutSurface = 0f;
                missingSurfaceWarningIssued = false;
                RunSurfacePhysics(continuousAttachment);
                hasAcquiredSurface = true;
                ApplyWallRecovery(surfaceProbe.ContactNormal);
                SuppressPhysicsRotation();
                return;
            }

            HandleMissingSurface();
            if (wallRecoveryTimer > 0f)
            {
                ApplyWallRecovery(surfaceProbe.LastSurfaceNormal);
            }

            SuppressPhysicsRotation();
        }

        private void OnCollisionEnter(Collision collision)
        {
            SuppressPhysicsRotation();
        }

        private void OnCollisionStay(Collision collision)
        {
            SuppressPhysicsRotation();
        }

#endregion
#region Initialization
        // Called on reuse/grid placement so an old road frame cannot rotate a new
        // spawn's velocity or make it inherit another section's airborne gravity.
        public void ResetSurfaceTracking()
        {
            timeWithoutSurface = 0f;
            wallRecoveryTimer = 0f;
            missingSurfaceWarningIssued = false;
            debugHasSurface = false;
            debugTimeWithoutSurface = 0f;
            hasAcquiredSurface = false;
            surfaceAttached = false;
            curvatureNormalSpeed = 0f;
            previousSurfaceNormal = body != null ? body.rotation * Vector3.up : transform.up;
            surfaceRotation = body != null ? body.rotation : transform.rotation;
            surfaceForward = surfaceRotation * Vector3.forward;
            surfaceProbe?.ResetTracking();
        }

        public void SetPerformance(BikePerformance bikePerformance)
        {
            performance = bikePerformance;
            if (performance != null && body != null)
            {
                body.mass = performance.Mass;
            }
        }

        public void SetRuntimeModifiers(float speed, float acceleration, float handling)
        {
            speedMultiplier = Mathf.Max(0f, speed);
            accelerationMultiplier = Mathf.Max(0f, acceleration);
            handlingMultiplier = Mathf.Max(0f, handling);
        }

        public void SetControls(float throttle, float brake, float steering)
        {
            throttleInput = Mathf.Clamp01(throttle);
            brakeInput = Mathf.Clamp01(brake);
            targetSteeringInput = Mathf.Clamp(steering, -1f, 1f);
            debugThrottle = throttleInput;
            debugBrake = brakeInput;
            debugSteering = steeringInput;
        }

#endregion
#region Collision Response
        public void ApplyCollisionRebound(Vector3 collisionNormal, float velocityChange)
        {
            if (body == null || velocityChange <= 0f || collisionNormal.sqrMagnitude < 0.001f)
            {
                return;
            }

            Vector3 reboundDirection = collisionNormal.normalized;
            if (surfaceProbe != null && surfaceProbe.HasSurface)
            {
                Vector3 planarDirection = Vector3.ProjectOnPlane(reboundDirection, surfaceProbe.ContactNormal);
                if (planarDirection.sqrMagnitude > 0.001f)
                {
                    reboundDirection = planarDirection.normalized;
                }
            }

            body.AddForce(reboundDirection * velocityChange, ForceMode.VelocityChange);
            debugLastCollisionRebound = velocityChange;
            BeginWallRecovery();
            if (surfaceProbe != null)
            {
                Vector3 surfaceNormal = surfaceProbe.HasSurface ? surfaceProbe.ContactNormal : surfaceProbe.LastSurfaceNormal;
                ApplyWallRecovery(surfaceNormal);
            }

            SuppressPhysicsRotation();
        }

        public void ApplyExternalVelocityChange(Vector3 worldDirection, float velocityChange)
        {
            if (body == null || velocityChange <= 0f || worldDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            Vector3 direction = worldDirection.normalized;
            if (surfaceProbe != null)
            {
                Vector3 surfaceNormal = surfaceProbe.HasSurface ? surfaceProbe.ContactNormal : surfaceProbe.LastSurfaceNormal;
                if (surfaceNormal.sqrMagnitude > 0.001f)
                {
                    Vector3 planar = Vector3.ProjectOnPlane(direction, surfaceNormal);
                    if (planar.sqrMagnitude > 0.001f)
                        direction = planar.normalized;
                }
            }

            body.AddForce(direction * velocityChange, ForceMode.VelocityChange);
            SuppressPhysicsRotation();
        }

        public void ApplyLateralDash(float signedDistance, float duration)
        {
            if (body == null || Mathf.Abs(signedDistance) <= 0.001f)
            {
                return;
            }

            float safeDuration = Mathf.Max(0.04f, duration);
            float requestedVelocityChange = Mathf.Abs(signedDistance) / safeDuration;
            Vector3 lateralDirection = body.rotation * Vector3.right * Mathf.Sign(signedDistance);
            ApplyExternalVelocityChange(lateralDirection, requestedVelocityChange);
        }

        private void BeginWallRecovery()
        {
            wallRecoveryTimer = Mathf.Max(wallRecoveryTimer, wallRecoveryDuration);
            debugWallRecoveryActive = wallRecoveryTimer > 0f;
            debugWallRecoveryTimer = wallRecoveryTimer;
        }

        private void UpdateWallRecoveryTimer()
        {
            if (wallRecoveryTimer <= 0f)
            {
                wallRecoveryTimer = 0f;
                debugWallRecoveryTimer = 0f;
                debugWallRecoveryActive = false;
                debugWallOutwardSpeed = 0f;
                return;
            }

            wallRecoveryTimer = Mathf.Max(0f, wallRecoveryTimer - Time.fixedDeltaTime);
            debugWallRecoveryTimer = wallRecoveryTimer;
            debugWallRecoveryActive = wallRecoveryTimer > 0f;
        }

        private void ApplyWallRecovery(Vector3 surfaceNormal)
        {
            if (wallRecoveryTimer <= 0f || body == null || surfaceNormal.sqrMagnitude < 0.001f)
            {
                return;
            }

            surfaceNormal.Normalize();
            float outwardSpeed = Vector3.Dot(body.linearVelocity, surfaceNormal) - curvatureNormalSpeed;
            debugWallOutwardSpeed = outwardSpeed;
            if (outwardSpeed <= wallMaximumSurfaceSeparationSpeed)
            {
                return;
            }

            float excess = outwardSpeed - wallMaximumSurfaceSeparationSpeed;
            body.linearVelocity -= surfaceNormal * excess;
            debugWallOutwardSpeed = Vector3.Dot(body.linearVelocity, surfaceNormal) - curvatureNormalSpeed;
        }

#endregion
#region Surface Physics
        private void RunSurfacePhysics(bool continuousAttachment)
        {
            Vector3 normal = surfaceProbe.ContactNormal;
            bool closeToRoad = surfaceProbe.SurfaceDistance <= surfaceLockDistance;
            if (continuousAttachment && closeToRoad && Vector3.Angle(previousSurfaceNormal, normal) <= maximumTransportAngle)
            {
                // Parallel transport only the road's tilt/bank, never its centreline
                // heading. This preserves speed AND lateral ram/slide velocity.
                Quaternion transport = Quaternion.FromToRotation(previousSurfaceNormal, normal);
                Vector3 velocity = body.linearVelocity - previousSurfaceNormal * curvatureNormalSpeed;
                Vector3 tangent = Vector3.ProjectOnPlane(velocity, previousSurfaceNormal);
                // Physics advances position using the next velocity. A half-step
                // curvature offset follows the arc instead of its tangent chord.
                // Replace last tick's offset so it never accumulates extra force.
                curvatureNormalSpeed = 0.5f * Vector3.Dot(transport * tangent - tangent, normal);
                body.linearVelocity = transport * velocity + normal * curvatureNormalSpeed;
                surfaceRotation = transport * body.rotation;
                surfaceForward = surfaceRotation * Vector3.forward;
            }
            else
            {
                curvatureNormalSpeed = 0f;
            }
            previousSurfaceNormal = normal;
            surfaceAttached = closeToRoad;
            debugSurfaceDistance = surfaceProbe.SurfaceDistance;
            ApplySurfaceLock(normal);
            ClampSurfaceSeparation(normal);
            AlignToSurface(surfaceProbe.SurfaceNormal);
            ApplyDrive(normal);
            ApplyBraking(normal);
            ApplyLateralGrip(normal);
        }

#endregion
#region Magnetic Suspension
        private void ApplySurfaceLock(Vector3 surfaceNormal)
        {
            float surfaceDistance = surfaceProbe.SurfaceDistance;
            float distanceError = surfaceDistance - desiredSurfaceDistance;
            float normalVelocity = Vector3.Dot(body.linearVelocity, surfaceNormal) - curvatureNormalSpeed;
            debugSurfaceNormalSpeed = normalVelocity;
            float correctionAcceleration = distanceError * surfaceSpringStrength;
            correctionAcceleration += normalVelocity * surfaceSpringDamping;
            correctionAcceleration = Mathf.Clamp(correctionAcceleration, -maximumSurfaceCorrection, maximumSurfaceCorrection);
            debugSurfaceCorrection = correctionAcceleration;
            body.AddForce(-surfaceNormal * correctionAcceleration, ForceMode.Acceleration);
        }

        private void ClampSurfaceSeparation(Vector3 surfaceNormal)
        {
            if (surfaceProbe.SurfaceDistance > surfaceLockDistance)
            {
                return;
            }

            float outwardSpeed = Vector3.Dot(body.linearVelocity, surfaceNormal) - curvatureNormalSpeed;
            if (outwardSpeed <= maximumSurfaceSeparationSpeed)
            {
                return;
            }

            float excess = outwardSpeed - maximumSurfaceSeparationSpeed;
            body.linearVelocity -= surfaceNormal * excess;
        }

#endregion
#region Alignment / Steering
        private void SuppressPhysicsRotation()
        {
            if (!lockPhysicsRotation || body == null)
            {
                return;
            }

            body.angularVelocity = Vector3.zero;
        }

        private void AlignToSurface(Vector3 normal)
        {
            Vector3 forward = Vector3.ProjectOnPlane(surfaceForward, normal);
            if (forward.sqrMagnitude < 0.001f)
                return;
            forward.Normalize();
            float effectiveHandling = Mathf.Clamp(performance.ConfiguredHandling * handlingMultiplier, 0.1f, 3f);
            float speedRatio = Mathf.Clamp01(SpeedMetersPerSecond / Mathf.Max(0.1f, performance.TopSpeedMetersPerSecond));
            float turnRate = Mathf.Lerp(lowSpeedTurnRate, highSpeedTurnRate, speedRatio);
            float steeringAuthority = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(SpeedMetersPerSecond / 8f));
            float turnAngle = steeringInput * turnRate * effectiveHandling * steeringAuthority * Time.fixedDeltaTime;
            forward = Quaternion.AngleAxis(turnAngle, normal) * forward;
            Quaternion targetRotation = Quaternion.LookRotation(forward, normal);
            float alignmentT = 1f - Mathf.Exp(-alignmentSpeed * Time.fixedDeltaTime);
            Quaternion aligned = Quaternion.Slerp(surfaceRotation, targetRotation, alignmentT);
            body.MoveRotation(aligned);
            surfaceForward = aligned * Vector3.forward;
        }

#endregion
#region Drive
        private void ApplyDrive(Vector3 surfaceNormal)
        {
            Vector3 forward = Vector3.ProjectOnPlane(surfaceForward, surfaceNormal);
            if (forward.sqrMagnitude < 0.001f)
            {
                debugForwardSpeed = 0f;
                return;
            }

            forward.Normalize();
            float forwardSpeed = Vector3.Dot(body.linearVelocity, forward);
            debugForwardSpeed = forwardSpeed;
            float maximumSpeed = performance.TopSpeedMetersPerSecond * speedMultiplier;
            if (throttleInput > 0f && forwardSpeed < maximumSpeed)
            {
                float acceleration = performance.Acceleration * accelerationMultiplier;
                float draft = Perk(RaceFatal.Career.RacerPerkEffect.SlipstreamAcceleration);
                if (draft > 0f && Physics.Raycast(transform.position + forward * 2.5f, forward, out var hit, 20f, racerMask, QueryTriggerInteraction.Ignore) &&
                    hit.collider.attachedRigidbody != body && Vector3.Dot(hit.collider.transform.forward, forward) > .8f)
                    acceleration *= 1f + draft;
                if (racer?.Participant?.Perks.Has(RaceFatal.Career.RacerPerkEffect.IronWill) == true &&
                    racer.Participant.Vehicle.Damage.CurrentIntegrity < performance.MaxIntegrity * .25f) acceleration *= .95f;
                body.AddForce(forward * acceleration * throttleInput, ForceMode.Acceleration);
            }

            if (forwardSpeed > maximumSpeed)
            {
                float excess = forwardSpeed - maximumSpeed;
                body.AddForce(-forward * excess * overspeedCorrection, ForceMode.Acceleration);
            }
        }

#endregion
#region Braking
        private void ApplyBraking(Vector3 surfaceNormal)
        {
            if (brakeInput <= 0f)
                return;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, surfaceNormal);
            if (planarVelocity.sqrMagnitude < 0.001f)
                return;
            body.AddForce(-planarVelocity.normalized * brakeDeceleration * (1f + Perk(RaceFatal.Career.RacerPerkEffect.LateBraking)) * brakeInput, ForceMode.Acceleration);
        }

#endregion
#region Lateral Grip
        private void ApplyLateralGrip(Vector3 surfaceNormal)
        {
            Vector3 forward = Vector3.ProjectOnPlane(surfaceForward, surfaceNormal);
            if (forward.sqrMagnitude < 0.001f)
                return;
            forward.Normalize();
            Vector3 right = Vector3.Cross(surfaceNormal, forward);
            if (right.sqrMagnitude < 0.001f)
                return;
            right.Normalize();
            float lateralSpeed = Vector3.Dot(body.linearVelocity, right);
            float effectiveGrip = lateralGrip * performance.StabilityMultiplier * Mathf.Clamp(performance.ConfiguredHandling * handlingMultiplier, 0.1f, 3f);
            effectiveGrip *= 1f + Mathf.Abs(steeringInput) * Perk(RaceFatal.Career.RacerPerkEffect.CorneringStability);
            if (Mathf.Abs(forward.y) > .35f) effectiveGrip *= 1f + Perk(RaceFatal.Career.RacerPerkEffect.DescentStability);
            if (brakeInput > .7f && Perk(RaceFatal.Career.RacerPerkEffect.LateBraking) > 0f) effectiveGrip *= .97f;
            float lateralAcceleration = -lateralSpeed * effectiveGrip;
            lateralAcceleration = Mathf.Clamp(lateralAcceleration, -maximumLateralGripAcceleration, maximumLateralGripAcceleration);
            body.AddForce(right * lateralAcceleration, ForceMode.Acceleration);
        }

#endregion
#region Surface Loss
        private void HandleMissingSurface()
        {
            timeWithoutSurface += Time.fixedDeltaTime;
            debugTimeWithoutSurface = timeWithoutSurface;
            debugSurfaceCorrection = 0f;
            if (!hasAcquiredSurface)
            {
                if (timeWithoutSurface <= initialSurfaceGraceTime)
                {
                    return;
                }

                WarnAboutMissingSurface();
                ApplyAirGravity();
                return;
            }

            if (surfaceAttached && timeWithoutSurface <= surfaceLossGraceTime)
            {
                Vector3 estimatedNormal = surfaceProbe.LastSurfaceNormal;
                AlignToSurface(estimatedNormal);
                body.AddForce(-estimatedNormal * graceAdhesionAcceleration, ForceMode.Acceleration);
                ApplyDrive(estimatedNormal);
                ApplyBraking(estimatedNormal);
                ApplyLateralGrip(estimatedNormal);
                return;
            }

            ApplyAirGravity();
            surfaceAttached = false;
            curvatureNormalSpeed = 0f;
        }

        private void ApplyAirGravity()
        {
            Vector3 gravityDirection = -surfaceProbe.LastSurfaceNormal;
            if (gravityDirection.sqrMagnitude < 0.001f)
            {
                gravityDirection = -transform.up;
            }

            gravityDirection.Normalize();
            body.AddForce(gravityDirection * airGravityAcceleration, ForceMode.Acceleration);
        }

#endregion
#region Diagnostics
        private void WarnAboutMissingSurface()
        {
            if (!logMissingSurface || missingSurfaceWarningIssued)
            {
                return;
            }

            missingSurfaceWarningIssued = true;
            Debug.LogWarning($"BikeMotor on '{name}' could not acquire " + $"a track surface after " + $"{initialSurfaceGraceTime:F2} seconds.", this);
        }
#endregion
    }
}
