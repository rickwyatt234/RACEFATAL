using System;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    [Serializable]
    public class BikeEquipmentMountBinding
    {
        [SerializeField] private NodeSize nodeSize;

        [Min(0)]
        [SerializeField] private int nodeIndex;

        [Tooltip("Root transform representing this physical equipment node.")]
        [SerializeField] private Transform mountRoot;

        [Tooltip("Optional separate origin used for firing/activation. Falls back to Mount Root.")]
        [SerializeField] private Transform equipmentOrigin;

        public NodeSize NodeSize => nodeSize;
        public int NodeIndex => nodeIndex;

        public Transform MountRoot =>
            mountRoot != null
                ? mountRoot
                : equipmentOrigin;

        public Transform EquipmentOrigin =>
            equipmentOrigin != null
                ? equipmentOrigin
                : mountRoot;

        public T GetComponentInChildren<T>(
            bool includeInactive = true)
            where T : Component
        {
            Transform root =
                MountRoot;

            if (root == null)
                return null;

            return root.GetComponentInChildren<T>(
                includeInactive);
        }
    }
}