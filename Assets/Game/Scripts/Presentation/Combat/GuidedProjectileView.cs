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
        [Min(0f)][SerializeField] private float loftDuration = 0.3f;
        [Range(0f, 45f)][SerializeField] private float loftAngle = 14f;
        [Min(0f)][SerializeField] private float guidanceDelay = 0.2f;

        [Header("Cruise / Terminal Guidance")]
        [Tooltip("Height above the target maintained during normal missile cruise.")]
        [Min(0f)][SerializeField] private float cruiseHeight = 6f;

        [Tooltip("Distance from the target at which the missile begins descending from cruise height.")]
        [Min(0.1f)][SerializeField] private float terminalDiveStartDistance = 16f;

        [Tooltip("Distance at which the missile finishes most of its descent and aims almost directly at the bike.")]
        [Min(0.1f)][SerializeField] private float impactDiveDistance = 4f;

        [Tooltip("Minimum height retained above the target until the missile reaches its final impact distance.")]
        [Min(0f)][SerializeField] private float minimumTerminalHeight = 0.5f;

        [Tooltip("Local impact point on the target motorcycle.")]
        [SerializeField] private Vector3 targetLocalOffset = new Vector3(0f, 0.9f, 0f);

        [Header("Steering")]
        [Min(0f)][SerializeField] private float turnRateDegreesPerSecond = 150f;

        [Tooltip("0 = pure pursuit. 1 = full simple target-velocity prediction.")]
        [Range(0f, 1f)][SerializeField] private float leadAmount = 0.5f;

        [Min(0f)][SerializeField] private float maximumLeadTime = 0.5f;

        [Tooltip("If the target exceeds this angle, the missile permanently loses guidance.")]
        [Range(1f, 180f)][SerializeField] private float maximumTrackingAngle = 135f;

        [Header("Speed")]
        [Range(0.1f, 1f)][SerializeField] private float initialSpeedMultiplier = 0.7f;
        [Min(0.01f)][SerializeField] private float accelerationDuration = 0.4f;

        [Header("Countermeasure Defeat")]
        [Min(0f)][SerializeField] private float countermeasureTurnRate = 240f;
        [Min(0f)][SerializeField] private float countermeasureClimbBias = 2f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugHasTarget;
        [SerializeField] private bool debugGuidanceActive;
        [SerializeField] private bool debugLofting;
        [SerializeField] private bool debugTerminalDive;
        [SerializeField] private bool debugLostLock;
        [SerializeField] private bool debugCountered;

        [SerializeField] private string debugTargetRacer = "None";

        [SerializeField] private float debugFlightAge;
        [SerializeField] private float debugTargetDistance;
        [SerializeField] private float debugTargetAngle;
        [SerializeField] private float debugAimHeight;
        [SerializeField] private float debugSpeed;

        [SerializeField] private Vector3 debugDesiredDirection;

        private RacerViewController target;
        private Rigidbody targetBody;
        private MissileThreatReceiver targetThreatReceiver;

        private Vector3 launchForward;
        private Vector3 launchUp;
        private Vector3 countermeasureEscapeDirection;

        private float flightAge;

        private bool guidanceLost;
        private bool countered;

        public float AcquisitionHalfAngle =>
            acquisitionHalfAngle;

        public float MinimumAcquisitionDistance =>
            minimumAcquisitionDistance;

        public RacerViewController Target =>
            target;

        public bool HasTarget =>
            target != null &&
            !guidanceLost &&
            !countered;

        public bool IsActiveThreat =>
            target != null &&
            !guidanceLost &&
            !countered &&
            !IsResolved;

        public void InitializeGuidance(
            RacerViewController targetRacer)
        {
            UnregisterThreat();

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
            countered = false;

            debugHasTarget = target != null;
            debugGuidanceActive = false;
            debugLofting = true;
            debugTerminalDive = false;
            debugLostLock = false;
            debugCountered = false;

            debugTargetRacer =
                target != null
                    ? target.RacerId
                    : "None";

            RegisterThreat();
        }

        protected override float ResolveMovementSpeed(
            float deltaTime)
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

        protected override Vector3 ResolveMovementDirection(
            float deltaTime)
        {
            Vector3 currentDirection =
                CurrentDirection.sqrMagnitude > 0.001f
                    ? CurrentDirection.normalized
                    : launchForward;

            if (countered)
            {
                debugGuidanceActive = false;
                debugLofting = false;
                debugTerminalDive = false;

                return RotateToward(
                    currentDirection,
                    countermeasureEscapeDirection,
                    countermeasureTurnRate,
                    deltaTime);
            }

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

                return currentDirection;
            }

            Vector3 predictedTargetPoint =
                target.transform.TransformPoint(
                    targetLocalOffset);

            Vector3 rawToTarget =
                predictedTargetPoint -
                transform.position;

            float distance =
                rawToTarget.magnitude;

            debugTargetDistance =
                distance;

            if (distance <= 0.001f)
                return currentDirection;

            /*
             * Predict where the bike will be first.
             */
            if (targetBody != null &&
                CurrentSpeed > 0.01f &&
                leadAmount > 0f)
            {
                float leadTime =
                    Mathf.Min(
                        maximumLeadTime,
                        distance /
                        CurrentSpeed);

                predictedTargetPoint +=
                    targetBody.linearVelocity *
                    leadTime *
                    leadAmount;
            }

            /*
             * Stay at full cruise height until we are genuinely
             * close to the bike.
             *
             * Then descend progressively:
             *
             * > terminalDiveStartDistance = full cruise height
             * impactDiveDistance          = minimum terminal height
             */
            float aimHeight =
                ResolveAimHeight(
                    distance);

            debugAimHeight =
                aimHeight;

            debugTerminalDive =
                distance <=
                terminalDiveStartDistance;

            /*
             * Use target-local up, not world up.
             * This keeps guidance correct on banked, vertical,
             * and upside-down track sections.
             */
            Vector3 aimPoint =
                predictedTargetPoint +
                target.transform.up *
                aimHeight;

            Vector3 toAimPoint =
                aimPoint -
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
                turnRateDegreesPerSecond,
                deltaTime);
        }

        private float ResolveAimHeight(
            float targetDistance)
        {
            /*
             * Long range:
             * stay fully elevated.
             */
            if (targetDistance >=
                terminalDiveStartDistance)
            {
                return cruiseHeight;
            }

            /*
             * Extremely close:
             * aim directly at the target's configured
             * local impact point.
             */
            if (targetDistance <=
                impactDiveDistance)
            {
                return 0f;
            }

            /*
             * Between those distances, smoothly descend
             * from cruise height toward a small amount of
             * remaining clearance.
             */
            float t =
                Mathf.InverseLerp(
                    impactDiveDistance,
                    terminalDiveStartDistance,
                    targetDistance);

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t);

            return Mathf.Lerp(
                minimumTerminalHeight,
                cruiseHeight,
                t);
        }

        public void DefeatByCountermeasure(
            Vector3 escapeUp)
        {
            if (!IsActiveThreat)
                return;

            Vector3 currentDirection =
                CurrentDirection.sqrMagnitude > 0.001f
                    ? CurrentDirection.normalized
                    : transform.forward;

            Vector3 up =
                escapeUp.sqrMagnitude > 0.001f
                    ? escapeUp.normalized
                    : transform.up;

            countermeasureEscapeDirection =
                (currentDirection +
                 up *
                 countermeasureClimbBias)
                .normalized;

            countered = true;
            guidanceLost = true;

            debugCountered = true;
            debugLostLock = true;
            debugHasTarget = false;
            debugGuidanceActive = false;

            UnregisterThreat();

            target = null;
            targetBody = null;
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
            float turnRate,
            float deltaTime)
        {
            if (desiredDirection.sqrMagnitude < 0.001f)
                return currentDirection;

            float maximumTurnRadians =
                turnRate *
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

        private void RegisterThreat()
        {
            if (target == null)
                return;

            targetThreatReceiver =
                target.GetComponent<
                    MissileThreatReceiver>();

            if (targetThreatReceiver != null)
            {
                targetThreatReceiver.RegisterThreat(
                    this);
            }
        }

        private void UnregisterThreat()
        {
            if (targetThreatReceiver == null)
                return;

            targetThreatReceiver.UnregisterThreat(
                this);

            targetThreatReceiver = null;
        }

        private void LoseGuidance()
        {
            if (guidanceLost)
                return;

            guidanceLost = true;

            debugHasTarget = false;
            debugGuidanceActive = false;
            debugLostLock = true;

            UnregisterThreat();
        }

        private void OnDestroy()
        {
            UnregisterThreat();
        }
    }
}