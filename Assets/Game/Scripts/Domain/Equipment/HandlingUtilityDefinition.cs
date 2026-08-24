using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public sealed class HandlingUtilityDefinition :
        EquipmentDefinition
    {
        public float HandlingMultiplier { get; }

        public HandlingUtilityDefinition(
            string id,
            string displayName,
            NodeSize requiredNodeSize,
            float handlingMultiplier,
            int creditCost,
            string requiredTechnologyId)
            : base(
                id,
                displayName,
                EquipmentCategory.Utility,
                requiredNodeSize,
                EquipmentActivationMode.Passive,
                creditCost,
                requiredTechnologyId)
        {
            HandlingMultiplier =
                handlingMultiplier;
        }
    }
}

//DYNAMIC GYRO CONTROLLER
//HANDLING: x1.15