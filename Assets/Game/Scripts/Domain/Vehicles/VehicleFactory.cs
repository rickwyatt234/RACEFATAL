using System;

namespace RaceFatal.Vehicles
{
    public class VehicleFactory
    {
        public BikeState CreateBike(
            BikeDefinition bikeDefinition,
            string primaryColor,
            string secondaryColor)
        {
            string bikeId = Guid.NewGuid().ToString("N");

            return new BikeState(
                bikeId,
                bikeDefinition.Id,
                bikeDefinition.SmallNodeCount,
                bikeDefinition.MediumNodeCount,
                bikeDefinition.LargeNodeCount,
                primaryColor,
                secondaryColor);
        }

        public EngineState CreateEngine(EngineDefinition engineDefinition)
        {
            string engineId = Guid.NewGuid().ToString("N");

            return new EngineState(
                engineId,
                engineDefinition.Id,
                engineDefinition.EngineClass);
        }

        public ChassisState CreateChassis(ChassisDefinition chassisDefinition)
        {
            string chassisId = Guid.NewGuid().ToString("N");

            return new ChassisState(
                chassisId,
                chassisDefinition.Id);
        }
    }
}
