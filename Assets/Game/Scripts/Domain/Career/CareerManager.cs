/*
    OWNED CURRENTLY ACTIVE CAREER RUN
    DOES NOT OWN PERSISTENT SAVE DATA
    STARTS NEW RUNS, EITHER BY CREATING NEW TEAM IF SLOT IS EMPTY OR BY 
    CREATING A NEW PLAYER CHARACTER FOR AN EXISTING TEAM.
*/


using System;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public class CareerManager
    {
        private readonly CharacterFactory characterFactory;
        public TeamState Team { get; private set; }
        public CareerRun CurrentRun { get; private set; }
        public bool HasTeam => Team != null;
        public bool HasActiveRun => CurrentRun != null && CurrentRun.IsActive;

        public CareerManager(CharacterFactory characterFactory)
        {
            this.characterFactory = characterFactory;
        }

        public void LoadTeam(TeamState team)
        {
            if (HasTeam)
            {
                throw new InvalidOperationException("Cannot load team when one is already loaded.");
            }
            Team = team;
        }

        public CareerRun StartNewRun(string playerName)
        {
            if (Team == null)
            {
                throw new InvalidOperationException("Cannot start a new run without a team.");
            }
            if (HasActiveRun)
            {
                throw new InvalidOperationException("Cannot start a new run when one is already active.");
            }

            RacerState player = characterFactory.CreateNewPlayerCharacter(Team.TeamId, playerName);

            CurrentRun = new CareerRun(
                runId: Guid.NewGuid().ToString("N"),
                team: Team,
                player: player);

            return CurrentRun;
        }

        public void KillCurrentRun()
        {
            CurrentRun.Kill();
            CurrentRun = null;
        }

        public void RetireCurrentRun()
        {
            CurrentRun.Retire();
            CurrentRun = null;
        }
    }
}