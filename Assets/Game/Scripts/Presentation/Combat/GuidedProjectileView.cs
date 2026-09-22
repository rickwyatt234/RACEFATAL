using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class GuidedProjectileView : ProjectileView
    {
        #region Target Acquisition

        [Header("Target Acquisition")]

        [Range(1f, 90f)]
        [SerializeField]
        private float acquisitionHalfAngle = 15f;

        [Min(0f)]
        [SerializeField]
        private float minimumAcquisitionDistance = 5f;

        #endregion

        #region Launch

        [Header("Launch")]

        [Min(0f)]
        [SerializeField]
        private float loftDuration = 0.3f;

        [Range(0f, 45f)]
        [SerializeField]
        private float loftAngle = 14f;

        [Min(0f)]
        [SerializeField]
        private float guidanceDelay = 0.2f;

        #endregion

        #region Cruise / Terminal Guidance

        [Header("Cruise / Terminal Guidance")]

        [Tooltip(
            "Height above the predicted target position maintained " +
            "during normal missile cruise.")]
        [Min(0f)]
        [SerializeField]
        private float cruiseHeight = 6f;

        [Tooltip(
            "Distance from the target where the missile begins its terminal dive.")]
        [Min(0.1f)]
        [SerializeField]
        private float terminalDiveStartDistance = 30f;

        [Tooltip(
            "Inside this distance the missile stops retaining cruise height " +
            "and aims directly at the predicted motorcycle impact point.")]
        [Min(0.1f)]
        [SerializeField]
        private float impactDiveDistance = 10f;

        [Tooltip(
            "Minimum height retained during the middle portion of the terminal dive.")]
        [Min(0f)]
        [SerializeField]
        private float minimumTerminalHeight = 0.5f;

        [Tooltip(
            "Local impact point on the target motorcycle.")]
        [SerializeField]
        private Vector3 targetLocalOffset =
            new Vector3(
                0f,
                0.9f,
                0f);

        #endregion

        #region Steering

        [Header("Steering")]

        [Tooltip(
            "Normal missile steering rate while cruising.")]
        [Min(0f)]
        [SerializeField]
        private float cruiseTurnRateDegreesPerSecond = 180f;

        [Tooltip(
            "Steering rate once the missile enters terminal guidance.")]
        [Min(0f)]
        [SerializeField]
        private float terminalTurnRateDegreesPerSecond = 360f;

        [Tooltip(
            "Very aggressive steering rate during the final direct approach.")]
        [Min(0f)]
        [SerializeField]
        private float finalTurnRateDegreesPerSecond = 540f;

        [Tooltip(
            "0 = aim at the target's current position. " +
            "1 = use the full calculated interception point.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float predictionStrength = 1f;

        [Tooltip(
            "Maximum number of seconds into the future the missile may predict.")]
        [Min(0f)]
        [SerializeField]
        private float maximumPredictionTime = 3f;

        [Tooltip(
            "If the required steering angle exceeds this amount, " +
            "the missile permanently loses guidance.")]
        [Range(1f, 180f)]
        [SerializeField]
        private float maximumTrackingAngle = 165f;

        #endregion

        #region Speed

        [Header("Speed")]

        [Range(0.1f, 1f)]
        [SerializeField]
        private float initialSpeedMultiplier = 0.7f;

        [Min(0.01f)]
        [SerializeField]
        private float accelerationDuration = 0.4f;

        #endregion

        #region Countermeasure Defeat

        [Header("Countermeasure Defeat")]

        [Min(0f)]
        [SerializeField]
        private float countermeasureTurnRate = 240f;

        [Min(0f)]
        [SerializeField]
        private float countermeasureClimbBias = 2f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]

        [SerializeField]
        private bool debugHasTarget;

        [SerializeField]
        private bool debugGuidanceActive;

        [SerializeField]
        private bool debugLofting;

        [SerializeField]
        private bool debugTerminalDive;

        [SerializeField]
        private bool debugFinalGuidance;

        [SerializeField]
        private bool debugLostLock;

        [SerializeField]
        private bool debugCountered;

        [SerializeField]
        private bool debugInterceptSolutionFound;

        [SerializeField]
        private string debugTargetRacer =
            "None";

        [SerializeField]
        private string debugGuidancePhase =
            "None";

        [SerializeField]
        private float debugFlightAge;

        [SerializeField]
        private float debugTargetDistance;

        [SerializeField]
        private float debugTargetAngle;

        [SerializeField]
        private float debugAimHeight;

        [SerializeField]
        private float debugSpeed;

        [SerializeField]
        private float debugPredictionTime;

        [SerializeField]
        private float debugCurrentTurnRate;

        [SerializeField]
        private Vector3 debugTargetVelocity;

        [SerializeField]
        private Vector3 debugPredictedTargetPoint;

        [SerializeField]
        private Vector3 debugDesiredDirection;

        #endregion

        #region Runtime

        private RacerViewController target;
        private Rigidbody targetBody;
        private MissileThreatReceiver targetThreatReceiver;

        private Vector3 launchForward;
        private Vector3 launchUp;

        private Vector3 countermeasureEscapeDirection;

        private float flightAge;

        private bool guidanceLost;
        private bool countered;

        #endregion

        #region Public State

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

        #endregion

        #region Initialization

        public void InitializeGuidance(
            RacerViewController targetRacer)
        {
            UnregisterThreat();

            target =
                targetRacer;

            targetBody =
                target != null
                    ? target.GetComponent<Rigidbody>()
                    : null;

            if (targetBody == null &&
                target != null)
            {
                targetBody =
                    target.GetComponentInParent<Rigidbody>();
            }

            launchForward =
                CurrentDirection.sqrMagnitude >
                0.001f
                    ? CurrentDirection.normalized
                    : transform.forward;

            launchUp =
                transform.up.sqrMagnitude >
                0.001f
                    ? transform.up.normalized
                    : Vector3.up;

            flightAge =
                0f;

            guidanceLost =
                false;

            countered =
                false;

            debugHasTarget =
                target != null;

            debugGuidanceActive =
                false;

            debugLofting =
                true;

            debugTerminalDive =
                false;

            debugFinalGuidance =
                false;

            debugLostLock =
                false;

            debugCountered =
                false;

            debugInterceptSolutionFound =
                false;

            debugPredictionTime =
                0f;

            debugCurrentTurnRate =
                0f;

            debugGuidancePhase =
                "Launch";

            debugTargetRacer =
                target != null
                    ? target.RacerId
                    : "None";

            RegisterThreat();
        }

        #endregion

        #region Speed

        protected override float ResolveMovementSpeed(
            float deltaTime)
        {
            flightAge +=
                deltaTime;

            float t =
                accelerationDuration <=
                0f
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

            debugFlightAge =
                flightAge;

            debugSpeed =
                resolvedSpeed;

            return resolvedSpeed;
        }

        #endregion

        #region Guidance

        protected override Vector3 ResolveMovementDirection(
            float deltaTime)
        {
            Vector3 currentDirection =
                CurrentDirection.sqrMagnitude >
                0.001f
                    ? CurrentDirection.normalized
                    : launchForward;

            /*
             * Countermeasures completely override ordinary
             * target guidance.
             */
            if (countered)
            {
                debugGuidanceActive =
                    false;

                debugLofting =
                    false;

                debugTerminalDive =
                    false;

                debugFinalGuidance =
                    false;

                debugGuidancePhase =
                    "Countered";

                debugCurrentTurnRate =
                    countermeasureTurnRate;

                return RotateToward(
                    currentDirection,
                    countermeasureEscapeDirection,
                    countermeasureTurnRate,
                    deltaTime);
            }

            /*
             * Initial launch loft.
             */
            if (flightAge <
                loftDuration)
            {
                debugLofting =
                    true;

                debugGuidanceActive =
                    false;

                debugTerminalDive =
                    false;

                debugFinalGuidance =
                    false;

                debugGuidancePhase =
                    "Loft";

                debugCurrentTurnRate =
                    0f;

                return ResolveLoftDirection();
            }

            debugLofting =
                false;

            /*
             * Guidance may be intentionally delayed for a short
             * time after launch.
             */
            if (guidanceLost ||
                flightAge <
                    guidanceDelay ||
                !IsTargetValid())
            {
                debugGuidanceActive =
                    false;

                debugGuidancePhase =
                    guidanceLost
                        ? "Guidance Lost"
                        : "Guidance Delay";

                return currentDirection;
            }

            Vector3 currentTargetPoint =
                target.transform.TransformPoint(
                    targetLocalOffset);

            Vector3 rawToTarget =
                currentTargetPoint -
                transform.position;

            float targetDistance =
                rawToTarget.magnitude;

            debugTargetDistance =
                targetDistance;

            if (targetDistance <=
                0.001f)
            {
                return currentDirection;
            }

            /*
             * Calculate an actual interception point rather than
             * simply aiming at where the bike is right now.
             */
            Vector3 predictedTargetPoint =
                ResolvePredictedTargetPoint(
                    currentTargetPoint,
                    CurrentSpeed,
                    out float predictionTime,
                    out bool interceptFound);

            debugPredictionTime =
                predictionTime;

            debugInterceptSolutionFound =
                interceptFound;

            debugPredictedTargetPoint =
                predictedTargetPoint;

            /*
             * Cruise above the target while far away, descend
             * during terminal guidance, then aim directly at the
             * predicted impact point during final guidance.
             */
            float aimHeight =
                ResolveAimHeight(
                    targetDistance);

            debugAimHeight =
                aimHeight;

            bool terminalGuidance =
                targetDistance <=
                terminalDiveStartDistance;

            bool finalGuidance =
                targetDistance <=
                impactDiveDistance;

            debugTerminalDive =
                terminalGuidance;

            debugFinalGuidance =
                finalGuidance;

            Vector3 aimPoint =
                predictedTargetPoint;

            /*
             * Only retain the elevated cruise path outside the
             * final direct-impact envelope.
             */
            if (!finalGuidance &&
                aimHeight > 0f)
            {
                aimPoint +=
                    target.transform.up *
                    aimHeight;
            }

            Vector3 toAimPoint =
                aimPoint -
                transform.position;

            if (toAimPoint.sqrMagnitude <=
                0.001f)
            {
                return currentDirection;
            }

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

            /*
             * A missile can still lose lock if the target gets
             * sufficiently far outside its tracking envelope.
             */
            if (targetAngle >
                maximumTrackingAngle)
            {
                LoseGuidance();

                return currentDirection;
            }

            float activeTurnRate =
                ResolveTurnRate(
                    targetDistance);

            debugCurrentTurnRate =
                activeTurnRate;

            debugGuidanceActive =
                true;

            if (finalGuidance)
            {
                debugGuidancePhase =
                    "Final Intercept";
            }
            else if (terminalGuidance)
            {
                debugGuidancePhase =
                    "Terminal";
            }
            else
            {
                debugGuidancePhase =
                    "Cruise";
            }

            return RotateToward(
                currentDirection,
                desiredDirection,
                activeTurnRate,
                deltaTime);
        }

        #endregion

        #region Interception

        private Vector3 ResolvePredictedTargetPoint(
            Vector3 currentTargetPoint,
            float missileSpeed,
            out float predictionTime,
            out bool interceptFound)
        {
            predictionTime =
                0f;

            interceptFound =
                false;

            if (targetBody == null ||
                missileSpeed <= 0.01f ||
                predictionStrength <= 0f)
            {
                debugTargetVelocity =
                    Vector3.zero;

                return currentTargetPoint;
            }

            Vector3 targetVelocity =
                targetBody.linearVelocity;

            debugTargetVelocity =
                targetVelocity;

            if (targetVelocity.sqrMagnitude <=
                0.001f)
            {
                return currentTargetPoint;
            }

            Vector3 relativePosition =
                currentTargetPoint -
                transform.position;

            if (TryCalculateInterceptTime(
                    relativePosition,
                    targetVelocity,
                    missileSpeed,
                    out float interceptTime))
            {
                interceptFound =
                    true;

                predictionTime =
                    Mathf.Min(
                        interceptTime,
                        maximumPredictionTime);
            }
            else
            {
                /*
                 * There is no exact constant-velocity intercept
                 * solution. This can happen when the target is
                 * temporarily moving away too quickly.
                 *
                 * Fall back to a bounded future prediction instead
                 * of reverting completely to pure pursuit.
                 */
                float approximateTime =
                    relativePosition.magnitude /
                    Mathf.Max(
                        0.01f,
                        missileSpeed);

                predictionTime =
                    Mathf.Min(
                        approximateTime,
                        maximumPredictionTime);
            }

            predictionTime =
                Mathf.Max(
                    0f,
                    predictionTime);

            Vector3 fullPrediction =
                currentTargetPoint +
                targetVelocity *
                predictionTime;

            return Vector3.Lerp(
                currentTargetPoint,
                fullPrediction,
                predictionStrength);
        }

        private bool TryCalculateInterceptTime(
            Vector3 relativePosition,
            Vector3 targetVelocity,
            float missileSpeed,
            out float interceptTime)
        {
            interceptTime =
                0f;

            float speedSquared =
                missileSpeed *
                missileSpeed;

            float targetSpeedSquared =
                Vector3.Dot(
                    targetVelocity,
                    targetVelocity);

            float a =
                targetSpeedSquared -
                speedSquared;

            float b =
                2f *
                Vector3.Dot(
                    relativePosition,
                    targetVelocity);

            float c =
                Vector3.Dot(
                    relativePosition,
                    relativePosition);

            const float epsilon =
                0.0001f;

            /*
             * Near-linear case.
             *
             * This occurs when target speed is approximately equal
             * to missile speed.
             */
            if (Mathf.Abs(a) <
                epsilon)
            {
                if (Mathf.Abs(b) <
                    epsilon)
                {
                    return false;
                }

                float t =
                    -c /
                    b;

                if (t <= 0f)
                    return false;

                interceptTime =
                    t;

                return true;
            }

            float discriminant =
                b *
                b -
                4f *
                a *
                c;

            if (discriminant <
                0f)
            {
                return false;
            }

            float sqrtDiscriminant =
                Mathf.Sqrt(
                    discriminant);

            float denominator =
                2f *
                a;

            float t1 =
                (-b -
                 sqrtDiscriminant) /
                denominator;

            float t2 =
                (-b +
                 sqrtDiscriminant) /
                denominator;

            bool t1Valid =
                t1 > 0f;

            bool t2Valid =
                t2 > 0f;

            if (!t1Valid &&
                !t2Valid)
            {
                return false;
            }

            if (t1Valid &&
                t2Valid)
            {
                interceptTime =
                    Mathf.Min(
                        t1,
                        t2);
            }
            else
            {
                interceptTime =
                    t1Valid
                        ? t1
                        : t2;
            }

            return true;
        }

        #endregion

        #region Terminal Guidance

        private float ResolveAimHeight(
            float targetDistance)
        {
            /*
             * Cruise phase.
             */
            if (targetDistance >=
                terminalDiveStartDistance)
            {
                return cruiseHeight;
            }

            /*
             * Final guidance:
             * no artificial height offset at all.
             */
            if (targetDistance <=
                impactDiveDistance)
            {
                return 0f;
            }

            /*
             * Smoothly transition from cruise height down toward
             * the final impact path.
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

        private float ResolveTurnRate(
            float targetDistance)
        {
            if (targetDistance <=
                impactDiveDistance)
            {
                return
                    finalTurnRateDegreesPerSecond;
            }

            if (targetDistance <=
                terminalDiveStartDistance)
            {
                /*
                 * Increase steering continuously as the missile
                 * closes on the bike instead of suddenly switching
                 * from one value to another.
                 */
                float t =
                    Mathf.InverseLerp(
                        terminalDiveStartDistance,
                        impactDiveDistance,
                        targetDistance);

                return Mathf.Lerp(
                    terminalTurnRateDegreesPerSecond,
                    finalTurnRateDegreesPerSecond,
                    t);
            }

            return
                cruiseTurnRateDegreesPerSecond;
        }

        #endregion

        #region Countermeasures

        public void DefeatByCountermeasure(
            Vector3 escapeUp)
        {
            if (!IsActiveThreat)
                return;

            Vector3 currentDirection =
                CurrentDirection.sqrMagnitude >
                0.001f
                    ? CurrentDirection.normalized
                    : transform.forward;

            Vector3 up =
                escapeUp.sqrMagnitude >
                0.001f
                    ? escapeUp.normalized
                    : transform.up;

            countermeasureEscapeDirection =
                (currentDirection +
                 up *
                 countermeasureClimbBias)
                .normalized;

            countered =
                true;

            guidanceLost =
                true;

            debugCountered =
                true;

            debugLostLock =
                true;

            debugHasTarget =
                false;

            debugGuidanceActive =
                false;

            debugGuidancePhase =
                "Countered";

            UnregisterThreat();

            target =
                null;

            targetBody =
                null;
        }

        #endregion

        #region Launch Direction

        private Vector3 ResolveLoftDirection()
        {
            Vector3 planarForward =
                Vector3.ProjectOnPlane(
                    launchForward,
                    launchUp);

            if (planarForward.sqrMagnitude <
                0.001f)
            {
                planarForward =
                    launchForward;
            }

            planarForward.Normalize();

            float angleRadians =
                loftAngle *
                Mathf.Deg2Rad;

            Vector3 loftDirection =
                planarForward *
                    Mathf.Cos(
                        angleRadians) +
                launchUp *
                    Mathf.Sin(
                        angleRadians);

            debugDesiredDirection =
                loftDirection.normalized;

            return
                loftDirection.normalized;
        }

        #endregion

        #region Steering Helper

        private Vector3 RotateToward(
            Vector3 currentDirection,
            Vector3 desiredDirection,
            float turnRate,
            float deltaTime)
        {
            if (desiredDirection.sqrMagnitude <
                0.001f)
            {
                return
                    currentDirection;
            }

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

        #endregion

        #region Target Validation

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

        #endregion

        #region Threat Registration

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

            targetThreatReceiver =
                null;
        }

        #endregion

        #region Guidance Loss

        private void LoseGuidance()
        {
            if (guidanceLost)
                return;

            guidanceLost =
                true;

            debugHasTarget =
                false;

            debugGuidanceActive =
                false;

            debugLostLock =
                true;

            debugGuidancePhase =
                "Guidance Lost";

            UnregisterThreat();
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            UnregisterThreat();
        }

        #endregion
    }
}