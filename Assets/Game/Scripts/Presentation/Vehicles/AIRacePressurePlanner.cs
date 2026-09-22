using System;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    public enum AIRacePressureRole
    {
        Normal,
        Pursuer,
        PursuerSupport
    }

    [Serializable]
    public class AIRacePressurePlanner
    {
        #region Pressure Envelope

        [Header("Pressure Envelope")]

        [Tooltip(
            "Player lead where catch-up pressure begins. " +
            "Inside this distance the AI receives no pursuit assistance.")]
        [Min(0f)]
        [SerializeField]
        private float pressureStartDistance = 10f;

        [Tooltip(
            "Player lead where pursuit pressure reaches full strength.")]
        [Min(1f)]
        [SerializeField]
        private float fullPressureDistance = 70f;

        [Tooltip(
            "Maximum player lead considered by the pressure calculation.")]
        [Min(1f)]
        [SerializeField]
        private float maximumPressureDistance = 180f;

        #endregion

        #region Pursuit Roles

        [Header("Pursuit Roles")]

        [Tooltip(
            "Pressure strength for the racer immediately behind the player.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float pursuerPressureScale = 1f;

        [Tooltip(
            "Pressure strength for the second racer behind the player.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float supportPressureScale = 0.7f;

        [Tooltip(
            "Very small background pressure available to the rest of the field.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float fieldPressureScale = 0.15f;

        #endregion

        #region Pursuer Assistance

        [Header("Primary Pursuer Assistance")]

        [Tooltip(
            "Maximum top-speed multiplier available to the primary pursuer.")]
        [Range(1f, 1.2f)]
        [SerializeField]
        private float maximumPursuerSpeedMultiplier = 1.08f;

        [Tooltip(
            "Maximum acceleration multiplier available to the primary pursuer.")]
        [Range(1f, 1.3f)]
        [SerializeField]
        private float maximumPursuerAccelerationMultiplier = 1.15f;

        [Tooltip(
            "Maximum cornering capability multiplier available to the primary pursuer.")]
        [Range(1f, 1.3f)]
        [SerializeField]
        private float maximumPursuerCorneringMultiplier = 1.12f;

        [Tooltip(
            "Maximum braking capability multiplier available to the primary pursuer.")]
        [Range(1f, 1.3f)]
        [SerializeField]
        private float maximumPursuerBrakingMultiplier = 1.10f;

        #endregion

        #region Support Assistance

        [Header("Pursuer Support Assistance")]

        [Tooltip(
            "Maximum top-speed multiplier available to the support pursuer.")]
        [Range(1f, 1.2f)]
        [SerializeField]
        private float maximumSupportSpeedMultiplier = 1.04f;

        [Tooltip(
            "Maximum acceleration multiplier available to the support pursuer.")]
        [Range(1f, 1.3f)]
        [SerializeField]
        private float maximumSupportAccelerationMultiplier = 1.08f;

        [Tooltip(
            "Maximum cornering capability multiplier available to the support pursuer.")]
        [Range(1f, 1.3f)]
        [SerializeField]
        private float maximumSupportCorneringMultiplier = 1.06f;

        [Tooltip(
            "Maximum braking capability multiplier available to the support pursuer.")]
        [Range(1f, 1.3f)]
        [SerializeField]
        private float maximumSupportBrakingMultiplier = 1.05f;

        #endregion

        #region AI Leader Relaxation

        [Header("AI Leader Relaxation")]

        [Tooltip(
            "If an AI racer has a very large lead over the player, " +
            "its maximum pace can begin relaxing slightly.")]
        [Min(0f)]
        [SerializeField]
        private float leaderRelaxationStartDistance = 120f;

        [Tooltip(
            "Maximum top-speed reduction for an AI racer far ahead of the player.")]
        [Range(0f, 0.05f)]
        [SerializeField]
        private float maximumLeaderSpeedReduction = 0.01f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]

        [SerializeField]
        private AIRacePressureRole debugRole;

        [SerializeField]
        private int debugPlayerPosition;

        [SerializeField]
        private int debugSelfPosition;

        [SerializeField]
        private float debugGapToPlayerMeters;

        [SerializeField]
        private float debugPressure;

        [SerializeField]
        private float debugSpeedMultiplier = 1f;

        [SerializeField]
        private float debugAccelerationMultiplier = 1f;

        [SerializeField]
        private float debugCorneringMultiplier = 1f;

        [SerializeField]
        private float debugBrakingMultiplier = 1f;

        #endregion

        #region Runtime

        private RaceParticipant participant;
        private RaceRuntimeController raceRuntime;
        private TrackProgressPath progressPath;

        #endregion

        #region Public State

        public AIRacePressureRole Role
        {
            get;
            private set;
        }

        public float Pressure
        {
            get;
            private set;
        }

        public float GapToPlayerMeters
        {
            get;
            private set;
        }

        public float SpeedMultiplier
        {
            get;
            private set;
        } = 1f;

        public float AccelerationMultiplier
        {
            get;
            private set;
        } = 1f;

        public float CorneringMultiplier
        {
            get;
            private set;
        } = 1f;

        public float BrakingMultiplier
        {
            get;
            private set;
        } = 1f;

        public bool IsPriorityPursuit =>
            Role == AIRacePressureRole.Pursuer ||
            Role == AIRacePressureRole.PursuerSupport;

        #endregion

        #region Initialization

        public bool Initialize(
            RaceParticipant raceParticipant,
            RaceRuntimeController runtime,
            TrackProgressPath path)
        {
            if (raceParticipant == null ||
                runtime == null ||
                runtime.Director?.State == null ||
                path == null)
            {
                Debug.LogError(
                    $"{nameof(AIRacePressurePlanner)} is missing required runtime state.");

                return false;
            }

            participant =
                raceParticipant;

            raceRuntime =
                runtime;

            progressPath =
                path;

            ResetRuntimeState();

            return true;
        }

        #endregion

        #region Planning

        public void UpdatePlan()
        {
            if (participant == null ||
                raceRuntime?.Director?.State == null ||
                progressPath == null ||
                progressPath.TotalLength <= 0f)
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

            float playerDistance =
                GetRaceDistance(
                    player);

            float selfDistance =
                GetRaceDistance(
                    participant);

            GapToPlayerMeters =
                playerDistance -
                selfDistance;

            Role =
                DetermineRole(
                    playerPosition,
                    selfPosition,
                    GapToPlayerMeters);

            Pressure =
                CalculatePressure(
                    GapToPlayerMeters,
                    Role);

            CalculateRuntimeMultipliers(
                GapToPlayerMeters,
                Role,
                Pressure);

            UpdateDebug(
                playerPosition,
                selfPosition);
        }

        private AIRacePressureRole DetermineRole(
            int playerPosition,
            int selfPosition,
            float gapToPlayer)
        {
            /*
             * Only racers physically behind the player are
             * eligible for Pursuer / Support roles.
             */
            if (gapToPlayer <= 0f ||
                playerPosition <= 0 ||
                selfPosition <= 0)
            {
                return AIRacePressureRole.Normal;
            }

            if (selfPosition ==
                playerPosition + 1)
            {
                return AIRacePressureRole.Pursuer;
            }

            if (selfPosition ==
                playerPosition + 2)
            {
                return AIRacePressureRole.PursuerSupport;
            }

            return AIRacePressureRole.Normal;
        }

        private float CalculatePressure(
            float gapToPlayer,
            AIRacePressureRole role)
        {
            if (gapToPlayer <=
                pressureStartDistance)
            {
                return 0f;
            }

            float clampedGap =
                Mathf.Min(
                    gapToPlayer,
                    maximumPressureDistance);

            float normalizedPressure =
                Mathf.InverseLerp(
                    pressureStartDistance,
                    Mathf.Max(
                        pressureStartDistance + 1f,
                        fullPressureDistance),
                    clampedGap);

            float roleScale;

            switch (role)
            {
                case AIRacePressureRole.Pursuer:
                    roleScale =
                        pursuerPressureScale;
                    break;

                case AIRacePressureRole.PursuerSupport:
                    roleScale =
                        supportPressureScale;
                    break;

                default:
                    roleScale =
                        fieldPressureScale;
                    break;
            }

            return Mathf.Clamp01(
                normalizedPressure *
                roleScale);
        }

        private void CalculateRuntimeMultipliers(
            float gapToPlayer,
            AIRacePressureRole role,
            float pressure)
        {
            SpeedMultiplier = 1f;
            AccelerationMultiplier = 1f;
            CorneringMultiplier = 1f;
            BrakingMultiplier = 1f;

            /*
             * Racer is behind the player.
             */
            if (gapToPlayer > 0f)
            {
                switch (role)
                {
                    case AIRacePressureRole.Pursuer:

                        SpeedMultiplier =
                            Mathf.Lerp(
                                1f,
                                maximumPursuerSpeedMultiplier,
                                pressure);

                        AccelerationMultiplier =
                            Mathf.Lerp(
                                1f,
                                maximumPursuerAccelerationMultiplier,
                                pressure);

                        CorneringMultiplier =
                            Mathf.Lerp(
                                1f,
                                maximumPursuerCorneringMultiplier,
                                pressure);

                        BrakingMultiplier =
                            Mathf.Lerp(
                                1f,
                                maximumPursuerBrakingMultiplier,
                                pressure);

                        break;

                    case AIRacePressureRole.PursuerSupport:

                        SpeedMultiplier =
                            Mathf.Lerp(
                                1f,
                                maximumSupportSpeedMultiplier,
                                pressure);

                        AccelerationMultiplier =
                            Mathf.Lerp(
                                1f,
                                maximumSupportAccelerationMultiplier,
                                pressure);

                        CorneringMultiplier =
                            Mathf.Lerp(
                                1f,
                                maximumSupportCorneringMultiplier,
                                pressure);

                        BrakingMultiplier =
                            Mathf.Lerp(
                                1f,
                                maximumSupportBrakingMultiplier,
                                pressure);

                        break;
                }

                return;
            }

            /*
             * Racer is ahead of the player.
             *
             * No cornering, braking or acceleration penalty is
             * applied. Only a very small maximum-speed relaxation
             * can occur if the AI has escaped by a huge distance.
             */
            float leadOverPlayer =
                -gapToPlayer;

            if (leadOverPlayer <=
                leaderRelaxationStartDistance)
            {
                return;
            }

            float relaxation =
                Mathf.InverseLerp(
                    leaderRelaxationStartDistance,
                    Mathf.Max(
                        leaderRelaxationStartDistance + 1f,
                        maximumPressureDistance),
                    leadOverPlayer);

            SpeedMultiplier =
                1f -
                maximumLeaderSpeedReduction *
                relaxation;
        }

        #endregion

        #region Race Distance

        private float GetRaceDistance(
            RaceParticipant racer)
        {
            if (racer == null ||
                progressPath == null)
            {
                return 0f;
            }

            return
                (racer.CompletedLaps +
                 racer.CourseProgress) *
                progressPath.TotalLength;
        }

        #endregion

        #region Debug

        private void UpdateDebug(
            int playerPosition,
            int selfPosition)
        {
            debugRole =
                Role;

            debugPlayerPosition =
                playerPosition;

            debugSelfPosition =
                selfPosition;

            debugGapToPlayerMeters =
                GapToPlayerMeters;

            debugPressure =
                Pressure;

            debugSpeedMultiplier =
                SpeedMultiplier;

            debugAccelerationMultiplier =
                AccelerationMultiplier;

            debugCorneringMultiplier =
                CorneringMultiplier;

            debugBrakingMultiplier =
                BrakingMultiplier;
        }

        #endregion

        #region Reset

        private void ResetRuntimeState()
        {
            Role =
                AIRacePressureRole.Normal;

            Pressure =
                0f;

            GapToPlayerMeters =
                0f;

            SpeedMultiplier =
                1f;

            AccelerationMultiplier =
                1f;

            CorneringMultiplier =
                1f;

            BrakingMultiplier =
                1f;

            debugRole =
                Role;

            debugPlayerPosition =
                0;

            debugSelfPosition =
                0;

            debugGapToPlayerMeters =
                0f;

            debugPressure =
                0f;

            debugSpeedMultiplier =
                1f;

            debugAccelerationMultiplier =
                1f;

            debugCorneringMultiplier =
                1f;

            debugBrakingMultiplier =
                1f;
        }

        #endregion
    }
}