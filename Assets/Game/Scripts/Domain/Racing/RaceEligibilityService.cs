using RaceFatal.Shared;
using RaceFatal.Career;

namespace RaceFatal.Racing
{
    public class RaceEligibilityService
    {
        public Result<bool> CheckEngineClassEligibility(TeamState team, EngineClass requiredEngineClass)
        {
            if (!team.Garage.HasRaceReadyBikeFor(requiredEngineClass))
            {
                return Result<bool>.Failure($"Team does not have a race-ready bike for engine class {requiredEngineClass}.");
            }
            return Result<bool>.Success(true);
        }
    }
}
