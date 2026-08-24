using RaceFatal.Equipment;
using UnityEngine;

namespace RaceFatal.Content.Equipment
{
    [CreateAssetMenu(
        fileName = "HandlingUtilityDefinition",
        menuName = "RaceFatal/Equipment/Handling Utility")]
    public sealed class HandlingUtilityDefinitionSO :
        EquipmentDefinitionSO
    {
        [Header("Handling")]
        [Min(0f)]
        [SerializeField]
        private float handlingMultiplier = 1f;

        public override EquipmentDefinition
            CreateEquipmentDefinition()
        {
            return new HandlingUtilityDefinition(
                id,
                displayName,
                requiredNodeSize,
                handlingMultiplier,
                creditCost,
                requiredTechnologyId);
        }
    }
}