using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public abstract class EquipmentDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }

        public EquipmentCategory Category { get; }

        public NodeSize RequiredNodeSize { get; }
        public EquipmentActivationMode ActivationMode { get; }
        public int CreditCost { get; }
        public string RequiredTechnologyId { get; }

        public EquipmentDefinition(
            string id,
            string displayName,
            EquipmentCategory category,
            NodeSize requiredNodeSize,
            EquipmentActivationMode activationMode,
            int creditCost,
            string requiredTechnologyId)
        {
            Id = id;
            DisplayName = displayName;

            Category = category;

            RequiredNodeSize = requiredNodeSize;

            ActivationMode = activationMode;

            CreditCost = creditCost;

            RequiredTechnologyId =
                requiredTechnologyId;
        }
    }
}