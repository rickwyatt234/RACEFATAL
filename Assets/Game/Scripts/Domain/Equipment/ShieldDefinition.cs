using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public class ShieldDefinition :
        EquipmentDefinition
    {
        public float Capacity { get; }

        public float RechargePerSecond { get; }

        public float RechargeDelay { get; }

        public ShieldDefinition(
            string id,
            string displayName,
            NodeSize requiredNodeSize,
            float capacity,
            float rechargePerSecond,
            float rechargeDelay,
            int creditCost,
            string requiredTechnologyId)
            : base(
                id,
                displayName,
                EquipmentCategory.Shield,
                requiredNodeSize,
                EquipmentActivationMode.Passive,
                creditCost,
                requiredTechnologyId)
        {
            Capacity = capacity;
            RechargePerSecond = rechargePerSecond;
            RechargeDelay = rechargeDelay;
        }
    }
}