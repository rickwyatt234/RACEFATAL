using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class GuidedProjectileView : ProjectileView
    {
        [Header("Target Acquisition")]
        [Range(1f, 90f)][SerializeField] private float acquisitionHalfAngle = 15f;
        [Min(0f)][SerializeField] private float minimumAcquisitionDistance = 5f;

        [Header("Launch")]
        [Tooltip("How long the missile deliberately climbs after leaving the launcher.")]
        [Min(0f)][SerializeField] private float loftDuration = 0.3f;

        [Tooltip("Initial climb angle relative to the launcher's local surface plane.")]
        [Range(0f, 45f)][SerializeField] private float loftAngle = 14f;

        [Tooltip("Time before target guidance is allowed to begin.")]
        [Min(0f)][SerializeField] private float guidanceDelay = 0.2f;

        [Header("Cruise / Terminal Guidance")]
        [Tooltip("How far above the target the missile aims while still at long range.")]
        [Min(0f)][SerializeField] private float cruiseHeight = 4f;

        [Tooltip("Inside this distance, the missile begins lowering its aim point toward the target.")]
        [Min(0.1f)][SerializeField] private float terminalDiveDistance = 22f;

        [Tooltip("Distance beyond Terminal Dive Distance over which full cruise height is reached.")]
        [Min(0.1f)][SerializeField] private float cruiseHeightFadeDistance = 28f;

        [Tooltip("Local point on the target motorcycle used as the final impact aim point.")]
        [SerializeField] private Vector3 targetLocalOffset = new Vector3(0f, 0.7f, 0f);

        [Header("Steering")]
        [Min(0f)][SerializeField] private float turnRateDegreesPerSecond = 150f;

        [Tooltip("0 = pure pursuit. 1 = full simple target-velocity prediction.")]
        [Range(0f, 1f)][SerializeField] private float leadAmount = 0.5f;

        [Min(0f)][SerializeField] private float maximumLeadTime = 0.5f;

        [Tooltip("If the target exceeds this angle, the missile permanently loses guidance.")]
        [Range(1f, 180f)][SerializeField] private float maximumTrackingAngle = 135f;

        [Header("Speed")]
        [Tooltip("Fraction of Projectile Speed used immediately after launch.")]
        [Range(0.1f, 1f)][SerializeField] private float initialSpeedMultiplier = 0.7f;

        [Tooltip("Seconds required to accelerate to the WeaponDefinition Projectile Speed.")]
        [Min(0.01f)][SerializeField] private float accelerationDuration = 0.4f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugHasTarget;
        [SerializeField] private bool debugGuidanceActive;
        [SerializeField] private bool debugLofting;
        [SerializeField] private bool debugTerminalDive;
        [SerializeField] private bool debugLostLock;
        [SerializeField] private string debugTargetRacer = "None";
        [SerializeField] private float debugFlightAge;
        [SerializeField] private float debugTargetDistance;
        [SerializeField] private float debugTargetAngle;
        [SerializeField] private float debugAimHeight;
        [SerializeField] private float debugSpeed;
        [SerializeField] private Vector3 debugDesiredDirection;

        private RacerViewController target;
        private Rigidbody targetBody;

        private Vector3 launchForward;
        private Vector3 launchUp;

        private float flightAge;
        private bool guidanceLost;

        public float AcquisitionHalfAngle => acquisitionHalfAngle;
        public float MinimumAcquisitionDistance => minimumAcquisitionDistance;

        public RacerViewController Target => target;

        public bool HasTarget =>
            target != null &&
            !guidanceLost;

        public void InitializeGuidance(
            RacerViewController targetRacer)
        {
            target = targetRacer;

            targetBody =
                target != null
                    ? target.GetComponent<Rigidbody>()
                    : null;

            launchForward =
                CurrentDirection.sqrMagnitude > 0.001f
                    ? CurrentDirection.normalized
                    : transform.forward;

            launchUp =
                transform.up.sqrMagnitude > 0.001f
                    ? transform.up.normalized
                    : Vector3.up;

            flightAge = 0f;
            guidanceLost = false;

            debugHasTarget = target != null;
            debugGuidanceActive = false;
            debugLofting = true;
            debugTerminalDive = false;
            debugLostLock = false;

            debugTargetRacer =
                target != null
                    ? target.RacerId
                    : "None";
        }

        protected override float ResolveMovementSpeed(float deltaTime)
        {
            flightAge += deltaTime;

            float t =
                accelerationDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        flightAge /
                        accelerationDuration);

            float multiplier =
                Mathf.Lerp(
                    initialSpeedMultiplier,
                    1f,
                    t);

            float resolvedSpeed =
                BaseSpeed *
                multiplier;

            debugFlightAge = flightAge;
            debugSpeed = resolvedSpeed;

            return resolvedSpeed;
        }

        protected override Vector3 ResolveMovementDirection(float deltaTime)
        {
            Vector3 currentDirection =
                CurrentDirection.sqrMagnitude > 0.001f
                    ? CurrentDirection.normalized
                    : launchForward;

            if (flightAge < loftDuration)
            {
                debugLofting = true;
                debugGuidanceActive = false;
                debugTerminalDive = false;

                return ResolveLoftDirection();
            }

            debugLofting = false;

            if (guidanceLost ||
                flightAge < guidanceDelay ||
                !IsTargetValid())
            {
                debugGuidanceActive = false;

                return RotateToward(
                    currentDirection,
                    launchForward,
                    deltaTime);
            }

            Vector3 targetPoint =
                target.transform.TransformPoint(
                    targetLocalOffset);

            Vector3 rawToTarget =
                targetPoint -
                transform.position;

            float distance =
                rawToTarget.magnitude;

            debugTargetDistance =
                distance;

            if (distance <= 0.001f)
                return currentDirection;

            float heightFactor =
                Mathf.Clamp01(
                    (distance - terminalDiveDistance) /
                    cruiseHeightFadeDistance);

            float currentAimHeight =
                cruiseHeight *
                heightFactor;

            debugAimHeight =
                currentAimHeight;

            debugTerminalDive =
                heightFactor < 1f;

            /*
             * Target.transform.up is important here rather than
             * Vector3.up because RACE//FATAL tracks can bank,
             * curve vertically, and go upside-down.
             */
            targetPoint +=
                target.transform.up *
                currentAimHeight;

            if (targetBody != null &&
                CurrentSpeed > 0.01f &&
                leadAmount > 0f)
            {
                float leadTime =
                    Mathf.Min(
                        maximumLeadTime,
                        distance /
                        CurrentSpeed);

                targetPoint +=
                    targetBody.linearVelocity *
                    leadTime *
                    leadAmount;
            }

            Vector3 toAimPoint =
                targetPoint -
                transform.position;

            if (toAimPoint.sqrMagnitude <= 0.001f)
                return currentDirection;

            Vector3 desiredDirection =
                toAimPoint.normalized;

            float targetAngle =
                Vector3.Angle(
                    currentDirection,
                    desiredDirection);

            debugTargetAngle =
                targetAngle;

            debugDesiredDirection =
                desiredDirection;

            if (targetAngle >
                maximumTrackingAngle)
            {
                LoseGuidance();

                return currentDirection;
            }

            debugGuidanceActive = true;

            return RotateToward(
                currentDirection,
                desiredDirection,
                deltaTime);
        }

        private Vector3 ResolveLoftDirection()
        {
            Vector3 planarForward =
                Vector3.ProjectOnPlane(
                    launchForward,
                    launchUp);

            if (planarForward.sqrMagnitude < 0.001f)
                planarForward = launchForward;

            planarForward.Normalize();

            float angleRadians =
                loftAngle *
                Mathf.Deg2Rad;

            Vector3 loftDirection =
                planarForward *
                    Mathf.Cos(angleRadians) +
                launchUp *
                    Mathf.Sin(angleRadians);

            debugDesiredDirection =
                loftDirection.normalized;

            return loftDirection.normalized;
        }

        private Vector3 RotateToward(
            Vector3 currentDirection,
            Vector3 desiredDirection,
            float deltaTime)
        {
            if (desiredDirection.sqrMagnitude < 0.001f)
                return currentDirection;

            float maximumTurnRadians =
                turnRateDegreesPerSecond *
                Mathf.Deg2Rad *
                deltaTime;

            return Vector3.RotateTowards(
                    currentDirection,
                    desiredDirection.normalized,
                    maximumTurnRadians,
                    0f)
                .normalized;
        }

        private bool IsTargetValid()
        {
            if (target == null ||
                !target.IsInitialized ||
                target.Participant == null)
            {
                if (target != null)
                    LoseGuidance();

                return false;
            }

            RaceParticipant participant =
                target.Participant;

            if (participant.Status !=
                RaceParticipantStatus.Racing)
            {
                LoseGuidance();
                return false;
            }

            if (participant.Vehicle == null ||
                participant.Vehicle.IsDestroyed)
            {
                LoseGuidance();
                return false;
            }

            return true;
        }

        private void LoseGuidance()
        {
            guidanceLost = true;

            debugHasTarget = false;
            debugGuidanceActive = false;
            debugLostLock = true;
        }
    }
}