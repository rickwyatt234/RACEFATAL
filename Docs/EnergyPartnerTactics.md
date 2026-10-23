# Energy, partner orders, and tactical race control

## Setup in Unity

Open the race scene, then choose **RACE//FATAL > Racing > Build Tactical UI and Commands**. Save the scene. This creates the editable tactical canvas and information windows, attaches the controllers, adds missing recharge-strip AI anchors, and raises scene strip recharge rates to at least 200 energy/second. It is safe to rerun without duplicating the canvas. Exit Play Mode before using it.

The shipped booster, shield, and weapon definitions are already migrated. **RACE//FATAL > Racing > Apply Energy and Survivability Rebalance** applies the starting baseline to additional/custom booster and shield assets. It does not replace custom weapon energy costs. Save prefab overrides deliberately when editing a prefab instance.

The runtime also creates a missing tactical controller/UI so an older scene remains usable. Inspector input bindings and camera tuning are on TacticalRaceController. The scene builder is preferred for editing the panels before running.

## Controls

| Input | Action |
|---|---|
| Hold existing Focus | Existing focus view |
| Press Space while holding Focus | Toggle overhead tactical view |
| Release Focus | Return to racing |
| Up / Down arrow | Pan forward/backward along the circuit |
| Mouse wheel | Zoom |
| Tab | Cycle visible racers and follow the selected racer |
| Enter | Open/close selected racer's radial |
| Click racer label | Select and open its radial |
| Right mouse button | Close radial |
| Home | Return camera to player |

Your bike remains labeled and cannot be selected. Team commands live on the partner's radial. Opponent radials contain tag, tail/assign, attack, pressure left/right, and ram. Hover a side command for its requested position marker. Zoom levels reveal name/team/speed, then shield/integrity, then energy/capacity/weapons/readiness. Recharge strips are marked in tactical view.

Tactical use is unlimited. The default timescale is 0.01. Menu/camera input uses unscaled time; race movement, cooldowns, shields, strip recharge, and spending use race time. A conservative centerline assist handles the player's bike while commanding; it does not fire or boost. Pause suspends tactical input and UI. Exiting restores the cockpit camera and cursor state. Race completion/player destruction closes tactical view before outcome/death-camera handling.

## Resource rules

- No passive energy regeneration. Only track strips replenish current energy.
- Weapons spend energy per successful domain firing activation; existing cooldowns/charge timing remain. Flame costs are per emission interval, shotgun costs per discharge, and turrets per shot.
- Boost consumes current energy and removes capacity per powered second. Strain is race-local and cannot be repaired by strips. Capacity can reach zero.
- Ordinary propulsion/steering/braking remain available with no energy. Shields remain available at zero capacity.
- Shields retain fixed equipment capacity and recharge freely after their damage delay. Damage during the delay resets it. Only the portion of a tick after the delay contributes recharge.
- Integrity damage remains permanent within the race; zero integrity still causes death through the existing career/destruction pipeline.
- The energy HUD uses starting capacity as its full width. Gold shows one second of equipped boost energy use; red shows permanently unavailable capacity. Readout includes energy drain and strain per second. Weapon labels show activation energy costs.
- Legacy ammo-named snapshot/selection APIs now report affordable activation counts for compatibility with existing targeting code. They are not ammunition inventories. Countermeasure charges are unchanged.

Starting content tuning: booster capacities multiplied by 10, boost consumption multiplied by 8, capacity strain 8/second; shield recovery at least 40% capacity/second after 2 seconds. Weapon costs: MG 5, railgun 85, rocket 75, shotgun 50, flame 8 per pulse, rear turret 6, mine 45, ram 65. These are initial tuning values, not a measured survivability guarantee.

## Partner and opponent behavior

The partner starts passive, conserving resources, tracking a formation slot about 7 meters behind and 2.5 meters to one side. Speed matching uses actual bike pace; no teleporting or free catch-up energy. If separated, regrouping remains limited by track conditions and bike performance.

Orders persist until replaced, their target becomes invalid, or a ram activation completes. Ram returns to formation after activation. Attack orders constrain the main combat planner to the assigned target. Tail holds fire. Cover keeps the partner near the player and selects nearby threats. Side pressure requests a target-relative lane; road bounds, strip recovery, and defensive avoidance take precedence. Ramming requires a suitable weapon, energy, close alignment, steering toward the target, and a sufficiently straight road. These are attempts, not guaranteed maneuvers.

Partner aggression and conservation are separate controls. Passive is the default; balanced/aggressive formation can acquire local threats when spending is authorized. High aggression lowers the weapon energy reserve. Critical integrity overrides optional combat. Automatic turrets obey conservation/survival authorization as well.

Opponent philosophy sets energy/capacity reserve preferences, adjusted by distance to the next strip and final-lap spending. These are AI decision thresholds, not physical capacity floors. Exhausted-capacity AI stops seeking strips that cannot help it.

## Validation and playtest checklist

Automated console checks cover strip-only energy, irreversible strain, zero-capacity behavior, independent shield recovery, partial boost duration, energy-event reentry, weapon cooldowns, pause cancellation, vehicle stats, audience accounting, calendar/deathmatch/succession, and campaign creation. Partner order tests use lightweight presentation doubles for policy and lane/speed intentions; they do not simulate Unity physics.

Unity Editor and Play Mode are required to validate actual compilation against Unity packages, HDRP overhead rendering, windshield layout, banked/stacked track camera behavior, controller bindings, formation driving in traffic, and ram execution. No Unity Editor is available in the implementation environment.

1. Build the UI in the prototype race scene and check Console for compile/reference errors.
2. Spend energy, cross a strip, boost, and cross again: energy refills only to the reduced ceiling. Exhaust capacity and confirm normal driving and shields still work.
3. Fire each weapon, including passive turret and charged railgun. Confirm no ammo depletion and no firing without sufficient energy.
4. Enter tactical mode, pan around the lap, zoom, cycle/select, tag, and issue each partner order. Confirm your own bike never opens a wheel.
5. Pause/resume inside tactical view; release Focus during pause; finish/die while tactical view is open. Check cursor, camera, time, audio, and weapon cancellation.
6. Test formation, cover, side pressure, and ram on straights, banks, narrow sections, and near strips. Tune formation spacing/AI driving before judging coordinated attacks.
7. Run complete 12-bike races and record finishers/deaths, shield breaks, energy droughts, and remaining capacity per lap. Tune weapon costs, shield delay/rate, and strip length/rate together.
