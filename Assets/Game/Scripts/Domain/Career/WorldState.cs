using System.Collections.Generic;

namespace RaceFatal.Career
{
    public sealed class WorldState
    {
        private readonly List<TeamState>
            opponentTeams =
                new List<TeamState>();

        public IReadOnlyList<TeamState>
            OpponentTeams =>
                opponentTeams;

        internal void AddOpponentTeam(
            TeamState team)
        {
            if (team == null)
                return;

            opponentTeams.Add(team);
        }

        public TeamState FindOpponentTeam(
            string teamId)
        {
            foreach (TeamState team
                     in opponentTeams)
            {
                if (team.TeamId ==
                    teamId)
                {
                    return team;
                }
            }

            return null;
        }
    }
}