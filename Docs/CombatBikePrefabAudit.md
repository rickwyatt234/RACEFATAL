# CombatBike prefab audit

Audited `Assets/Game/Prefabs/BikeModels/CombatBike.prefab` on main commit `9a811341c62c3d4cdfec0a36006e20ced030cc14`, including the merged shield-bubble change. This is a repository/static audit, not a Unity import or Play Mode certification.

## Confirmed issues fixed in this change

| Issue | Effect | Fix |
| --- | --- | --- |
| Six weapon mounts, collision/damage, destruction, countermeasures, lock acquisition and lock confirmation all referenced the same CombatAudioSource | Weapon voice playback calls Stop and changes clip/pitch; lock acquisition loops/stops it; several systems overwrite mixer routing and spatial settings. Unrelated sounds can stop or use the wrong settings. | Give each weapon mount its own fire source on its mount GameObject. Give damage, destruction and countermeasures separate sources on the bike, and acquisition/confirmation separate sources on CockpitRig. Keep the original source for collisions. Runtime charge sources remain automatically created, separately for each mount. |
| All three lap-box roots referenced HUD_Lap_Tracker | A one- or two-lap race hides the entire tracker when the loop reaches an unused box. | Point each root to its own Lap_1, Lap_2 or Lap_3 GameObject. |
| BikeLeanController.cameraLeanPivot pointed at the live CameraRigRoot | Lean rotates the camera's parent while PlayerCockpitView writes camera world pose in LateUpdate; ordering can cause unintended camera rotation. The camera anchor is already beneath VisualLeanRoot. | Clear the optional extra camera-lean pivot. Camera pose remains controlled by PlayerCockpitView, following the already-leaning anchor. Full inherited lean is preserved; cameraLeanMultiplier no longer adds a second lean in this prefab. |
| Rear-view cameras were serialized enabled despite being explicitly rendered by PlayerRearViewController | Relies on Awake to prevent automatic rendering. | Serialize both disabled; the controller still renders them on demand. |
| Cockpit AudioListener was serialized enabled | Relies on role resolution/Awake to disable listeners on non-player bikes. | Serialize disabled; PlayerCockpitView enables it for the player. |

No model material was replaced with a guess. No empty optional effect slot was filled with an unrelated effect.

## Remaining asset problems

These four material GUIDs have no matching `.meta` anywhere in the checked-in Assets tree (10,126 metadata files checked). They may exist in an uncommitted local Unity project. Restore/commit the intended material and its original `.meta`, or deliberately reassign the slot in Unity.

| Renderer(s) | Missing material GUID |
| --- | --- |
| VisualLeanRoot/Meshes/Seat | `8acc12c2e5aba2345ac9f5ecd44d0898` |
| VisualLeanRoot/Meshes/FrontWheel and RearWheel | `2354929234ab4064593244ab27967e1e` |
| VisualLeanRoot/Meshes/BikeChassis | `661246a051bb4f44aa7b03b0507aa965` |
| Feedback/ShieldFeedback/{Left,Front,Rear,Right}/vfx_SciFiShield01 | `e823cd5b5d27c0f4b8256e7c12ee3e6d` |

The last row belongs to retained directional fallback effects, not the new four bubble prefabs. It still constitutes a broken asset reference.

`Assets/Game/Materials/Glass.mat` also retains two texture GUIDs absent from the repository:

- `9cbbbe4a584fc1b439f069f9cbd07a5f`: `_BaseColorMap`, `_MainTex`, `_UnlitColorMap`.
- `d48e084d31b06e94ca6464324c919840`: `_NormalMap`.

Some saved material properties can be leftovers from a previous shader; inspect the current HDRP material before restoring or clearing them. Package-provided shader/editor script references were not treated as missing project assets merely because they are outside Assets.

## Empty slots and resulting feature gaps

