using System;
using RaceFatal.Equipment;
using RaceFatal.Racing;
using UnityEngine;
using RaceFatal.Presentation.Racing;

namespace RaceFatal.Presentation.Vehicles
{
    [Serializable]
    public class AIBoostPlanner
    {
        #region Energy Strategy

        [Header("Energy Strategy")]
        [Range(0f, 1f)][SerializeField] private float aggressiveEnergyReserve = 0.12f;
        [Range(0f, 1f)][SerializeField] private float conservativeEnergyReserve = 0.22f;
        [Range(0f, 0.5f)][SerializeField] private float restartEnergyMargin = 0.08f;

        [Range(0f, 1f)][SerializeField] private float huntEnergyReserveMultiplier = 0.75f;
        [Range(0f, 1f)][SerializeField] private float passingEnergyReserveMultiplier = 0.5f;

        #endregion

        #region Racing Pressure

        [Header("Racing Pressure")]
        [Min(1f)][SerializeField] private float attackPriorityDistance = 55f;

        [Range(0f, 0.5f)][SerializeField] private float huntStartCornerBonus = 0.08f;
        [Range(0f, 0.5f)][SerializeField] private float passingStartCornerBonus = 0.16f;

        [Range(0f, 1f)][SerializeField] private float huntStopCornerSeverity = 0.34f;
        [Range(0f, 1f)][SerializeField] private float passingStopCornerSeverity = 0.42f;

        #endregion

        #region Driving Conditions

        [Header("Driving Conditions")]
        [Min(0f)][SerializeField] private float minimumBoostSpeedKph = 70f;

        [Range(0f, 1f)][SerializeField] private float conservativeStartCornerSeverity = 0.10f;
        [Range(0f, 1f)][SerializeField] private float aggressiveStartCornerSeverity = 0.20f;
        [Range(0f, 1f)][SerializeField] private float stopCornerSeverity = 0.28f;

        [Range(0f, 1f)][SerializeField] private float maximumBrakeToStart = 0.05f;
        [Range(0f, 1f)][SerializeField] private float stopBrakeThreshold = 0.15f;

        #endregion

        #region Commitment

        [Header("Boost Commitment")]
        [Min(0f)][SerializeField] private float aggressiveStraightCommitTime = 0.2f;
        [Min(0f)][SerializeField] private float conservativeStraightCommitTime = 0.7f;
        [Min(0f)][SerializeField] private float boostCooldown = 0.35f;

        [Range(0.05f, 1f)][SerializeField] private float huntingCommitTimeMultiplier = 0.45f;
        [Range(0.05f, 1f)][SerializeField] private float passingCommitTimeMultiplier = 0.2f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private bool debugHasBooster;
        [SerializeField] private bool debugBoostActive;
        [SerializeField] private bool debugHunting;
        [SerializeField] private bool debugPassing;
        [SerializeField] private bool debugAttackPriority;
        [SerializeField] private bool debugTrafficLimited;
        [SerializeField] private bool debugSeekingEnergy;

        [SerializeField] private float debugTargetDistance;
        [SerializeField] private float debugEnergyPercent;
        [SerializeField] private float debugRuntimeAggression;
        [SerializeField] private float debugRuntimeEnergyReserve;
        [SerializeField] private float debugEffectiveEnergyReserve;
        [SerializeField] private float debugRuntimeRestartThreshold;
        [SerializeField] private float debugRuntimeStartCornerSeverity;
        [SerializeField] private float debugEffectiveStartCornerSeverity;
        [SerializeField] private float debugRuntimeStraightCommitTime;

        [SerializeField] private float debugStraightTimer;
        [SerializeField] private float debugCooldownTimer;
        [SerializeField] private string debugDecision = "Not Initialized";

        #endregion

        #region Runtime

        private RaceParticipant participant;
        private RaceRuntimeController raceRuntime;
        private RaceEquipmentSystem equipment;
        private AIOvertakePlanner overtakePlanner;

        private float runtimeEnergyReserve;
        private float runtimeRestartThreshold;
        private float runtimeStartCornerSeverity;
        private float runtimeStraightCommitTime;

        private float straightTimer;
        private float cooldownTimer;

        private bool initialized;

        #endregion

        public bool IsInitialized => initialized;

