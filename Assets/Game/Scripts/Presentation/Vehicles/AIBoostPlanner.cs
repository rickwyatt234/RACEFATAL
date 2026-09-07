using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    public class AIBoostPlanner : MonoBehaviour
    {
        #region Energy Strategy

        [Header("Energy Strategy")]
        [Tooltip("Energy reserve used by the most aggressive prototype AI.")]
        [Range(0f, 1f)][SerializeField] private float aggressiveEnergyReserve = 0.20f;

        [Tooltip("Energy reserve used by the most conservative prototype AI.")]
        [Range(0f, 1f)][SerializeField] private float conservativeEnergyReserve = 0.35f;

        [Tooltip("Energy must recover this far above the reserve before boost can start again.")]
        [Range(0f, 0.5f)][SerializeField] private float restartEnergyMargin = 0.12f;

        #endregion

        #region Driving Conditions

        [Header("Driving Conditions")]
        [Tooltip("AI will not intentionally begin boosting below this speed.")]
        [Min(0f)][SerializeField] private float minimumBoostSpeedKph = 80f;

        [Tooltip("Maximum corner severity at which a conservative AI begins boosting.")]
        [Range(0f, 1f)][SerializeField] private float conservativeStartCornerSeverity = 0.08f;

        [Tooltip("Maximum corner severity at which an aggressive AI begins boosting.")]
        [Range(0f, 1f)][SerializeField] private float aggressiveStartCornerSeverity = 0.18f;

        [Tooltip("An active booster is stopped once corner severity reaches this level.")]
        [Range(0f, 1f)][SerializeField] private float stopCornerSeverity = 0.28f;

        [Tooltip("Maximum brake input allowed when beginning a boost.")]
        [Range(0f, 1f)][SerializeField] private float maximumBrakeToStart = 0.05f;

        [Tooltip("An active boost is cancelled once braking reaches this amount.")]
        [Range(0f, 1f)][SerializeField] private float stopBrakeThreshold = 0.15f;

        #endregion

        #region Commitment

        [Header("Boost Commitment")]
        [Tooltip("Shortest time an aggressive AI must see good boost conditions before activating.")]
        [Min(0f)][SerializeField] private float aggressiveStraightCommitTime = 0.25f;

        [Tooltip("Longest time a conservative AI must see good boost conditions before activating.")]
        [Min(0f)][SerializeField] private float conservativeStraightCommitTime = 1f;

        [Tooltip("Delay after stopping boost before another activation may occur.")]
        [Min(0f)][SerializeField] private float boostCooldown = 0.5f;

        [Tooltip("When actively passing, the straight commitment time is multiplied by this value.")]
        [Range(0.1f, 1f)][SerializeField] private float passingCommitTimeMultiplier = 0.4f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private bool debugHasBooster;
        [SerializeField] private bool debugBoostActive;
        [SerializeField] private bool debugPassing;
        [SerializeField] private bool debugTrafficLimited;
        [SerializeField] private bool debugSeekingEnergy;

        [SerializeField] private float debugEnergyPercent;
        [SerializeField] private float debugRuntimeAggression;
        [SerializeField] private float debugRuntimeEnergyReserve;
        [SerializeField] private float debugRuntimeRestartThreshold;
        [SerializeField] private float debugRuntimeStartCornerSeverity;
        [SerializeField] private float debugRuntimeStraightCommitTime;

        [SerializeField] private float debugStraightTimer;
        [SerializeField] private float debugCooldownTimer;
        [SerializeField] private string debugDecision = "Not Initialized";

        #endregion

        #region Runtime

        private RaceParticipant participant;
        private RaceRuntimeController raceRuntime;
        private RaceEquipmentSystem equipment;

        private float runtimeEnergyReserve;
        private float runtimeRestartThreshold;
        private float runtimeStartCornerSeverity;
        private float runtimeStraightCommitTime;

        private float straightTimer;
        private float cooldownTimer;

        private bool initialized;

        #endregion

        #region Public

        public bool IsInitialized => initialized;
        public bool IsBoosting => equipment != null && equipment.IsBoosterActive;

        #endregion

        #region Initialization

        public bool Initialize(
            RaceParticipant raceParticipant,
            RaceRuntimeController runtime)
        {
            if (raceParticipant == null)
            {
                Debug.LogError(
                    $"{nameof(AIBoostPlanner)} requires a RaceParticipant.",
                    this);

                return false;
            }

            if (runtime == null)
            {
                Debug.LogError(
                    $"{nameof(AIBoostPlanner)} requires a RaceRuntimeController.",
                    this);

                return false;
            }

            if (raceParticipant.Vehicle?.EquipmentSystem == null ||
                raceParticipant.Vehicle.EnergyPool == null)
            {
                Debug.LogError(
                    $"{nameof(AIBoostPlanner)} requires a race vehicle with Energy and equipment.",
                    this);

                return false;
            }

            participant = raceParticipant;
            raceRuntime = runtime;
            equipment = participant.Vehicle.EquipmentSystem;

            float aggression =
                CalculateDeterministicValue(
                    participant.RacerId);

            runtimeEnergyReserve =
                Mathf.Lerp(
                    conservativeEnergyReserve,
                    aggressiveEnergyReserve,
                    aggression);

            runtimeRestartThreshold =
                Mathf.Clamp01(
                    runtimeEnergyReserve +
                    restartEnergyMargin);

            runtimeStartCornerSeverity =
                Mathf.Lerp(
                    conservativeStartCornerSeverity,
                    aggressiveStartCornerSeverity,
                    aggression);

            runtimeStraightCommitTime =
                Mathf.Lerp(
                    conservativeStraightCommitTime,
                    aggressiveStraightCommitTime,
                    aggression);

            straightTimer = 0f;
            cooldownTimer = 0f;

            debugRuntimeAggression =
                aggression;

            debugRuntimeEnergyReserve =
                runtimeEnergyReserve;

            debugRuntimeRestartThreshold =
                runtimeRestartThreshold;

            debugRuntimeStartCornerSeverity =
                runtimeStartCornerSeverity;

            debugRuntimeStraightCommitTime =
                runtimeStraightCommitTime;

            debugHasBooster =
                equipment.HasBooster;

            debugInitialized = true;
            initialized = true;

            return true;
        }

        #endregion

        #region Planning

        public bool UpdatePlan(
            float cornerSeverity,
            float brakeInput,
            float speedMetersPerSecond,
            bool trafficLimited,
            bool passing,
            bool seekingEnergy)
        {
            if (!initialized)
                return false;

            bool wasActive =
                equipment.IsBoosterActive;

            debugHasBooster =
                equipment.HasBooster;

            debugBoostActive =
                equipment.IsBoosterActive;

            debugPassing =
                passing;

            debugTrafficLimited =
                trafficLimited;

            debugSeekingEnergy =
                seekingEnergy;

            debugEnergyPercent =
                participant.Vehicle.EnergyPool.MaxEnergy > 0f
                    ? participant.Vehicle.EnergyPool.CurrentEnergy /
                      participant.Vehicle.EnergyPool.MaxEnergy
                    : 0f;

            if (!equipment.HasBooster)
            {
                StopBoost("No Booster");
                return wasActive != equipment.IsBoosterActive;
            }

            if (!RaceIsActive())
            {
                StopBoost("Race Inactive");
                return wasActive != equipment.IsBoosterActive;
            }

            if (participant.Vehicle.IsDestroyed)
            {
                StopBoost("Vehicle Destroyed");
                return wasActive != equipment.IsBoosterActive;
            }

            if (seekingEnergy)
            {
                StopBoost("Seeking Energy");
                return wasActive != equipment.IsBoosterActive;
            }

            if (cooldownTimer > 0f)
            {
                cooldownTimer =
                    Mathf.Max(
                        0f,
                        cooldownTimer -
                        Time.fixedDeltaTime);
            }

            if (equipment.IsBoosterActive)
            {
                UpdateActiveBoost(
                    cornerSeverity,
                    brakeInput,
                    trafficLimited,
                    passing);
            }
            else
            {
                UpdateInactiveBoost(
                    cornerSeverity,
                    brakeInput,
                    speedMetersPerSecond,
                    trafficLimited,
                    passing);
            }

            debugBoostActive =
                equipment.IsBoosterActive;

            debugStraightTimer =
                straightTimer;

            debugCooldownTimer =
                cooldownTimer;

            return wasActive != equipment.IsBoosterActive;
        }

        private void UpdateActiveBoost(
            float cornerSeverity,
            float brakeInput,
            bool trafficLimited,
            bool passing)
        {
            if (debugEnergyPercent <=
                runtimeEnergyReserve)
            {
                StopBoost("Energy Reserve");
                return;
            }

            if (cornerSeverity >=
                stopCornerSeverity)
            {
                StopBoost("Corner Ahead");
                return;
            }

            if (brakeInput >=
                stopBrakeThreshold)
            {
                StopBoost("Braking");
                return;
            }

            if (trafficLimited &&
                !passing)
            {
                StopBoost("Blocked By Traffic");
                return;
            }

            debugDecision =
                passing
                    ? "Boosting / Passing"
                    : "Boosting";
        }

        private void UpdateInactiveBoost(
            float cornerSeverity,
            float brakeInput,
            float speedMetersPerSecond,
            bool trafficLimited,
            bool passing)
        {
            if (cooldownTimer > 0f)
            {
                straightTimer = 0f;
                debugDecision = "Cooldown";
                return;
            }

            if (debugEnergyPercent <
                runtimeRestartThreshold)
            {
                straightTimer = 0f;
                debugDecision = "Conserving Energy";
                return;
            }

            if (speedMetersPerSecond * 3.6f <
                minimumBoostSpeedKph)
            {
                straightTimer = 0f;
                debugDecision = "Below Boost Speed";
                return;
            }

            if (cornerSeverity >
                runtimeStartCornerSeverity)
            {
                straightTimer = 0f;
                debugDecision = "Corner Ahead";
                return;
            }

            if (brakeInput >
                maximumBrakeToStart)
            {
                straightTimer = 0f;
                debugDecision = "Braking";
                return;
            }

            if (trafficLimited &&
                !passing)
            {
                straightTimer = 0f;
                debugDecision = "Blocked By Traffic";
                return;
            }

            float requiredCommitTime =
                passing
                    ? runtimeStraightCommitTime *
                      passingCommitTimeMultiplier
                    : runtimeStraightCommitTime;

            straightTimer +=
                Time.fixedDeltaTime;

            debugDecision =
                passing
                    ? "Preparing Pass Boost"
                    : "Straight Confirmed";

            if (straightTimer <
                requiredCommitTime)
            {
                return;
            }

            bool activated =
                equipment.SetBoostActive(
                    true);

            straightTimer = 0f;

            debugDecision =
                activated
                    ? passing
                        ? "Boost Started / Passing"
                        : "Boost Started"
                    : "Boost Activation Failed";
        }

        private void StopBoost(
            string reason)
        {
            if (equipment != null &&
                equipment.IsBoosterActive)
            {
                equipment.SetBoostActive(
                    false);

                cooldownTimer =
                    boostCooldown;
            }

            straightTimer = 0f;
            debugDecision = reason;
            debugBoostActive = false;
        }

        public void StopBoost()
        {
            StopBoost("Stopped");
        }

        #endregion

        #region Helpers

        private bool RaceIsActive()
        {
            if (raceRuntime == null ||
                !raceRuntime.HasStarted)
            {
                return false;
            }

            if (raceRuntime.Director?.State == null)
                return false;

            return !raceRuntime
                .Director
                .State
                .IsFinished;
        }

        private float CalculateDeterministicValue(
            string id)
        {
            uint hash =
                2166136261u;

            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0;
                     i < id.Length;
                     i++)
                {
                    hash ^= id[i];
                    hash *= 16777619u;
                }
            }

            return
                (hash & 0x00FFFFFFu) /
                16777215f;
        }

        #endregion

        #region Unity

        private void OnDisable()
        {
            if (initialized)
                StopBoost();
        }

        #endregion
    }
}