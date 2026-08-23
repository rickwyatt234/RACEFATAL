using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public class EquipmentDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }

        public EquipmentCategory Category { get; }

        public NodeSize RequiredNodeSize { get; }

        public WeaponAimMode AimMode { get; }

        public float EnergyCost { get; }

        public float Damage { get; }

        public int CreditCost { get; }

        public string RequiredTechnologyId { get; }

        public EquipmentDefinition(
            string id,
            string displayName,
            EquipmentCategory category,
            NodeSize requiredNodeSize,
            WeaponAimMode aimMode,
            float energyCost,
            float damage,
            int creditCost,
            string requiredTechnologyId)
        {
            Id = id;
            DisplayName = displayName;

            Category = category;

            RequiredNodeSize = requiredNodeSize;

            AimMode = aimMode;

            EnergyCost = energyCost;

            Damage = damage;

            CreditCost = creditCost;

            RequiredTechnologyId =
                requiredTechnologyId;
        }
    }
}