        public bool IsBoosting =>
            equipment != null &&
            equipment.IsBoosterActive;

        public bool Initialize(
            RaceParticipant raceParticipant,
            RaceRuntimeController runtime,
            AIOvertakePlanner overtake,
            float aggression)
        {
            if (raceParticipant == null ||
                runtime == null ||
                overtake == null)
            {
                Debug.LogError(
                    $"{nameof(AIBoostPlanner)} is missing required runtime state.");

                return false;
            }

            if (raceParticipant.Vehicle?.EquipmentSystem == null ||
                raceParticipant.Vehicle.EnergyPool == null)
            {
                Debug.LogError(
                    $"{nameof(AIBoostPlanner)} requires a race vehicle with Energy and equipment.");

                return false;
            }

            participant = raceParticipant;
            raceRuntime = runtime;
            equipment = participant.Vehicle.EquipmentSystem;
            overtakePlanner = overtake;

            float runtimeAggression =
                Mathf.Clamp01(
                    aggression);

            runtimeEnergyReserve =
                Mathf.Lerp(
                    conservativeEnergyReserve,
                    aggressiveEnergyReserve,
                    runtimeAggression);

            runtimeRestartThreshold =
                Mathf.Clamp01(
                    runtimeEnergyReserve +
                    restartEnergyMargin);

            runtimeStartCornerSeverity =
                Mathf.Lerp(
                    conservativeStartCornerSeverity,
                    aggressiveStartCornerSeverity,
                    runtimeAggression);

            runtimeStraightCommitTime =
                Mathf.Lerp(
                    conservativeStraightCommitTime,
                    aggressiveStraightCommitTime,
                    runtimeAggression);

            straightTimer = 0f;
            cooldownTimer = 0f;

            debugRuntimeAggression = runtimeAggression;
            debugRuntimeEnergyReserve = runtimeEnergyReserve;
            debugRuntimeRestartThreshold = runtimeRestartThreshold;
            debugRuntimeStartCornerSeverity = runtimeStartCornerSeverity;
            debugRuntimeStraightCommitTime = runtimeStraightCommitTime;

            debugHasBooster = equipment.HasBooster;
            debugInitialized = true;
            initialized = true;

            return true;
        }

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

            bool hunting =
                overtakePlanner != null &&
                overtakePlanner.IsHunting;

            float targetDistance =
                overtakePlanner != null
                    ? overtakePlanner.HuntTargetDistance
                    : float.PositiveInfinity;

            bool attackPriority =
                passing ||
                (hunting &&
                 targetDistance <=
                 attackPriorityDistance);

            debugHasBooster = equipment.HasBooster;
            debugBoostActive = equipment.IsBoosterActive;
            debugHunting = hunting;
            debugPassing = passing;
            debugAttackPriority = attackPriority;
            debugTrafficLimited = trafficLimited;
            debugSeekingEnergy = seekingEnergy;

            debugTargetDistance =
                float.IsPositiveInfinity(
                    targetDistance)
                    ? 0f
                    : targetDistance;

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

            if (seekingEnergy &&
                !attackPriority)
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

            float effectiveReserve =
                GetEffectiveEnergyReserve(
                    hunting,
                    passing);

            float effectiveRestartThreshold =
                GetEffectiveRestartThreshold(
                    effectiveReserve,
                    hunting,
                    passing);

            float effectiveStartCornerSeverity =
                GetEffectiveStartCornerSeverity(
                    hunting,
                    passing);

            float effectiveStopCornerSeverity =
                GetEffectiveStopCornerSeverity(
                    hunting,
                    passing);

            debugEffectiveEnergyReserve =
                effectiveReserve;

            debugEffectiveStartCornerSeverity =
                effectiveStartCornerSeverity;

            if (equipment.IsBoosterActive)
            {
                UpdateActiveBoost(
                    cornerSeverity,
                    brakeInput,
                    trafficLimited,
                    hunting,
                    passing,
                    effectiveReserve,
                    effectiveStopCornerSeverity);
            }
            else
            {
                UpdateInactiveBoost(
                    cornerSeverity,
                    brakeInput,
                    speedMetersPerSecond,
                    trafficLimited,
                    hunting,
                    passing,
                    effectiveRestartThreshold,
                    effectiveStartCornerSeverity);
            }

