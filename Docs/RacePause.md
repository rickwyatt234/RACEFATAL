# Race pause

`RaceRuntimeController` automatically installs `RacePauseController`. Pull the branch and let Unity compile; no scene or prefab rebuild is required. Controls use the project's existing legacy input API (Active Input Handling is Both).

| Control | Behavior |
| --- | --- |
| Escape during countdown/racing | Freeze and open the pause menu |
| Escape with the menu visible / Resume | Resume racing |
| F1 while racing | Freeze immediately with the menu and cursor hidden |
| F1 while paused | Show/hide the menu without resuming |
| Escape with the menu hidden | Show the menu, keeping the race frozen |
| Quit Game | Open confirmation; Escape/Back cancels |

The hidden-menu view preserves the cockpit HUD and camera framing. Use your usual screenshot shortcut or Unity Scene view to inspect the frozen scene. This is not a free-camera photo mode. Quit exits the application (stops Play Mode in the Editor) without recording an unfinished race; the last campaign save is retained.

Pause preserves the previous time scale, fixed timestep, audio pause state, cursor state and selected UI element. Focus mode cannot override pause. Disabling/destroying the pause controller restores the state. The overlay blocks clicks into race/results UI even while invisible. Gameplay updates are blocked on resume's input frame so clicking Resume cannot fire a weapon. A held/charged player weapon is canceled on resume without firing or spending ammunition; press fire again to reactivate it.

## Verification

Run `dotnet run --project Tests/RacePause/RacePause.csproj` for production-domain checks covering held-fire cancellation, frozen ammunition/charge, safe release of canceled shots (including zero-duration charges), fresh activation after resume, and empty loadouts.

Unity 6000.3.6f1 Play Mode/build checks:

1. Start a race normally. Pause during the countdown, wait, then resume; the countdown must continue from the same point.
2. While riding, press Escape. Bikes, projectiles, race time, audience favor, shields/energy, cooldowns and race audio should stop. Buttons remain clickable and keyboard navigation works.
3. Pause during focus slow motion. Wait several seconds and release focus while paused. Resume: time must recover normally and physics must retain its original fixed timestep.
4. Hold a weapon trigger, pause, release it over the menu and click Resume. There must be no stuck firing or charged discharge. Repeat with a charged weapon; fresh input should work.
5. Press F1 directly during racing. The scene freezes with no pause overlay or cursor. F1 or Escape reveals the menu without advancing the race; Resume continues.
6. Cancel Quit confirmation with Escape and Back; the race remains paused. Confirm Quit in a disposable test run and verify the last saved campaign is intact on relaunch.
7. Exit Play Mode or unload the scene while paused, then start again. Check normal time, sound, cursor and a single pause overlay. Also check with domain reload disabled.

The .NET checks do not validate Unity rendering, physics, EventSystem behavior or platform screenshot output; those require the Editor/build checks above.
