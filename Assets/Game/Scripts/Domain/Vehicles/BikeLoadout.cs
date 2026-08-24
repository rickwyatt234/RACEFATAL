using System.Collections.Generic;
using RaceFatal.Equipment;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public class BikeLoadout
    {
        private readonly List<BikeNode> nodes = new List<BikeNode>();
        public EngineState Engine { get; private set; }
        public ChassisState Chassis { get; private set; }
        public IReadOnlyList<BikeNode> Nodes => nodes;

        public BikeLoadout(
            int smallNodes,
            int mediumNodes,
            int largeNodes)
        {
            CreateNodes(NodeSize.Small, smallNodes);
            CreateNodes(NodeSize.Medium, mediumNodes);
            CreateNodes(NodeSize.Large, largeNodes);
        }

        private void CreateNodes(NodeSize size, int count)
        {
            for (int i = 0; i < count; i++)
            {
                nodes.Add(new BikeNode(size, i));
            }
        }

        public Result<EngineState> InstallEngine(EngineState engine)
        {
            if (Engine.IsDestroyed)
            {
                return Result<EngineState>.Failure("Cannot install a destroyed engine.");
            }
            Engine = engine;
            return Result<EngineState>.Success(engine);
        }
        public Result<EngineState> RemoveEngine()
        {
            if (Engine == null)
            {
                return Result<EngineState>.Failure("No engine installed to remove.");
            }
            var removedEngine = Engine;
            Engine = null;
            return Result<EngineState>.Success(removedEngine);
        }

        public Result<ChassisState> InstallChassis(ChassisState chassis)
        {
            if (Chassis.IsDestroyed)
            {
                return Result<ChassisState>.Failure("Cannot install a destroyed chassis.");
            }
            Chassis = chassis;
            return Result<ChassisState>.Success(chassis);
        }
        public Result<ChassisState> RemoveChassis()
        {
            if (Chassis == null)
            {
                return Result<ChassisState>.Failure("No chassis installed to remove.");
            }
            var removedChassis = Chassis;
            Chassis = null;
            return Result<ChassisState>.Success(removedChassis);
        }

        public Result<EquipmentState> InstallEquipment(EquipmentState equipment, NodeSize nodeSize, int index)
        {
            if (equipment.Category == EquipmentCategory.Shield && ContainsCategory(EquipmentCategory.Shield))
            {
                return Result<EquipmentState>.Failure("Cannot install more than one shield.");
            }
            BikeNode node = FindNode(nodeSize, index);
            if (node == null)
            {
                return Result<EquipmentState>.Failure($"No node found with size {nodeSize} and index {index}.");
            }
            return node.Install(equipment);
        }

        public Result<EquipmentState> RemoveEquipment(NodeSize nodeSize, int index)
        {
            BikeNode node = FindNode(nodeSize, index);
            if (node == null)
            {
                return Result<EquipmentState>.Failure($"No node found with size {nodeSize} and index {index}.");
            }
            return node.Remove();
        }

        public bool HasEquipment(string equipmentId)
        {
            foreach (var node in nodes)
            {
                if (node.InstalledEquipment != null && node.InstalledEquipment.EquipmentId == equipmentId)
                {
                    return true;
                }
            }
            return false;
        }

        public BikeNode FindNode(NodeSize nodeSize, int index)
        {
            foreach (var node in nodes)
            {
                if (node.NodeSize == nodeSize && node.Index == index)
                {
                    return node;
                }
            }
            return null;
        }

        internal void DestroyInstalledEquipment()
        {
            foreach (var node in nodes)
            {
                if (node.InstalledEquipment != null)
                {
                    node.InstalledEquipment.Destroy();
                }
            }
        }

        public bool ContainsCategory(EquipmentCategory category)
        {
            foreach (BikeNode node in nodes)
            {
                if (!node.IsOccupied)
                {
                    continue;
                }
                if (node.InstalledEquipment.Category == category)
                {
                    return true;
                }
            }
            return false;
        }
    }
}