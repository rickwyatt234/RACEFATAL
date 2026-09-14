using RaceFatal.Equipment;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Content.Equipment
{
    [CreateAssetMenu(
        fileName = "CountermeasureDefinition",
        menuName = "RaceFatal/Equipment/Countermeasure")]
    public class CountermeasureDefinitionSO :
        EquipmentDefinitionSO
    {
        [Header("Countermeasure")]
        [SerializeField] private CountermeasureType countermeasureType;

        [Tooltip("Minimum time between automatic deployments.")]
        [Min(0f)][SerializeField] private float cooldown;

        [Tooltip("Number of deployments available during one race.")]
        [Min(1)][SerializeField] private int usesPerRace = 4;

        [Tooltip("Nearest incoming missile distance at which the countermeasure automatically deploys.")]
        [Min(0.1f)][SerializeField] private float triggerDistance = 25f;

        [Tooltip("All incoming missiles targeting this racer within this radius are defeated by one deployment.")]
        [Min(0.1f)][SerializeField] private float defeatRadius = 35f;

        public override EquipmentDefinition
            CreateEquipmentDefinition()
        {
            return new CountermeasureDefinition(
                id,
                displayName,
                requiredNodeSize,
                countermeasureType,
                cooldown,
                usesPerRace,
                triggerDistance,
                defeatRadius,
                creditCost,
                requiredTechnologyId);
        }
    }
}