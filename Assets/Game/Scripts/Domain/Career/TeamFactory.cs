using System;

namespace RaceFatal.Career
{
    public class TeamFactory
    {
        public TeamState CreateNewTeam(string teamName, string primaryColor, string secondaryColor)
        {
            string teamId = Guid.NewGuid().ToString("N");
            return new TeamState(
                teamId: teamId,
                teamName: teamName,
                primaryColor: primaryColor,
                secondaryColor: secondaryColor);
        }
    }
}
