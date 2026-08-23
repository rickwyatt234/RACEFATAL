using RaceFatal.Shared;
using RaceFatal.Equipment;

namespace RaceFatal.Vehicles
{
    public class BikeNode
    {
        public NodeSize NodeSize { get; }
        public int Index { get; }

        public EquipmentState InstalledEquipment { get; private set; }

        public bool IsOccupied => InstalledEquipment != null;

        public BikeNode(NodeSize nodeSize, int index)
        {
            NodeSize = nodeSize;
            Index = index;
        }

        internal Result<EquipmentState> Install(EquipmentState equipment)
        {
            if (IsOccupied)
            {
                return Result<EquipmentState>.Failure("Node is already occupied.");
            }

            if (equipment.RequiredNodeSize != NodeSize)
            {
                return Result<EquipmentState>.Failure($"Equipment requires a {equipment.RequiredNodeSize} node, but this node is {NodeSize}.");
            }

            InstalledEquipment = equipment;
            return Result<EquipmentState>.Success(equipment);
        }

        internal Result<EquipmentState> Remove()
        {
            if (!IsOccupied)
            {
                return Result<EquipmentState>.Failure("Node is not occupied.");
            }

            var removedEquipment = InstalledEquipment;
            InstalledEquipment = null;
            return Result<EquipmentState>.Success(removedEquipment);
        }
    }
}
