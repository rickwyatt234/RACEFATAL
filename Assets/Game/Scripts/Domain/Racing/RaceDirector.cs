using System;
using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Combat;
using RaceFatal.Equipment;
using RaceFatal.Shared;

namespace RaceFatal.Racing
{
    /// Coordinates the lifecycle and rules of a single race.
    /// RaceDirector does not control Unity objects directly.
    /// Scene-facing code reports events such as lap completion,
    /// equipment input, damage, and energy-strip contact to this class.
    public sealed class RaceDirector
    {
        private readonly RaceState state;
        private readonly LapTracker lapTracker;
        private readonly CareerManager careerManager;

        private int nextFinishPosition = 1;

        public RaceState State => state;

#region Events

        public event Action<RaceParticipant> RacerFinished;
        public event Action<RaceParticipant> RacerDestroyed;
        public event Action<RaceParticipant> RacerRetired;

        public event Action<DamageEvent> DamageApplied;

        public event Action<WeaponFireEvent> WeaponFired;

        public event Action<RaceResult> RaceCompleted;
#endregion

#region Initialization
        public RaceDirector(
            RaceState state,
            CareerManager careerManager)
        {
            this.state = state
                ?? throw new ArgumentNullException(
                    nameof(state));

            this.careerManager = careerManager
                ?? throw new ArgumentNullException(
                    nameof(careerManager));

            lapTracker = new LapTracker(
                state.RaceDefinition.LapCount);

            SubscribeToParticipants();
        }

        private void SubscribeToParticipants()
        {
            foreach (RaceParticipant participant
                     in state.Participants)
            {
                participant.Vehicle
                    .EquipmentSystem
                    .WeaponFired += OnWeaponFired;
            }
        }

        private void OnWeaponFired(
            WeaponFireEvent fireEvent)
        {
            WeaponFired?.Invoke(fireEvent);
        }
#endregion

#region Race Lifecycle

        public void StartRace()
        {
            if (state.IsStarted ||
                state.IsFinished)
            {
                return;
            }

            state.StartRace();

            foreach (RaceParticipant participant
                     in state.Participants)
            {
                participant.Racer
                    .RecordRaceEntered();
            }
        }

        /// <summary>
        /// Updates race-time systems such as shields,
        /// weapon firing, charging, boosters, and countermeasure
        /// cooldowns.
        ///
        /// Unity should call this once per frame while the race
        /// is active.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!CanProcessRaceEvent())
                return;

            if (deltaTime <= 0f)
                return;

            foreach (RaceParticipant participant
                     in state.Participants)
            {
                if (participant.Status !=
                    RaceParticipantStatus.Racing)
                {
                    continue;
                }

                participant.Vehicle.Tick(
                    deltaTime);
            }
        }
        #endregion

#region Race Event Reporting
        public void ReportCourseProgress(
            string racerId,
            float progress)
        {
            if (!CanProcessRaceEvent())
                return;

            RaceParticipant participant =
                state.FindParticipant(racerId);

            if (participant == null)
                return;

            participant.SetCourseProgress(
                progress);
        }

        public void ReportLapCompleted(
            string racerId)
        {
            if (!CanProcessRaceEvent())
                return;

            RaceParticipant participant =
                state.FindParticipant(racerId);

            if (participant == null)
                return;

            if (participant.Status !=
                RaceParticipantStatus.Racing)
            {
                return;
            }

            bool completedRace =
                lapTracker.CompleteLap(
                    participant);

            if (!completedRace)
                return;

            participant.Finish(
                nextFinishPosition);

            nextFinishPosition++;

            participant.Racer.RecordFinish(
                participant.FinishPosition);

            RacerFinished?.Invoke(
                participant);
        }
#endregion

#region Equipment Selection

        public string SelectNextEquipment(
            string racerId)
        {
            if (!CanProcessRaceEvent())
                return null;

            RaceParticipant participant =
                GetRacingParticipant(
                    racerId);

            if (participant == null)
                return null;

            return participant.Vehicle
                .EquipmentSystem
                .SelectNext();
        }

