using RaceFatal.Equipment;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Content.Equipment
{
    [CreateAssetMenu(
        fileName = "CountermeasureDefinition",
        menuName = "RaceFatal/Equipment/Countermeasure")]
    public sealed class CountermeasureDefinitionSO :
        EquipmentDefinitionSO
    {
        [Header("Countermeasure")]
        [SerializeField]
        private CountermeasureType countermeasureType;

        [Min(0f)]
        [SerializeField]
        private float cooldown;

        public override EquipmentDefinition
            CreateEquipmentDefinition()
        {
            return new CountermeasureDefinition(
                id,
                displayName,
                requiredNodeSize,
                countermeasureType,
                cooldown,
                creditCost,
                requiredTechnologyId);
        }
    }
}