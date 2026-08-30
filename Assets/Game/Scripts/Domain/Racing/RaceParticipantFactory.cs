using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Energy;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Racing
{
    public sealed class RaceParticipantFactory
    {
        private readonly GameDatabase database;

        private readonly BikePerformanceCalculator
            performanceCalculator;

        public RaceParticipantFactory(
            GameDatabase database,
            BikePerformanceCalculator performanceCalculator)
        {
            this.database = database;
            this.performanceCalculator =
                performanceCalculator;
        }

        public Result<RaceParticipant> Create(
            RacerState racer,
            BikeState bike,
            RaceParticipantRole role)
        {
            if (racer == null)
            {
                return Result<RaceParticipant>.Failure(
                    "Racer is required.");
            }

            if (bike == null)
            {
                return Result<RaceParticipant>.Failure(
                    "Bike is required.");
            }

            if (bike.IsDestroyed)
            {
                return Result<RaceParticipant>.Failure(
                    "Destroyed bikes cannot enter races.");
            }

            BikeDefinition bikeDefinition =
                database.GetBikeDefinition(
                    bike.BikeDefinitionId);

            if (bikeDefinition == null)
            {
                return Result<RaceParticipant>.Failure(
                    $"Bike definition '{bike.BikeDefinitionId}' was not found.");
            }

            Result<BikePerformance>
                performanceResult =
                    performanceCalculator.Calculate(
                        bike);


            var energy =
                new EnergyPool(
                    bikeDefinition.EnergyCapacity);

            Result<RaceEquipmentSystem>
                equipmentResult =
                    RaceEquipmentSystem.Create(
                        racer.RacerId,
                        bike.Loadout,
                        database,
                        energy);

            var vehicle =
                new RaceVehicleState(
                    bike,
                    performanceResult.Value,
                    bikeDefinition.EnergyCapacity,
                    equipmentResult.Value);

            return Result<RaceParticipant>.Success(
                new RaceParticipant(
                    racer,
                    vehicle,
                    role));
        }
    }
}