# Windshield HUD stability

The reported recording shows thin HUD lines shifting and smearing during movement. CombatBike previously moved the offscreen HUD camera/canvas rig with the racer while its source camera was positioned at roughly 100,000 units. At that distance float precision becomes comparable to individual UI pixels. Its transparent projection also rendered before world post-processing, making the HUD susceptible to scene motion blur. Collision camera kick moved the camera independently of the projection anchor.

## Changes

- PlayerCockpitView holds the offscreen WindshieldHUDRenderRig at world origin with identity rotation before rendering. It remains in the bike hierarchy so player/AI visibility and destruction ownership are preserved. The source camera is returned to zero local position; the source canvas remains on the dedicated HUD layer, excluded from the cockpit camera.
- The projection anchor keeps its authored pose relative to Camera Anchor, then follows the final cockpit camera pose in LateUpdate. Camera kick therefore moves the world view while the projected HUD remains aligned. The Stabilize HUD Projection toggle allows restoring the original anchor behavior.
- M_HUDProjection uses HDRP/Unlit's After Post-process transparent queue (3700), with additive offscreen alpha blending and Always depth test. It renders outside scene motion blur/temporal effects and remains readable in front of geometry. World rendering settings and blur remain unchanged. The HUD no longer receives world post-processing bloom/exposure/color grading; tune its own UI colors for the desired glow/brightness.

PlayerCockpitView has explicit Windshield HUD Render Rig and HUD Projection Anchor references assigned in CombatBike. Maintain this wiring on other bike prefabs. Adjust the projection's initial fit on HUDProjectionAnchor/HUDProjectionPlane in Prefab Mode; the starting pose is cached at Awake.

## Verification

The existing nine prefab structural checks and git diff --check pass. The source camera/canvas share the stabilized rig, and layer masks keep source UI out of the cockpit render. These checks do not exercise Unity rendering or camera motion.

Unity import and Play Mode verification are required: compare the same straight/turn/boost section from the recording, trigger a collision camera kick, pause/resume, and verify death/outcome HUD hiding. Check target boxes, reticle alignment, rear-view feeds and final brightness. Rear-view video still reflects scene motion and its configured update cadence; these changes stabilize the surrounding HUD graphics.
