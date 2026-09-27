using System;
using RaceFatal.Career;
using RaceFatal.Combat;
using RaceFatal.Data;
using RaceFatal.Energy;
using RaceFatal.Equipment;
using RaceFatal.Racing;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

static class Program
{
    static int checks;
    static void Equal(float actual, float expected, string message)
    {
        checks++;
        if (Math.Abs(actual - expected) > .0001f || float.IsNaN(actual))
            throw new Exception($"{message}: expected {expected}, got {actual}");
    }
    static T Ok<T>(Result<T> result)
    {
        if (!result.IsSuccess) throw new Exception(result.ErrorMessage);
        return result.Value;
    }
    static void Main()
    {
        var db = new GameDatabase();
        var platform = new BikeDefinition("bike", "Bike", 4, 0, 0);
        var chassis = new ChassisDefinition("chassis", "Heavy", 350, .9f, 0, null,
            maxIntegrity: 150, steeringResponseMultiplier: .95f,
            leanResponseMultiplier: .9f, stabilityMultiplier: 1.1f, impactResistance: .2f);
        var engine = new EngineDefinition("engine", "Engine", default, 100, 10, 0, null);
        var booster = new BoosterDefinition("booster", "Booster", NodeSize.Small, 5, 1.2f, 1.3f, 0, null, 80);
        var gyro = new HandlingUtilityDefinition("gyro", "Gyro", NodeSize.Small, 1.1f, 0, null);
        var shield = new ShieldDefinition("shield", "Shield", NodeSize.Small, 50, 10, 1, 0, null);
        db.AddBikeDefinition(platform); db.AddChassisDefinition(chassis); db.AddEngineDefinition(engine);
        db.AddEquipmentDefinition(booster); db.AddEquipmentDefinition(gyro); db.AddEquipmentDefinition(shield);
        var factory = new VehicleFactory();
        var bike = factory.CreateBike(platform, "white", "black");
        bike.Loadout.InstallEngine(factory.CreateEngine(engine));
        bike.Loadout.InstallChassis(factory.CreateChassis(chassis));
        var calculator = new BikePerformanceCalculator(db);
        var racer = new RacerState("racer", "Racer", "team", true);
        var participants = new RaceParticipantFactory(db, calculator);
        var empty = Ok(participants.Create(racer, bike, RaceParticipantRole.Player)).Vehicle;
        Equal(empty.EnergyPool.MaxEnergy, 0, "No booster means no base energy");
        Equal(empty.EnergyPool.Recharge(100), 0, "Zero capacity cannot recharge");
        Equal(empty.Damage.MaxIntegrity, 150, "Chassis integrity reaches race");
        Equal(empty.Performance.Mass, 350, "Absolute chassis mass");
        Equal(empty.Performance.SteeringResponseMultiplier, .95f, "Steering response");
        Equal(empty.Performance.LeanResponseMultiplier, .9f, "Lean response");
        Equal(empty.Performance.StabilityMultiplier, 1.1f, "Stability");
        var equipmentFactory = new EquipmentFactory();
        Ok(bike.Loadout.InstallEquipment(equipmentFactory.Create(booster), NodeSize.Small, 0));
        Ok(bike.Loadout.InstallEquipment(equipmentFactory.Create(booster), NodeSize.Small, 1));
        Ok(bike.Loadout.InstallEquipment(equipmentFactory.Create(gyro), NodeSize.Small, 2));
        Ok(bike.Loadout.InstallEquipment(equipmentFactory.Create(shield), NodeSize.Small, 3));
        var vehicle = Ok(participants.Create(racer, bike, RaceParticipantRole.Player)).Vehicle;
        Equal(vehicle.EnergyPool.MaxEnergy, 160, "Multiple booster capacities sum");
        Equal(vehicle.Performance.BaseHandling, .9f, "Chassis handling");
        Equal(vehicle.Performance.ConfiguredHandling, .99f, "Passive handling applies once");
        Equal(vehicle.RuntimeHandling, .99f, "Runtime does not repeat passive handling");
        vehicle.TemporaryHandlingMultiplier = .5f;
        Equal(vehicle.RuntimeHandling, .495f, "Temporary handling applies once");
        Equal(vehicle.Performance.ConfiguredHandling, .99f, "Temporary effects do not mutate configured handling");
        var hit = vehicle.ApplyDamage(100, DamageCause.Collision);
        Equal(hit.ShieldAbsorbed, 50, "Impact resistance does not protect shields");
        Equal(hit.BikeDamage, 40, "Resistance reduces remaining structural collision damage");
        Equal(vehicle.Damage.CurrentIntegrity, 110, "Remaining integrity");
        Equal(vehicle.Damage.DamageRatio, 40f / 150f, "HUD ratio scales with integrity");
        Equal(vehicle.ApplyDamage(20, DamageCause.Weapon).BikeDamage, 20, "Weapons bypass impact resistance");
        Equal(vehicle.ApplyDamage(20, DamageCause.Environmental).BikeDamage, 20, "Hazards bypass impact resistance");
        var lethal = vehicle.ApplyDamage(10000, DamageCause.Environmental);
        Equal(vehicle.Damage.CurrentIntegrity, 0, "Lethal damage clamps to zero integrity");
        Equal(vehicle.Damage.Percent, 100, "Destroyed HUD percentage");
        if (!lethal.CausedDestruction) throw new Exception("Missing destruction transition");
        Equal(vehicle.ApplyDamage(100, DamageCause.Weapon).BikeDamage, 0, "No repeated structural damage after destruction");
        db.AddRacerPerkDefinition(new RacerPerkDefinition("energy-perk", "Reserve", "", 0,
            RacerPerkEffect.EnergyCapacity, 15));
        racer.Progression.TryPurchasePerk("energy-perk", 0);
        Equal(Ok(participants.Create(racer, bike, RaceParticipantRole.Player)).Vehicle.EnergyPool.MaxEnergy,
            175, "Energy perk adds to booster capacity");
        bike.Loadout.RemoveEquipment(NodeSize.Small, 0);
        bike.Loadout.RemoveEquipment(NodeSize.Small, 1);
        Equal(Ok(participants.Create(racer, bike, RaceParticipantRole.Player)).Vehicle.EnergyPool.MaxEnergy,
            15, "Perk remains an independent bonus without a booster");
        var missing = factory.CreateBike(platform, "white", "black");
        if (participants.Create(racer, missing, RaceParticipantRole.Player).IsSuccess)
            throw new Exception("Missing engine/chassis should return failure");
        Console.WriteLine($"PASS: {checks} vehicle stat assertions and failure/destruction checks.");
    }
}
