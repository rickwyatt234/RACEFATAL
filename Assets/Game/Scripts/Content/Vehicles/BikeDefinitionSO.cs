using RaceFatal.Vehicles;
using UnityEngine;

namespace RaceFatal.Content.Vehicles
{
    [CreateAssetMenu(fileName = "BikeDefinition", menuName = "RaceFatal/Vehicles/Bike")]
    public class BikeDefinitionSO : ScriptableObject
    {
        [Header("Identity")] [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Header("Scene Content")] [SerializeField] private GameObject bikePrefab;
        [SerializeField] private Sprite shopPreview;
        public Sprite ShopPreview => shopPreview;

        [Header("Equipment Nodes")] [Min(0)] [SerializeField] private int smallNodeCount;
        [Min(0)] [SerializeField] private int mediumNodeCount;
        [Min(0)] [SerializeField] private int largeNodeCount;
        [Header("Platform")] [SerializeField] private BikeWeightClass weightClass = BikeWeightClass.Medium;
        [Range(0f, .99f)] [SerializeField] private float speedPenalty;
        [Range(0f, .99f)] [SerializeField] private float accelerationPenalty;
        [Range(0f, .99f)] [SerializeField] private float integrityPenalty;
        [Range(0f, .99f)] [SerializeField] private float handlingPenalty;
        public string Id => id;
        public string DisplayName => displayName;
        public GameObject BikePrefab => bikePrefab;

        public BikeDefinition CreateBikeDefinition()
        {
            return new BikeDefinition(id, displayName, smallNodeCount, mediumNodeCount, largeNodeCount,
                weightClass, speedPenalty, accelerationPenalty, integrityPenalty, handlingPenalty);
        }

        private void OnValidate()
        {
            smallNodeCount = Mathf.Max(0, smallNodeCount);
            mediumNodeCount = Mathf.Max(0, mediumNodeCount);
            largeNodeCount = Mathf.Max(0, largeNodeCount);
        }
    }
}
