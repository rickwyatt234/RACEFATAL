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
        [Header("Pressure Envelope")]
        [Tooltip("Player lead where catch-up pressure begins.")]
        [Min(0f)][SerializeField] private float pressureStartDistance = 25f;

        [Tooltip("Player lead where the configured catch-up pressure reaches full strength.")]
        [Min(1f)][SerializeField] private float fullPressureDistance = 140f;

        [Tooltip("Maximum player lead considered for pressure calculations.")]
        [Min(1f)][SerializeField] private float maximumPressureDistance = 220f;

        [Header("Pursuit Roles")]
        [Range(0f, 1f)][SerializeField] private float pursuerPressureScale = 1f;
        [Range(0f, 1f)][SerializeField] private float supportPressureScale = 0.7f;
        [Range(0f, 1f)][SerializeField] private float fieldPressureScale = 0.15f;

        [Header("Subtle Pace Assistance")]
        [Range(1f, 1.1f)][SerializeField] private float maximumPursuerSpeedMultiplier = 1.045f;
        [Range(1f, 1.1f)][SerializeField] private float maximumSupportSpeedMultiplier = 1.025f;
        [Range(1f, 1.15f)][SerializeField] private float maximumPursuerAccelerationMultiplier = 1.08f;
        [Range(1f, 1.15f)][SerializeField] private float maximumSupportAccelerationMultiplier = 1.05f;

        [Header("AI Leader Relaxation")]
        [Tooltip("If an AI racer has a very large lead over the player, its pace can relax by a tiny amount.")]
        [Min(0f)][SerializeField] private float leaderRelaxationStartDistance = 120f;

        [Tooltip("Maximum top-speed reduction when the AI is far ahead of the player.")]
        [Range(0f, 0.03f)][SerializeField] private float maximumLeaderSpeedReduction = 0.01f;

        [Header("Runtime Debug")]
        [SerializeField] private AIRacePressureRole debugRole;
        [SerializeField] private int debugPlayerPosition;
        [SerializeField] private int debugSelfPosition;
        [SerializeField] private float debugGapToPlayerMeters;
        [SerializeField] private float debugPressure;
        [SerializeField] private float debugSpeedMultiplier = 1f;
        [SerializeField] private float debugAccelerationMultiplier = 1f;

        private RaceParticipant participant;
        private RaceRuntimeController raceRuntime;
        private TrackProgressPath progressPath;

        public AIRacePressureRole Role { get; private set; }
        public float Pressure { get; private set; }
        public float GapToPlayerMeters { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public float AccelerationMultiplier { get; private set; } = 1f;

        public bool IsPriorityPursuit =>
            Role == AIRacePressureRole.Pursuer ||
            Role == AIRacePressureRole.PursuerSupport;

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
                return false;
            }

            participant = raceParticipant;
            raceRuntime = runtime;
            progressPath = path;

            ResetRuntimeState();
            return true;
        }

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
                player.Status != RaceParticipantStatus.Racing ||
                participant.Status != RaceParticipantStatus.Racing)
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
                GetRaceDistance(player);

            float selfDistance =
                GetRaceDistance(participant);

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

            debugRole = Role;
            debugPlayerPosition = playerPosition;
            debugSelfPosition = selfPosition;
            debugGapToPlayerMeters = GapToPlayerMeters;
            debugPressure = Pressure;
            debugSpeedMultiplier = SpeedMultiplier;
            debugAccelerationMultiplier =
                AccelerationMultiplier;
        }

        private AIRacePressureRole DetermineRole(
            int playerPosition,
            int selfPosition,
            float gapToPlayer)
        {
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

            float normalized =
                Mathf.InverseLerp(
                    pressureStartDistance,
                    Mathf.Max(
                        pressureStartDistance + 1f,
                        fullPressureDistance),
                    clampedGap);

            float roleScale =
                role switch
                {
                    AIRacePressureRole.Pursuer =>
                        pursuerPressureScale,

                    AIRacePressureRole.PursuerSupport =>
                        supportPressureScale,

                    _ =>
                        fieldPressureScale
                };

            return Mathf.Clamp01(
                normalized *
                roleScale);
        }

        private void CalculateRuntimeMultipliers(
            float gapToPlayer,
            AIRacePressureRole role,
            float pressure)
        {
            SpeedMultiplier = 1f;
            AccelerationMultiplier = 1f;

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
                        break;
                }

                return;
            }

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

        private void ResetRuntimeState()
        {
            Role = AIRacePressureRole.Normal;
            Pressure = 0f;
            GapToPlayerMeters = 0f;
            SpeedMultiplier = 1f;
            AccelerationMultiplier = 1f;

            debugRole = Role;
            debugPlayerPosition = 0;
            debugSelfPosition = 0;
            debugGapToPlayerMeters = 0f;
            debugPressure = 0f;
            debugSpeedMultiplier = 1f;
            debugAccelerationMultiplier = 1f;
        }
    }
}
