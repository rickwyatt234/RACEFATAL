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

    static RaceEquipmentSystem Create(EquipmentActivationMode mode, float interval = .5f, int count = 1)
    {
        var db = new GameDatabase();
        var loadout = new BikeLoadout(count, 0, 0);
        for (int i = 0; i < count; i++)
        {
            var definition = new WeaponDefinition("weapon" + i, "Test", NodeSize.Small, mode,
                default, default, 100, 100, 10, 100, interval, .25f, 0, 0, null);
            db.AddEquipmentDefinition(definition);
            if (!loadout.InstallEquipment(new EquipmentFactory().Create(definition), NodeSize.Small, i).IsSuccess)
                throw new Exception("Install failed");
        }
        var result = RaceEquipmentSystem.Create("player", loadout, db, new EnergyPool(100));
        if (!result.IsSuccess) throw new Exception(result.ErrorMessage);
        result.Value.SelectNext();
        return result.Value;
    }

    static void Main()
    {
        var press = Create(EquipmentActivationMode.Press);
        int shots = 0;
        press.WeaponFired += _ => shots++;
        Check(press.BeginSelectedActivation(), "First press should fire immediately");
        for (int i = 0; i < 20; i++)
        {
            press.EndSelectedActivation();
            Check(!press.BeginSelectedActivation(), "Repeated click bypassed cooldown");
        }
        Check(shots == 1 && press.SelectedWeaponAmmo == 99, "Blocked clicks consumed ammo/published shots");
        press.Tick(.25f);
        Check(!press.BeginSelectedActivation(), "Partial recovery allowed a shot");
        press.Tick(.25f);
        Check(press.BeginSelectedActivation() && shots == 2, "Idle recovery did not unlock shot");
        press.CancelSelectedActivation();
        press.Tick(0);
        Check(!press.BeginSelectedActivation() && press.SelectedWeaponCooldownRemaining == .5f, "Cancel/pause reset cooldown");
        press.SetRaceActivationPermissions(true, false);
        press.SetRaceActivationPermissions(true, true);
        Check(!press.BeginSelectedActivation(), "Weapon permission toggle reset cooldown");

        var switched = Create(EquipmentActivationMode.Press, count: 2);
        string first = switched.SelectedEquipmentId;
        switched.BeginSelectedActivation();
        switched.SelectNext();
        Check(switched.SelectedEquipmentId != first && switched.BeginSelectedActivation(), "Different weapon should have its own interval");
        switched.SelectPrevious();
        Check(switched.SelectedEquipmentId == first && !switched.BeginSelectedActivation(), "Switch away/back reset cooldown");
        switched.SelectNext();
        switched.Tick(.5f);
        switched.SelectPrevious();
        Check(switched.BeginSelectedActivation(), "Unselected weapon cooldown did not recover");

        var hold = Create(EquipmentActivationMode.Hold);
        int heldShots = 0;
        hold.WeaponFired += _ => heldShots++;
        hold.BeginSelectedActivation(); hold.Tick(.05f);
        Check(heldShots == 1, "Hold first shot missing");
        Check(!hold.BeginSelectedActivation(), "Duplicate hold activation accepted");
        hold.EndSelectedActivation(); hold.BeginSelectedActivation(); hold.Tick(.01f);
        Check(heldShots == 1, "Release/repress accelerated held fire");
        hold.EndSelectedActivation(); hold.Tick(.5f);
        hold.BeginSelectedActivation(); hold.Tick(.01f);
        Check(heldShots == 2, "Released hold did not recover");
        foreach (float step in new[] { 1f, .125f })
        {
            var automatic = Create(EquipmentActivationMode.Hold, .25f);
            int count = 0; automatic.WeaponFired += _ => count++;
            automatic.BeginSelectedActivation();
            for (int i = 0; i < (int)(1f / step); i++) automatic.Tick(step);
            Check(count == 5, "Held cadence changed with tick size");
        }

        var charge = Create(EquipmentActivationMode.ChargeRelease);
        charge.BeginSelectedActivation(); charge.Tick(.125f);
        Check(!charge.BeginSelectedActivation() && charge.SelectedWeaponChargeTime == .125f, "Duplicate charge press reset charge");
        charge.Tick(.125f);
        Check(charge.EndSelectedActivation(), "Full charge failed to fire");
        Check(!charge.BeginSelectedActivation(), "Charged weapon rearmed during cooldown");
        Check(!charge.EndSelectedActivation() && charge.SelectedWeaponAmmo == 99, "Repeated release fired/consumed ammo");
        charge.Tick(.5f);
        Check(charge.BeginSelectedActivation(), "Charged weapon did not recover");
        charge.Tick(.125f);
        Check(!charge.EndSelectedActivation() && charge.SelectedWeaponCooldownRemaining == 0, "Aborted charge spent cooldown");
        charge.BeginSelectedActivation(); charge.Tick(.25f);
        Check(charge.EndSelectedActivation() && charge.SelectedWeaponAmmo == 98, "Next completed charge failed");

        var passive = Create(EquipmentActivationMode.Passive);
        passive.TryGetWeaponSnapshot(0, out var passiveSnapshot);
        string passiveId = passiveSnapshot.EquipmentId;
        Check(passive.TryFirePassiveWeapon(passiveId), "Passive first shot failed");
        Check(!passive.TryFirePassiveWeapon(passiveId), "Passive repeated shot bypassed interval");
        passive.Tick(.5f);
        Check(passive.TryFirePassiveWeapon(passiveId), "Passive interval was applied twice");

        var reentrant = Create(EquipmentActivationMode.Press);
        bool nested = true;
        reentrant.WeaponFired += _ => nested = reentrant.BeginSelectedActivation();
        reentrant.BeginSelectedActivation();
        Check(!nested && reentrant.SelectedWeaponAmmo == 99, "Shot event reentry bypassed cooldown");
        Console.WriteLine($"PASS: {checks} weapon cooldown checks.");
    }
}
