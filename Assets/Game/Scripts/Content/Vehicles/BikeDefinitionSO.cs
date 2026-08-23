using UnityEngine;
using RaceFatal.Vehicles;

namespace RaceFatal.Content.Vehicles
{
[CreateAssetMenu(fileName = "BikeDefinition", menuName = "RaceFatal/Vehicles/Bike")]
    public class BikeDefinitionSO : ScriptableObject
    {
        [Header("Bike Info")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Nodes")]
        [Min(0)][SerializeField] private int smallNodeCount;
        [Min(0)][SerializeField] private int mediumNodeCount;
        [Min(0)][SerializeField] private int largeNodeCount;

        [Header("Base Stats")]
        [Min(0)][SerializeField] private float baseWeight = 250f;
        [Min(0)][SerializeField] private float baseHandling = 1f;

        public BikeDefinition CreateBikeDefinition()
        {
            return new BikeDefinition(
                id,
                displayName,
                smallNodeCount,
                mediumNodeCount,
                largeNodeCount,
                baseWeight,
                baseHandling);
        }
    }
}