| Component/field | Assessment |
| --- | --- |
| RacerDestructionView.explosionClips | Empty: the destruction code has no primary explosion sound to play. The assigned primary explosion particle prefab has no AudioSource of its own. Choose a suitable clip. |
| RacerDestructionView.secondaryExplosionPrefab/Clips, residualEffectPrefab, debrisPrefabs | Optional content not configured. `spawnDebris` is enabled but its empty prefab list makes it a no-op. |
| RacerDestructionView.preserveOnDestruction | Contains one null entry. Harmless; the player camera is preserved automatically. |
| PlayerCockpitHUD.damageFill / damageText | Unassigned: no hull-damage readout through these fields. The damage flash is assigned. Add an intentional hull-health UI element if desired. |
| PlayerCockpitHUD.lapText | Unassigned. Normal races use the three lap boxes, but deathmatch ALIVE/WINNERS/timer/minimum-speed/disqualification countdown text is written only to this field and therefore absent in this prefab. Races longer than three laps also exceed the authored box count. |
| PlayerCockpitHUD.weaponText / ammoText | Legacy single-weapon fields; the four weaponPanelSlots are assigned, so these nulls are not themselves a missing weapon panel. |
| PlayerCockpitHUD.audienceRoot | Intentionally runtime-created by TryCreateAudienceHUD when audience display is enabled. No missing CockpitAudienceHUD component needs adding manually. |
| PlayerCockpitHUD.crowdCheeringLoop / crowdBooingLoop | Not serialized/assigned in the prefab; no authored crowd loops are available to the generated audience HUD. |
| WeaponMountFeedbackView.chargeAudioSource | Automatically created as a dedicated source in Awake; safe to leave unassigned. |
| ShieldBubbleEffectView.bubbleOrigin | Defaults to the bike transform; safe to leave unassigned. |

## Placement and reference checks

- Original prefab: 621 serialized objects, 167 GameObjects, 121 distinct external GUIDs. Updated prefab adds 11 AudioSource components, with no new GameObjects.
- No dangling internal nonzero fileID references found.
- All 65 checked direct project-script object reference types matched their target component types.
- No missing same-object RequireComponent dependencies found for the attached project scripts.
- Rigidbody, collider, motor, surface probe, racer state, progress reporter, collision/damage, lock/threat receivers, countermeasures, charge presentation and bubble view are on CombatBike.
- Player controls/HUD/targeting are under PlayerDriverRig/CockpitRig; AI driver is under AIDriverRig. BikeRuntimeController points to the matching roots/drivers and switches them by participant role.
- All six size/index equipment mounts are assigned and carry WeaponMountFeedbackView components.
- Energy/shield/speed/position text and fill references, four weapon panel slots, targeting warnings/ID boxes, rear camera textures and windshield projection references are assigned.
- Bubble component is assigned on CollisionFeedbackView; Blue, Teal, Purple and Red external prefab root IDs and GUIDs resolve correctly, with descending 75/50/25 thresholds.
- Checked 141 direct references into 21 native serialized external assets (materials, font assets, mixer, render textures, effects, catalog): all referenced object IDs exist in those assets. Imported mesh/audio/sprite subasset IDs and package imports still require Unity to validate fully.
- UI/HDRP package script references are present alongside the expected package dependencies; these are not project-script GUID failures.

## Validation performed

`python Tests/PrefabAudit/test_combat_bike.py` requires Python 3 and PyYAML. Seven structural regression checks cover internal references/component ownership, driver/core placement, independent audio ownership, lap roots, camera-pose ownership, disabled-by-default manual cameras/listener, and bubble references. All seven pass after the changes. The original prefab fails the four checks for shared audio, shared lap roots, camera ownership and initially enabled manual cameras/listener. `git diff --check` passes.

## Unity checks still required

1. Import/compile with the project's actual Unity/HDRP packages. Confirm no missing scripts/materials after the missing assets above are restored.
2. Run player plus AI bikes: exactly one active listener; player HUD/cameras only on the player; independent engine/fire/charge/lock/impact/countermeasure audio.
3. Turn and collide while watching cockpit pose, then destroy the player and verify the detached death camera remains stable.
4. Run one-, two- and three-lap races. Check lap colors and layout against the desired visual design. Author a text display for deathmatch and/or longer races.
5. Verify rear-view RenderTextures update in HDRP. The existing controller calls Camera.Render; Unity documents RenderPipeline.StandardRequest/SubmitRenderRequest for explicit HDRP renders. This audit has not changed that render API or proven the existing path works in the editor. Reference: https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/rendering/renderpipeline/submitrenderrequest
6. Test bubble fit, rapid threshold crossings, shield break, pause and destruction. Check glass/HUD transparency from the cockpit after material restoration.
7. Inspect scene-level prefab overrides, which are outside this asset-only audit and can override the fixes.
