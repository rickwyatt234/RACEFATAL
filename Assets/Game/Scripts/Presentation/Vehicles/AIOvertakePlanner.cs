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
        [Tooltip("Base distance at which another racer ahead becomes worth actively pursuing.")]
        [Min(1f)][SerializeField] private float huntDistance = 90f;

        [Tooltip("Base distance at which the AI begins preparing an actual passing lane.")]
        [Min(1f)][SerializeField] private float passPreparationDistance = 55f;

        [Tooltip("Base speed deficit still considered potentially passable.")]
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
        [Tooltip("Base minimum time the AI remains committed to its chosen side.")]
        [Min(0f)][SerializeField] private float minimumCommitTime = 1.25f;

        [Tooltip("Base maximum duration of one continuous passing attempt.")]
        [Min(0.1f)][SerializeField] private float maximumPassTime = 10f;

        [Tooltip("Distance ahead of the target required before the pass is considered complete.")]
        [Min(0f)][SerializeField] private float completionLeadDistance = 4f;

        [Tooltip("Base cooldown after a successfully completed pass.")]
        [Min(0f)][SerializeField] private float passCooldown = 0.75f;

        [Tooltip("Base retry delay after a failed passing attempt.")]
        [Min(0f)][SerializeField] private float failedPassRetryDelay = 0.35f;

        [Tooltip("Base speed at which the temporary passing line moves sideways.")]
        [Min(0.1f)][SerializeField] private float tacticalOffsetShiftSpeed = 5.5f;

        #endregion

        #region Following

        [Header("Following")]
        [Tooltip("Distance where the AI begins respecting traffic speed ahead.")]
        [Min(0.1f)][SerializeField] private float followingDistance = 16f;

        [Tooltip("Very close distance where collision avoidance strongly limits closing speed.")]
        [Min(0.1f)][SerializeField] private float emergencyFollowingDistance = 6f;

        [Tooltip("Additional speed allowed at the outer edge of the following zone.")]
        [Min(0f)][SerializeField] private float followingSpeedBuffer = 4.5f;

        [Tooltip("Small closing-speed allowance retained during emergency following.")]
        [Min(0f)][SerializeField] private float emergencyFollowingSpeedBuffer = 1.5f;

        [Tooltip("Once this fraction of the passing offset has been reached, normal traffic speed matching is removed.")]
        [Range(0f, 1f)][SerializeField] private float establishedPassLaneThreshold = 0.35f;

        #endregion

        #region Skill Mapping

        [Header("Overtaking Skill Mapping")]
        [Tooltip("Multiplier applied to Hunt Distance at Overtaking Skill = 0.")]
        [Range(0.1f, 1f)][SerializeField] private float lowSkillHuntDistanceMultiplier = 0.6f;

        [Tooltip("Multiplier applied to Pass Preparation Distance at Overtaking Skill = 0.")]
        [Range(0.1f, 1f)][SerializeField] private float lowSkillPreparationDistanceMultiplier = 0.65f;

        [Tooltip("Multiplier applied to lateral shift speed at Overtaking Skill = 0.")]
        [Range(0.1f, 1f)][SerializeField] private float lowSkillShiftSpeedMultiplier = 0.55f;

        [Tooltip("Multiplier applied to lateral shift speed at Overtaking Skill = 1.")]
        [Min(1f)][SerializeField] private float highSkillShiftSpeedMultiplier = 1.15f;

        [Tooltip("Multiplier applied to successful-pass cooldown at Overtaking Skill = 0.")]
        [Min(1f)][SerializeField] private float lowSkillCooldownMultiplier = 1.6f;

        [Tooltip("Multiplier applied to successful-pass cooldown at Overtaking Skill = 1.")]
        [Range(0.1f, 1f)][SerializeField] private float highSkillCooldownMultiplier = 0.65f;

        [Tooltip("Multiplier applied to failed-pass retry delay at Overtaking Skill = 0.")]
        [Min(1f)][SerializeField] private float lowSkillRetryMultiplier = 2f;

        [Tooltip("Multiplier applied to failed-pass retry delay at Overtaking Skill = 1.")]
        [Range(0.1f, 1f)][SerializeField] private float highSkillRetryMultiplier = 0.75f;

        [Tooltip("Low-skill racers may stay trapped in an unsuccessful pass longer.")]
        [Min(1f)][SerializeField] private float lowSkillMaximumPassTimeMultiplier = 1.25f;

        [Tooltip("High-skill racers identify a failed attempt and reset sooner.")]
        [Range(0.1f, 1f)][SerializeField] private float highSkillMaximumPassTimeMultiplier = 0.8f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private float debugOvertakingSkill;

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
        [SerializeField] private string debugDecision = "Not Initialized";

        [Header("Runtime Skill Values")]
        [SerializeField] private float debugRuntimeHuntDistance;
        [SerializeField] private float debugRuntimePreparationDistance;
        [SerializeField] private float debugRuntimeShiftSpeed;
        [SerializeField] private float debugRuntimePassCooldown;
        [SerializeField] private float debugRuntimeFailedRetryDelay;
        [SerializeField] private float debugRuntimeMaximumPassTime;

        #endregion

        #region Runtime

        private AIRacerSensor sensor;
        private AIRacerSensor passTarget;

        private float overtakingSkill;

        private float runtimeHuntDistance;
        private float runtimePassPreparationDistance;
        private float runtimeTacticalOffsetShiftSpeed;
        private float runtimePassCooldown;
        private float runtimeFailedPassRetryDelay;
        private float runtimeMaximumPassTime;

        private float passTimer;
        private float cooldownTimer;
        private float currentTacticalOffset;

        private bool isHunting;
        private bool initialized;

        public OvertakeState CurrentState => currentState;
        public float CurrentTacticalOffset => currentTacticalOffset;
        public float TrafficSpeedLimitMetersPerSecond { get; private set; } = float.PositiveInfinity;

        public bool IsInitialized => initialized;
        public bool IsHunting => isHunting;
        public float OvertakingSkill => overtakingSkill;

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
            ResetRuntimeState();
        }

        #endregion

        #region Initialization

        public bool Initialize(float skill)
        {
            if (sensor == null)
            {
                Debug.LogError(
                    $"{nameof(AIOvertakePlanner)} requires an {nameof(AIRacerSensor)}.",
                    this);

                return false;
            }

            overtakingSkill = Mathf.Clamp01(skill);

            runtimeHuntDistance =
                huntDistance *
                Mathf.Lerp(
                    lowSkillHuntDistanceMultiplier,
                    1f,
                    overtakingSkill);

            runtimePassPreparationDistance =
                passPreparationDistance *
                Mathf.Lerp(
                    lowSkillPreparationDistanceMultiplier,
                    1f,
                    overtakingSkill);

            runtimeTacticalOffsetShiftSpeed =
                tacticalOffsetShiftSpeed *
                Mathf.Lerp(
                    lowSkillShiftSpeedMultiplier,
                    highSkillShiftSpeedMultiplier,
                    overtakingSkill);

            runtimePassCooldown =
                passCooldown *
                Mathf.Lerp(
                    lowSkillCooldownMultiplier,
                    highSkillCooldownMultiplier,
                    overtakingSkill);

            runtimeFailedPassRetryDelay =
                failedPassRetryDelay *
                Mathf.Lerp(
                    lowSkillRetryMultiplier,
                    highSkillRetryMultiplier,
                    overtakingSkill);

            runtimeMaximumPassTime =
                maximumPassTime *
                Mathf.Lerp(
                    lowSkillMaximumPassTimeMultiplier,
                    highSkillMaximumPassTimeMultiplier,
                    overtakingSkill);

            ResetRuntimeState();

            initialized = true;
            debugInitialized = true;
            debugOvertakingSkill = overtakingSkill;

            debugRuntimeHuntDistance = runtimeHuntDistance;
            debugRuntimePreparationDistance = runtimePassPreparationDistance;
            debugRuntimeShiftSpeed = runtimeTacticalOffsetShiftSpeed;
            debugRuntimePassCooldown = runtimePassCooldown;
            debugRuntimeFailedRetryDelay = runtimeFailedPassRetryDelay;
            debugRuntimeMaximumPassTime = runtimeMaximumPassTime;

            debugDecision = "Idle";

            return true;
        }

        private void ResetRuntimeState()
        {
            passTarget = null;

            passTimer = 0f;
            cooldownTimer = 0f;
            currentTacticalOffset = 0f;

            currentState = OvertakeState.Idle;
            isHunting = false;

            TrafficSpeedLimitMetersPerSecond =
                float.PositiveInfinity;

            debugHunting = false;
            debugHuntTarget = "None";
            debugPassTarget = "None";
            debugPassTimer = 0f;
            debugCooldownTimer = 0f;
            debugTacticalOffset = 0f;
        }

        #endregion

        #region Planning

        public float UpdatePlan(
            float baseLateralOffset,
            float maximumAbsoluteOffset,
            float currentSpeed,
            bool allowNewPass)
        {
            if (!initialized)
                return 0f;

            sensor.Scan();

            float deltaTime =
                Time.fixedDeltaTime;

            UpdateHuntState();

            if (cooldownTimer > 0f)
            {
                cooldownTimer =
                    Mathf.Max(
                        0f,
                        cooldownTimer -
                        deltaTime);
            }

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
                    UpdateActivePass(
                        deltaTime);
                    break;

                case OvertakeState.Cooldown:
                    if (cooldownTimer <= 0f)
                    {
                        currentState =
                            OvertakeState.Idle;

                        debugDecision =
                            isHunting
                                ? "Hunting / Ready"
                                : "Idle";
                    }
                    break;
            }

            float desiredTacticalOffset = 0f;

            if (currentState ==
                OvertakeState.PassingLeft)
            {
                desiredTacticalOffset =
                    -passOffset;
            }
            else if (currentState ==
                     OvertakeState.PassingRight)
            {
                desiredTacticalOffset =
                    passOffset;
            }

            currentTacticalOffset =
                Mathf.MoveTowards(
                    currentTacticalOffset,
                    desiredTacticalOffset,
                    runtimeTacticalOffsetShiftSpeed *
                    deltaTime);

            UpdateTrafficSpeedLimit();
            UpdateDebug();

            return currentTacticalOffset;
        }

        private void UpdateHuntState()
        {
            AIRacerSensor target =
                sensor.AheadRacer;

            isHunting =
                target != null &&
                target.isActiveAndEnabled &&
                sensor.AheadDistance <=
                runtimeHuntDistance;

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

            if (currentState !=
                OvertakeState.Idle)
            {
                return;
            }

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
                debugDecision =
                    "Hunting / Corner Hold";

                return;
            }

            if (cooldownTimer > 0f)
            {
                debugDecision =
                    "Hunting / Cooldown";

                return;
            }

            if (currentSpeed <
                minimumPassSpeed)
            {
                debugDecision =
                    "Hunting / Below Pass Speed";

                return;
            }

            if (sensor.AheadDistance >
                runtimePassPreparationDistance)
            {
                debugDecision =
                    "Hunting / Closing Gap";

                return;
            }

            bool targetTooFast =
                sensor.AheadClosingSpeed <
                -maximumPassSpeedDeficit;

            bool forcedAttack =
                sensor.AheadDistance <=
                forcedPassDistance;

            if (targetTooFast &&
                !forcedAttack)
            {
                debugDecision =
                    "Hunting / Target Pulling Away";

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
            AIRacerSensor target =
                sensor.AheadRacer;

            if (target == null)
                return;

            float leftCandidate =
                baseOffset -
                passOffset;

            float rightCandidate =
                baseOffset +
                passOffset;

            bool leftWithinTrack =
                Mathf.Abs(
                    leftCandidate) <=
                maximumAbsoluteOffset;

            bool rightWithinTrack =
                Mathf.Abs(
                    rightCandidate) <=
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
                debugDecision =
                    "Hunting / No Passing Lane";

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
             * With both sides available, skilled racers pay
             * more attention to the opponent's positioning.
             *
             * Low-skill racers simply favor whichever side
             * currently has more track room.
             */
            if (overtakingSkill >= 0.45f)
            {
                if (sensor.AheadLateralOffset >
                    0.25f)
                {
                    return -1;
                }

                if (sensor.AheadLateralOffset <
                    -0.25f)
                {
                    return 1;
                }
            }

            float leftWallRoom =
                maximumAbsoluteOffset -
                Mathf.Abs(
                    leftCandidate);

            float rightWallRoom =
                maximumAbsoluteOffset -
                Mathf.Abs(
                    rightCandidate);

            return
                rightWallRoom >
                leftWallRoom
                    ? 1
                    : -1;
        }

        private void UpdateActivePass(
            float deltaTime)
        {
            passTimer +=
                deltaTime;

            if (passTarget == null ||
                !passTarget.isActiveAndEnabled)
            {
                if (passTimer >=
                    minimumCommitTime)
                {
                    FinishPass(true);
                }

                return;
            }

            float longitudinalDistance =
                sensor.GetLongitudinalDistanceTo(
                    passTarget);

            bool clearlyAhead =
                longitudinalDistance <=
                -completionLeadDistance;

            if (passTimer >=
                    minimumCommitTime &&
                clearlyAhead)
            {
                debugDecision =
                    "Pass Complete";

                FinishPass(true);

                return;
            }

            if (passTimer >=
                runtimeMaximumPassTime)
            {
                debugDecision =
                    "Pass Retry";

                FinishPass(false);

                return;
            }

            debugDecision =
                currentState ==
                OvertakeState.PassingLeft
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
                    ? runtimePassCooldown
                    : runtimeFailedPassRetryDelay;

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

            bool establishedInPassLane =
                IsPassing &&
                Mathf.Abs(
                    currentTacticalOffset) >=
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

            if (distance >=
                followingDistance)
            {
                return;
            }

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