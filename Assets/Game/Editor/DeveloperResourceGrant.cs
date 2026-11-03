using System;
using RaceFatal.Career;

// Editor assembly only. Validate both amounts before mutating either balance.
public static class DeveloperResourceGrant
{
    public static void Apply(TeamState team, int credits, int fame)
    {
        if (team == null) throw new ArgumentNullException(nameof(team));
        if (credits < 0 || fame < 0 || (credits == 0 && fame == 0))
            throw new ArgumentException("Enter positive credits and/or fame.");
        checked
        {
            _ = team.Credits + credits;
            _ = team.Fame + fame;
            _ = team.LifetimeFame + fame;
        }
        team.AddCredits(credits);
        team.AddFame(fame);
    }
}
