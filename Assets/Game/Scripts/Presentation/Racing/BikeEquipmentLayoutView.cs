using System.Collections.Generic;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    public class BikeEquipmentLayoutView : MonoBehaviour
    {
        [Header("Physical Equipment Nodes")]
        [SerializeField] private List<BikeEquipmentMountBinding> mounts =
            new List<BikeEquipmentMountBinding>();

        public IReadOnlyList<BikeEquipmentMountBinding> Mounts =>
            mounts;

        public bool TryGetMount(
            NodeSize nodeSize,
            int nodeIndex,
            out BikeEquipmentMountBinding mount)
        {
            for (int i = 0;
                 i < mounts.Count;
                 i++)
            {
                BikeEquipmentMountBinding candidate =
                    mounts[i];

                if (candidate == null)
                    continue;

                if (candidate.NodeSize !=
                    nodeSize)
                {
                    continue;
                }

                if (candidate.NodeIndex !=
                    nodeIndex)
                {
                    continue;
                }

                mount = candidate;
                return true;
            }

            mount = null;
            return false;
        }

        public bool ContainsNode(
            NodeSize nodeSize,
            int nodeIndex)
        {
            return TryGetMount(
                nodeSize,
                nodeIndex,
                out _);
        }

        private void OnValidate()
        {
            for (int i = 0;
                 i < mounts.Count;
                 i++)
            {
                BikeEquipmentMountBinding first =
                    mounts[i];

                if (first == null)
                    continue;

                for (int j = i + 1;
                     j < mounts.Count;
                     j++)
                {
                    BikeEquipmentMountBinding second =
                        mounts[j];

                    if (second == null)
                        continue;

                    if (first.NodeSize !=
                            second.NodeSize ||
                        first.NodeIndex !=
                            second.NodeIndex)
                    {
                        continue;
                    }

                    Debug.LogWarning(
                        $"{nameof(BikeEquipmentLayoutView)} on '{name}' " +
                        $"contains duplicate node " +
                        $"{first.NodeSize} {first.NodeIndex}.",
                        this);
                }

                if (first.EquipmentOrigin == null)
                {
                    Debug.LogWarning(
                        $"{nameof(BikeEquipmentLayoutView)} on '{name}' " +
                        $"has no Transform assigned for node " +
                        $"{first.NodeSize} {first.NodeIndex}.",
                        this);
                }
            }
        }
    }
}