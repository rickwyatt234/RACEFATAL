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

        #region Passing

        [Header("Pass Initiation")]
        [Tooltip("Minimum speed required before the AI attempts an overtake.")]
        [Min(0f)][SerializeField] private float minimumPassSpeed = 20f;

        [Tooltip("Maximum distance to a slower racer at which a pass may begin.")]
        [Min(1f)][SerializeField] private float passTriggerDistance = 30f;

        [Tooltip("Minimum speed advantage required before normally attempting a pass.")]
        [Min(0f)][SerializeField] private float minimumClosingSpeed = 0.6f;

        [Tooltip("At this distance the AI may attempt a pass even with a very small speed advantage.")]
        [Min(0f)][SerializeField] private float forcedPassDistance = 9f;

        [Tooltip("Lateral distance added to the normal racing line while overtaking.")]
        [Min(0.1f)][SerializeField] private float passOffset = 2.6f;

        #endregion

        #region Commitment

        [Header("Pass Commitment")]
        [Tooltip("Minimum time the AI remains committed to a chosen passing side.")]
        [Min(0f)][SerializeField] private float minimumCommitTime = 1.25f;

        [Tooltip("Maximum time an attempted pass may remain active.")]
        [Min(0.1f)][SerializeField] private float maximumPassTime = 5f;

        [Tooltip("How far ahead of the passed racer the AI should be before declaring the pass complete.")]
        [Min(0f)][SerializeField] private float completionLeadDistance = 5f;

        [Tooltip("Delay before another pass may be attempted.")]
        [Min(0f)][SerializeField] private float passCooldown = 2f;

        [Tooltip("How quickly the temporary passing offset moves laterally.")]
        [Min(0.1f)][SerializeField] private float tacticalOffsetShiftSpeed = 2.5f;

        #endregion

        #region Following

        [Header("Following")]
        [Tooltip("Distance at which the AI begins matching traffic if it cannot pass.")]
        [Min(0.1f)][SerializeField] private float followingDistance = 12f;

        [Tooltip("Very close traffic causes the AI to approximately match the leading racer's speed.")]
        [Min(0.1f)][SerializeField] private float emergencyFollowingDistance = 5f;

        [Tooltip("Extra speed allowed while following at the outer edge of the following zone.")]
        [Min(0f)][SerializeField] private float followingSpeedBuffer = 2.5f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private OvertakeState currentState;
        [SerializeField] private string debugPassTarget;
        [SerializeField] private float debugPassTimer;
        [SerializeField] private float debugCooldownTimer;
        [SerializeField] private float debugTacticalOffset;
        [SerializeField] private float debugTrafficSpeedLimitKph;
        [SerializeField] private bool debugLeftClear;
        [SerializeField] private bool debugRightClear;

        #endregion

        #region Runtime

        private AIRacerSensor sensor;
        private AIRacerSensor passTarget;

        private int passSide;
        private float passTimer;
        private float cooldownTimer;
        private float currentTacticalOffset;

        public OvertakeState CurrentState => currentState;
        public float CurrentTacticalOffset => currentTacticalOffset;
        public float TrafficSpeedLimitMetersPerSecond { get; private set; } = float.PositiveInfinity;

        #endregion

        #region Unity

        private void Awake()
        {
            sensor = GetComponent<AIRacerSensor>();
        }

        private void OnDisable()
        {
            passTarget = null;
            passSide = 0;
            passTimer = 0f;
            cooldownTimer = 0f;
            currentTacticalOffset = 0f;
            currentState = OvertakeState.Idle;
            TrafficSpeedLimitMetersPerSecond = float.PositiveInfinity;
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

            if (cooldownTimer > 0f)
                cooldownTimer -= deltaTime;

            switch (currentState)
            {
                case OvertakeState.Idle:
                    if (allowNewPass &&
                        cooldownTimer <= 0f &&
                        currentSpeed >= minimumPassSpeed)
                    {
                        TryBeginPass(
                            baseLateralOffset,
                            maximumAbsoluteOffset);
                    }
                    break;

                case OvertakeState.PassingLeft:
                case OvertakeState.PassingRight:
                    UpdateActivePass(deltaTime);
                    break;

                case OvertakeState.Cooldown:
                    if (cooldownTimer <= 0f)
                        currentState = OvertakeState.Idle;
                    break;
            }

            float desiredTacticalOffset =
                currentState == OvertakeState.PassingLeft
                    ? -passOffset
                    : currentState == OvertakeState.PassingRight
                        ? passOffset
                        : 0f;

            currentTacticalOffset = Mathf.MoveTowards(
                currentTacticalOffset,
                desiredTacticalOffset,
                tacticalOffsetShiftSpeed * deltaTime);

            UpdateTrafficSpeedLimit();

            debugPassTarget = passTarget != null ? passTarget.RacerId : "None";
            debugPassTimer = passTimer;
            debugCooldownTimer = Mathf.Max(0f, cooldownTimer);
            debugTacticalOffset = currentTacticalOffset;
            debugTrafficSpeedLimitKph =
                float.IsPositiveInfinity(TrafficSpeedLimitMetersPerSecond)
                    ? 0f
                    : TrafficSpeedLimitMetersPerSecond * 3.6f;

            return currentTacticalOffset;
        }

        private void TryBeginPass(float baseOffset, float maximumAbsoluteOffset)
        {
            AIRacerSensor target = sensor.AheadRacer;

            if (target == null) return;
            if (sensor.AheadDistance > passTriggerDistance) return;

            bool hasSpeedAdvantage =
                sensor.AheadClosingSpeed >= minimumClosingSpeed;

            bool veryClose =
                sensor.AheadDistance <= forcedPassDistance &&
                sensor.AheadClosingSpeed > 0f;

            if (!hasSpeedAdvantage && !veryClose)
                return;

            float leftCandidate = baseOffset - passOffset;
            float rightCandidate = baseOffset + passOffset;

            bool leftWithinTrack =
                Mathf.Abs(leftCandidate) <= maximumAbsoluteOffset;

            bool rightWithinTrack =
                Mathf.Abs(rightCandidate) <= maximumAbsoluteOffset;

            debugLeftClear =
                leftWithinTrack &&
                sensor.IsSideClear(-1, passOffset);

            debugRightClear =
                rightWithinTrack &&
                sensor.IsSideClear(1, passOffset);

            if (!debugLeftClear && !debugRightClear)
                return;

            int chosenSide;

            if (debugLeftClear && !debugRightClear)
            {
                chosenSide = -1;
            }
            else if (!debugLeftClear && debugRightClear)
            {
                chosenSide = 1;
            }
            else
            {
                /*
                 * If the leading racer is slightly to one side,
                 * prefer passing on the opposite side.
                 */
                if (sensor.AheadLateralOffset > 0.25f)
                {
                    chosenSide = -1;
                }
                else if (sensor.AheadLateralOffset < -0.25f)
                {
                    chosenSide = 1;
                }
                else
                {
                    /*
                     * Otherwise prefer the candidate that leaves
                     * more room between the desired line and wall.
                     */
                    float leftWallRoom =
                        maximumAbsoluteOffset -
                        Mathf.Abs(leftCandidate);

                    float rightWallRoom =
                        maximumAbsoluteOffset -
                        Mathf.Abs(rightCandidate);

                    chosenSide =
                        rightWallRoom > leftWallRoom
                            ? 1
                            : -1;
                }
            }

            passTarget = target;
            passSide = chosenSide;
            passTimer = 0f;

            currentState =
                chosenSide < 0
                    ? OvertakeState.PassingLeft
                    : OvertakeState.PassingRight;
        }

        private void UpdateActivePass(float deltaTime)
        {
            passTimer += deltaTime;

            if (passTarget == null || !passTarget.isActiveAndEnabled)
            {
                if (passTimer >= minimumCommitTime)
                    FinishPass();

                return;
            }

            float longitudinalDistance =
                sensor.GetLongitudinalDistanceTo(passTarget);

            bool clearlyAhead =
                longitudinalDistance <= -completionLeadDistance;

            if (passTimer >= minimumCommitTime && clearlyAhead)
            {
                FinishPass();
                return;
            }

            if (passTimer >= maximumPassTime)
                FinishPass();
        }

        private void FinishPass()
        {
            passTarget = null;
            passSide = 0;
            passTimer = 0f;

            cooldownTimer = passCooldown;
            currentState = OvertakeState.Cooldown;
        }

        #endregion

        #region Traffic Speed

        private void UpdateTrafficSpeedLimit()
        {
            TrafficSpeedLimitMetersPerSecond = float.PositiveInfinity;

            AIRacerSensor ahead = sensor.AheadRacer;

            if (ahead == null)
                return;

            float distance = sensor.AheadDistance;

            /*
             * If we've already shifted substantially into our
             * passing lane, do not unnecessarily match the
             * target's speed.
             */
            bool establishedInPassLane =
                (currentState == OvertakeState.PassingLeft ||
                 currentState == OvertakeState.PassingRight) &&
                Mathf.Abs(currentTacticalOffset) >= passOffset * 0.5f;

            if (establishedInPassLane)
                return;

            if (distance <= emergencyFollowingDistance)
            {
                TrafficSpeedLimitMetersPerSecond =
                    ahead.SpeedMetersPerSecond;

                return;
            }

            if (distance >= followingDistance)
                return;

            float normalizedDistance = Mathf.InverseLerp(
                emergencyFollowingDistance,
                followingDistance,
                distance);

            TrafficSpeedLimitMetersPerSecond =
                ahead.SpeedMetersPerSecond +
                followingSpeedBuffer * normalizedDistance;
        }

        #endregion
    }
}