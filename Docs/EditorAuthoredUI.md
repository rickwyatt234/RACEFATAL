# Editor-authored UI

This cleanup follows PR #55. Brands behavior and its authoring commands are unchanged. No game UI hierarchy is constructed during play: runtime code binds saved controls, fills fixed row slots, and updates presentation state.

## Migration (required once in Unity)

1. Merge PR #55 first, then this cleanup. Keep the #55 commit ancestry when merging (Create a merge commit); this branch is stacked on its exact reviewed head. If #55 is squash/rebase merged, rebase this cleanup's single commit onto main before merging it.
2. Pull, allow Unity to compile, and exit Play Mode.
3. Select `Assets/Game/Editor/UIAuthoring.asset` in the Project window. In its inspector click **Author missing UI in scenes and prefabs**.
4. The action asks Unity to save dirty open scenes, bakes missing controls into relevant scenes and bike prefabs, saves them, and restores the previously open scenes. Review and commit those asset changes. Repeating the action preserves completed layouts.
5. Use **RACE//FATAL > Career > Build Brands UI** separately as described in PR #55. The general migration never rebuilds or resets Brands.
6. Review the screens and HUDs in Unity before marking the PR ready. Play and Build are blocked until the general migration completes, rather than silently shipping missing controls.

The migration is deliberately an inspector action. The only remaining project-defined `MenuItem` commands are **Build Brands UI** and **Rebuild Brands UI (Reset Layout)**. Validation helpers remain callable from tests/code; they no longer add menu entries. Imported editor extensions and Unity's own menus are outside the RF toolbar cleanup.

## Audit and changes

| Area | Former runtime work | New behavior |
| --- | --- | --- |
| Career home | Create calendar and rearrange identity/resource labels | Saved calendar, day/event slots and label geometry |
| Garage, Shop, Roster | Clone and destroy every option on refresh | Six authored rows per list, paginated over an unlimited data collection |
| Races | Clone/destroy event cards | Three authored cards with pagination; selection data independent of visible rows |
| Shop details | Create scrolling text, preview and Brands shortcut; resize detail area per item | Saved scroll area, preview and shortcut; fixed reserved preview space |
| Preparation/recovery | Create host, launcher buttons and modal | Saved buttons and modal, runtime callbacks only |
| Succession | Create retirement/new-racer dialog; resize actions between pages | Saved modal, fixed three-column action layout |
| Campaign creation/review | Add preview buttons/color picker and starting-loadout panel | Authored buttons, picker and scroll panel |
| Pause | Create overlay Canvas, controls and EventSystem on first pause | Saved pause view and scene EventSystem |
| Tactical overlay | Build Canvas, action window, markers and hover components | Saved canvas, seven action slots and marker pools; callbacks rebound when selection changes |
| Cockpit | Build audience display, energy overlays, integrity bar, focus brackets and gun reticle | Saved bike-prefab controls and serialized references |
| Results | Overwrite text sizing when rendering | Initial sizing authored in Editor, designer changes retained |

Tactical pools initially contain 128 racer markers and 256 recharge labels. These are presentation capacities, not new gameplay limits; increase the serialized arrays with authored slots before adding content that exceeds them. Overflow reports an explicit error and never creates extra UI. Race/card/inventory/roster data lists have no corresponding fixed capacity.

Dynamic **data visualization** remains: reticles/target markers track the world, energy/integrity bars show values, color selection updates its marker/gradient texture, scrolling moves content, and Brands animates its existing reveal controls. Bike/track prefab instantiation and combat VFX are gameplay objects, not runtime UI layout generation. Bike-prefab HUDs are authored before the bike is spawned.

Editor authoring methods are compiled only under `UNITY_EDITOR` and reject Play Mode at their public entry points. `EditorUiFactory` replaces `CareerRuntimeUi`; it does not exist in player builds. `ColorPickerPointer` has its own script file/GUID so Unity can serialize it correctly.

## Validation and remaining acceptance

Portable tests cover arbitrary-size pagination, partial/empty pages, collection shrink, retained designer styles, event rebinding, missing-layout errors, and zero object/component creation during rendering. Roslyn parses every first-party C# file in both Editor and player configurations and checks the UI paths for runtime creation calls and the menu allowlist. Existing Brands binding, weapon aim, and domain regressions are also exercised.

**Unity is not installed in the execution environment.** The migration has not been run there, and no baked scene/prefab output is represented as verified. Native compilation, serialized-reference reload, visuals, input navigation, prefab overrides, and repeat-migration preservation need Editor acceptance. This remains a draft until those pass.

Acceptance: run migration twice, reload scenes/prefabs, verify no duplicate controls or changed custom positions; create/load a campaign; test both color previews and starting summary; navigate every career tab with long lists; test recovery and retirement/succession; run a race and check all HUDs, tactical racer/strip markers and all seven partner actions; pause/resume/hide/quit-cancel; inspect results and Brands. Count UI GameObjects/components before and after repeated interactions to confirm reuse.
