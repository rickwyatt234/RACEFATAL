using System;
using RaceFatal.Data;
using RaceFatal.Energy;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

static class Program
{
    static int checks;
    static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    static RaceEquipmentSystem Equipment(EquipmentActivationMode mode, float chargeDuration = 1)
    {
        var database = new GameDatabase();
        var definition = new WeaponDefinition("weapon", "Test", NodeSize.Small, mode,
            default, default, 100, 100, 10, 100, .2f, chargeDuration, 0, 0, null);
        database.AddEquipmentDefinition(definition);
        var loadout = new BikeLoadout(1, 0, 0);
        Check(loadout.InstallEquipment(new EquipmentFactory().Create(definition), NodeSize.Small, 0).IsSuccess, "Install failed");
        var result = RaceEquipmentSystem.Create("player", loadout, database, new EnergyPool(100));
        Check(result.IsSuccess, "Equipment creation failed");
        result.Value.SelectNext();
        return result.Value;
    }

    static void Main()
    {
        var held = Equipment(EquipmentActivationMode.Hold);
        int shots = 0;
        held.WeaponFired += _ => shots++;
        held.BeginSelectedActivation();
        held.Tick(.05f);
        Check(shots == 1, "Held weapon must fire before pause");
        int ammo = held.SelectedWeaponAmmo;
        held.Tick(0);
        Check(held.SelectedWeaponAmmo == ammo, "Paused tick consumed ammunition");
        held.CancelSelectedActivation();
        held.CancelSelectedActivation();
        held.Tick(2);
        Check(shots == 1 && held.SelectedWeaponAmmo == ammo, "Held fire persisted after cancellation");
        held.BeginSelectedActivation();
        held.Tick(.05f);
        Check(shots == 2, "Fresh activation failed after resume");

        foreach (float duration in new[] { 0f, 1f })
        {
            var charged = Equipment(EquipmentActivationMode.ChargeRelease, duration);
            int chargedShots = 0;
            charged.WeaponFired += _ => chargedShots++;
            Check(!charged.EndSelectedActivation(), "Release without a press fired");
            charged.BeginSelectedActivation();
            charged.Tick(.5f);
            float chargeBeforePause = charged.SelectedWeaponChargeTime;
            charged.Tick(0);
            Check(charged.SelectedWeaponChargeTime == chargeBeforePause, "Charge advanced while frozen");
            charged.Tick(1);
            charged.CancelSelectedActivation();
            Check(!charged.SelectedWeaponIsCharging && charged.SelectedWeaponChargeTime == 0, "Charge was not canceled");
            Check(chargedShots == 0 && !charged.EndSelectedActivation(), "Cancel or delayed mouse release fired a shot");
            Check(charged.SelectedWeaponAmmo == 100, "Cancel consumed ammunition");
            charged.BeginSelectedActivation();
            charged.Tick(1);
            Check(charged.EndSelectedActivation() && chargedShots == 1, "Fresh charged shot failed after resume");
        }

        var empty = new RaceEquipmentSystem("empty", new EnergyPool(0));
        empty.CancelSelectedActivation();
        Check(!empty.EndSelectedActivation(), "Empty equipment should remain idle");
        Console.WriteLine($"Race pause equipment regressions passed ({checks} checks).");
    }
}
