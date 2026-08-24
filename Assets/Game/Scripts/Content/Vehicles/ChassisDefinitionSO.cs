using UnityEngine;
using RaceFatal.Vehicles;

namespace RaceFatal.Content.Vehicles
{
    [CreateAssetMenu(fileName = "ChassisDefinition", menuName = "RaceFatal/Vehicles/Chassis")]
    public class ChassisDefinitionSO : ScriptableObject
    {
        [Header("Chassis Info")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Performance Stats")]
        [Min(0)][SerializeField] private float massModifier = 1f;
        [Min(0)][SerializeField] private float handlingModifier = 1f;

        [Header("Cost")]
        [Min(0)][SerializeField] private int creditCost;

        [Header("Technology Requirement")]
        [SerializeField] private string requiredTechnologyId;

        public ChassisDefinition CreateChassisDefinition()
        {
            return new ChassisDefinition(
                id,
                displayName,
                massModifier,
                handlingModifier,
                creditCost,
                requiredTechnologyId);
        }   
    } 
}
