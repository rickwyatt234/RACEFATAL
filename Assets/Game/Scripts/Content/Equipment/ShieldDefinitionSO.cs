using RaceFatal.Equipment;
using UnityEngine;

namespace RaceFatal.Content.Equipment
{
    [CreateAssetMenu(
        fileName = "ShieldDefinition",
        menuName = "RaceFatal/Equipment/Shield")]
    public sealed class ShieldDefinitionSO : EquipmentDefinitionSO
    {
        [Header("Shield")]
        [Min(0f)]
        [SerializeField]
        private float capacity;

        [Min(0f)]
        [SerializeField]
        private float rechargePerSecond;

        [Min(0f)]
        [SerializeField]
        private float rechargeDelay;

        public override EquipmentDefinition
            CreateEquipmentDefinition()
        {
            return new ShieldDefinition(
                id,
                displayName,
                requiredNodeSize,
                capacity,
                rechargePerSecond,
                rechargeDelay,
                creditCost,
                requiredTechnologyId);
        }
    }
}