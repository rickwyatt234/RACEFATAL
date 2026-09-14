using System;
using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public class CountermeasureDefinition :
        EquipmentDefinition
    {
        public CountermeasureType CountermeasureType {
            get;
        }

        public float Cooldown {
            get;
        }

        public int UsesPerRace {
            get;
        }

        public float TriggerDistance {
            get;
        }

        public float DefeatRadius {
            get;
        }

        public CountermeasureDefinition(
            string id,
            string displayName,
            NodeSize requiredNodeSize,
            CountermeasureType countermeasureType,
            float cooldown,
            int usesPerRace,
            float triggerDistance,
            float defeatRadius,
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

            Cooldown =
                Math.Max(
                    0f,
                    cooldown);

            UsesPerRace =
                Math.Max(
                    1,
                    usesPerRace);

            TriggerDistance =
                Math.Max(
                    0f,
                    triggerDistance);

            DefeatRadius =
                Math.Max(
                    0f,
                    defeatRadius);
        }
    }
}