using RaceFatal.Equipment;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public class BikeNode
    {
        public NodeSize NodeSize { get; }
        public int Index { get; }

        public EquipmentState InstalledEquipment { get; private set; }

        public bool IsOccupied =>
            InstalledEquipment != null;

        public BikeNode(
            NodeSize nodeSize,
            int index)
        {
            NodeSize = nodeSize;
            Index = index;
        }

        internal Result<EquipmentState> Install(
            EquipmentState equipment)
        {
            if (equipment == null)
            {
                return Result<EquipmentState>.Failure(
                    "Equipment is required.");
            }

            if (equipment.IsDestroyed)
            {
                return Result<EquipmentState>.Failure(
                    "Destroyed equipment cannot be installed.");
            }

            if (IsOccupied)
            {
                return Result<EquipmentState>.Failure(
                    "Node is already occupied.");
            }

            if (!NodeSizeRules.CanFit(
                    equipment.RequiredNodeSize,
                    NodeSize))
            {
                return Result<EquipmentState>.Failure(
                    $"Equipment requires at least a " +
                    $"{equipment.RequiredNodeSize} node, " +
                    $"but this node is {NodeSize}.");
            }

            InstalledEquipment =
                equipment;

            return Result<EquipmentState>.Success(
                equipment);
        }

        internal Result<EquipmentState> Remove()
        {
            if (!IsOccupied)
            {
                return Result<EquipmentState>.Failure(
                    "Node is not occupied.");
            }

            EquipmentState removedEquipment =
                InstalledEquipment;

            InstalledEquipment = null;

            return Result<EquipmentState>.Success(
                removedEquipment);
        }
    }
}