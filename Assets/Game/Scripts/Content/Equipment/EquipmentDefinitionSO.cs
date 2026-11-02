using UnityEngine;
using RaceFatal.Equipment;
using RaceFatal.Shared;

namespace RaceFatal.Content.Equipment
{
    public abstract class EquipmentDefinitionSO : ScriptableObject
    {
        [Header("Equipment Info")] [SerializeField] protected string id;
        public string Id => id;

        [SerializeField] protected string displayName;
        [Header("Requirements")] [SerializeField] protected NodeSize requiredNodeSize;
        [Header("Cost")] [Min(0)] [SerializeField] protected int creditCost;
        public abstract EquipmentDefinition CreateEquipmentDefinition();
    }
}
