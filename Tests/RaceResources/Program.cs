using System;
using RaceFatal.Data;
using RaceFatal.Energy;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

static class Program
{
    static int checks;
    static void Check(bool condition,string message) { checks++; if(!condition)throw new Exception(message); }
    static void Near(float actual,float expected,string message) => Check(Math.Abs(actual-expected)<0.001f,message+$" ({actual} != {expected})");
    static RaceEquipmentSystem Create(EnergyPool pool,EquipmentActivationMode mode=EquipmentActivationMode.Press)
    {
        var db=new GameDatabase();var loadout=new BikeLoadout(2,0,0);
        var weapon=new WeaponDefinition("weapon","Weapon",NodeSize.Small,mode,default,default,100,100,10,1,0.5f,0.25f,0,0,null,energyPerActivation:10);
        var boost=new BoosterDefinition("boost","Boost",NodeSize.Small,20,1.2f,1.4f,0,null,100,5);
        db.AddEquipmentDefinition(weapon);db.AddEquipmentDefinition(boost);
        var factory=new EquipmentFactory();
        Check(loadout.InstallEquipment(factory.Create(weapon),NodeSize.Small,0).IsSuccess,"weapon install");
        Check(loadout.InstallEquipment(factory.Create(boost),NodeSize.Small,1).IsSuccess,"boost install");
        var result=RaceEquipmentSystem.Create("player",loadout,db,pool);Check(result.IsSuccess,"equipment create");result.Value.SelectNext();return result.Value;
    }
    static void Main()
    {
        var pool=new EnergyPool(100);var system=Create(pool);
        Check(system.BeginSelectedActivation(),"shot succeeds");Near(pool.CurrentEnergy,90,"shot energy");
        Check(!system.BeginSelectedActivation(),"cooldown enforced");Near(pool.CurrentEnergy,90,"blocked shot free");
        system.Tick(0.5f);Check(system.BeginSelectedActivation(),"legacy startingAmmo does not limit shots");Near(pool.CurrentEnergy,80,"second shot energy");
        system.SetBoostActive(true);system.Tick(1f);Near(pool.CurrentEnergy,60,"boost spend");Near(pool.MaxEnergy,95,"boost strain");Near(pool.StartingCapacity,100,"initial capacity stable");
        system.SetBoostActive(false);system.Tick(10f);Near(pool.CurrentEnergy,60,"no passive recharge");Near(pool.MaxEnergy,95,"strain permanent");
        Near(pool.Recharge(1000),35,"strip limited by strained capacity");Near(pool.MaxEnergy,95,"strip cannot repair strain");
        pool.ApplyBoostStrain(1000);Near(pool.MaxEnergy,0,"no capacity floor");Near(pool.CurrentEnergy,0,"energy clamped");Near(pool.Recharge(1000),0,"exhausted capacity cannot recharge");Check(!system.SetBoostActive(true),"no boost at zero");
        Check(!system.BeginSelectedActivation(),"no weapon at zero");
        var shield=new RaceShieldState(new ShieldDefinition("shield","Shield",NodeSize.Small,100,40,2,0,null),pool);
        Near(shield.Maximum,100,"shield independent of zero capacity");Near(shield.AbsorbDamage(60),0,"shield protects");shield.Tick(1f);Near(shield.Current,40,"shield delay");shield.Tick(2f);Check(shield.Current>40,"free shield recovery");Near(pool.CurrentEnergy,0,"shield does not consume energy");
        var partial=new EnergyPool(100);var partialSystem=Create(partial);partial.SpendUpTo(95);partialSystem.SetBoostActive(true);partialSystem.Tick(1f);Near(partial.MaxEnergy,98.75f,"strain only for powered boost duration");partialSystem.Tick(1f);Near(partial.MaxEnergy,98.75f,"no strain without thrust");
        var heldPool=new EnergyPool(100);var held=Create(heldPool,EquipmentActivationMode.Hold);held.BeginSelectedActivation();held.Tick(10f);Near(heldPool.CurrentEnergy,0,"hold cannot overspend");
        Check(!heldPool.TrySpend(float.NaN),"reject NaN spend");Near(heldPool.Recharge(float.PositiveInfinity),0,"reject invalid recharge");
        var notified=new EnergyPool(100);var reentrant=Create(notified);bool nested=true;
        notified.OnEnergyChanged += (_,__) => nested=reentrant.BeginSelectedActivation();
        Check(reentrant.BeginSelectedActivation(),"outer activation succeeds");Check(!nested,"energy event cannot bypass cooldown");Near(notified.CurrentEnergy,90,"one spend during callback");
        var preciseShield=new RaceShieldState(new ShieldDefinition("precise","Shield",NodeSize.Small,100,40,2,0,null),new EnergyPool(0));
        preciseShield.AbsorbDamage(100);preciseShield.Tick(2.5f);Near(preciseShield.Current,20,"only recharge after delay crossing");
        var fresh=new EnergyPool(100);Near(fresh.MaxEnergy,100,"new race restores capacity");
        Console.WriteLine($"PASS: {checks} resource checks.");
    }
}