        public string SelectPreviousEquipment(
            string racerId)
        {
            if (!CanProcessRaceEvent())
                return null;

            RaceParticipant participant =
                GetRacingParticipant(
                    racerId);

            if (participant == null)
                return null;

            return participant.Vehicle
                .EquipmentSystem
                .SelectPrevious();
        }
#endregion

#region Equipment Activation
        public bool BeginEquipmentActivation(
            string racerId)
        {
            if (!CanProcessRaceEvent())
                return false;

            RaceParticipant participant =
                GetRacingParticipant(
                    racerId);

            if (participant == null)
                return false;

            return participant.Vehicle
                .EquipmentSystem
                .BeginSelectedActivation();
        }
        public bool EndEquipmentActivation(
            string racerId)
        {
            if (!CanProcessRaceEvent())
                return false;

            RaceParticipant participant =
                GetRacingParticipant(
                    racerId);

            if (participant == null)
                return false;

            return participant.Vehicle
                .EquipmentSystem
                .EndSelectedActivation();
        }

#endregion

#region Damage
        public Result<DamageEvent> ApplyDamage(
            string attackerRacerId,
            string victimRacerId,
            float amount,
            DamageCause cause)
        {
            if (!CanProcessRaceEvent())
            {
                return Result<DamageEvent>.Failure(
                    "Race is not active.");
            }

            if (amount <= 0f)
            {
                return Result<DamageEvent>.Failure(
                    "Damage must be greater than zero.");
            }

            RaceParticipant victim =
                GetRacingParticipant(
                    victimRacerId);

            if (victim == null)
            {
                return Result<DamageEvent>.Failure(
                    "Victim was not found or is no longer racing.");
            }

            DamageResolution resolution =
                victim.Vehicle.ApplyDamage(
                    amount);

            var damageEvent =
                new DamageEvent(
                    attackerRacerId,
                    victimRacerId,
                    resolution.IncomingDamage,
                    resolution.ShieldAbsorbed,
                    resolution.BikeDamage,
                    cause,
                    resolution.CausedDestruction);

            DamageApplied?.Invoke(
                damageEvent);

            if (resolution.CausedDestruction)
            {
                PermanentlyDestroy(
                    victim);
            }

            return Result<DamageEvent>.Success(
                damageEvent);
        }
#endregion

#region Energy
        public float RechargeEnergy(
            string racerId,
            float amount)
        {
            if (!CanProcessRaceEvent())
                return 0f;

            if (amount <= 0f)
                return 0f;

            RaceParticipant participant =
                GetRacingParticipant(
                    racerId);

            if (participant == null)
                return 0f;

            return participant.Vehicle
                .RechargeEnergy(amount);
        }
#endregion

#region Countermeasures
        public bool TryTriggerCountermeasure(
            string racerId,
            CountermeasureType type)
        {
            if (!CanProcessRaceEvent())
                return false;

            RaceParticipant participant =
                GetRacingParticipant(
                    racerId);

            if (participant == null)
                return false;

            return participant.Vehicle.EquipmentSystem.TryTriggerCountermeasure(type);
        }
#endregion

#region Retirement

        public void RetireRacer(
            string racerId)
        {
            if (!CanProcessRaceEvent())
                return;

            RaceParticipant participant =
                GetRacingParticipant(
                    racerId);

            if (participant == null)
                return;

            participant.Retire();

            RacerRetired?.Invoke(
                participant);
        }
#endregion

#region Race Completion
        public RaceResult CompleteRace()
        {
            if (state.IsFinished)
            {
                return BuildResult();
            }

            state.FinishRace();

            RaceResult result =
                BuildResult();

            RaceCompleted?.Invoke(
                result);

            return result;
        }

        private RaceResult BuildResult()
        {
            IReadOnlyList<RaceParticipant> order =
                state.GetCurrentOrder();

            var results =
                new List<RaceResultEntry>(
                    order.Count);

            for (int i = 0;
                 i < order.Count;
                 i++)
            {
                RaceParticipant participant =
                    order[i];

                results.Add(
                    new RaceResultEntry(
                        participant.RacerId,
                        participant.TeamId,
                        i + 1,
                        participant.CompletedLaps,
                        participant.Status));
            }

            return new RaceResult(
                state.RaceDefinition.Id,
                results);
        }
#endregion

#region Permanent Destruction
        private void PermanentlyDestroy(
            RaceParticipant participant)
        {
            // Permanently destroys the physical bike and every
            // component currently installed on it.
            participant.Bike.Destroy();

            // Marks this participant as destroyed for this race.
            participant.Destroy();

            if (participant.Role ==
                RaceParticipantRole.Player)
            {
                // CareerRun.Kill() kills the player's RacerState
                // and ends the current character's career.
                careerManager.KillCurrentRun();
            }
            else
            {
                // AI partner and opponent racers also die
                // permanently when destroyed.
                participant.Racer.Kill();
            }

            RacerDestroyed?.Invoke(
                participant);
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private bool CanProcessRaceEvent()
        {
            return state.IsStarted &&
                   !state.IsFinished;
        }

        private RaceParticipant GetRacingParticipant(
            string racerId)
        {
            if (string.IsNullOrWhiteSpace(
                    racerId))
            {
                return null;
            }

            RaceParticipant participant =
                state.FindParticipant(
                    racerId);

            if (participant == null)
                return null;

            if (participant.Status !=
                RaceParticipantStatus.Racing)
            {
                return null;
            }

            return participant;
        }
#endregion
    }
}