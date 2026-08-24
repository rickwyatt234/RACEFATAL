using UnityEngine;
using RaceFatal.Vehicles;
using RaceFatal.Shared;

namespace RaceFatal.Content.Vehicles
{
    [CreateAssetMenu(fileName = "EngineDefinition", menuName = "RaceFatal/Vehicles/Engine")]
    public class EngineDefinitionSO : ScriptableObject
    {
        [Header("Engine Info")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Engine Class")]
        [SerializeField] private EngineClass engineClass;

        [Header("Performance Stats")]
        [Min(0)][SerializeField] private float topSpeed;
        [Min(0)][SerializeField] private float acceleration;

        [Header("Cost")]
        [Min(0)][SerializeField] private int creditCost;

        [Header("Technology Requirement")]
        [SerializeField] private string requiredTechnologyId;

        public EngineDefinition CreateEngineDefinition()
        {
            return new EngineDefinition(
                id,
                displayName,
                engineClass,
                topSpeed,
                acceleration,
                creditCost,
                requiredTechnologyId);
        }
    }
}

