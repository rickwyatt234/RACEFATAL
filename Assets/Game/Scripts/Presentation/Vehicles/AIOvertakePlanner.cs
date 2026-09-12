using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(AIRacerSensor))]
    public class AIOvertakePlanner : MonoBehaviour
    {
        public enum OvertakeState
        {
            Idle,
            PassingLeft,
            PassingRight,
            Cooldown
        }

        #region Hunt

        [Header("Hunt")]
        [Tooltip("Distance at which another racer ahead becomes a target worth actively pursuing.")]
        [Min(1f)][SerializeField] private float huntDistance = 90f;

        [Tooltip("Distance at which the AI begins setting up an actual passing lane.")]
        [Min(1f)][SerializeField] private float passPreparationDistance = 55f;

        [Tooltip("How much faster the target may be while the AI still attempts to chase and pass it.")]
        [Min(0f)][SerializeField] private float maximumPassSpeedDeficit = 4f;

        #endregion

        #region Passing

        [Header("Pass Initiation")]
        [Tooltip("Minimum speed required before the AI commits to an overtake.")]
        [Min(0f)][SerializeField] private float minimumPassSpeed = 15f;

        [Tooltip("At this distance the AI will attempt an available pass even if the opponent currently has a speed advantage.")]
        [Min(0f)][SerializeField] private float forcedPassDistance = 16f;

        [Tooltip("Lateral distance added to the racing line while overtaking.")]
        [Min(0.1f)][SerializeField] private float passOffset = 3f;

        #endregion

        #region Commitment

        [Header("Pass Commitment")]
        [Tooltip("Minimum time the AI remains committed to its chosen side.")]
        [Min(0f)][SerializeField] private float minimumCommitTime = 1.25f;

        [Tooltip("Maximum duration of one continuous passing attempt before briefly resetting.")]
        [Min(0.1f)][SerializeField] private float maximumPassTime = 10f;

        [Tooltip("Distance ahead of the target required before the pass is considered complete.")]
        [Min(0f)][SerializeField] private float completionLeadDistance = 4f;

        [Tooltip("Cooldown after a successfully completed pass.")]
        [Min(0f)][SerializeField] private float passCooldown = 0.75f;

        [Tooltip("Short retry delay when a pass times out without being completed.")]
        [Min(0f)][SerializeField] private float failedPassRetryDelay = 0.35f;

        [Tooltip("How quickly the temporary passing line moves sideways.")]
        [Min(0.1f)][SerializeField] private float tacticalOffsetShiftSpeed = 5.5f;

        #endregion

        #region Following

        [Header("Following")]
        [Tooltip("Distance where the AI begins respecting the speed of traffic directly ahead.")]
        [Min(0.1f)][SerializeField] private float followingDistance = 16f;

        [Tooltip("Very close distance where collision avoidance strongly limits closing speed.")]
        [Min(0.1f)][SerializeField] private float emergencyFollowingDistance = 6f;

        [Tooltip("Additional speed allowed at the outer edge of the following zone.")]
        [Min(0f)][SerializeField] private float followingSpeedBuffer = 4.5f;

        [Tooltip("Small closing-speed allowance retained even during emergency following.")]
        [Min(0f)][SerializeField] private float emergencyFollowingSpeedBuffer = 1.5f;

        [Tooltip("Once this fraction of the passing offset has been reached, normal traffic speed matching is removed.")]
        [Range(0f, 1f)][SerializeField] private float establishedPassLaneThreshold = 0.35f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private OvertakeState currentState;
        [SerializeField] private bool debugHunting;
        [SerializeField] private string debugHuntTarget = "None";
        [SerializeField] private string debugPassTarget = "None";
        [SerializeField] private float debugTargetDistance;
        [SerializeField] private float debugClosingSpeed;
        [SerializeField] private float debugPassTimer;
        [SerializeField] private float debugCooldownTimer;
        [SerializeField] private float debugTacticalOffset;
        [SerializeField] private float debugTrafficSpeedLimitKph;
        [SerializeField] private bool debugLeftClear;
        [SerializeField] private bool debugRightClear;
        [SerializeField] private string debugDecision = "Idle";

        #endregion

        #region Runtime

        private AIRacerSensor sensor;
        private AIRacerSensor passTarget;

        private float passTimer;
        private float cooldownTimer;
        private float currentTacticalOffset;
        private bool isHunting;

        public OvertakeState CurrentState => currentState;
        public float CurrentTacticalOffset => currentTacticalOffset;
        public float TrafficSpeedLimitMetersPerSecond { get; private set; } = float.PositiveInfinity;

        public bool IsHunting => isHunting;

        public bool IsPassing =>
            currentState == OvertakeState.PassingLeft ||
            currentState == OvertakeState.PassingRight;

        public float HuntTargetDistance =>
            isHunting && sensor != null
                ? sensor.AheadDistance
                : float.PositiveInfinity;

        #endregion

        #region Unity

        private void Awake()
        {
            sensor = GetComponent<AIRacerSensor>();
        }

        private void OnDisable()
        {
            passTarget = null;
            passTimer = 0f;
            cooldownTimer = 0f;
            currentTacticalOffset = 0f;
            currentState = OvertakeState.Idle;
            isHunting = false;

            TrafficSpeedLimitMetersPerSecond =
                float.PositiveInfinity;
        }

        #endregion

        #region Planning

        public float UpdatePlan(
            float baseLateralOffset,
            float maximumAbsoluteOffset,
            float currentSpeed,
            bool allowNewPass)
        {
            sensor.Scan();

            float deltaTime = Time.fixedDeltaTime;

            UpdateHuntState();

            if (cooldownTimer > 0f)
                cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);

            switch (currentState)
            {
                case OvertakeState.Idle:
                    UpdateIdle(
                        baseLateralOffset,
                        maximumAbsoluteOffset,
                        currentSpeed,
                        allowNewPass);
                    break;

                case OvertakeState.PassingLeft:
                case OvertakeState.PassingRight:
                    UpdateActivePass(deltaTime);
                    break;

                case OvertakeState.Cooldown:
                    if (cooldownTimer <= 0f)
                    {
                        currentState = OvertakeState.Idle;
                        debugDecision =
                            isHunting
                                ? "Hunting / Ready"
                                : "Idle";
                    }
                    break;
            }

            float desiredTacticalOffset = 0f;

            if (currentState == OvertakeState.PassingLeft)
                desiredTacticalOffset = -passOffset;
            else if (currentState == OvertakeState.PassingRight)
                desiredTacticalOffset = passOffset;

            currentTacticalOffset = Mathf.MoveTowards(
                currentTacticalOffset,
                desiredTacticalOffset,
                tacticalOffsetShiftSpeed * deltaTime);

            UpdateTrafficSpeedLimit();
            UpdateDebug();

            return currentTacticalOffset;
        }

        private void UpdateHuntState()
        {
            AIRacerSensor target = sensor.AheadRacer;

            isHunting =
                target != null &&
                target.isActiveAndEnabled &&
                sensor.AheadDistance <= huntDistance;

            debugHunting = isHunting;
            debugHuntTarget =
                isHunting
                    ? target.RacerId
                    : "None";

            debugTargetDistance =
                target != null
                    ? sensor.AheadDistance
                    : 0f;

            debugClosingSpeed =
                target != null
                    ? sensor.AheadClosingSpeed
                    : 0f;

            if (currentState != OvertakeState.Idle)
                return;

            debugDecision =
                isHunting
                    ? "Hunting"
                    : "Idle";
        }

        private void UpdateIdle(
            float baseOffset,
            float maximumAbsoluteOffset,
            float currentSpeed,
            bool allowNewPass)
        {
            if (!isHunting)
                return;

            if (!allowNewPass)
            {
                debugDecision = "Hunting / Corner Hold";
                return;
            }

            if (cooldownTimer > 0f)
            {
                debugDecision = "Hunting / Cooldown";
                return;
            }

            if (currentSpeed < minimumPassSpeed)
            {
                debugDecision = "Hunting / Below Pass Speed";
                return;
            }

            if (sensor.AheadDistance > passPreparationDistance)
            {
                debugDecision = "Hunting / Closing Gap";
                return;
            }

            bool targetTooFast =
                sensor.AheadClosingSpeed <
                -maximumPassSpeedDeficit;

            bool forcedAttack =
                sensor.AheadDistance <= forcedPassDistance;

            if (targetTooFast &&
                !forcedAttack)
            {
                debugDecision = "Hunting / Target Pulling Away";
                return;
            }

            TryBeginPass(
                baseOffset,
                maximumAbsoluteOffset);
        }

        private void TryBeginPass(
            float baseOffset,
            float maximumAbsoluteOffset)
        {
            AIRacerSensor target = sensor.AheadRacer;

            if (target == null)
                return;

            float leftCandidate =
                baseOffset - passOffset;

            float rightCandidate =
                baseOffset + passOffset;

            bool leftWithinTrack =
                Mathf.Abs(leftCandidate) <=
                maximumAbsoluteOffset;

            bool rightWithinTrack =
                Mathf.Abs(rightCandidate) <=
                maximumAbsoluteOffset;

            debugLeftClear =
                leftWithinTrack &&
                sensor.IsSideClear(
                    -1,
                    passOffset);

            debugRightClear =
                rightWithinTrack &&
                sensor.IsSideClear(
                    1,
                    passOffset);

            if (!debugLeftClear &&
                !debugRightClear)
            {
                debugDecision = "Hunting / No Passing Lane";
                return;
            }

            int chosenSide =
                ChoosePassingSide(
                    leftCandidate,
                    rightCandidate,
                    maximumAbsoluteOffset);

            if (chosenSide == 0)
                return;

            passTarget = target;
            passTimer = 0f;

            currentState =
                chosenSide < 0
                    ? OvertakeState.PassingLeft
                    : OvertakeState.PassingRight;

            debugDecision =
                chosenSide < 0
                    ? "Pass Started / Left"
                    : "Pass Started / Right";
        }

        private int ChoosePassingSide(
            float leftCandidate,
            float rightCandidate,
            float maximumAbsoluteOffset)
        {
            if (debugLeftClear &&
                !debugRightClear)
            {
                return -1;
            }

            if (!debugLeftClear &&
                debugRightClear)
            {
                return 1;
            }

            if (!debugLeftClear &&
                !debugRightClear)
            {
                return 0;
            }

            /*
             * Prefer the opposite side of the leading racer
             * when its lateral position makes that obvious.
             */
            if (sensor.AheadLateralOffset > 0.25f)
                return -1;

            if (sensor.AheadLateralOffset < -0.25f)
                return 1;

            /*
             * Otherwise use whichever candidate leaves more
             * remaining room to the track boundary.
             */
            float leftWallRoom =
                maximumAbsoluteOffset -
                Mathf.Abs(leftCandidate);

            float rightWallRoom =
                maximumAbsoluteOffset -
                Mathf.Abs(rightCandidate);

            return
                rightWallRoom > leftWallRoom
                    ? 1
                    : -1;
        }

        private void UpdateActivePass(
            float deltaTime)
        {
            passTimer += deltaTime;

            if (passTarget == null ||
                !passTarget.isActiveAndEnabled)
            {
                if (passTimer >= minimumCommitTime)
                    FinishPass(true);

                return;
            }

            float longitudinalDistance =
                sensor.GetLongitudinalDistanceTo(
                    passTarget);

            bool clearlyAhead =
                longitudinalDistance <=
                -completionLeadDistance;

            if (passTimer >= minimumCommitTime &&
                clearlyAhead)
            {
                debugDecision = "Pass Complete";
                FinishPass(true);
                return;
            }

            if (passTimer >= maximumPassTime)
            {
                debugDecision = "Pass Retry";
                FinishPass(false);
                return;
            }

            debugDecision =
                currentState == OvertakeState.PassingLeft
                    ? "Passing Left"
                    : "Passing Right";
        }

        private void FinishPass(
            bool completed)
        {
            passTarget = null;
            passTimer = 0f;

            cooldownTimer =
                completed
                    ? passCooldown
                    : failedPassRetryDelay;

            currentState =
                OvertakeState.Cooldown;
        }

        #endregion

        #region Traffic Speed

        private void UpdateTrafficSpeedLimit()
        {
            TrafficSpeedLimitMetersPerSecond =
                float.PositiveInfinity;

            AIRacerSensor ahead =
                sensor.AheadRacer;

            if (ahead == null)
                return;

            float distance =
                sensor.AheadDistance;

            /*
             * Once sufficiently established beside the opponent,
             * don't make the AI match their speed. At this point
             * it should continue attacking.
             */
            bool establishedInPassLane =
                IsPassing &&
                Mathf.Abs(currentTacticalOffset) >=
                passOffset *
                establishedPassLaneThreshold;

            if (establishedInPassLane)
                return;

            if (distance <=
                emergencyFollowingDistance)
            {
                TrafficSpeedLimitMetersPerSecond =
                    ahead.SpeedMetersPerSecond +
                    emergencyFollowingSpeedBuffer;

                return;
            }

            if (distance >= followingDistance)
                return;

            float normalizedDistance =
                Mathf.InverseLerp(
                    emergencyFollowingDistance,
                    followingDistance,
                    distance);

            float buffer =
                Mathf.Lerp(
                    emergencyFollowingSpeedBuffer,
                    followingSpeedBuffer,
                    normalizedDistance);

            TrafficSpeedLimitMetersPerSecond =
                ahead.SpeedMetersPerSecond +
                buffer;
        }

        #endregion

        #region Debug

        private void UpdateDebug()
        {
            debugPassTarget =
                passTarget != null
                    ? passTarget.RacerId
                    : "None";

            debugPassTimer =
                passTimer;

            debugCooldownTimer =
                Mathf.Max(
                    0f,
                    cooldownTimer);

            debugTacticalOffset =
                currentTacticalOffset;

            debugTrafficSpeedLimitKph =
                float.IsPositiveInfinity(
                    TrafficSpeedLimitMetersPerSecond)
                    ? 0f
                    : TrafficSpeedLimitMetersPerSecond *
                      3.6f;
        }

        #endregion
    }
}