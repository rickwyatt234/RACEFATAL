using System;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;
using UnityEditor;
using UnityEngine;

// Dependency-free domain checks: run from the menu or Unity -executeMethod.
public static class CareerShopValidation
{
    [MenuItem("RACE//FATAL/Career/Validate Shop Purchases")]
    public static void Run()
    {
        var database = new GameDatabase();
        database.AddEngineDefinition(new EngineDefinition("engine", "Engine", default, 100, 10, 100, "tech"));
        database.AddChassisDefinition(new ChassisDefinition("chassis", "Chassis", 1, 1, 50, null));
        database.AddChassisDefinition(new ChassisDefinition("invalid", "Invalid", 1, 1, -1, null));
        database.AddEquipmentDefinition(new ShieldDefinition("shield", "Shield", default, 100, 10, 2, 25, null));
        database.AddEquipmentDefinition(new ShieldDefinition("free", "Free", default, 100, 10, 2, 0, null));
        var shop = new ShopService(database);
        var team = new TeamState("test", "Test", "#ffffff", "#000000");
        team.AddCredits(250);

        Require(!shop.Purchase(null, ShopItemKind.Engine, "engine").IsSuccess, "Missing team rejected");
        Require(!shop.Purchase(team, ShopItemKind.Engine, null).IsSuccess, "Missing ID rejected");
        Require(!shop.Purchase(team, ShopItemKind.Engine, "unknown").IsSuccess, "Unknown ID rejected");
        Require(!shop.Purchase(team, ShopItemKind.Engine, "engine").IsSuccess, "Technology lock enforced");
        Require(!shop.Purchase(team, ShopItemKind.Chassis, "invalid").IsSuccess, "Negative price rejected");
        Require(team.Credits == 250 && team.Garage.Engines.Count == 0 && team.Garage.Chassis.Count == 0,
            "Failed purchases leave funds and inventory unchanged");

        team.UnlockTechnology("tech");
        Require(shop.Purchase(team, ShopItemKind.Engine, "engine").IsSuccess, "Unlocked engine purchased");
        Require(shop.Purchase(team, ShopItemKind.Engine, "engine").IsSuccess, "Second instance purchased");
        Require(team.Garage.Engines.Count == 2 && team.Credits == 50, "Exact engine debits");
        Require(team.Garage.Engines[0].EngineId != team.Garage.Engines[1].EngineId, "Unique ownership IDs");
        Require(!shop.Purchase(team, ShopItemKind.Engine, "engine").IsSuccess, "Insufficient funds rejected");
        Require(team.Credits == 50 && team.Garage.Engines.Count == 2, "Rejected purchase leaves inventory intact");
        Require(shop.Purchase(team, ShopItemKind.Equipment, "shield").IsSuccess, "Equipment purchased");
        Require(team.Credits == 25 && team.Garage.Equipment.Count == 1, "Equipment debit and ownership");
        team.AddCredits(25);
        Require(shop.Purchase(team, ShopItemKind.Chassis, "chassis").IsSuccess, "Exact-balance chassis purchase");
        Require(team.Credits == 0 && team.Garage.Chassis.Count == 1, "Exact balance reaches zero");
        Require(shop.Purchase(team, ShopItemKind.Equipment, "free").IsSuccess, "Configured zero-cost item supported");
        Require(team.Credits == 0 && team.Garage.Equipment.Count == 2, "Zero-cost item changes only inventory");
        Require(!team.Garage.IsEquipmentInstalled(team.Garage.Equipment[0].EquipmentId), "Purchase does not auto-install");
        Debug.Log("Shop purchase validation passed: locks, funds, item kinds, duplicate instances and zero/invalid prices.");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Shop validation failed: " + description);
    }
}
