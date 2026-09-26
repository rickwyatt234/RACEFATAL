# Races and championships

The Races screen owns the weekly calendar, unlocked event catalog, Fame milestones, entry details, optional track diagram and championship standings. Event unlock information has moved out of Team.

## Unity setup

1. Pull this branch and let Unity compile. Exit Play Mode.
2. Select the `GameContentCatalogSO` used by Bootstrap. Optionally run **RACE//FATAL → Career → Create Calendar Development Events**. This registers five race offers and one three-round championship, with one Fame-gated offer. Existing development assets are preserved. The sample championship repeats one existing race three times; change its ordered round references to build a varied series.
3. Run **RACE//FATAL → Career → Build Races Screen**. This replaces children of `RacesRoot`, wires all controls and saves `01_Career`. Other screens are preserved. Commit the generated scene/catalog/assets in your local Unity project.
4. Run **RACE//FATAL → Career → Validate Race Calendar**. Start from `00_Bootstrap`, load a campaign and open Races.
5. For a track diagram, assign a Sprite to **Calendar preview → Track Diagram** on the track's `TrackDefinitionSO`. Missing diagrams show a placeholder.

Without authored event assets, each existing race gets a free open event at runtime, retaining the previous credit payout scale. This allows old projects to start using the calendar without authoring content first. If you subsequently add event assets, the current saved draw stays fixed; skip a week to draw from the updated pool. A championship is selected at random like other eligible events, so the development championship may not appear every week.

## Event authoring

Create **RaceFatal → Career → Calendar Event** assets and register them in the catalog's **Calendar Events** list. Each has an ID, display name, description, event kind, required Team Fame, one entry fee, ordered race references, per-position race payouts, championship completion prizes and position points. Standalone events contain one race. Championships contain two or more rounds with matching engine class and grid size. Repeated race references are supported.

The current formats are Race and Championship. Deathmatch is reserved as an authoring value, excluded from the calendar until its rules are implemented. Race formats currently require two racers per team. Entry requirements also use the existing owned-bike, racer-status, engine-class and track-prefab checks.

Race purse is the sum of the position payout table up to the configured grid size. The player receives the payout for their own finishing position; a finalized DNF receives 40%, rounded away from zero. Positions beyond the authored table pay zero. Existing Team Fame, Character Fame, research rewards and researcher income still apply. Championship prizes are additional credits based on the team's final rank.

## Calendar rules

- Team Fame unlocks events permanently and is never spent. Existing matching championship unlock IDs are respected. The weekly draw shows only unlocked supported events. The Fame Milestones view is where future unlocks are visible.
- Up to four distinct events are selected each week. A free standard race is retained whenever the unlocked content pool contains one. The draw and random seed are saved, so reopening/reloading does not reroll it. New Fame unlocks enter the next weekly draw.
- One finalized race advances one week, including a DNF. A championship reserves the next round until it is finished or withdrawn from. Garage, Shop and other screens remain available between rounds.
- Skipping advances the calendar without researcher income or contract consumption. Researcher contracts count completed races, not skipped weeks.
- Confirmation charges the event fee once for the entire event. Failed preparation charges nothing. A failed pre-race save rolls back the fee and registration. After a successful save, an interrupted entry is resumable with the same attempt ID and no second charge; the race restarts from its beginning.
- Withdrawal forfeits the entry fee, awards no completion prize and advances one week. The withdrawn attempt is recorded so it cannot settle later.

## Championships

Both racers' finish points count toward their team's standing; DNF scores zero. The original team field is retained. A team that cannot field two active racers and eligible bikes misses that round and receives zero points while keeping earlier points. The grid shrinks to the surviving eligible teams. Player/partner eligibility still blocks entry until the loadout or assignment is corrected, or the event is withdrawn from.

Equal point totals share rank and completion prize. Final prizes are awarded once after the final round, with the result receipt and calendar state saved together by the existing results flow. Player death before the final round ends the unfinished championship without a completion prize. The Standings view shows the active event or most recent completed/withdrawn event.

The event's fee, payout tables, ordered race IDs and points are captured on entry so later edits to event assets do not change a paid registration. Race and track assets themselves must remain available. Save data uses optional version-1 calendar fields; older campaigns begin at week one. Existing progression and inventory are preserved.

## Verification and Unity smoke test

The included validation exercises real race preparation, stable draws, unlock filtering, entry fees, failed-save rollback, interrupted-entry recovery, result identity, duplicate settlement, JSON roundtrips, championship standings/prizes, opponent attrition, withdrawal, DNF, player death and older saves.

1. Generate/build twice; check for duplicate UI or content entries.
2. Open Races, inspect each view and the detail scroll panel, cancel an entry confirmation, then reload and compare the calendar and credits.
3. Enter a paid event. Stop/reload after the pre-race save and confirm entry is still paid. Complete it and confirm one week's advancement and one payout.
4. Complete a championship across reloads. Check cumulative team points and the final bonus. Test withdrawal and an ineligible player loadout.
5. Assign a diagram sprite and verify aspect ratio. Run without one and verify the placeholder.

Standalone verification uses Roslyn and substitutes System.Text.Json for Unity JsonUtility. Unity API stand-ins check the new UI's C# references, but Unity Editor compilation, rendering and scene flow still require the smoke test above.
