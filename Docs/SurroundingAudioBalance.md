# Surrounding audio balance

Based on main bc51b2492ba4ec58ce04458eb9f7e1b1fb2dcdeb.

The committed CombatBike uses fully positional opponent audio with logarithmic rolloff starting at 1 metre, plus a 0.5 engine multiplier. Every weapon profile starts logarithmic attenuation at 3 metres. These settings strongly suppress other racers at typical track separations. The mixer snapshot has no explicit attenuation values for OpponentVehicles or OpponentWeapons. Bike destruction and ballistic mine impact clip arrays are empty, making those playback paths silent regardless of gain.

Changes to existing serialized assets:

| Setting | Before | After |
| --- | --- | --- |
| Opponent engine multiplier | 0.5 | 0.8 |
| Opponent boost loop multiplier | 0.5 | 0.75 |
| Opponent boost start/stop multiplier | 0.45 | 0.7 |
| Engine/boost/start-stop source min/max distance | 1 / 500 m | 12 / 180 m |
| Weapon profile min/max distance (all eight) | 3 / 120 m | 15 / 180 m |
| Bike destruction source min/max distance | 1 / 500 m | 30 / 300 m |
| Bike destruction and mine impact clips | Empty | Existing Aeon EXPLDsgn_Explosion_01, 02, 03 |

These are an initial mix, not a listening-tested final master. Opponent spatial blends remain 1; logarithmic rolloff, weapon fire volumes, player volume multipliers and mixer gains are unchanged. Source distances are shared by the player and opponents; nearby player audio should already lie inside the new minimum distances. WeaponWorldFeedback uses the same profile distances for impacts. Minimum distance controls the full-volume radius; maximum distance on logarithmic sources is not a guaranteed silence cutoff.

Nine existing prefab regression checks and git diff --check pass. Clip GUIDs were verified against committed AudioImporter metadata. No scripts, model objects, component ordering, hierarchy, or prefab identity were changed. Rocket launch range is tuned here; its separate particle impact prefab is not changed by this patch.

In Unity, compare one opponent at 10, 30 and 60 metres, then test the full 12-bike pack. Listen for pass-by direction, sustained weapon masking, bike destruction and mine detonation. Check the Audio Profiler for excessive levels or voice virtualization. If the pack becomes too loud, reduce OpponentVehicles/OpponentWeapons mixer levels rather than returning the audible radius to 1–3 metres. Final clip choice and loudness should be auditioned in the actual race.
