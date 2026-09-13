using RaceFatal.Combat;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(RacerViewController))]
    [RequireComponent(typeof(BikeMotor))]
    [RequireComponent(typeof(CollisionFeedbackView))]
    public class RacerCollisionDamage : MonoBehaviour
    {
        #region Racer Collisions

        [Header("Racer Collisions")]
        [Tooltip("Minimum velocity into another bike required before collision damage occurs.")]
        [Min(0f)][SerializeField] private float minimumRacerImpactSpeed = 5f;

        [Tooltip("Damage produced per m/s above the minimum impact speed.")]
        [Min(0f)][SerializeField] private float racerDamageMultiplier = 0.5f;

        [Tooltip("Maximum damage a single bike-on-bike collision can cause.")]
        [Min(0f)][SerializeField] private float maximumRacerCollisionDamage = 15f;

        [Tooltip("Prevents compound colliders from rapidly registering the same racer collision multiple times.")]
        [Min(0f)][SerializeField] private float racerCollisionCooldown = 0.15f;

        #endregion

        #region Wall Collisions

        [Header("Track Wall Collisions")]
        [Tooltip("Minimum velocity into a wall before wall damage or rebound occurs.")]
        [Min(0f)][SerializeField] private float minimumWallImpactSpeed = 5f;

        [Tooltip("Hull/shield damage produced per m/s above the wall-impact threshold.")]
        [Min(0f)][SerializeField] private float wallDamageMultiplier = 0.15f;

        [Tooltip("Maximum damage from one non-fatal wall impact.")]
        [Min(0f)][SerializeField] private float maximumWallCollisionDamage = 6f;

        [Tooltip("Velocity-change impulse applied away from the wall per m/s of impact above the threshold.")]
        [Min(0f)][SerializeField] private float wallReboundMultiplier = 0.08f;

        [Tooltip("Maximum velocity-change impulse applied by a wall collision.")]
        [Min(0f)][SerializeField] private float maximumWallRebound = 2f;

        [Tooltip("Prevents collider seams from repeatedly damaging the bike during one wall impact.")]
        [Min(0f)][SerializeField] private float wallCollisionCooldown = 0.15f;

        #endregion

        #region Fatal Wall Impacts

        [Header("Fatal Wall Impacts")]
        [Tooltip("If enabled, sufficiently direct and high-speed wall impacts instantly destroy the bike.")]
        [SerializeField] private bool enableFatalWallImpacts = true;

        [Tooltip("If enabled, only the player's bike can be instantly destroyed by catastrophic wall impacts.")]
        [SerializeField] private bool fatalWallImpactsPlayerOnly = true;

        [Tooltip("Minimum wall-impact speed required for an instant destruction.")]
        [Min(0f)][SerializeField] private float fatalWallImpactSpeed = 20f;

        [Tooltip("Minimum impact angle for instant destruction. 0 = parallel/grazing, 90 = perfectly head-on.")]
        [Range(0f, 90f)][SerializeField] private float fatalWallImpactAngle = 65f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private string debugCollisionType = "None";
        [SerializeField] private string debugOtherRacer = "None";
        [SerializeField] private float debugImpactSpeed;
        [SerializeField] private float debugWallImpactAngle;
        [SerializeField] private float debugDamage;
        [SerializeField] private float debugRebound;
        [SerializeField] private float debugFeedbackSeverity;
        [SerializeField] private bool debugFatalWallImpact;
        [SerializeField] private bool debugRuntimeResolved;

        #endregion

        #region Runtime

        private RaceRuntimeController runtime;
        private RacerViewController racer;
        private BikeMotor motor;
        private CollisionFeedbackView collisionFeedback;

        private string lastRacerCollisionId;
        private float lastRacerCollisionTime = -1000f;
        private float lastWallCollisionTime = -1000f;

        #endregion

        private void Awake()
        {
            racer = GetComponent<RacerViewController>();
            motor = GetComponent<BikeMotor>();
            collisionFeedback = GetComponent<CollisionFeedbackView>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null ||
                collision.collider == null)
            {
                return;
            }

            if (!CanProcessCollision())
                return;

            RacerViewController other =
                collision.collider.GetComponentInParent<
                    RacerViewController>();

            if (other != null)
            {
                HandleRacerCollision(
                    collision,
                    other);

                return;
            }

            TrackWall wall =
                collision.collider.GetComponentInParent<
                    TrackWall>();

            if (wall != null)
                HandleWallCollision(collision);
        }

        #region Racer Collision

        private void HandleRacerCollision(
            Collision collision,
            RacerViewController other)
        {
            if (other == null ||
                !other.IsInitialized ||
                other.Participant == null)
            {
                return;
            }

            if (other == racer ||
                other.RacerId == racer.RacerId)
            {
                return;
            }

            if (lastRacerCollisionId == other.RacerId &&
                Time.time - lastRacerCollisionTime <
                racerCollisionCooldown)
            {
                return;
            }

            if (!TryGetImpact(
                    collision,
                    out float impactSpeed,
                    out Vector3 contactPoint,
                    out Vector3 contactNormal,
                    out _))
            {
                return;
            }

            debugCollisionType = "Racer";
            debugOtherRacer = other.RacerId;
            debugImpactSpeed = impactSpeed;
            debugWallImpactAngle = 0f;
            debugFatalWallImpact = false;
            debugRebound = 0f;

            if (impactSpeed < minimumRacerImpactSpeed)
            {
                debugDamage = 0f;
                debugFeedbackSeverity = 0f;
                return;
            }

            float damage =
                Mathf.Min(
                    maximumRacerCollisionDamage,
                    (impactSpeed - minimumRacerImpactSpeed) *
                    racerDamageMultiplier);

            if (damage <= 0f)
                return;

            lastRacerCollisionId = other.RacerId;
            lastRacerCollisionTime = Time.time;

            debugDamage = damage;

            float severity =
                CalculateRacerFeedbackSeverity(
                    impactSpeed);

            debugFeedbackSeverity = severity;

            collisionFeedback?.PlayImpact(
                CollisionImpactType.Racer,
                contactPoint,
                contactNormal,
                severity);

            runtime.Director.ApplyDamage(
                other.RacerId,
                racer.RacerId,
                damage,
                DamageCause.Collision);
        }

        #endregion

        #region Wall Collision

        private void HandleWallCollision(
            Collision collision)
        {
            if (Time.time - lastWallCollisionTime <
                wallCollisionCooldown)
            {
                return;
            }

            if (!TryGetImpact(
                    collision,
                    out float impactSpeed,
                    out Vector3 contactPoint,
                    out Vector3 contactNormal,
                    out float impactAngle))
            {
                return;
            }

            debugCollisionType = "Track Wall";
            debugOtherRacer = "None";
            debugImpactSpeed = impactSpeed;
            debugWallImpactAngle = impactAngle;
            debugFatalWallImpact = false;

            lastWallCollisionTime = Time.time;

            if (IsFatalWallImpact(
                    impactSpeed,
                    impactAngle))
            {
                debugFatalWallImpact = true;
                debugDamage = GetLethalDamage();
                debugRebound = 0f;
                debugFeedbackSeverity = 1f;

                collisionFeedback?.PlayImpact(
                    CollisionImpactType.TrackWall,
                    contactPoint,
                    contactNormal,
                    1f,
                    true);

                runtime.Director.ApplyDamage(
                    null,
                    racer.RacerId,
                    debugDamage,
                    DamageCause.Environmental);

                return;
            }

            if (impactSpeed < minimumWallImpactSpeed)
            {
                debugDamage = 0f;
                debugRebound = 0f;
                debugFeedbackSeverity = 0f;
                return;
            }

            float excessImpact =
                impactSpeed -
                minimumWallImpactSpeed;

            float damage =
                Mathf.Min(
                    maximumWallCollisionDamage,
                    excessImpact *
                    wallDamageMultiplier);

            float rebound =
                Mathf.Min(
                    maximumWallRebound,
                    excessImpact *
                    wallReboundMultiplier);

            float severity =
                CalculateWallFeedbackSeverity(
                    impactSpeed);

            debugDamage = damage;
            debugRebound = rebound;
            debugFeedbackSeverity = severity;

            collisionFeedback?.PlayImpact(
                CollisionImpactType.TrackWall,
                contactPoint,
                contactNormal,
                severity);

            if (damage > 0f)
            {
                runtime.Director.ApplyDamage(
                    null,
                    racer.RacerId,
                    damage,
                    DamageCause.Environmental);
            }

            if (rebound > 0f &&
                motor != null)
            {
                motor.ApplyCollisionRebound(
                    contactNormal,
                    rebound);
            }
        }

        private bool IsFatalWallImpact(
            float impactSpeed,
            float impactAngle)
        {
            if (!enableFatalWallImpacts)
                return false;

            if (fatalWallImpactsPlayerOnly &&
                racer.Participant.Role !=
                RaceParticipantRole.Player)
            {
                return false;
            }

            return
                impactSpeed >= fatalWallImpactSpeed &&
                impactAngle >= fatalWallImpactAngle;
        }

        private float GetLethalDamage()
        {
            RaceVehicleState vehicle =
                racer.Participant.Vehicle;

            float remainingHull =
                Mathf.Max(
                    0f,
                    DamageMeter.MaxDamage -
                    vehicle.Damage.Percent);

            RaceShieldState shield =
                vehicle.EquipmentSystem.Shield;

            float remainingShield =
                shield != null
                    ? shield.Current
                    : 0f;

            return
                remainingShield +
                remainingHull +
                1f;
        }

        #endregion

        #region Feedback Severity

        private float CalculateRacerFeedbackSeverity(
            float impactSpeed)
        {
            float speedForMaximumDamage;

            if (racerDamageMultiplier <= 0.001f)
            {
                speedForMaximumDamage =
                    minimumRacerImpactSpeed + 1f;
            }
            else
            {
                speedForMaximumDamage =
                    minimumRacerImpactSpeed +
                    maximumRacerCollisionDamage /
                    racerDamageMultiplier;
            }

            return Mathf.Clamp01(
                Mathf.InverseLerp(
                    minimumRacerImpactSpeed,
                    speedForMaximumDamage,
                    impactSpeed));
        }

        private float CalculateWallFeedbackSeverity(
            float impactSpeed)
        {
            float maximumReferenceSpeed =
                Mathf.Max(
                    minimumWallImpactSpeed + 0.01f,
                    fatalWallImpactSpeed);

            return Mathf.Clamp01(
                Mathf.InverseLerp(
                    minimumWallImpactSpeed,
                    maximumReferenceSpeed,
                    impactSpeed));
        }

        #endregion

        #region Helpers

        private bool CanProcessCollision()
        {
            if (racer == null ||
                !racer.IsInitialized ||
                racer.Participant == null ||
                racer.Participant.Vehicle == null)
            {
                return false;
            }

            if (racer.Participant.Status !=
                RaceParticipantStatus.Racing)
            {
                return false;
            }

            if (runtime == null)
            {
                runtime =
                    FindFirstObjectByType<
                        RaceRuntimeController>();
            }

            debugRuntimeResolved =
                runtime != null &&
                runtime.Director != null;

            return debugRuntimeResolved;
        }

        private bool TryGetImpact(
            Collision collision,
            out float impactSpeed,
            out Vector3 contactPoint,
            out Vector3 contactNormal,
            out float impactAngle)
        {
            impactSpeed = 0f;
            impactAngle = 0f;
            contactPoint = Vector3.zero;
            contactNormal = Vector3.zero;

            if (collision == null ||
                collision.contactCount <= 0 ||
                collision.relativeVelocity.sqrMagnitude < 0.001f)
            {
                return false;
            }

            ContactPoint contact =
                collision.GetContact(0);

            contactPoint = contact.point;
            contactNormal = contact.normal.normalized;

            Vector3 velocityDirection =
                collision.relativeVelocity.normalized;

            float normalRatio =
                Mathf.Clamp01(
                    Mathf.Abs(
                        Vector3.Dot(
                            velocityDirection,
                            contactNormal)));

            impactSpeed =
                collision.relativeVelocity.magnitude *
                normalRatio;

            impactAngle =
                Mathf.Asin(normalRatio) *
                Mathf.Rad2Deg;

            return true;
        }

        #endregion
    }
}