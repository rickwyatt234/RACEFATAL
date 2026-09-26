using System;
using System.Collections.Generic;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public class RacerState
    {
        public string RacerId { get; }
        public string DefinitionId { get; }

        public string Name { get; }

        public string TeamId { get; }

        public bool IsPlayerCharacter { get; }

        public RacerCareerStatus Status {
            get;
            private set;
        }

        public bool IsAlive =>
            Status != RacerCareerStatus.Dead;

        public bool CanRace =>
            Status == RacerCareerStatus.Active;

        public int RacesEntered {
            get;
            private set;
        }

        public int RacesWon {
            get;
            private set;
        }

        public int Podiums {
            get;
            private set;
        }

        public int RacersDestroyed {
            get;
            private set;
        }

        public CharacterProgression Progression {
            get;
        }

        public RacerState(
            string racerId,
            string name,
            string teamId,
            bool isPlayerCharacter,
            string definitionId = null)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? racerId : definitionId;
            RacerId = racerId
                ?? throw new ArgumentNullException(
                    nameof(racerId));

            Name = name
                ?? throw new ArgumentNullException(
                    nameof(name));

            TeamId = teamId
                ?? throw new ArgumentNullException(
                    nameof(teamId));

            IsPlayerCharacter =
                isPlayerCharacter;

            Status =
                RacerCareerStatus.Active;

            Progression =
                new CharacterProgression();
        }

        public static Result<RacerState> Restore(
            string racerId,
            string name,
            string teamId,
            bool isPlayerCharacter,
            RacerCareerStatus status,
            int racesEntered,
            int racesWon,
            int podiums,
            int racersDestroyed,
            int progressionFame,
            IEnumerable<string> purchasedPerkIds,
            string definitionId = null)
        {
            if (string.IsNullOrWhiteSpace(
                    racerId))
            {
                return Result<RacerState>.Failure(
                    "Racer ID is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    name))
            {
                return Result<RacerState>.Failure(
                    "Racer name is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    teamId))
            {
                return Result<RacerState>.Failure(
                    "Racer team ID is required.");
            }

            if (racesEntered < 0 ||
                racesWon < 0 ||
                podiums < 0 ||
                racersDestroyed < 0 ||
                progressionFame < 0)
            {
                return Result<RacerState>.Failure(
                    "Racer career values cannot be negative.");
            }

            if (racesWon > racesEntered)
            {
                return Result<RacerState>.Failure(
                    "Races won cannot exceed races entered.");
            }

            if (podiums > racesEntered)
            {
                return Result<RacerState>.Failure(
                    "Podiums cannot exceed races entered.");
            }

            var racer =
                new RacerState(
                    racerId,
                    name,
                    teamId,
                    isPlayerCharacter, definitionId);

            racer.Status =
                status;

            racer.RacesEntered =
                racesEntered;

            racer.RacesWon =
                racesWon;

            racer.Podiums =
                podiums;

            racer.RacersDestroyed =
                racersDestroyed;

            racer.Progression.RestoreState(
                progressionFame,
                purchasedPerkIds);

            return Result<RacerState>.Success(
                racer);
        }

        public void RecordRaceEntered()
        {
            if (!CanRace)
                return;

            RacesEntered++;
        }

        public void RecordFinish(
            int position)
        {
            if (position <= 0)
                return;

            if (position == 1)
            {
                RacesWon++;
            }

            if (position <= 3)
            {
                Podiums++;
            }
        }

        public void RecordDestruction()
        {
            RacersDestroyed++;
        }

        public void Kill()
        {
            if (Status ==
                RacerCareerStatus.Dead)
            {
                return;
            }

            Status =
                RacerCareerStatus.Dead;
        }

        public void Retire()
        {
            if (Status !=
                RacerCareerStatus.Active)
            {
                return;
            }

            Status =
                RacerCareerStatus.Retired;
        }
    }
}
