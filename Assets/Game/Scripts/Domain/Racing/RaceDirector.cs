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
        public RaceAudience Audience { get; } = new RaceAudience();
        private RaceParticipant audiencePlayer;

        public void ConfigureAudience(AudienceSettings settings)
        {
            if (state.IsStarted) throw new InvalidOperationException("Cannot change audience rules during a race.");
            Audience.Configure(settings);
        }

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
            audiencePlayer = FindPlayerParticipant();

            foreach (RaceParticipant participant in state.Participants)
                participant.Racer.RecordRaceEntered();
        }

        public void Tick(float deltaTime)
        {
            if (!CanProcessRaceEvent() || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                return;

            var rules = state.Deathmatch;
            if (rules != null) deltaTime = Math.Min(deltaTime, Math.Max(0, rules.TimeLimitSeconds - ElapsedRaceTime));
            float previousTime = ElapsedRaceTime;
            ElapsedRaceTime += deltaTime;
            Audience.Tick(deltaTime, state, audiencePlayer);

            foreach (RaceParticipant participant in state.Participants)
            {
                if (participant.Status != RaceParticipantStatus.Racing)
                    continue;

                participant.Vehicle.Tick(deltaTime);
            }
            if (rules == null) return;
            float speedDelta = Math.Max(0, ElapsedRaceTime - Math.Max(previousTime, rules.StartGraceSeconds));
            // Process the entire frame before checking survival, avoiding iteration-order winners.
            foreach (var participant in state.Participants)
            {
                if (participant.Status != RaceParticipantStatus.Racing) continue;
                participant.BelowSpeedSeconds = rules.MinimumSpeedKph > 0 && participant.SpeedKph < rules.MinimumSpeedKph
                    ? participant.BelowSpeedSeconds + speedDelta : 0;
                if (participant.BelowSpeedSeconds >= rules.BelowSpeedGraceSeconds)
                {
                    participant.EliminationReason = "BELOW MINIMUM SPEED";
                    RetireRacer(participant.RacerId);
                }
            }
            if (state.SurvivingContenders <= rules.AllowedWinners || ElapsedRaceTime >= rules.TimeLimitSeconds)
                FinalizeDeathmatch();
        }

        public void UseDeathmatchRules(DeathmatchRules rules)
        {
            if (state.IsStarted) throw new InvalidOperationException("Cannot change started event rules.");
            state.Deathmatch = rules;
        }

        public void ReportSpeed(string racerId, float speedKph)
        {
            var participant = GetRacingParticipant(racerId);
            if (participant != null) participant.SpeedKph = float.IsNaN(speedKph) || float.IsInfinity(speedKph) ? 0 : Math.Max(0, speedKph);
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

            if (state.Deathmatch != null) { lapTracker.CompleteLap(participant); return; }
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
            DamageImpactSide impactSide = DamageImpactSide.Unknown, bool isRamAttack = false)
        {
            if (!CanProcessRaceEvent())
                return Result<DamageEvent>.Failure("Race is not active.");

            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
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
                victim.Vehicle.ApplyDamage(resolvedDamage, cause);

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

            if (attacker != null && attacker == audiencePlayer && attacker != victim &&
                (state.Deathmatch?.Mode == DeathmatchVictoryMode.Individual || attacker.TeamId != victim.TeamId) &&
                (cause == DamageCause.Weapon || cause == DamageCause.Collision))
                Audience.RecordDamage(damageEvent, isRamAttack);

            DamageApplied?.Invoke(damageEvent);

            if (resolution.CausedDestruction)
            {
                if (attacker != null && attacker != victim && (state.Deathmatch?.Mode == DeathmatchVictoryMode.Individual || attacker.TeamId != victim.TeamId))
                {
                    attacker.Eliminations++;
                    if (state.Deathmatch != null) attacker.Racer.RecordDestruction();
                }
                PermanentlyDestroy(victim);
            }

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

            participant.EliminationTime = ElapsedRaceTime;
            if (string.IsNullOrEmpty(participant.EliminationReason)) participant.EliminationReason = "RETIRED";
            participant.Retire();
            RacerRetired?.Invoke(participant);
        }

        public RaceResult ResolveRemainingRace()
        {
            if (state.IsFinished)
                return finalRaceResult ?? BuildResult();

            if (!state.IsStarted)
                return null;

            // Survival must run to completion; the player cannot fast-resolve a live deathmatch.
            if (state.Deathmatch != null) return null;

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

            if (state.Deathmatch != null && !deathmatchClassified) return null;
            state.FinishRace();

            finalRaceResult = BuildResult();

            ResolvePostRace(finalRaceResult);

            RaceCompleted?.Invoke(finalRaceResult);

            return finalRaceResult;
        }

        private bool deathmatchClassified;
        private void FinalizeDeathmatch()
        {
            var rules = state.Deathmatch;
            bool naturalFinish = state.SurvivingContenders <= rules.AllowedWinners;
            // Freeze all ranks before changing any participant status.
            foreach (var p in state.Participants) p.DeathmatchPosition = state.DeathmatchRank(p);
            foreach (var p in state.Participants)
            {
                bool survives = false;
                foreach (var other in state.Participants)
                    if (state.ContenderId(other) == state.ContenderId(p) && other.Status == RaceParticipantStatus.Racing) survives = true;
                p.DeathmatchWinner = survives && (naturalFinish || p.DeathmatchPosition <= rules.AllowedWinners);
            }
            foreach (var p in state.Participants)
            {
                if (p.Status != RaceParticipantStatus.Racing) continue;
                if (p.DeathmatchWinner)
                {
                    p.Finish(p.DeathmatchPosition, ElapsedRaceTime, false);
                    p.Racer.RecordFinish(p.FinishPosition, true);
                    RacerFinished?.Invoke(p);
                }
                else { p.EliminationReason = "TIME LIMIT"; RetireRacer(p.RacerId); }
            }
            deathmatchClassified = true;
            CompleteRace();
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
                        state.Deathmatch != null ? participant.DeathmatchPosition : i + 1,
                        participant.CompletedLaps,
                        participant.Status,
                        participant.FinishTimeSeconds,
                        participant.WasFastResolved, participant.DeathmatchWinner, participant.Eliminations, participant.EliminationReason,
                        participant == audiencePlayer ? Audience.AverageFavor : 100f));
            }

            return new RaceResult(
                state.RaceDefinition.Id,
                results, raceInstanceId, state.RaceDefinition.ResearchPointBonus, state.IsFinished, state.Deathmatch);
        }

        private void PermanentlyDestroy(RaceParticipant participant)
        {
            participant.EliminationTime = ElapsedRaceTime;
            participant.EliminationReason = "DESTROYED";
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
