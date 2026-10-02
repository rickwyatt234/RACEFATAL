# Dated recurring events and post-race pages

The campaign calendar is fixed from the event bank at campaign creation. It stores each event's first date, repeat interval, championship round spacing, and round count. Calendar pages expand these saved recurrence rules; there is no random weekly draw and no reshuffling when browsing or loading. Locked events are included and remain visible with their fame requirements.

## Time and entry

- Entering a race on Week 1 / Day 4 moves a brand-new campaign from Day 1 to Day 4 when the entry is saved. Completing the race leaves today on Day 4.
- A later event on Day 6 is still enterable in the same week. A different event on the same day is also allowed. Earlier dates are unavailable.
- An occurrence is consumed on registration. It cannot be entered again after completion or withdrawal, but the same named event's later repeats remain available.
- Failed registration-save rollback restores the previous date, fee, active entry, and consumed-occurrence list. Resuming a pending race retains its attempt ID and does not charge again. Existing result receipts still prevent duplicate rewards.
- Championships have ordered, dated rounds. Entering each round advances to its date; completing it does not advance time. An active championship must be completed or withdrawn before entering another event.
- Withdrawal and retirement no longer add a week. The optional Next Event Date action skips to the next unlocked start date after today and awards no research.

## Authoring

Select an event asset and set First Week, First Day Of Week (1–7), Repeat Every Weeks (4–6), and Round Spacing Days. A repeat interval is fixed per event: a Week 1 / Day 4 event repeating every five weeks returns on Week 6 / Day 4, Week 11 / Day 4, etc. Championship rounds must finish before the next repeat.

The event's Rounds references determine its actual race(s), including track venue, laps, grid, engine class, deathmatch rules, and research bonus. Its payout/prize tables remain event-specific. `Other` is an additional calendar category for an authored single standard race; it does not introduce new gameplay rules.

The six development events now reference independent race assets, all retaining the original prototype venue/settings until authored otherwise. Their starting days are 4, 6, 10, 12, 17, and 20, with intervals of 4, 5, 6, 4, 5, and 6 weeks respectively. Deathmatch events already have dedicated race assets and retain their default start dates unless edited. The development builder also creates independent round assets for future generated events.

Timing edits or newly added events apply to new campaigns. Saved campaigns keep their captured event bank and dates. Race/reward content is still resolved from the event definition when entering; active entries retain their saved round IDs, rules, and payout tables. Changing an event's round count is rejected for an existing campaign; use a new campaign after structural schedule changes. Keep referenced event/race assets available for old saves.

## Save compatibility

Week-only saves retain their week and migrate to Day 1. Existing active entries retain paid fees, standings, pending attempts and race IDs; remaining legacy championship rounds use seven-day spacing. The first refresh captures the event bank for migrated saves. No save deletion or scene rebuild is required.

## UI

Home displays day-specific event symbols (R race, C championship, ! deathmatch, O other), today's exact day, locked/past/entered states, and paged event lists for busy dates. Selecting an event opens its race details and exact occurrence date, including future championship rounds. Entry confirmation states the destination date and warns that earlier events will be missed.

The post-race UI now uses separate pages: live standings → final classification → payout → return to career. Payout callbacks cache rewards until the payout page is selected; late callbacks cannot reopen standings on top of it.

## Validation

- `dotnet run --project Tests/Calendar/Calendar.csproj`: dated entry, same-week events, fixed repeats, venue/reward identity, bank persistence, migration, rollback, championship dates, and existing calendar/deathmatch/succession validations. Domain is a separate assembly; a fields-only System.Text.Json shim runs the editor validations headlessly.
- `dotnet run --project Tests/CampaignCreation/CampaignCreation.csproj`: campaign preparation and save regressions.
- `dotnet run --project Tests/OutcomePages/OutcomePages.csproj`: production outcome-controller transitions with presentation doubles, including early/late callbacks.

Unity editor compilation and visual play-mode checks remain necessary, particularly calendar layout, event selection, and final-results/payout navigation. The headless JSON shim and presentation doubles do not replace Unity runtime verification.
