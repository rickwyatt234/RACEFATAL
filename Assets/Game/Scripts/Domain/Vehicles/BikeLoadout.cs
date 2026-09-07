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

        public Result InstallEngine(
            EngineState engine)
        {
            if (engine == null)
            {
                return Result.Failure(
                    "Engine is required.");
            }

            if (engine.IsDestroyed)
            {
                return Result.Failure(
                    "A destroyed engine cannot be installed.");
            }

            if (Engine != null)
            {
                if (Engine.EngineId ==
                    engine.EngineId)
                {
                    return Result.Failure(
                        "This engine is already installed.");
                }

                return Result.Failure(
                    "This bike already has an engine installed.");
            }

            Engine = engine;

            return Result.Success();
        }
        public Result<EngineState>
            RemoveEngine()
        {
            if (Engine == null)
            {
                return Result<EngineState>.Failure(
                    "No engine is installed.");
            }

            EngineState removed =
                Engine;

            Engine = null;

            return Result<EngineState>.Success(
                removed);
        }

        public Result InstallChassis(
            ChassisState chassis)
        {
            if (chassis == null)
            {
                return Result.Failure(
                    "Chassis is required.");
            }

            if (chassis.IsDestroyed)
            {
                return Result.Failure(
                    "A destroyed chassis cannot be installed.");
            }

            if (Chassis != null)
            {
                if (Chassis.ChassisId ==
                    chassis.ChassisId)
                {
                    return Result.Failure(
                        "This chassis is already installed.");
                }

                return Result.Failure(
                    "This bike already has a chassis installed.");
            }

            Chassis = chassis;

            return Result.Success();
        }
        public Result<ChassisState>
            RemoveChassis()
        {
            if (Chassis == null)
            {
                return Result<ChassisState>.Failure(
                    "No chassis is installed.");
            }

            ChassisState removed =
                Chassis;

            Chassis = null;

            return Result<ChassisState>.Success(
                removed);
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