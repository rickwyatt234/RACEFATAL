using System.Collections.Generic;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public class WorldState
    {
        private readonly List<TeamState>
            opponentTeams =
                new List<TeamState>();

        public IReadOnlyList<TeamState>
            OpponentTeams =>
                opponentTeams;

        public static Result<WorldState> Restore(
            IEnumerable<TeamState> teams)
        {
            var world =
                new WorldState();

            if (teams == null)
            {
                return Result<WorldState>.Success(
                    world);
            }

            foreach (TeamState team in teams)
            {
                if (team == null)
                {
                    return Result<WorldState>.Failure(
                        "Opponent world contains a null team.");
                }

                if (world.FindOpponentTeam(
                        team.TeamId) != null)
                {
                    return Result<WorldState>.Failure(
                        $"Opponent team '{team.TeamId}' " +
                        "appears more than once in the world save.");
                }

                world.AddOpponentTeam(
                    team);
            }

            return Result<WorldState>.Success(
                world);
        }

        internal void AddOpponentTeam(
            TeamState team)
        {
            if (team == null)
                return;

            opponentTeams.Add(
                team);
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