            debugBoostActive =
                equipment.IsBoosterActive;

            debugStraightTimer =
                straightTimer;

            debugCooldownTimer =
                cooldownTimer;

            return wasActive !=
                equipment.IsBoosterActive;
        }

        private void UpdateActiveBoost(
            float cornerSeverity,
            float brakeInput,
            bool trafficLimited,
            bool hunting,
            bool passing,
            float effectiveReserve,
            float effectiveStopCornerSeverity)
        {
            if (debugEnergyPercent <=
                effectiveReserve)
            {
                StopBoost(
                    passing
                        ? "Pass Energy Reserve"
                        : hunting
                            ? "Hunt Energy Reserve"
                            : "Energy Reserve");

                return;
            }

            if (cornerSeverity >=
                effectiveStopCornerSeverity)
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
                !hunting &&
                !passing)
            {
                StopBoost(
                    "Blocked By Traffic");

                return;
            }

            debugDecision =
                passing
                    ? "Boosting / Pass Attack"
                    : hunting
                        ? "Boosting / Hunting"
                        : "Boosting";
        }

        private void UpdateInactiveBoost(
            float cornerSeverity,
            float brakeInput,
            float speedMetersPerSecond,
            bool trafficLimited,
            bool hunting,
            bool passing,
            float effectiveRestartThreshold,
            float effectiveStartCornerSeverity)
        {
            if (cooldownTimer > 0f)
            {
                straightTimer = 0f;
                debugDecision = "Cooldown";
                return;
            }

            if (debugEnergyPercent <
                effectiveRestartThreshold)
            {
                straightTimer = 0f;

                debugDecision =
                    passing
                        ? "Pass / Conserving Energy"
                        : hunting
                            ? "Hunt / Conserving Energy"
                            : "Conserving Energy";

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
                effectiveStartCornerSeverity)
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
                !hunting &&
                !passing)
            {
                straightTimer = 0f;
                debugDecision = "Blocked By Traffic";
                return;
            }

            float requiredCommitTime =
                runtimeStraightCommitTime;

            if (passing)
            {
                requiredCommitTime *=
                    passingCommitTimeMultiplier;
            }
            else if (hunting)
            {
                requiredCommitTime *=
                    huntingCommitTimeMultiplier;
            }

            straightTimer +=
                Time.fixedDeltaTime;

            debugDecision =
                passing
                    ? "Preparing Pass Boost"
                    : hunting
                        ? "Preparing Hunt Boost"
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

            if (!activated)
            {
                debugDecision =
                    "Boost Activation Failed";

                return;
            }

            debugDecision =
                passing
                    ? "Boost Started / Passing"
                    : hunting
                        ? "Boost Started / Hunting"
                        : "Boost Started";
        }

        private float GetEffectiveEnergyReserve(
            bool hunting,
            bool passing)
        {
            if (passing)
            {
                return Mathf.Clamp01(
                    runtimeEnergyReserve *
                    passingEnergyReserveMultiplier);
            }

            if (hunting)
            {
                return Mathf.Clamp01(
                    runtimeEnergyReserve *
                    huntEnergyReserveMultiplier);
            }

            return runtimeEnergyReserve;
        }

        private float GetEffectiveRestartThreshold(
            float effectiveReserve,
            bool hunting,
            bool passing)
        {
            float marginMultiplier = 1f;

            if (passing)
                marginMultiplier = 0.25f;
            else if (hunting)
                marginMultiplier = 0.5f;

            return Mathf.Clamp01(
                effectiveReserve +
                restartEnergyMargin *
                marginMultiplier);
        }

        private float GetEffectiveStartCornerSeverity(
            bool hunting,
            bool passing)
        {
            float value =
                runtimeStartCornerSeverity;

            if (passing)
                value += passingStartCornerBonus;
            else if (hunting)
                value += huntStartCornerBonus;

            return Mathf.Clamp01(value);
        }

        private float GetEffectiveStopCornerSeverity(
            bool hunting,
            bool passing)
        {
            if (passing)
                return passingStopCornerSeverity;

            if (hunting)
                return huntStopCornerSeverity;

            return stopCornerSeverity;
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
    }
}