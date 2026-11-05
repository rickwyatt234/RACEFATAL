using RaceFatal.Equipment;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Content.Equipment
{
    [CreateAssetMenu(fileName = "RaceUtility", menuName = "RaceFatal/Equipment/Race Utility")]
    public sealed class RaceUtilityDefinitionSO : EquipmentDefinitionSO
    {
        [SerializeField] private UtilityEffect effect;
        [Min(0f)] [SerializeField] private float strength;
        [SerializeField] private EquipmentActivationMode activation;

        public override EquipmentDefinition CreateEquipmentDefinition()
        {
            return new RaceUtilityDefinition(id, displayName, requiredNodeSize, creditCost, effect, strength, activation);
        }
    }
}
