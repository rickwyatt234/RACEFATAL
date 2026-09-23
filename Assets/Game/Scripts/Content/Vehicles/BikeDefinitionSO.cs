using RaceFatal.Vehicles;
using UnityEngine;

namespace RaceFatal.Content.Vehicles
{
    [CreateAssetMenu(
        fileName = "BikeDefinition",
        menuName = "RaceFatal/Vehicles/Bike")]
    public class BikeDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Scene Content")]
        [SerializeField] private GameObject bikePrefab;

        [Header("Equipment Nodes")]
        [Min(0)][SerializeField] private int smallNodeCount;
        [Min(0)][SerializeField] private int mediumNodeCount;
        [Min(0)][SerializeField] private int largeNodeCount;

        [Header("Base Characteristics")]
        [Min(0f)][SerializeField] private float baseMass = 250f;
        [Min(0f)][SerializeField] private float baseHandling = 1f;

        [Header("Energy")]
        [Min(1f)][SerializeField] private float energyCapacity = 100f;

        [Header("Shop")]
        [Min(0)][SerializeField] private int creditCost;

        [SerializeField]
        private string requiredTechnologyId;

        public string Id => id;
        public string DisplayName => displayName;
        public GameObject BikePrefab => bikePrefab;

        public BikeDefinition CreateBikeDefinition()
        {
            return new BikeDefinition(
                id,
                displayName,
                smallNodeCount,
                mediumNodeCount,
                largeNodeCount,
                baseMass,
                baseHandling,
                energyCapacity,
                creditCost,
                requiredTechnologyId);
        }

        private void OnValidate()
        {
            smallNodeCount = Mathf.Max(0, smallNodeCount);
            mediumNodeCount = Mathf.Max(0, mediumNodeCount);
            largeNodeCount = Mathf.Max(0, largeNodeCount);

            baseMass = Mathf.Max(0f, baseMass);
            baseHandling = Mathf.Max(0f, baseHandling);
            energyCapacity = Mathf.Max(1f, energyCapacity);
            creditCost = Mathf.Max(0, creditCost);
        }
    }
}