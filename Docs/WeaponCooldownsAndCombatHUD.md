# Weapon cooldowns and combat HUD

## Activation timing

WeaponDefinitionSO → Timing → Fire Interval now applies to every weapon activation mode. Press weapons cannot fire another shot until this interval passes. Hold weapons retain automatic cadence without allowing release/repress to accelerate it. Charged weapons wait after a successful shot before beginning another charge. Aborted charges consume no ammunition or cooldown. Passive weapons retain their existing cadence.

Cooldown belongs to each installed weapon and continues recovering while released or unselected. Weapon switching, input cancellation and activation-permission changes do not reset it. Zero-delta paused ticks freeze it. The first press remains immediate. Holding again during cooldown waits until the next eligible automatic shot. The selected cooldown is exposed as RaceEquipmentSystem.SelectedWeaponCooldownRemaining.

Authored starting values: shotgun 0.8 s, mine 0.75 s, ram 1.5 s, railgun 0.75 s after release (in addition to charging). Rocket launcher retains 3 s, MG 0.1 s, flamethrower 0.2 s and rear turret 0.16 s. All remain editable per definition. Runtime uses a 0.01 s minimum to avoid a zero-interval automatic loop.

## Focus inspection

Hold the existing focus key (Left Ctrl by default) to show one visible racer's information panel. First selection prefers the existing missile target or the racer closest to the HUD center. Tab cycles through visible, living racers in stable roster order; the same racer remains focused until it leaves view or is eliminated. Releasing focus hides the information panel. Existing line-of-sight requirements apply.

During focus, guided targeting follows the inspected racer only if that racer is a valid target in the selected launcher's range. Inspecting a teammate or an out-of-range racer creates no missile lock. Outside focus, Tab retains its existing guided-target cycling behavior. Only the first authored ID box is used, so the other info panels cannot overlap it.

## Integrity HUD

PlayerCockpitHUD → Integrity Display creates an integrity panel beneath the central top readout by default. It shows remaining integrity as a colored bar, current/max values and percentage. The bar shrinks toward zero and shifts from cyan to red as integrity falls. Shield damage alone does not reduce it.

Enable Show Integrity and tune Integrity Anchor, Position and Size; or assign a RectTransform to Integrity Root. Defaults: top-center anchor, offset (0, -260), size (220, 70). The display inherits the windshield UI layer and speed font, and follows normal HUD visibility. Existing damage fields and damage-flash behavior remain available.

## Validation

46 new weapon cooldown checks, 23 race-pause checks and 24 vehicle-stat assertions passed against production Domain/Shared code. They cover click spam, release/repress, switching, charge cancellation/recovery, automatic cadence, passive cadence, event reentry, frozen time and integrity damage. Run dotnet run --project Tests/WeaponCooldowns/WeaponCooldowns.csproj and the existing RacePause/VehicleStats projects.

Unity compilation and Play Mode verification remain required for focus input, ID placement and the generated integrity panel. Test crowded racers with Ctrl + Tab, teammate/out-of-range inspection, missiles, destruction and pausing; verify integrity loss after shield depletion and final panel fit against the authored windshield layout.
