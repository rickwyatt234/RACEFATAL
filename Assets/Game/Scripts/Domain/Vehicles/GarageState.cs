using System.Collections.Generic;
using RaceFatal.Shared;
using RaceFatal.Equipment;

namespace RaceFatal.Vehicles
{
    public class GarageState
    {
        private readonly List<BikeState> bikes = new List<BikeState>();
        private readonly List<EngineState> engines = new List<EngineState>();
        private readonly List<ChassisState> chassis = new List<ChassisState>();
        private readonly List<EquipmentState> equipment = new List<EquipmentState>();
        public IReadOnlyList<BikeState> Bikes => bikes;
        public IReadOnlyList<EngineState> Engines => engines;
        public IReadOnlyList<ChassisState> Chassis => chassis;
        public IReadOnlyList<EquipmentState> Equipment => equipment;

#region Add Items to Garage
        public Result<BikeState> AddBike(BikeState bike)
        {
            if (bikes.Exists(b => b.BikeId == bike.BikeId))
            {
                return Result<BikeState>.Failure($"Bike with ID {bike.BikeId} already exists in the garage.");
            }

            bikes.Add(bike);
            return Result<BikeState>.Success(bike);
        }

        public Result<EngineState> AddEngine(EngineState engine)
        {
            if (engines.Exists(e => e.EngineId == engine.EngineId))
            {
                return Result<EngineState>.Failure($"Engine with ID {engine.EngineId} already exists in the garage.");
            }

            engines.Add(engine);
            return Result<EngineState>.Success(engine);
        }

        public Result<ChassisState> AddChassis(ChassisState chassis)
        {
            if (this.chassis.Exists(c => c.ChassisId == chassis.ChassisId))
            {
                return Result<ChassisState>.Failure($"Chassis with ID {chassis.ChassisId} already exists in the garage.");
            }

            this.chassis.Add(chassis);
            return Result<ChassisState>.Success(chassis);
        }

        public Result<EquipmentState> AddEquipment(EquipmentState equipment)
        {
            if (this.equipment.Exists(e => e.EquipmentId == equipment.EquipmentId))
            {
                return Result<EquipmentState>.Failure($"Equipment with ID {equipment.EquipmentId} already exists in the garage.");
            }

            this.equipment.Add(equipment);
            return Result<EquipmentState>.Success(equipment);
        }
#endregion

#region Engine Installation and Removal
        public Result<EngineState> InstallEngine(string bikeId, string engineId)
        {
            var bike = bikes.Find(b => b.BikeId == bikeId);
            if (bike == null)
            {
                return Result<EngineState>.Failure($"Bike with ID {bikeId} not found.");
            }

            var engine = engines.Find(e => e.EngineId == engineId);
            if (engine == null)
            {
                return Result<EngineState>.Failure($"Engine with ID {engineId} not found.");
            }

            return bike.Loadout.InstallEngine(engine);
        }
        public Result<EngineState> RemoveEngine(string bikeId)
        {
            var bike = bikes.Find(b => b.BikeId == bikeId);
            if (bike == null)
            {
                return Result<EngineState>.Failure($"Bike with ID {bikeId} not found.");
            }

            return bike.Loadout.RemoveEngine();
        }
#endregion

#region Chassis Installation and Removal
        public Result<ChassisState> InstallChassis(string bikeId, string chassisId)
        {
            var bike = bikes.Find(b => b.BikeId == bikeId);
            if (bike == null)
            {
                return Result<ChassisState>.Failure($"Bike with ID {bikeId} not found.");
            }

            var chassis = this.chassis.Find(c => c.ChassisId == chassisId);
            if (chassis == null)
            {
                return Result<ChassisState>.Failure($"Chassis with ID {chassisId} not found.");
            }

            return bike.Loadout.InstallChassis(chassis);
        }
        public Result<ChassisState> RemoveChassis(string bikeId)
        {
            var bike = bikes.Find(b => b.BikeId == bikeId);
            if (bike == null)
            {
                return Result<ChassisState>.Failure($"Bike with ID {bikeId} not found.");
            }

            return bike.Loadout.RemoveChassis();
        }
#endregion

#region Equipment Installation and Removal
        public Result<EquipmentState> InstallEquipment(string bikeId, string equipmentId, NodeSize nodeSize, int index)
        {
            var bike = bikes.Find(b => b.BikeId == bikeId);
            if (bike == null)
            {
                return Result<EquipmentState>.Failure($"Bike with ID {bikeId} not found.");
            }

            var equipment = this.equipment.Find(e => e.EquipmentId == equipmentId);
            if (equipment == null)
            {
                return Result<EquipmentState>.Failure($"Equipment with ID {equipmentId} not found.");
            }

            return bike.Loadout.InstallEquipment(equipment, nodeSize, index);
        }
        public Result<EquipmentState> RemoveEquipment(string bikeId, NodeSize nodeSize, int index)
        {
            var bike = bikes.Find(b => b.BikeId == bikeId);
            if (bike == null)
            {
                return Result<EquipmentState>.Failure($"Bike with ID {bikeId} not found.");
            }

            return bike.Loadout.RemoveEquipment(nodeSize, index);
        }
#endregion 
    
public Result<BikeState> DestroyBike(string bikeId)
        {
            var bike = bikes.Find(b => b.BikeId == bikeId);
            if (bike == null)
            {
                return Result<BikeState>.Failure($"Bike with ID {bikeId} not found.");
            }

            bike.Destroy();
            return Result<BikeState>.Success(bike);
        }

public bool HasRaceReadyBikeFor(EngineClass engineClass)
        {
            foreach (var bike in bikes)
            {
                if (!bike.IsDestroyed && bike.EngineClass == engineClass && bike.IsRaceReady)
                {
                    return true;
                }
            }
            return false;
        }

private bool IsEquipmentInstalled(string bikeId, string equipmentId)
        {
            var bike = bikes.Find(b => b.BikeId == bikeId);
            if (bike == null)
            {
                return false;
            }

            return bike.Loadout.HasEquipment(equipmentId);
        }
    }
}
