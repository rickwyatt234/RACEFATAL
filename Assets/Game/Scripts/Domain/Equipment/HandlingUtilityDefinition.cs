using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public sealed class HandlingUtilityDefinition : EquipmentDefinition
    {
        public float HandlingMultiplier { get; }

        public HandlingUtilityDefinition(string id, string displayName, NodeSize requiredNodeSize, float handlingMultiplier, int creditCost) : base(id, displayName, EquipmentCategory.Utility, requiredNodeSize, EquipmentActivationMode.Passive, creditCost)
        {
            HandlingMultiplier = handlingMultiplier;
        }
    }
}
