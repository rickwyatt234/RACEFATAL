using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Energy;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Racing
{
    public class RaceParticipantFactory
    {
        private readonly GameDatabase gameDatabase;

        public RaceParticipantFactory(GameDatabase gameDatabase)
        {
            this.gameDatabase = gameDatabase;
        }

        public Result<RaceParticipant> CreateRaceParticipant(RacerState racer, BikeState bike, RaceParticipantRole role)
        {
            if (racer == null)
            {
                return Result<RaceParticipant>.Failure("Racer is null.");
            }
            if (bike == null)
            {
                return Result<RaceParticipant>.Failure("Bike is null.");
            }
            if (bike.IsDestroyed)
            {
                return Result<RaceParticipant>.Failure("Bike is destroyed.");
            }
            if (!bike.IsRaceReady)
            {
                return Result<RaceParticipant>.Failure("Bike is not race-ready.");
            }

            BikeDefinition bikeDefinition = gameDatabase.GetBikeDefinition(bike.BikeDefinitionId);
            if (bikeDefinition == null)
            {
                return Result<RaceParticipant>.Failure($"Bike definition with ID '{bike.BikeDefinitionId}' not found in the database.");
            }

            var energy = new EnergyPool(bikeDefinition.EnergyCapacity);

            Result<RaceEquipmentSystem> equipmentResult = 
                RaceEquipmentSystem.Create(
                    racer.RacerId,
                    bike.Loadout,
                    gameDatabase,
                    energy);

            if (!equipmentResult.IsSuccess)
            {
                return Result<RaceParticipant>.Failure($"Failed to create RaceEquipmentSystem: {equipmentResult.ErrorMessage}");
            }

            var vehicleState = new RaceVehicleState(
                bike,
                bikeDefinition.EnergyCapacity,
                equipmentResult.Value);
            
            return Result<RaceParticipant>.Success(
                new RaceParticipant(racer, vehicleState, role));
        }
    }
}