/*
    LIFETIME OF ONE PLAYER CHARACTER'S CAREER. ENDS WHEN PLAYER QUITS OR DIES.
*/
using System;
using System.Collections.Generic;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public class CareerRun
    {
        private readonly Dictionary<EngineClass, List<string>>
            rivalsByEngineClass =
                new Dictionary<EngineClass, List<string>>();

        public bool NeedsIntroduction { get; private set; }
        public string StartingBikeSource { get; private set; }
        public string StartingPerkId { get; private set; }

        internal void PrepareIntroduction(string bikeSource, string perkId)
        {
            NeedsIntroduction = true;
            StartingBikeSource = bikeSource;
            StartingPerkId = perkId;
        }

        internal void AcknowledgeIntroduction() { NeedsIntroduction = false; }

        public string RunId { get; }
        public TeamState Team { get; }
        public RacerState Player { get; }

        public bool IsActive {
            get;
            private set;
        } = true;

        public string ActiveChampionshipId {
            get;
            private set;
        }

        public CareerRun(
            string runId,
            TeamState team,
            RacerState player)
        {
            RunId = runId;
            Team = team;
            Player = player;
        }

        public static Result<CareerRun> Restore(
            string runId,
            TeamState team,
            RacerState player,
            bool isActive,
            string activeChampionshipId,
            bool needsIntroduction = false,
            string startingBikeSource = null,
            string startingPerkId = null)
        {
            if (string.IsNullOrWhiteSpace(
                    runId))
            {
                return Result<CareerRun>.Failure(
                    "Career run ID is required.");
            }

            if (team == null)
            {
                return Result<CareerRun>.Failure(
                    "Career team is required.");
            }

            if (player == null)
            {
                return Result<CareerRun>.Failure(
                    "Career player is required.");
            }

            if (player.TeamId != team.TeamId)
            {
                return Result<CareerRun>.Failure(
                    "Career player does not belong to the career team.");
            }

            if (!player.IsPlayerCharacter)
            {
                return Result<CareerRun>.Failure(
                    "Career player must be a player character.");
            }

            if (isActive && !player.CanRace)
            {
                return Result<CareerRun>.Failure(
                    "An active career run requires an active player racer.");
            }

            var run =
                new CareerRun(
                    runId,
                    team,
                    player);

            run.NeedsIntroduction = needsIntroduction;
            run.StartingBikeSource = startingBikeSource;
            run.StartingPerkId = startingPerkId;
            run.IsActive =
                isActive;

            run.ActiveChampionshipId =
                string.IsNullOrWhiteSpace(
                    activeChampionshipId)
                    ? null
                    : activeChampionshipId;

            return Result<CareerRun>.Success(
                run);
        }

        public IReadOnlyCollection<string>
            GetRivalsForEngineClass(
                EngineClass engineClass)
        {
            if (!rivalsByEngineClass.TryGetValue(
                    engineClass,
                    out List<string> rivals))
            {
                return Array.Empty<string>();
            }

            return rivals;
        }

        public bool AddRivalForEngineClass(
            EngineClass engineClass,
            string rivalId)
        {
            if (string.IsNullOrWhiteSpace(
                    rivalId))
            {
                return false;
            }

            if (!rivalsByEngineClass.TryGetValue(
                    engineClass,
                    out List<string> rivals))
            {
                rivals =
                    new List<string>();

                rivalsByEngineClass[
                    engineClass] = rivals;
            }

            if (rivals.Contains(
                    rivalId))
            {
                return false;
            }

            if (rivals.Count >= 2)
            {
                return false;
            }

            rivals.Add(
                rivalId);

            return true;
        }

        public void EnterChampionship(
            string championshipId)
        {
            ActiveChampionshipId =
                championshipId;
        }

        public void ExitChampionship()
        {
            ActiveChampionshipId =
                null;
        }

        public void Kill()
        {
            Player.Kill();
            Team.PermanentlyEliminateRacer(
                Player.RacerId);

            IsActive = false;
        }

        public void Retire()
        {
            Player.Retire();
            IsActive = false;
        }
    }
}
