using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public class CountermeasureDefinition :
        EquipmentDefinition
    {
        public CountermeasureType CountermeasureType {
            get;
        }

        public float Cooldown { get; }

        public CountermeasureDefinition(
            string id,
            string displayName,
            NodeSize requiredNodeSize,
            CountermeasureType countermeasureType,
            float cooldown,
            int creditCost,
            string requiredTechnologyId)
            : base(
                id,
                displayName,
                EquipmentCategory.Utility,
                requiredNodeSize,
                EquipmentActivationMode.Reactive,
                creditCost,
                requiredTechnologyId)
        {
            CountermeasureType =
                countermeasureType;

            Cooldown = cooldown;
        }
    }
}
