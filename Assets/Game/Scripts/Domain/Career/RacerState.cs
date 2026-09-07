using System;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public class RacerState
    {
        public string RacerId { get; }

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
            bool isPlayerCharacter)
        {
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