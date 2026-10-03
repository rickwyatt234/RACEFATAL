# Gun reticles and AI banking

Forward machine guns, shotguns and railguns now aim through the center of the selected weapon's reticle. The existing projectile/hitscan delivery still starts at the physical muzzle, including muzzle-side collision checks and shotgun spread. Camera framing for the windshield HUD no longer steers these guns downward by default.

## Per-weapon setup

Select the weapon's `WeaponPresentationProfile` in `Assets/Game/Scripts/Presentation/WeaponPresentation` and edit **Gun Reticle**:

- **Reticle Sprite**: assign a UI Sprite for this weapon. An empty field draws a built-in crosshair.
- **Reticle Size / Color**: control appearance. The included machine gun, shotgun and railgun use 32, 56 and 24 unit crosshairs respectively.
- **Reticle Placement → Bike Forward** (default): projects a point straight ahead of the bike's chassis from its muzzle into the cockpit camera. Lowering the camera moves the reticle higher in the image while preserving forward gun aim. Camera roll and boost FOV are also accounted for.
- **Reticle Viewport Offset**: moves the reticle and its aim together in normalized screen coordinates. For example, Y = 0.05 moves both upward by 5% of the viewport height.
- **Reticle Placement → Fixed Viewport**: use **Reticle Viewport Position** for an absolute screen location, e.g. (0.5, 0.65). In this mode camera rotation intentionally affects the ray through that fixed pixel; adjust the position if you reframe the camera.

These values control the actual firing ray as well as the displayed reticle. Offsets can intentionally aim above/beside chassis-forward. Nearby geometry under the reticle becomes the convergence point; the owner's colliders and camera-visible geometry behind the muzzle are ignored. Shots still originate at the muzzle, so an obstacle beside/in front of the gun can block a shot even when the camera has a clear view.

The reticle is created under the player's existing **Reticle Canvas**, using its camera and Canvas scaling. This is a cockpit-camera overlay in the Game view, separate from **HUD_Root** and the windshield RenderTexture preview. Do not manually add gun reticles to HUD_Root. CombatBike uses Screen Space – Camera with Plane Distance **0.05**, just beyond its 0.01 near clip plane; the previous 100-unit distance let nearby geometry obscure the reticle. Keep this distance close to the camera and beyond its near clip plane on other prefabs. It switches with the selected gun, hides for special/locking weapons and follows outcome/death visibility. No scene or CombatBike prefab rebuild is required. Other bike prefabs need a configured Reticle Canvas on PlayerCockpitView. A missing presentation profile falls back to a built-in crosshair and chassis-forward placement.

Missile target-lock indicators, guided aiming, rear automatic turrets, mines, ram attacks and the directional flamethrower keep their existing targeting behavior. The gun reticle applies only to Forward weapons delivered by Hitscan, Projectile or ConeProjectile.

## AI lean

`BikeLeanController` now banks non-player racers from measured chassis turn rate and speed. AI steering values can be very small during a real corner, so deriving the bank from actual cornering makes the visible motorcycle lean perceptible. The lean is smoothed, limited to 16 degrees by default and fades away at low speed/on straights. It rotates the existing Visual Lean Root; physical chassis rotation/colliders are unchanged. Player steering-based lean is preserved.

On CombatBike → **BikeLeanController**, tune **Maximum AI Lean Angle** and **AI Turn Response**, along with existing lean/upright response settings. The same visual treatment applies to opponents and the AI teammate. Race pause freezes the lean; disabling the component restores the authored visual rotation.

## Verification

`dotnet run --project Tests/WeaponAim/WeaponAim.csproj` compiles the production PlayerWeaponAim, WeaponPresentationProfile and BikeLeanController with presentation doubles against the actual Domain assembly. Checks cover camera pitch/roll, FOV, fixed/relative reticle placement, muzzle convergence, owner/behind-muzzle exclusion, selected sprite/size, locking/death/outcome visibility, enemy left/right banking, speed limits, straight-line recovery, pause and unchanged player lean.

These checks use a pinhole camera/physics/UI substitute. Unity compilation and Play Mode verification remain required: lower the cockpit camera, confirm gun shots pass through the reticle, swap guns/missiles, test nearby walls and shotgun spread, and observe AI banking through turns from cockpit/external views.
