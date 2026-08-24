using RaceFatal.Shared;
using RaceFatal.Vehicles;
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

        public Result<bool> CheckBike(RaceDefinition raceDefinition, BikeState bike)
        {
            if (raceDefinition == null)
            {
                return Result<bool>.Failure("Race definition is null.");
            }
            if (bike == null)
            {
                return Result<bool>.Failure("Bike is null.");
            }
            if (bike.IsDestroyed)
            {
                return Result<bool>.Failure("Bike is destroyed.");
            }
            if (!bike.IsRaceReady)
            {
                return Result<bool>.Failure("Bike is not race-ready.");
            }
            if (!bike.EngineClass.HasValue)
            {
                return Result<bool>.Failure("Bike does not have an engine.");
            }
            if (bike.EngineClass.Value != raceDefinition.EngineClass)
            {
                return Result<bool>.Failure($"Bike engine class {bike.EngineClass.Value} does not match required engine class {raceDefinition.EngineClass}.");
            }
            return Result<bool>.Success(true);
        }
        
    }
}
