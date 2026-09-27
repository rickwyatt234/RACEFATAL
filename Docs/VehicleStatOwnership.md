# Vehicle stat ownership

| Definition | Authored values |
| --- | --- |
| Bike | Identity, prefab/presentation and equipment nodes |
| Chassis | Absolute mass (kg), MaxIntegrity, handling/steering/lean/stability multipliers, ImpactResistance |
| Booster | EnergyCapacity plus existing energy consumption and speed/acceleration multipliers |
| Engine, shield, other equipment, racer | Existing responsibilities retained |

All chassis multipliers use 1 as neutral. ImpactResistance is a fraction from 0 to 0.9;
0.2 prevents 20% of structural collision damage remaining after shields. It applies to
bike collisions and ordinary wall hits, not weapons or catastrophic environmental impacts.
It does not change collision impulse or calculate offensive ram damage; that mechanic remains future work.

## Handling

- BaseHandling: chassis HandlingMultiplier.
- ConfiguredHandling: BaseHandling multiplied by each installed passive handling utility once.
- RuntimeHandling: ConfiguredHandling multiplied by RaceVehicleState.TemporaryHandlingMultiplier.
- Overall handling controls turning authority and lateral grip, preserving those existing motor uses.
- SteeringResponseMultiplier controls how quickly steering input reaches its target, independently of maximum turning authority.
  BikeMotor.steeringResponse is the baseline rate (20/second); this introduces modest input smoothing at neutral settings.
- LeanResponseMultiplier scales entry and return rates of visual bike/cockpit lean. It is not physical tire lean.
- StabilityMultiplier scales lateral grip recovery. Existing grip acceleration caps and rotation locks still apply;
  it is not a replacement for future angular knockback/ram physics.
- The motor retains its existing 0.1–3 handling clamp.

## Energy

Installed booster capacities add together, then racer energy perks apply. No booster means zero base capacity.
A zero-capacity pool cannot recharge and energy-dependent shields have zero capacity; perks can still supply energy.
All four checked-in starter/opponent builds already contain the prototype booster and retain their 100 base energy.

## Content migration

The checked-in prototype chassis is migrated to 250 kg, 100 MaxIntegrity, neutral multipliers and 0 resistance.
The prototype booster receives 100 energy. Equipment references and definition IDs remain unchanged.
Bike mass/handling/energy authoring fields are removed. Old chassis offsets are deliberately not reinterpreted
as absolute mass or multipliers. For any additional local chassis assets, author the new fields explicitly;
use the old bike mass plus chassis offset as the starting absolute mass, and retune old additive handling.
Additional booster assets default to 100 capacity and should be reviewed individually.
Campaign saves store definition IDs, so equipment ownership/loadouts remain intact and derive these new values.

DamageMeter now owns per-instance MaxIntegrity, CurrentIntegrity, AccumulatedDamage and DamageRatio.
Percent is a normalized 0–100 display value. HUD and AI comparisons therefore remain normalized for every chassis.

## Verification

The standalone harness compiles Domain/Shared source and checks energy, handling and damage behavior.
Before merging, open Unity and verify Inspector fields, shop text, HUD normalization, player/AI steering,
lean/grip differences, ordinary collision resistance and catastrophic wall destruction in play mode.
