using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public class BoosterDefinition :
        EquipmentDefinition
    {
        public float EnergyCapacity { get; }

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
            string requiredTechnologyId,
            float energyCapacity = 100f)
            : base(
                id,
                displayName,
                EquipmentCategory.Utility,
                requiredNodeSize,
                EquipmentActivationMode.Hold,
                creditCost,
                requiredTechnologyId)
        {
            if (float.IsNaN(energyCapacity) || float.IsInfinity(energyCapacity) || energyCapacity < 0f)
                throw new System.ArgumentOutOfRangeException(nameof(energyCapacity));
            EnergyCapacity = energyCapacity;
            EnergyPerSecond = energyPerSecond;
            SpeedMultiplier = speedMultiplier;
            AccelerationMultiplier =
                accelerationMultiplier;
        }
    }
}