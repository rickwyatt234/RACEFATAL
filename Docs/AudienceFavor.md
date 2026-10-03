# Audience favor

Audience favor starts at 100 and stays between 0 and 200. It records only the player's active racing time, from the start signal until their finish, retirement or destruction. Countdown, pauses with zero game delta time, spectating and fast resolution add no audience time. An event with no active time uses neutral favor.

Fame multiplier = 0.5 + average favor / 200. The average is time-weighted, including exact integration of idle decay down to zero. A final burst cannot replace an uneventful race's average. This scales both Team Fame and Character Fame earned from the player's result after finish/DNF rules. Credits, research, and teammates' individual fame remain unchanged. Rounding uses the existing reward policy. Existing settlement receipts still prevent duplicate awards. Results and save receipts retain the average; old saves default to 100.

## Default tuning

Select the scene's **RaceRuntimeController → Audience Favor** to tune:

| Action | Favor |
| --- | --- |
| Overtake a live opponent | +3; 12-second cooldown per opponent |
| Successful weapon hit | +2; at most once per second across all targets |
| Actual shield/integrity damage | +0.04 per damage; budget of +5 per one-second window |
| Deplete a shield | +12; 10-second cooldown per victim |
| Sustained successful weapon hits on one victim | +3 every 2 seconds; gaps over 1.25 seconds reset the streak |
| Damaging bike collision | +5; 2-second impact cooldown |
| Activated ram hit | +10; shares the impact cooldown |
| Destroy an opponent | +30 once on destruction |
| No successful combat | −1.5 per second after a 4-second grace period |

Damage, shield break and destruction can stack. Pellets and damage-over-time ticks share the hit cooldown and damage budget. Friendly fire, self-damage, environmental damage, firing into empty space and damage to eliminated racers earn nothing. Individual deathmatches treat all other racers as opponents, including racers from the same team. Overtakes are disabled in deathmatches; ordinary race passes require a live opponent, forward progress, separation hysteresis and a change from behind to ahead, including lapping an opponent. Overtakes do not reset the combat grace period. A long-range successful hit still counts as combat.

## Windshield HUD

**PlayerCockpitHUD → Audience** creates a child display under the existing HUD Root automatically. It contains a segmented equalizer, current favor out of 200, average favor, and the projected fame percentage. It inherits the HUD layer for the existing render-texture camera.

CombatBike assigns an authored **HUD_Root → HUD_Audience** rectangle beneath the speed readout (top-center anchor, position −272 / −260, size 260 × 96). Move/resize this RectTransform in Prefab Mode to tune its windshield placement. Its equalizer and labels are populated at runtime. The previous generated bottom placement can fall outside the visible projection after cockpit reframing.

For prefabs without an assigned Audience Root, set Audience Anchor, Position and Size for the final layout. The default is bottom-center, 100 pixels up, 260 × 96 canvas units. Alternatively, assign an empty RectTransform to Audience Root for an authored location. The runtime adds its own graphic and labels to that rectangle. The panel inherits the speed label's font when available.

Assign optional **Crowd Cheering Loop** and **Crowd Booing Loop** clips and set Crowd Volume. The sources crossfade with crowd favor, pause with gameplay and stop when the panel is disabled. No crowd recording is bundled; the visual meter works without clips. The equalizer is a stylized favor visualization, not an audio spectrum analyzer.

On RaceOutcomeController, optionally assign Audience Summary Text. If unassigned, the crowd average and fame adjustment appear below the existing Team Fame value.

## Verification

Run `dotnet run --project Tests/Audience/Audience.csproj` and `dotnet run --project Tests/VehicleStats/VehicleStats.csproj` with .NET 8. These compile production Domain/Shared code; audience checks also compile and exercise save mapping. Unity 6000.3.6f1 play-mode verification is still required for windshield placement, clipping, font sizing and chosen audio levels.

In Unity: race without combat, engage with rapid-fire weapons and a shotgun, break a shield, ram, destroy an enemy, cross the line while overtaking, and finish early. Verify the live average freezes at the player's outcome and matches the results breakdown. Save and reload to confirm the same receipt and payout. Try a deathmatch timeout and spectator period as well.
