using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    public class BikeEquipmentMountView : MonoBehaviour
    {
        [SerializeField] private NodeSize nodeSize;
        [SerializeField] private int nodeIndex;

        [Tooltip("The origin used when firing or activating equipment.")]
        [SerializeField] private Transform equipmentOrigin;

        public NodeSize NodeSize => nodeSize;
        public int NodeIndex => nodeIndex;
        public Transform EquipmentOrigin => equipmentOrigin != null ? equipmentOrigin : transform;
    }
}