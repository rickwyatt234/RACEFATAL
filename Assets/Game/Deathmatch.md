# Deathmatch milestone

## Enable development content

1. Select the Bootstrap `GameContentCatalog` asset.
2. Run **RACE//FATAL → Career → Create Deathmatch Development Events**.
3. Two events and their race definitions appear in `Assets/Game/DeathmatchDevelopment`: Team Survival and Last Racer Standing. The tool registers them in the selected catalog and preserves existing assets on repeat runs.
4. Restart from Bootstrap. Advance to another week if the saved calendar already has its draw. Events participate in the normal random weekly draw, so a particular event need not appear every week.
5. Run **RACE//FATAL → Career → Validate Deathmatch**, then the existing **Validate Race Calendar** command.

The development races copy the first catalog race's track and engine class, with twelve bikes in six teams. The campaign must have five eligible opponent teams. Reduce the authored grid to an even number if your test world has fewer teams.

## Tune in the Inspector

Use the race definition for survival rules and the calendar event for fees, Fame unlocks, and position payouts.

| Race field | Meaning |
| --- | --- |
| Deathmatch | Enables survival instead of lap-based completion. |
| Victory Mode | Individual racers, or teams that remain eligible while one member is still racing. |
| Allowed Winners | Number of surviving racers/teams needed to end naturally. One individual winner means last racer standing. Must be less than the starting field. |
| Time Limit Seconds | Mandatory stalemate limit; default 300 seconds. |
| Minimum Speed Kph | Actual bike velocity threshold. Zero disables speed disqualification. Default 30 km/h. |
| Start Grace Seconds | Time to accelerate from the grid; default 15 seconds. |
| Below Speed Grace Seconds | Continuous time below the threshold before disqualification; default 8 seconds. Returning to the threshold resets this timer. |
| Share Timeout Ties | Off by default: winner count is capped. On: exact survivor/elimination ties share rank and may exceed winner slots. |

Deathmatch calendar events must reference exactly one deathmatch race. Championships continue using circuit races. Rules are copied into paid entries; restarting an entered event restores its saved rules and attempt ID without charging again. Entry resumes from the grid, not from a saved mid-race physics state.

## Outcomes

- Laps do not finish deathmatches. Racers continue around the circuit.
- At timeout, rank by surviving members, then combat eliminations. With shared ties disabled, remaining ties use total course distance, then original grid order. Eliminated entrants rank by when their last member left the event.
- A speed violation disqualifies the racer from this event. It does not kill the character or destroy the bike.
- Destruction retains existing permanent character/bike consequences. The event continues after player elimination, displaying live standings until survival or timeout resolves it.
- Team-mode wins include eliminated teammates in the team classification. The team receives its position payout once, in full if a teammate wins. Character death/status and Character Fame remain individual.
- Non-winners receive the existing DNF reward treatment. Eliminated riders cannot fast-resolve remaining contestants into winners.
- Rewards, researcher output, calendar advancement, and settlement receipts use the existing campaign transaction flow. Returning to Career saves the campaign.
- Individual mode allows targeting every other racer, including the assigned partner. Team mode keeps existing teammate targeting restrictions.
- The cockpit's lap area shows survivor count, time remaining, and speed/grace warnings. Final results and saved Races standings show winners, eliminations, and exit reasons.

## Verification

The Unity menu validator covers startup and below-speed grace, exact-threshold reset, nonlethal disqualification, teammate survival, lap-independent completion, multiple winners, individual teammate competition, timeout ranking, strict/shared ties, simultaneous elimination, permanent death, paid-entry JSON reload, frozen rules, and duplicate-settlement receipts. Existing calendar validation retains circuit/championship regression coverage.

Unity is not installed in the implementation workspace. These menu validations and Play Mode checks must run in Unity before merging or treating this milestone as tested. Static diff and API-reference checks were performed here.

Play Mode acceptance pass: enter each development event; tune the speed threshold to trigger a warning and recover; trigger DQ; confirm AI continues; check individual guided targeting of a partner; finish by survival and timeout; return to Career and reload. Confirm Credits/RP/Fame, permanent losses, last-event standings, and next calendar week persist once.

## Project checkpoint

Career creation/succession, Garage/loadouts, components and complete-bike Shop, Research/staff, Roster/perks/recovery, Team, weekly events, championships, and Deathmatch now connect through campaign state. The next review should measure the whole loop: campaign initialization, race readiness, combat readability, event balance, replacement costs, research pacing, and recovery after losses. Run that review before choosing another feature milestone.
