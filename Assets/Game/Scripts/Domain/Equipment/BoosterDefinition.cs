using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public class BoosterDefinition :
        EquipmentDefinition
    {
        public float EnergyPerSecond { get; }

        public float SpeedMultiplier { get; }

        public float AccelerationMultiplier { get; }

        public BoosterDefinition(
            string id,
            string displayName,
            NodeSize requiredNodeSize,
            float energyPerSecond,
            float speedMultiplier,
            float accelerationMultiplier,
            int creditCost,
            string requiredTechnologyId)
            : base(
                id,
                displayName,
                EquipmentCategory.Utility,
                requiredNodeSize,
                EquipmentActivationMode.Hold,
                creditCost,
                requiredTechnologyId)
        {
            EnergyPerSecond = energyPerSecond;
            SpeedMultiplier = speedMultiplier;
            AccelerationMultiplier =
                accelerationMultiplier;
        }
    }
}