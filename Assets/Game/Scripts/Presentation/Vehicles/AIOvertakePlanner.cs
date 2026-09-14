using System;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [Serializable]
    public class AIOvertakePlanner
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
        [Tooltip("Base distance at which another racer ahead becomes worth pursuing.")]
        [Min(1f)][SerializeField] private float huntDistance = 90f;

        [Tooltip("Base distance at which the AI begins setting up a passing lane.")]
        [Min(1f)][SerializeField] private float passPreparationDistance = 55f;

        [Tooltip("How much faster the target may be while the AI still attempts to chase and pass it.")]
        [Min(0f)][SerializeField] private float maximumPassSpeedDeficit = 4f;

        #endregion

        #region Passing

        [Header("Pass Initiation")]
        [Tooltip("Minimum speed required before the AI commits to an overtake.")]
        [Min(0f)][SerializeField] private float minimumPassSpeed = 15f;

        [Tooltip("At this distance the AI may force an available pass even if the opponent has a speed advantage.")]
        [Min(0f)][SerializeField] private float forcedPassDistance = 16f;

        [Tooltip("Lateral distance added to the racing line while overtaking.")]
        [Min(0.1f)][SerializeField] private float passOffset = 3f;

        #endregion

        #region Commitment

        [Header("Pass Commitment")]
        [Min(0f)][SerializeField] private float minimumCommitTime = 1.25f;
        [Min(0.1f)][SerializeField] private float maximumPassTime = 10f;
        [Min(0f)][SerializeField] private float completionLeadDistance = 4f;
        [Min(0f)][SerializeField] private float passCooldown = 0.75f;
        [Min(0f)][SerializeField] private float failedPassRetryDelay = 0.35f;
        [Min(0.1f)][SerializeField] private float tacticalOffsetShiftSpeed = 5.5f;

        #endregion

        #region Following

        [Header("Following")]
        [Min(0.1f)][SerializeField] private float followingDistance = 16f;
        [Min(0.1f)][SerializeField] private float emergencyFollowingDistance = 6f;
        [Min(0f)][SerializeField] private float followingSpeedBuffer = 4.5f;
        [Min(0f)][SerializeField] private float emergencyFollowingSpeedBuffer = 1.5f;

        [Tooltip("Once this fraction of the pass offset has been reached, traffic speed matching is removed.")]
        [Range(0f, 1f)][SerializeField] private float establishedPassLaneThreshold = 0.35f;

        #endregion

        #region Skill Mapping

        [Header("Skill Mapping")]
        [Tooltip("Hunt-distance multiplier at minimum Overtaking Skill.")]
        [Range(0.5f, 1f)][SerializeField] private float lowSkillHuntMultiplier = 0.8f;

        [Tooltip("Hunt-distance multiplier at maximum Overtaking Skill.")]
        [Range(1f, 1.5f)][SerializeField] private float highSkillHuntMultiplier = 1.1f;

        [Tooltip("Pass-preparation distance multiplier at minimum Overtaking Skill.")]
        [Range(0.5f, 1f)][SerializeField] private float lowSkillPreparationMultiplier = 0.8f;

        [Tooltip("Pass-preparation distance multiplier at maximum Overtaking Skill.")]
        [Range(1f, 1.5f)][SerializeField] private float highSkillPreparationMultiplier = 1.1f;

        [Tooltip("Lateral passing response multiplier at minimum Overtaking Skill.")]
        [Range(0.25f, 1f)][SerializeField] private float lowSkillShiftMultiplier = 0.7f;

        [Tooltip("Lateral passing response multiplier at maximum Overtaking Skill.")]
        [Range(1f, 2f)][SerializeField] private float highSkillShiftMultiplier = 1.2f;

        [Tooltip("Cooldown multiplier at minimum Overtaking Skill.")]
        [Range(1f, 3f)][SerializeField] private float lowSkillCooldownMultiplier = 1.4f;

        [Tooltip("Cooldown multiplier at maximum Overtaking Skill.")]
        [Range(0.1f, 1f)][SerializeField] private float highSkillCooldownMultiplier = 0.7f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private OvertakeState currentState;
        [SerializeField] private float debugOvertakingSkill;

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

        #endregion

        #region Runtime

        private AIRacerSensor sensor;
        private AIRacerSensor passTarget;

        private float runtimeHuntDistance;
        private float runtimePassPreparationDistance;
        private float runtimeShiftSpeed;
        private float runtimePassCooldown;
        private float runtimeFailedPassRetryDelay;

        private float passTimer;
        private float cooldownTimer;
        private float currentTacticalOffset;

        private bool isHunting;
        private bool initialized;

        #endregion

        #region Public

        public bool IsInitialized => initialized;
        public OvertakeState CurrentState => currentState;
        public float CurrentTacticalOffset => currentTacticalOffset;

        public float TrafficSpeedLimitMetersPerSecond { get; private set; } =
            float.PositiveInfinity;

        public bool IsHunting => isHunting;

        public bool IsPassing =>
            currentState == OvertakeState.PassingLeft ||
            currentState == OvertakeState.PassingRight;

        public float HuntTargetDistance =>
            isHunting && sensor != null
                ? sensor.AheadDistance
                : float.PositiveInfinity;

        #endregion

        #region Initialization

        public bool Initialize(
            AIRacerSensor racerSensor,
            float overtakingSkill)
        {
            if (racerSensor == null)
            {
                Debug.LogError(
                    $"{nameof(AIOvertakePlanner)} requires an {nameof(AIRacerSensor)}.");

                return false;
            }

            sensor = racerSensor;

            float skill =
                Mathf.Clamp01(
                    overtakingSkill);

            runtimeHuntDistance =
                huntDistance *
                Mathf.Lerp(
                    lowSkillHuntMultiplier,
                    highSkillHuntMultiplier,
                    skill);

            runtimePassPreparationDistance =
                passPreparationDistance *
                Mathf.Lerp(
                    lowSkillPreparationMultiplier,
                    highSkillPreparationMultiplier,
                    skill);

            runtimeShiftSpeed =
                tacticalOffsetShiftSpeed *
                Mathf.Lerp(
                    lowSkillShiftMultiplier,
                    highSkillShiftMultiplier,
                    skill);

            float cooldownMultiplier =
                Mathf.Lerp(
                    lowSkillCooldownMultiplier,
                    highSkillCooldownMultiplier,
                    skill);

            runtimePassCooldown =
                passCooldown *
                cooldownMultiplier;

            runtimeFailedPassRetryDelay =
                failedPassRetryDelay *
                cooldownMultiplier;

            Reset();

            initialized = true;
            debugInitialized = true;
            debugOvertakingSkill = skill;
            debugDecision = "Ready";

            return true;
        }

        #endregion

        #region Planning

        public float UpdatePlan(
            float baseLateralOffset,
            float maximumAbsoluteOffset,
            float currentSpeed,
            bool allowNewPass)
        {
            if (!initialized ||
                sensor == null)
            {
                return 0f;
            }

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
                    runtimeShiftSpeed *
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

            debugHunting =
                isHunting;

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
                    FinishPass(
                        true);
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

                FinishPass(
                    true);

                return;
            }

            if (passTimer >=
                maximumPassTime)
            {
                debugDecision =
                    "Pass Retry";

                FinishPass(
                    false);

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

        #region Traffic

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

        #region Reset / Debug

        public void Reset()
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
            debugTargetDistance = 0f;
            debugClosingSpeed = 0f;
            debugPassTimer = 0f;
            debugCooldownTimer = 0f;
            debugTacticalOffset = 0f;
            debugTrafficSpeedLimitKph = 0f;
            debugLeftClear = false;
            debugRightClear = false;
        }

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