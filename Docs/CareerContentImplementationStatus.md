# Career content and perks: implementation status

This branch is a **reviewable first implementation slice**, not a claim that the full Categories 01–15 design is implemented. It intentionally leaves the existing campaign economy, authored bike assets, track prefabs, 21-team world roster, and calendar alone. Temporary Career Incident modifiers and R&D are excluded.

## Design baseline approved in conversation

- 9 bikes; 24 engines across 6 families; 11 chassis; 11 boosters; 26 weapons; 11 shields; 19 utilities.
- 14 manufacturers: Pilion, Pace Industries, Catalyx, Kappa Engineering, Monochrome, One Kinetics, Leistung, Scudo Migliore, Hivemind Corp, Osaki Labs, Lance Reba Group, Trident, Snapdragon Incorporated, Alion Limited.
- 7 tracks (Metropolis, mountainous temperate forest, ocean/beach, Southwestern desert, Japanese-inspired city, dark underground cave, Antarctic snow), forward and reverse.
- 21 opposing teams across seven philosophies; 42 initial racers; fixed bike models, stable personalities, immediate replacements, class-legal equipment progression, emergent individual rivalries, persistent deaths.
- Total equipment destruction; no resale or upkeep; free swaps; mixed event entry fees; failure-based recovery; no R&D.
- Unlimited unique Player Perks per racer, lost on death. Permanent unique Team Perks survive racer death. Temporary modifiers deferred.

## What this branch actually adds

- 30 Player Perk definitions and costs, with stable IDs and enum identifiers. The catalog is registered in the game database if no authored asset already uses a matching ID.
- All 30 are purchasable through the existing Character Fame and roster interface; duplicates remain blocked.
- Player starter perk pool expanded to include a few new starter effects. **Selection is still automatic**, not a new character-creation picker.
- Energy Capacity Percent perk wired into the race energy pool. Legacy flat energy capacity remains supported.
- 28 Team Perk definitions (two for each of the 14 approved manufacturers), with stable IDs and a permanent per-team ownership set.
- Team perk IDs saved/restored with existing campaign save mapping; old saves with no list load as empty.
- New BrandRewardKind.TeamPerk lets authored contracts grant manufacturer-matched unique Team Perks when claimed, refusing duplicate claims. Authoring those contracts in brand assets remains outstanding.

## Important gaps: not gameplay-complete

- Most new Player Perk effect identifiers are **data-only** until hooked into the actual movement, weapon, shield, audience, and payout systems. Do not balance or ship them as though they work. Max integrity, collision resistance and Character Fame bonuses have aggregate properties but no race integration yet.
- Team Perk effects are **catalog/ownership/reward-only**; they are not applied to vehicle, equipment, economy, or race rewards yet. Manufacturer eligibility is checked against the actual authored brand display name, so older fallback brands cannot award these 28 perks.
- Existing fallback brand data still creates 24 older placeholder brands, not the approved 14. The approved manufacturers need real product and contract catalog authoring, migration strategy, and UI integration.
- Existing starting perk remains random. Free selection needs a UI and persistence flow.
- Perk display in the roster still requires design polishing and precise conditional/negative effect visualization.
- There is no complete integration for the remaining Categories 01–14 content, including 7 authored track prefabs, 21 teams, equipment family variants, class legality and career economy policy.
- Unity playmode/build and automated tests were **not run** by this GitHub-only change. Verify in the Unity Editor before merging.

## Suggested follow-up implementation order

1. Finish shared perk modifier pipeline, including context filters, caps, tradeoffs and hooks; add tests.
2. Implement team-perk rewards/activation and contract authoring for the 14 approved manufacturers; migrate fallback brands carefully.
3. Implement 9 bike/24 engine/11 chassis/11 booster/26 weapon/11 shield/19 utility catalogs with valid ScriptableObject assets and actual behavior.
4. Implement 21-team roster, persistence/progression, class legality and deaths.
5. Implement 7 forward/reverse tracks and calendar/economy integration, plus 69 optional event templates and debug-only placeholder incidents.
6. Validate save/load, racer succession, equipment destruction, rewards, brand contracts, and race runtime.

Do not merge as feature-complete without completing the above work.
