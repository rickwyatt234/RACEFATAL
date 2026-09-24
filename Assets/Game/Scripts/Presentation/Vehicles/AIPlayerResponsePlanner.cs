using System;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    public enum AIPlayerResponseMode
    {
        Normal,
        Conserve,
        MatchPlayer,
        PursuePlayer,
        Recover
    }

    [Serializable]
    public class AIPlayerResponsePlanner
    {
        #region Response Strength

        [Header("Player Response Strength")]

        [Tooltip(
            "Response strength for the racer immediately behind the player.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float primaryPursuerResponseStrength = 1f;

        [Tooltip(
            "Response strength for the second racer behind the player.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float supportPursuerResponseStrength = 0.78f;

        [Tooltip(
            "Response strength for the third racer behind the player.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float hunterResponseStrength = 0.55f;

        [Tooltip(
            "Small background awareness for the rest of the field.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float fieldResponseStrength = 0.20f;

        #endregion

        #region Player Pace

        [Header("Player Pace Response")]

        [Tooltip(
            "Inside this gap the AI considers itself close enough " +
            "to match the player's pace rather than chase.")]
        [Min(1f)]
        [SerializeField]
        private float matchPlayerDistance = 30f;

        [Tooltip(
            "Player lead where pursuit behavior begins even if " +
            "the gap is not currently increasing rapidly.")]
        [Min(1f)]
        [SerializeField]
        private float pursuitStartDistance = 35f;

        [Tooltip(
            "Player lead treated as a maximum pursuit emergency.")]
        [Min(1f)]
        [SerializeField]
        private float fullPursuitDistance = 90f;

        [Tooltip(
            "Rate in meters per second at which an increasing player lead " +
            "begins triggering pursuit.")]
        [Min(0f)]
        [SerializeField]
        private float escapingGapRate = 2f;

        [Tooltip(
            "Rate in meters per second at which the player's escape is " +
            "treated as severe.")]
        [Min(0.1f)]
        [SerializeField]
        private float fullEscapeGapRate = 8f;

        [Tooltip(
            "Extra pursuit urgency when the player is actively boosting.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float playerBoostUrgencyBonus = 0.35f;

        [Tooltip(
            "How quickly noisy gap-rate measurements are smoothed.")]
        [Min(0.1f)]
        [SerializeField]
        private float gapTrendResponse = 3f;

        #endregion

        #region Player Boost Reaction

        [Header("Player Boost Reaction")]

        [Tooltip(
            "Fastest possible reaction to the player activating boost.")]
        [Min(0f)]
        [SerializeField]
        private float primaryBoostReactionDelay = 0.20f;

        [Tooltip(
            "Slowest reaction used by distant field racers.")]
        [Min(0f)]
        [SerializeField]
        private float fieldBoostReactionDelay = 0.80f;

        #endregion

        #region Resource Recovery

        [Header("Resource Recovery")]

        [Tooltip(
            "Energy level where emergency recovery mode begins.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float recoverEnterEnergyPercent = 0.30f;

        [Tooltip(
            "Energy level required before leaving recovery mode.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float recoverExitEnergyPercent = 0.58f;

        [Tooltip(
            "Actual shield percentage where recovery mode begins. " +
            "This uses Current / BaseMaximum, not Current / energy-limited Maximum.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float recoverEnterShieldPercent = 0.25f;

        [Tooltip(
            "Actual shield percentage required before leaving recovery mode.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float recoverExitShieldPercent = 0.42f;

        #endregion

        #region Strategic Energy Reserves

        [Header("Strategic Boost Reserves")]

        [Range(0f, 1f)]
        [SerializeField]
        private float conserveEnergyReserve = 0.65f;

        [Range(0f, 1f)]
        [SerializeField]
        private float matchPlayerEnergyReserve = 0.55f;

        [Range(0f, 1f)]
        [SerializeField]
        private float pursuePlayerEnergyReserve = 0.40f;

        [Range(0f, 1f)]
        [SerializeField]
        private float recoveryEnergyReserve = 0.75f;

        #endregion

        #region Energy Strip Strategy

        [Header("Energy Strip Strategy")]

        [Range(0f, 1f)]
        [SerializeField]
        private float conserveEnergySeekThreshold = 0.62f;

        [Range(0f, 1f)]
        [SerializeField]
        private float matchPlayerEnergySeekThreshold = 0.48f;

        [Range(0f, 1f)]
        [SerializeField]
        private float pursuePlayerEnergySeekThreshold = 0.28f;

        [Range(0f, 1f)]
        [SerializeField]
        private float recoveryEnergySeekThreshold = 0.90f;

        #endregion

        #region Combat Response

        [Header("Player Combat Response")]

        [Tooltip(
            "Background player target bonus even outside active pursuit.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float normalPlayerTargetBonus = 0.10f;

        [Tooltip(
            "Player target bonus while matching their pace.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float matchPlayerTargetBonus = 0.28f;

        [Tooltip(
            "Maximum player target bonus during active pursuit.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float pursuitPlayerTargetBonus = 0.60f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]

        [SerializeField]
        private AIPlayerResponseMode debugMode;

        [SerializeField]
        private float debugResponseStrength;

        [SerializeField]
        private int debugPlayerPosition;

        [SerializeField]
        private int debugSelfPosition;

        [SerializeField]
        private int debugPositionGap;

        [SerializeField]
        private float debugGapToPlayerMeters;

        [SerializeField]
        private float debugGapRateMetersPerSecond;

        [SerializeField]
        private bool debugPlayerBoosting;

        [SerializeField]
        private bool debugPlayerBoostResponseActive;

        [SerializeField]
        private float debugPlayerSpeedKph;

        [SerializeField]
        private float debugEnergyPercent;

        [SerializeField]
        private float debugAbsoluteShieldPercent;

        [SerializeField]
        private float debugPursuitUrgency;

        [SerializeField]
        private float debugResourceUrgency;

        [SerializeField]
        private float debugStrategicEnergyReserve;

        [SerializeField]
        private float debugEnergySeekThreshold;

        [SerializeField]
        private float debugPlayerTargetBonus;

        [SerializeField]
        private bool debugAllowBoost;

        #endregion

        #region Runtime

        private RaceParticipant participant;
        private RaceRuntimeController raceRuntime;

        private float previousGap;
        private float smoothedGapRate;

        private float playerBoostTimer;

        private bool hasPreviousGap;
        private bool recoveryLatched;

        #endregion

        #region Public State

        public AIPlayerResponseMode Mode
        {
            get;
            private set;
        }

        public float ResponseStrength
        {
            get;
            private set;
        }

        public float PursuitUrgency
        {
            get;
            private set;
        }

        public float ResourceUrgency
        {
            get;
            private set;
        }

        public float GapRateMetersPerSecond
        {
            get;
            private set;
        }

        public float EnergyPercent
        {
            get;
            private set;
        }

        public float AbsoluteShieldPercent
        {
            get;
            private set;
        }

        public bool PlayerBoosting
        {
            get;
            private set;
        }

        public bool PlayerBoostResponseActive
        {
            get;
            private set;
        }

        public float StrategicEnergyReserve
        {
            get;
            private set;
        }

        public float EnergySeekThreshold
        {
            get;
            private set;
        }

        public float BoostCommitTimeMultiplier
        {
            get;
            private set;
        } = 1f;

        public float PlayerTargetBonus
        {
            get;
            private set;
        }

        public float CombatUrgency
        {
            get;
            private set;
        }

        public bool AllowBoost
        {
            get;
            private set;
        } = true;

        public bool PreferPlayerTarget
        {
            get;
            private set;
        }

        public bool SuppressOptionalEnergyRecovery
        {
            get;
            private set;
        }

        #endregion

        #region Initialization

        public bool Initialize(
            RaceParticipant raceParticipant,
            RaceRuntimeController runtime)
        {
            if (raceParticipant == null ||
                runtime == null ||
                runtime.Director?.State == null ||
                raceParticipant.Vehicle?.EnergyPool == null)
            {
                Debug.LogError(
                    $"{nameof(AIPlayerResponsePlanner)} is missing required runtime state.");

                return false;
            }

            participant =
                raceParticipant;

            raceRuntime =
                runtime;

            ResetRuntimeState();

            return true;
        }

        #endregion

        #region Planning

        public void UpdatePlan(
            float gapToPlayerMeters,
            AIRacePressureRole pressureRole,
            float deltaTime)
        {
            if (participant == null ||
                raceRuntime?.Director?.State == null ||
                participant.Vehicle == null ||
                participant.Vehicle.EnergyPool == null)
            {
                ResetRuntimeState();
                return;
            }

            RaceState state =
                raceRuntime.Director.State;

            RaceParticipant player =
                state.FindParticipant(
                    raceRuntime.PlayerRacerId);

            if (player == null ||
                player.Status !=
                    RaceParticipantStatus.Racing ||
                participant.Status !=
                    RaceParticipantStatus.Racing)
            {
                ResetRuntimeState();
                return;
            }

            int playerPosition =
                state.GetCurrentPosition(
                    player.RacerId);

            int selfPosition =
                state.GetCurrentPosition(
                    participant.RacerId);

            int positionGap =
                selfPosition -
                playerPosition;

            ResponseStrength =
                ResolveResponseStrength(
                    pressureRole,
                    positionGap);

            UpdateGapTrend(
                gapToPlayerMeters,
                deltaTime);

            UpdatePlayerState(
                player,
                deltaTime);

            UpdateResourceState();

            UpdateRecoveryLatch();

            float rawPursuitSignal =
                CalculateRawPursuitSignal(
                    gapToPlayerMeters);

            PursuitUrgency =
                Mathf.Clamp01(
                    rawPursuitSignal *
                    ResponseStrength);

            ResolveMode(
                gapToPlayerMeters,
                positionGap,
                rawPursuitSignal);

            ResolveStrategicOutputs();

            debugMode =
                Mode;

            debugResponseStrength =
                ResponseStrength;

            debugPlayerPosition =
                playerPosition;

            debugSelfPosition =
                selfPosition;

            debugPositionGap =
                positionGap;

            debugGapToPlayerMeters =
                gapToPlayerMeters;

            debugGapRateMetersPerSecond =
                GapRateMetersPerSecond;

            debugPlayerBoosting =
                PlayerBoosting;

            debugPlayerBoostResponseActive =
                PlayerBoostResponseActive;

            debugEnergyPercent =
                EnergyPercent;

            debugAbsoluteShieldPercent =
                AbsoluteShieldPercent;

            debugPursuitUrgency =
                PursuitUrgency;

            debugResourceUrgency =
                ResourceUrgency;

            debugStrategicEnergyReserve =
                StrategicEnergyReserve;

            debugEnergySeekThreshold =
                EnergySeekThreshold;

            debugPlayerTargetBonus =
                PlayerTargetBonus;

            debugAllowBoost =
                AllowBoost;
        }

        #endregion

        #region Response Strength

        private float ResolveResponseStrength(
            AIRacePressureRole pressureRole,
            int positionGap)
        {
            if (pressureRole ==
                AIRacePressureRole.Pursuer)
            {
                return primaryPursuerResponseStrength;
            }

            if (pressureRole ==
                AIRacePressureRole.PursuerSupport)
            {
                return supportPursuerResponseStrength;
            }

            if (positionGap == 3)
            {
                return hunterResponseStrength;
            }

            return fieldResponseStrength;
        }

        #endregion

        #region Gap Trend

        private void UpdateGapTrend(
            float currentGap,
            float deltaTime)
        {
            if (!hasPreviousGap ||
                deltaTime <= 0f)
            {
                previousGap =
                    currentGap;

                smoothedGapRate =
                    0f;

                GapRateMetersPerSecond =
                    0f;

                hasPreviousGap =
                    true;

                return;
            }

            float rawRate =
                (currentGap -
                 previousGap) /
                deltaTime;

            float response =
                1f -
                Mathf.Exp(
                    -gapTrendResponse *
                    deltaTime);

            smoothedGapRate =
                Mathf.Lerp(
                    smoothedGapRate,
                    rawRate,
                    response);

            GapRateMetersPerSecond =
                smoothedGapRate;

            previousGap =
                currentGap;
        }

        #endregion

        #region Player Observation

        private void UpdatePlayerState(
            RaceParticipant player,
            float deltaTime)
        {
            PlayerBoosting =
                player.Vehicle?
                    .EquipmentSystem?
                    .IsBoosterActive ??
                false;

            float effectiveReactionDelay =
                Mathf.Lerp(
                    fieldBoostReactionDelay,
                    primaryBoostReactionDelay,
                    ResponseStrength);

            if (PlayerBoosting)
            {
                playerBoostTimer +=
                    Mathf.Max(
                        0f,
                        deltaTime);
            }
            else
            {
                playerBoostTimer =
                    0f;
            }

            PlayerBoostResponseActive =
                PlayerBoosting &&
                playerBoostTimer >=
                    effectiveReactionDelay;

            debugPlayerSpeedKph =
                0f;

            if (raceRuntime.TryGetRacerView(
                    player.RacerId,
                    out RacerViewController playerView) &&
                playerView != null)
            {
                BikeMotor playerMotor =
                    playerView.GetComponent<
                        BikeMotor>();

                if (playerMotor != null)
                {
                    debugPlayerSpeedKph =
                        playerMotor
                            .SpeedMetersPerSecond *
                        3.6f;
                }
            }
        }

        #endregion

        #region Resources

        private void UpdateResourceState()
        {
            float maxEnergy =
                participant.Vehicle
                    .EnergyPool
                    .MaxEnergy;

            EnergyPercent =
                maxEnergy > 0f
                    ? Mathf.Clamp01(
                        participant.Vehicle
                            .EnergyPool
                            .CurrentEnergy /
                        maxEnergy)
                    : 0f;

            RaceShieldState shield =
                participant.Vehicle
                    .EquipmentSystem?
                    .Shield;

            if (shield == null ||
                shield.BaseMaximum <= 0f)
            {
                AbsoluteShieldPercent =
                    1f;
            }
            else
            {
                AbsoluteShieldPercent =
                    Mathf.Clamp01(
                        shield.Current /
                        shield.BaseMaximum);
            }

            float energyUrgency =
                1f -
                Mathf.InverseLerp(
                    recoverEnterEnergyPercent,
                    Mathf.Max(
                        recoverEnterEnergyPercent + 0.01f,
                        recoverExitEnergyPercent),
                    EnergyPercent);

            float shieldUrgency =
                1f -
                Mathf.InverseLerp(
                    recoverEnterShieldPercent,
                    Mathf.Max(
                        recoverEnterShieldPercent + 0.01f,
                        recoverExitShieldPercent),
                    AbsoluteShieldPercent);

            ResourceUrgency =
                Mathf.Clamp01(
                    Mathf.Max(
                        energyUrgency,
                        shieldUrgency));
        }

        private void UpdateRecoveryLatch()
        {
            if (!recoveryLatched)
            {
                if (EnergyPercent <=
                        recoverEnterEnergyPercent ||
                    AbsoluteShieldPercent <=
                        recoverEnterShieldPercent)
                {
                    recoveryLatched =
                        true;
                }

                return;
            }

            if (EnergyPercent >=
                    recoverExitEnergyPercent &&
                AbsoluteShieldPercent >=
                    recoverExitShieldPercent)
            {
                recoveryLatched =
                    false;
            }
        }

        #endregion

        #region Pursuit

        private float CalculateRawPursuitSignal(
            float gapToPlayerMeters)
        {
            if (gapToPlayerMeters <= 0f)
                return 0f;

            float gapUrgency =
                Mathf.InverseLerp(
                    pursuitStartDistance,
                    Mathf.Max(
                        pursuitStartDistance + 1f,
                        fullPursuitDistance),
                    gapToPlayerMeters);

            float escapeUrgency =
                Mathf.InverseLerp(
                    escapingGapRate,
                    Mathf.Max(
                        escapingGapRate + 0.1f,
                        fullEscapeGapRate),
                    Mathf.Max(
                        0f,
                        GapRateMetersPerSecond));

            float urgency =
                Mathf.Max(
                    gapUrgency,
                    escapeUrgency);

            if (PlayerBoostResponseActive)
            {
                urgency +=
                    playerBoostUrgencyBonus;
            }

            return Mathf.Clamp01(
                urgency);
        }

        #endregion

        #region Mode Selection

        private void ResolveMode(
            float gapToPlayerMeters,
            int positionGap,
            float rawPursuitSignal)
        {
            if (recoveryLatched)
            {
                Mode =
                    AIPlayerResponseMode.Recover;

                return;
            }

            bool playerAhead =
                gapToPlayerMeters > 0f;

            bool playerEscaping =
                GapRateMetersPerSecond >=
                escapingGapRate;

            bool activePursuitTrigger =
                playerAhead &&
                (
                    gapToPlayerMeters >=
                        pursuitStartDistance ||
                    playerEscaping ||
                    PlayerBoostResponseActive
                );

            if (activePursuitTrigger &&
                rawPursuitSignal > 0.05f)
            {
                Mode =
                    AIPlayerResponseMode.PursuePlayer;

                return;
            }

            if (Mathf.Abs(
                    gapToPlayerMeters) <=
                    matchPlayerDistance ||
                (playerAhead &&
                 positionGap > 0 &&
                 positionGap <= 3))
            {
                Mode =
                    AIPlayerResponseMode.MatchPlayer;

                return;
            }

            Mode =
                AIPlayerResponseMode.Conserve;
        }

        #endregion

        #region Strategic Outputs

        private void ResolveStrategicOutputs()
        {
            AllowBoost =
                true;

            PreferPlayerTarget =
                false;

            SuppressOptionalEnergyRecovery =
                false;

            BoostCommitTimeMultiplier =
                1f;

            CombatUrgency =
                0.2f *
                ResponseStrength;

            PlayerTargetBonus =
                normalPlayerTargetBonus *
                ResponseStrength;

            switch (Mode)
            {
                case AIPlayerResponseMode.Recover:

                    AllowBoost =
                        false;

                    StrategicEnergyReserve =
                        recoveryEnergyReserve;

                    EnergySeekThreshold =
                        EnergyPercent <
                        recoverExitEnergyPercent
                            ? recoveryEnergySeekThreshold
                            : conserveEnergySeekThreshold;

                    BoostCommitTimeMultiplier =
                        2f;

                    CombatUrgency =
                        0.10f *
                        ResponseStrength;

                    PlayerTargetBonus =
                        normalPlayerTargetBonus *
                        0.5f *
                        ResponseStrength;

                    PreferPlayerTarget =
                        false;

                    break;

                case AIPlayerResponseMode.PursuePlayer:

                    float localPursuit =
                        ResponseStrength > 0.001f
                            ? Mathf.Clamp01(
                                PursuitUrgency /
                                ResponseStrength)
                            : 0f;

                    StrategicEnergyReserve =
                        Mathf.Lerp(
                            matchPlayerEnergyReserve,
                            pursuePlayerEnergyReserve,
                            localPursuit);

                    EnergySeekThreshold =
                        pursuePlayerEnergySeekThreshold;

                    BoostCommitTimeMultiplier =
                        Mathf.Lerp(
                            0.75f,
                            0.25f,
                            localPursuit);

                    PlayerTargetBonus =
                        Mathf.Lerp(
                            matchPlayerTargetBonus,
                            pursuitPlayerTargetBonus,
                            localPursuit) *
                        ResponseStrength;

                    CombatUrgency =
                        Mathf.Lerp(
                            0.50f,
                            1f,
                            localPursuit) *
                        ResponseStrength;

                    PreferPlayerTarget =
                        ResponseStrength >=
                        fieldResponseStrength;

                    SuppressOptionalEnergyRecovery =
                        EnergyPercent >
                        pursuePlayerEnergySeekThreshold;

                    break;

                case AIPlayerResponseMode.MatchPlayer:

                    StrategicEnergyReserve =
                        matchPlayerEnergyReserve;

                    EnergySeekThreshold =
                        matchPlayerEnergySeekThreshold;

                    BoostCommitTimeMultiplier =
                        0.85f;

                    PlayerTargetBonus =
                        matchPlayerTargetBonus *
                        ResponseStrength;

                    CombatUrgency =
                        0.50f *
                        ResponseStrength;

                    PreferPlayerTarget =
                        ResponseStrength >=
                        hunterResponseStrength;

                    break;

                case AIPlayerResponseMode.Conserve:
                case AIPlayerResponseMode.Normal:
                default:

                    StrategicEnergyReserve =
                        conserveEnergyReserve;

                    EnergySeekThreshold =
                        conserveEnergySeekThreshold;

                    BoostCommitTimeMultiplier =
                        1.35f;

                    PlayerTargetBonus =
                        normalPlayerTargetBonus *
                        ResponseStrength;

                    CombatUrgency =
                        0.20f *
                        ResponseStrength;

                    PreferPlayerTarget =
                        false;

                    break;
            }
        }

        #endregion

        #region Reset

        private void ResetRuntimeState()
        {
            Mode =
                AIPlayerResponseMode.Normal;

            ResponseStrength =
                0f;

            PursuitUrgency =
                0f;

            ResourceUrgency =
                0f;

            GapRateMetersPerSecond =
                0f;

            EnergyPercent =
                1f;

            AbsoluteShieldPercent =
                1f;

            PlayerBoosting =
                false;

            PlayerBoostResponseActive =
                false;

            StrategicEnergyReserve =
                conserveEnergyReserve;

            EnergySeekThreshold =
                conserveEnergySeekThreshold;

            BoostCommitTimeMultiplier =
                1f;

            PlayerTargetBonus =
                0f;

            CombatUrgency =
                0f;

            AllowBoost =
                true;

            PreferPlayerTarget =
                false;

            SuppressOptionalEnergyRecovery =
                false;

            previousGap =
                0f;

            smoothedGapRate =
                0f;

            playerBoostTimer =
                0f;

            hasPreviousGap =
                false;

            recoveryLatched =
                false;

            debugMode =
                Mode;
        }

        #endregion
    }
}