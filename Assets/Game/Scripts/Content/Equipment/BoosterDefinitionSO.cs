using RaceFatal.Equipment;
using UnityEngine;

namespace RaceFatal.Content.Equipment
{
    [CreateAssetMenu(
        fileName = "BoosterDefinition",
        menuName = "RaceFatal/Equipment/Booster")]
    public sealed class BoosterDefinitionSO :
        EquipmentDefinitionSO
    {
        [Header("Booster")]
        [Min(0f)]
        [SerializeField]
        private float energyPerSecond;

        [Min(1f)]
        [SerializeField]
        private float speedMultiplier = 1f;

        [Min(1f)]
        [SerializeField]
        private float accelerationMultiplier = 1f;

        public override EquipmentDefinition
            CreateEquipmentDefinition()
        {
            return new BoosterDefinition(
                id,
                displayName,
                requiredNodeSize,
                energyPerSecond,
                speedMultiplier,
                accelerationMultiplier,
                creditCost,
                requiredTechnologyId);
        }
    }
}