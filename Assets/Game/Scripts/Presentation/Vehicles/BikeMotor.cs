/*
    BIKE MOTOR COMPONENT
*/

using RaceFatal.Presentation.Tracks;
using RaceFatal.Vehicles;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(TrackSurfaceProbe))]
    public sealed class BikeMotor :
        MonoBehaviour
    {
        [Header("References")]

        [SerializeField]
        private Transform centerOfMass;

        [Header("Track Adhesion")]

        [Min(0f)]
        [SerializeField]
        private float adhesionAcceleration =
            35f;

        [Min(0f)]
        [SerializeField]
        private float airGravityAcceleration =
            12f;

        [Min(0f)]
        [SerializeField]
        private float alignmentSpeed =
            12f;

        [Header("Steering")]

        [Min(0f)]
        [SerializeField]
        private float lowSpeedTurnRate =
            80f;

        [Min(0f)]
        [SerializeField]
        private float highSpeedTurnRate =
            35f;

        [Min(0f)]
        [SerializeField]
        private float lateralGrip =
            8f;

        [Header("Braking")]

        [Min(0f)]
        [SerializeField]
        private float brakeDeceleration =
            30f;

        [Header("Overspeed")]

        [Min(0f)]
        [SerializeField]
        private float overspeedCorrection =
            3f;

        private Rigidbody body;

        private TrackSurfaceProbe surfaceProbe;

        private BikePerformance performance;

        private float throttleInput;
        private float brakeInput;
        private float steeringInput;

        private float speedMultiplier = 1f;
        private float accelerationMultiplier = 1f;
        private float handlingMultiplier = 1f;

        public float SteeringInput =>
            steeringInput;

        public float SpeedMetersPerSecond
        {
            get
            {
                if (body == null)
                    return 0f;

                return body.linearVelocity.magnitude;
            }
        }
        public float SpeedFeetPerSecond =>
            SpeedMetersPerSecond * 3.28084f;
        public float SpeedKph =>
            SpeedMetersPerSecond * 3.6f;
        public float SpeedMPH =>
            SpeedMetersPerSecond * 2.23694f;

        private void Awake()
        {
            body =
                GetComponent<Rigidbody>();

            surfaceProbe =
                GetComponent<TrackSurfaceProbe>();

            body.useGravity = false;

            if (centerOfMass != null)
            {
                body.centerOfMass =
                    transform.InverseTransformPoint(
                        centerOfMass.position);
            }
        }

        public void SetPerformance(
            BikePerformance bikePerformance)
        {
            performance =
                bikePerformance;

            if (performance != null)
            {
                body.mass =
                    performance.Mass;
            }
        }

        public void SetRuntimeModifiers(
            float speed,
            float acceleration,
            float handling)
        {
            speedMultiplier =
                Mathf.Max(0f, speed);

            accelerationMultiplier =
                Mathf.Max(0f, acceleration);

            handlingMultiplier =
                Mathf.Max(0f, handling);
        }

        public void SetControls(
            float throttle,
            float brake,
            float steering)
        {
            throttleInput =
                Mathf.Clamp01(throttle);

            brakeInput =
                Mathf.Clamp01(brake);

            steeringInput =
                Mathf.Clamp(
                    steering,
                    -1f,
                    1f);
        }

        private void FixedUpdate()
        {
            if (performance == null)
                return;

            bool hasSurface =
                surfaceProbe.Sample();

            if (hasSurface)
            {
                Vector3 normal =
                    surfaceProbe.SurfaceNormal;

                AlignToSurface(normal);

                ApplyAdhesion(normal);

                ApplyDrive(normal);

                ApplyBraking(normal);

                ApplyLateralGrip(normal);
            }
            else
            {
                ApplyAirGravity();
            }
        }

        private void AlignToSurface(
            Vector3 normal)
        {
            Vector3 forward =
                Vector3.ProjectOnPlane(
                    transform.forward,
                    normal);

            if (forward.sqrMagnitude <
                0.001f)
            {
                return;
            }

            forward.Normalize();

            float effectiveHandling =
                performance.Handling *
                handlingMultiplier;

            float speedRatio =
                Mathf.Clamp01(
                    SpeedKph /
                    Mathf.Max(
                        1f,
                        performance.TopSpeedMPH));

            float turnRate =
                Mathf.Lerp(
                    lowSpeedTurnRate,
                    highSpeedTurnRate,
                    speedRatio);

            float steeringAuthority =
                Mathf.Lerp(
                    0.2f,
                    1f,
                    Mathf.Clamp01(
                        SpeedMetersPerSecond /
                        8f));

            float turnAngle =
                steeringInput *
                turnRate *
                effectiveHandling *
                steeringAuthority *
                Time.fixedDeltaTime;

            forward =
                Quaternion.AngleAxis(
                    turnAngle,
                    normal) *
                forward;

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    forward,
                    normal);

            Quaternion aligned =
                Quaternion.Slerp(
                    body.rotation,
                    targetRotation,
                    alignmentSpeed *
                    Time.fixedDeltaTime);

            body.MoveRotation(
                aligned);
        }

        private void ApplyAdhesion(
            Vector3 surfaceNormal)
        {
            body.AddForce(
                -surfaceNormal *
                adhesionAcceleration,
                ForceMode.Acceleration);
        }

        private void ApplyDrive(
            Vector3 surfaceNormal)
        {
            if (throttleInput <= 0f)
                return;

            Vector3 forward =
                Vector3.ProjectOnPlane(
                    transform.forward,
                    surfaceNormal)
                .normalized;

            float forwardSpeed =
                Vector3.Dot(
                    body.linearVelocity,
                    forward);

            float maximumSpeed =
                performance
                    .TopSpeedMetersPerSecond *
                speedMultiplier;

            float acceleration =
                performance.Acceleration *
                accelerationMultiplier;

            if (forwardSpeed <
                maximumSpeed)
            {
                body.AddForce(
                    forward *
                    acceleration *
                    throttleInput,
                    ForceMode.Acceleration);
            }

            if (forwardSpeed >
                maximumSpeed)
            {
                float excess =
                    forwardSpeed -
                    maximumSpeed;

                body.AddForce(
                    -forward *
                    excess *
                    overspeedCorrection,
                    ForceMode.Acceleration);
            }
        }

        private void ApplyBraking(
            Vector3 surfaceNormal)
        {
            if (brakeInput <= 0f)
                return;

            Vector3 planarVelocity =
                Vector3.ProjectOnPlane(
                    body.linearVelocity,
                    surfaceNormal);

            if (planarVelocity.sqrMagnitude <
                0.001f)
            {
                return;
            }

            body.AddForce(
                -planarVelocity.normalized *
                brakeDeceleration *
                brakeInput,
                ForceMode.Acceleration);
        }

        private void ApplyLateralGrip(
            Vector3 surfaceNormal)
        {
            Vector3 forward =
                Vector3.ProjectOnPlane(
                    transform.forward,
                    surfaceNormal)
                .normalized;

            Vector3 right =
                Vector3.Cross(
                    surfaceNormal,
                    forward)
                .normalized;

            float lateralSpeed =
                Vector3.Dot(
                    body.linearVelocity,
                    right);

            float effectiveGrip =
                lateralGrip *
                performance.Handling *
                handlingMultiplier;

            body.AddForce(
                -right *
                lateralSpeed *
                effectiveGrip,
                ForceMode.Acceleration);
        }

        private void ApplyAirGravity()
        {
            Vector3 gravityDirection =
                -surfaceProbe
                    .LastSurfaceNormal;

            body.AddForce(
                gravityDirection *
                airGravityAcceleration,
                ForceMode.Acceleration);
        }
    }
}