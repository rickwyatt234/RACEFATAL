# Weekly career calendar

New campaigns capture the event pool and a random seed. Week 1 always contains Chromapex Open Circuit on day 2, Chromapex Closed Circuit on day 4, and Chromapex Open Circuit II on day 6. All three have zero Fame requirements. From week 2, each week selects three distinct events from the pool, respecting each event's earliest week. The same event may appear again in later weeks; there is no fixed repeat interval.

Every week is determined at campaign creation by the saved seed and captured pool. Weeks are calculated on demand without changing random state, so browsing order, reloads and later authoring edits cannot reroll the schedule. Existing Fame gates still apply to non-starter events and locked events remain visible on the calendar.

## Dates and conflicts

Each race takes one day. A three-round championship reserves three consecutive days and counts as one of the three weekly events. All events and rounds have exclusive day slots. Events try their preferred start day, then the nearest free block; equal distances prefer later. A block may not cross the end of the week, so it moves earlier when necessary. Selection and placement backtrack when needed to fit three events rather than leaving an avoidable gap.

Entering an event advances today to that event's date. Completing it leaves today on that date, allowing another later event in the same week. Past and consumed occurrences cannot be entered. A paid championship must be completed or withdrawn from before another event can be entered. Failed launch/save rollback restores date, fee and registration; interrupted races retain their attempt IDs. Withdrawal does not advance time.

## Authoring

The reorganized assets remain under `Assets/Game/Scripts/Content/Career/Calendar`, grouped by type and venue. On each Calendar Event asset:

- **Starter Order**: 1, 2 and 3 identify the fixed opening races; 0 means random pool only. Starters must be ordinary single races with no Fame requirement. Use exactly three unique starter orders.
- **First Week**: earliest eligible week for random selection.
- **First Day Of Week**: preferred start day, 1–7.
- **Rounds**: the event's own race/venue references. Championships support 2–5 consecutive rounds, leaving space for two other weekly events.
- Fees, rewards, engine class and Fame requirements still belong to each event/race asset.

Keep at least three eligible events whose combined round counts fit seven days. Small/incomplete development catalogs show as many fitting events as possible; catalogs without explicit starters fall back to three single races sorted by ID. The shipping catalog has three explicit starters and enough events for three every week. The renamed Chromapex solo deathmatch currently uses team size 1 (career requires 2), and its team deathmatch race has no enabled deathmatch rules. Existing validation excludes these two assets until those content/format issues are resolved.

Legacy repeat/spacing fields remain serialized but are hidden in the Inspector and do not control new campaigns.

The pool and timing are captured when creating the campaign. Start a new campaign to pick up pool changes. Entry still snapshots rules and rewards; referenced event and race assets must remain available for existing saves.

## Save compatibility and UI

Existing version-1 recurring calendars retain their dates, including paid championship rounds. Older week-only saves with progress retain legacy spacing. New campaigns use calendar version 2. No scene rebuild is required.

Home shows a symbol on each occupied date, including every championship round. Selecting a symbol opens details for that exact date and venue. Post-race pages remain separate: standings, final classification, payout, then career.

## Verification

`Tests/Calendar/Calendar.csproj` checks fixed starters, seed variation, weekly counts, collision direction, week boundaries, consecutive rounds, calendar/entry agreement and saved-pool stability over many seeds and weeks. It also retains version-1 entry, rollback, rewards, migration and championship regression coverage, plus the editor calendar/deathmatch/succession suites. `Tests/CampaignCreation` verifies campaign preparation and save boundaries; `Tests/OutcomePages` verifies exclusive result/payout transitions.

Unity compilation and visual play-mode checks are still required. Headless tests use a System.Text.Json substitute for Unity serialization.
