using System;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public class EquipmentMountDefinition
    {
        public string EquipmentDefinitionId { get; }

        public NodeSize NodeSize { get; }

        public int NodeIndex { get; }

        public EquipmentMountDefinition(
            string equipmentDefinitionId,
            NodeSize nodeSize,
            int nodeIndex)
        {
            EquipmentDefinitionId =
                equipmentDefinitionId
                ?? throw new ArgumentNullException(
                    nameof(equipmentDefinitionId));

            if (nodeIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nodeIndex));
            }

            NodeSize = nodeSize;
            NodeIndex = nodeIndex;
        }
    }
}