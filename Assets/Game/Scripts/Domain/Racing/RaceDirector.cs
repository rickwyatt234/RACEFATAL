using System;
using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Combat;
using RaceFatal.Equipment;
using RaceFatal.Shared;

namespace RaceFatal.Racing
{
    public class RaceDirector
    {
        private readonly RaceState state;
        private readonly LapTracker lapTracker;
        private readonly CareerManager careerManager;

        private string raceInstanceId = Guid.NewGuid().ToString("N");
        public string InstanceId => raceInstanceId;
        public void UseInstanceId(string id)
        {
            if (state.IsStarted || string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Cannot change a started race identity.");
            raceInstanceId = id;
        }
        private int nextFinishPosition = 1;
        public float AIVsAIDamageMultiplier { get; set; } = 0.45f;

        private RaceResult finalRaceResult;
        private PostRaceResult postRaceResult;

        public RaceState State => state;
        public RaceResult FinalRaceResult => finalRaceResult;
        public PostRaceResult PostRaceResult => postRaceResult;

        public float ElapsedRaceTime { get; private set; }

        public event Action<RaceParticipant> RacerFinished;
        public event Action<RaceParticipant> RacerDestroyed;
        public event Action<RaceParticipant> RacerRetired;

        public event Action<DamageEvent> DamageApplied;
        public event Action<WeaponFireEvent> WeaponFired;

        public event Action<RaceResult> RaceCompleted;
        public event Action<PostRaceResult> PostRaceResolved;

        public RaceDirector(RaceState state, CareerManager careerManager)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.careerManager = careerManager ?? throw new ArgumentNullException(nameof(careerManager));

            lapTracker = new LapTracker(state.RaceDefinition.LapCount);
            SubscribeToParticipants();
        }

        private void SubscribeToParticipants()
        {
            foreach (RaceParticipant participant in state.Participants)
                participant.Vehicle.EquipmentSystem.WeaponFired += OnWeaponFired;
        }

        private void OnWeaponFired(WeaponFireEvent fireEvent)
        {
            WeaponFired?.Invoke(fireEvent);
        }

        public void StartRace()
        {
            if (state.IsStarted || state.IsFinished)
                return;

            ElapsedRaceTime = 0f;
            state.StartRace();

            foreach (RaceParticipant participant in state.Participants)
                participant.Racer.RecordRaceEntered();
        }

        public void Tick(float deltaTime)
        {
            if (!CanProcessRaceEvent() || deltaTime <= 0f)
                return;

            ElapsedRaceTime += deltaTime;

            foreach (RaceParticipant participant in state.Participants)
            {
                if (participant.Status != RaceParticipantStatus.Racing)
                    continue;

                participant.Vehicle.Tick(deltaTime);
            }
        }

        public void ReportCourseProgress(string racerId, float progress)
        {
            if (!CanProcessRaceEvent())
                return;

            RaceParticipant participant = state.FindParticipant(racerId);
            participant?.SetCourseProgress(progress);
        }

        public void ReportLapCompleted(string racerId)
        {
            if (!CanProcessRaceEvent())
                return;

            RaceParticipant participant = state.FindParticipant(racerId);

            if (participant == null ||
                participant.Status != RaceParticipantStatus.Racing)
                return;

            if (lapTracker.CompleteLap(participant))
                ConfirmFinish(participant, false);
        }

        private void ConfirmFinish(RaceParticipant participant, bool fastResolved)
        {
            if (participant == null ||
                participant.Status != RaceParticipantStatus.Racing)
                return;

            float? finishTime = fastResolved ? null : ElapsedRaceTime;

            participant.Finish(nextFinishPosition, finishTime, fastResolved);
            nextFinishPosition++;

            participant.Racer.RecordFinish(participant.FinishPosition);
            RacerFinished?.Invoke(participant);
        }

        public string SelectNextEquipment(string racerId)
        {
            if (!CanProcessRaceEvent())
                return null;

            RaceParticipant participant = GetRacingParticipant(racerId);

            return participant?.Vehicle.EquipmentSystem.SelectNext();
        }

        public string SelectPreviousEquipment(string racerId)
        {
            if (!CanProcessRaceEvent())
                return null;

            RaceParticipant participant = GetRacingParticipant(racerId);

            return participant?.Vehicle.EquipmentSystem.SelectPrevious();
        }

        public bool BeginEquipmentActivation(string racerId)
        {
            if (!CanProcessRaceEvent())
                return false;

            RaceParticipant participant = GetRacingParticipant(racerId);

            return participant != null &&
                   participant.Vehicle.EquipmentSystem.BeginSelectedActivation();
        }

        public bool EndEquipmentActivation(string racerId)
        {
            if (!CanProcessRaceEvent())
                return false;

            RaceParticipant participant = GetRacingParticipant(racerId);

            return participant != null &&
                   participant.Vehicle.EquipmentSystem.EndSelectedActivation();
        }

