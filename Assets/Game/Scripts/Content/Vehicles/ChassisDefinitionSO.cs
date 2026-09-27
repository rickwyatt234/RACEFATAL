using UnityEngine;
using RaceFatal.Vehicles;

namespace RaceFatal.Content.Vehicles
{
    [CreateAssetMenu(fileName = "ChassisDefinition", menuName = "RaceFatal/Vehicles/Chassis")]
    public class ChassisDefinitionSO : ScriptableObject
    {
        [Header("Chassis Info")]
        [SerializeField] private string id;
        public string Id => id;
        [SerializeField] private string displayName;

        [Header("Performance Stats")]
        [Min(1f)][SerializeField] private float mass = 250f;
        [Min(1f)][SerializeField] private float maxIntegrity = 100f;
        [Min(0.1f)][SerializeField] private float handlingMultiplier = 1f;
        [Min(0.1f)][SerializeField] private float steeringResponseMultiplier = 1f;
        [Min(0.1f)][SerializeField] private float leanResponseMultiplier = 1f;
        [Min(0.1f)][SerializeField] private float stabilityMultiplier = 1f;
        [Tooltip("Fraction of structural collision damage prevented after shields. 0.2 = 20%. Does not prevent fatal wall impacts.")]
        [Range(0f, 0.9f)][SerializeField] private float impactResistance;

        [Header("Cost")]
        [Min(0)][SerializeField] private int creditCost;

        [Header("Technology Requirement")]
        [SerializeField] private string requiredTechnologyId;

        public ChassisDefinition CreateChassisDefinition()
        {
            return new ChassisDefinition(
                id,
                displayName,
                mass,
                handlingMultiplier,
                creditCost,
                requiredTechnologyId, maxIntegrity, steeringResponseMultiplier,
                leanResponseMultiplier, stabilityMultiplier, impactResistance);
        }

    } 
}