        public Result<DamageEvent> ApplyDamage(
            string attackerRacerId,
            string victimRacerId,
            float amount,
            DamageCause cause,
            DamageImpactSide impactSide = DamageImpactSide.Unknown)
        {
            if (!CanProcessRaceEvent())
                return Result<DamageEvent>.Failure("Race is not active.");

            if (amount <= 0f)
                return Result<DamageEvent>.Failure("Damage must be greater than zero.");

            RaceParticipant victim = GetRacingParticipant(victimRacerId);

            if (victim == null)
                return Result<DamageEvent>.Failure(
                    "Victim was not found or is no longer racing.");

            float resolvedDamage =
                amount;

            RaceParticipant attacker =
                GetRacingParticipant(
                    attackerRacerId);

            bool attackerIsAI =
                attacker != null &&
                attacker.Role !=
                    RaceParticipantRole.Player;

            bool victimIsAI =
                victim.Role !=
                    RaceParticipantRole.Player;

            if (attackerIsAI &&
                victimIsAI)
            {
                float multiplier =
                    Math.Max(
                        0f,
                        Math.Min(
                            1f,
                            AIVsAIDamageMultiplier));

                resolvedDamage *=
                    multiplier;
            } 

            RaceShieldState shield =
                victim.Vehicle.EquipmentSystem.Shield;

            bool shieldWasActive =
                shield != null &&
                !shield.IsDepleted &&
                shield.Current > 0f;

            DamageResolution resolution =
                victim.Vehicle.ApplyDamage(resolvedDamage);

            bool shieldDepleted =
                shieldWasActive &&
                resolution.ShieldAbsorbed > 0f &&
                shield != null &&
                shield.IsDepleted;

            DamageEvent damageEvent =
                new DamageEvent(
                    attackerRacerId,
                    victimRacerId,
                    resolution.IncomingDamage,
                    resolution.ShieldAbsorbed,
                    resolution.BikeDamage,
                    cause,
                    impactSide,
                    shieldDepleted,
                    resolution.CausedDestruction);

            DamageApplied?.Invoke(damageEvent);

            if (resolution.CausedDestruction)
                PermanentlyDestroy(victim);

            return Result<DamageEvent>.Success(damageEvent);
        }

        public float RechargeEnergy(string racerId, float amount)
        {
            if (!CanProcessRaceEvent() || amount <= 0f)
                return 0f;

            RaceParticipant participant = GetRacingParticipant(racerId);

            return participant != null
                ? participant.Vehicle.RechargeEnergy(amount)
                : 0f;
        }

        public bool TryTriggerCountermeasure(
            string racerId,
            CountermeasureType type)
        {
            if (!CanProcessRaceEvent())
                return false;

            RaceParticipant participant = GetRacingParticipant(racerId);

            return participant != null &&
                   participant.Vehicle.EquipmentSystem
                       .TryTriggerCountermeasure(type);
        }

        public void RetireRacer(string racerId)
        {
            if (!CanProcessRaceEvent())
                return;

            RaceParticipant participant = GetRacingParticipant(racerId);

            if (participant == null)
                return;

            participant.Retire();
            RacerRetired?.Invoke(participant);
        }

        public RaceResult ResolveRemainingRace()
        {
            if (state.IsFinished)
                return finalRaceResult ?? BuildResult();

            if (!state.IsStarted)
                return null;

            IReadOnlyList<RaceParticipant> currentOrder =
                state.GetCurrentOrder();

            foreach (RaceParticipant participant in currentOrder)
            {
                if (participant.Status == RaceParticipantStatus.Racing)
                    ConfirmFinish(participant, true);
            }

            return CompleteRace();
        }

        public RaceResult CompleteRace()
        {
            if (state.IsFinished)
                return finalRaceResult ?? BuildResult();

            state.FinishRace();

            finalRaceResult = BuildResult();

            ResolvePostRace(finalRaceResult);

            RaceCompleted?.Invoke(finalRaceResult);

            return finalRaceResult;
        }

        private void ResolvePostRace(RaceResult raceResult)
        {
            if (postRaceResult != null)
                return;

            RaceParticipant player = FindPlayerParticipant();

            if (player == null)
                return;

            postRaceResult =
                careerManager.ResolvePostRace(
                    raceResult,
                    player.Racer);

            PostRaceResolved?.Invoke(postRaceResult);
        }

        private RaceParticipant FindPlayerParticipant()
        {
            foreach (RaceParticipant participant in state.Participants)
            {
                if (participant.Role == RaceParticipantRole.Player)
                    return participant;
            }

            return null;
        }

        private RaceResult BuildResult()
        {
            IReadOnlyList<RaceParticipant> order =
                state.GetCurrentOrder();

            List<RaceResultEntry> results =
                new List<RaceResultEntry>(order.Count);

            for (int i = 0; i < order.Count; i++)
            {
                RaceParticipant participant = order[i];

                results.Add(
                    new RaceResultEntry(
                        participant.RacerId,
                        participant.Racer.Name,
                        participant.TeamId,
                        participant.TeamName,
                        i + 1,
                        participant.CompletedLaps,
                        participant.Status,
                        participant.FinishTimeSeconds,
                        participant.WasFastResolved));
            }

            return new RaceResult(
                state.RaceDefinition.Id,
                results, raceInstanceId, state.RaceDefinition.ResearchPointBonus, state.IsFinished);
        }

        private void PermanentlyDestroy(RaceParticipant participant)
        {
            participant.Bike.Destroy();
            participant.Destroy();

            if (participant.Role == RaceParticipantRole.Player)
                careerManager.KillCurrentRun();
            else
                participant.Racer.Kill();

            RacerDestroyed?.Invoke(participant);
        }

        private bool CanProcessRaceEvent()
        {
            return state.IsStarted &&
                   !state.IsFinished;
        }

        private RaceParticipant GetRacingParticipant(string racerId)
        {
            if (string.IsNullOrWhiteSpace(racerId))
                return null;

            RaceParticipant participant =
                state.FindParticipant(racerId);

            if (participant == null ||
                participant.Status != RaceParticipantStatus.Racing)
                return null;

            return participant;
        }
    }
}
