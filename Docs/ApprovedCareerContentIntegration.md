# RACE//FATAL — Approved Career Content Integration (Review Draft)

This PR introduces the code and data foundations for the career/content decisions made through Category 15. It is **not** a claim that all authored assets and combat mechanics are finished. Keep this PR as a draft until the Unity playtest and content integration items below are resolved.

## Approved direction

- **Nine bikes**: HARBINGER (1S/2M/2L), STELLA (2/2/1), CASTLE (2/2/2), WIRE (3/2/1), SERAPH (2/3/0), KATANA (1/3/1), CAFE (2/2/0), GOBLIN (3/2/0), DEMEANOR (2/1/1). Frame-wide base penalties remain an outstanding modeling task.
- **Six engine families**, Combustion/Gaussian/Plasma/Flux/Turbine/Atomic, each Mk I–IV (24 archetypes). Mk I usable in Classes 5 and 4; Mk II in Class 3; Mk III in Class 2; Mk IV in Class 1.
- **11 chassis**, **11 boosters**, **26 weapons**, **11 shields**. The universal lateral ram must ultimately be energy-free and use no weapon node. Ablative shields have no recharge.
- **19 approved utilities** retained in the planning blueprint, with 4 Small, 11 Medium and 4 Large slots. The actual advanced utility behaviors require new runtime classes (do not substitute plain HandlingUtilityDefinition instances).
- **14 manufacturers**: Pilion, Pace Industries, Catalyx, Kappa Engineering, Monochrome, One Kinetics, Leistung, Scudo Migliore, Hivemind Corp, Osaki Labs, Lance Reba Group, Trident, Snapdragon Incorporated, Alion Limited. Eight special exclusive variants recorded in the blueprint.
- Five manufacturer relationship levels: Unknown (0), Recognized (100), Established (300), Trusted (650), Partner (1100). Up to three active contracts; brand-specific permanent Team Perks are late standing rewards.
- **Seven tracks**, forward/reverse = 14 planned layouts: Metropolis, Mountain Forest, Ocean/Beach, Southwestern Desert, Japanese-inspired city, Underground Cave, Antarctica. No old eight-setting environments retained. Actual terrain, prefabs, graph/checkpoints and reverse testing are not supplied by this code PR.
- **21 opposing teams**, 42 initial racers, seven philosophies, distinct signature bikes, persistent identities, no inherited rivalries, immediate replacements. Team identities stay numbered placeholders.
- **Dense weekly calendar**: 12–20 regular offerings when the authored pool is large enough, plus 2–5 championship offerings; overlapping offerings are intentional. Week-one starters keep their authored days. Championships support up to seven rounds. Missed events are not simulated.
- **Economy**: total installed-equipment loss when bike is destroyed, no resale, no routine upkeep, no free successor bike, no subsidized recovery. Entry fees and payout balance remain authored per event; true terminal bankruptcy UI still requires work.
- **50 numbered Career Incident placeholders**, three debug choices, saveable pending state; only basic debug resolution is implemented, not a player-facing event overlay.
- **30 distinct Player Perks**, one free starter and purchasable additional perks with Character Fame, no slot cap, no duplicate ID, lost on racer death. Starting perk may be cycled before acknowledging the introduction.
- **28 manufacturer Team Perks** (two per brand) with permanent, nonrepeatable team ownership and contract grants. No Team Perk equip cap.
- Temporary Career Incident modifiers, R&D/research, UI polish and audio/presentation are **out of scope**.

## Code implemented in this draft

1. The 30-perk and 28-perk registries, plus 21-team/7-track/19-utility/8-exclusive planning blueprints.
2. Team perk contract claim, duplicate protection, persistence through save/restore, and several economic/racing modifiers.
3. Player perk catalog registration in GameDatabase, starter perk cycling, purchase/storage behavior, and several real stat hooks: energy capacity/recovery/drain, acceleration/steering/integrity, collision resistance, conditional defense, shield capacity/recharge, weapon output and finish Character Fame.
4. Default manufacturer names/catalog assignments and five-level standing.
5. Replacements for deceased opposing racers and destroyed preset bikes, drawn from legal authored bike builds (does **not** invent higher-class builds).
6. Seeded denser career event scheduling and seven-round validation.
7. Removal of free racer succession starter kits and recovery assistance.
8. Fifty incident definitions with basic one-time outcomes and pending state persistence.
9. Non-destructive opt-in editor authoring tool for missing bike/engine/chassis/booster/shield/weapon ScriptableObjects.

## Generating provisional content in Unity

1. Open the project in Unity and select the active `GameContentCatalogSO` asset in the Project browser.
2. Run **RACE//FATAL → Career → Generate Approved Vehicle and Equipment Definitions**.
3. The command creates missing definitions under `Assets/Game/GeneratedApprovedContent/` and registers them in the selected catalog. Existing registered IDs or matching names are preserved.
4. Inspect each generated definition. Prices, mass, speed, damage, shields, ranges and prefab references are **provisional**. New bikes reuse an existing prefab if available and are **not** finished bike models.
5. Verify available equipment nodes, weapon targeting modes, and engine-class legality in Play Mode. Do not bulk-publish these assets before checking each effect.

The tool intentionally does **not** generate imaginary playable tracks, 21 teams with invented racer/loadout links, or fake functional advanced utilities.

## Remaining integration before merge

- Implement each currently data-only Player Perk: conditional driving physics, projectile guidance/lock timing, anti-missile cooldown, leech/disruption, audience event categories, prestige rewards and utility activation effects. Several Team Perks also require specialized equipment/utility runtime hooks.
- Make all 19 utilities real equipment with the specified activation and cooldown mechanics. Existing `RaceEquipmentSystem` only supports its currently authored utility classes.
- Author the 21 teams with 42 individual racer definitions, two legal bike builds apiece, stable traits, and class-appropriate progression. This PR only supplies a team blueprint and baseline replacement logic.
- Author prefabs, checkpoints, AI routes, reverse starts and camera validation for all seven environments; code-only blueprints are not playable tracks.
- Fill out competition template assets so the weekly 12–20 targets can be reached. A small catalog intentionally shows fewer events rather than duplicating fictional races.
- Add a Career Incident presentation panel, invocation after races/day changes, cooldown/weight selection and more sophisticated result tables. Debug resolution does not yet roll random outcomes.
- Add explicit terminal bankruptcy handling and presentation, financial risk UI, real reward/payout price tuning, and remove stale recovery wording in any unmodified screens.
- Audit old save compatibility: replacing legacy fallback-brand names (24 old default companies → 14 approved companies) can leave old brand IDs beyond index 13 without a matching definition. A save migration or explicit reset policy is required.
- Run all .NET tests and Unity Editor validation/playtests, including save–quit–reload, racer death, full equipment loss, championship succession and manufacturer reward claiming. **Not executed from this connector session.**

## Test entrypoint

`dotnet run --project Tests/ApprovedContent/ApprovedContent.csproj`

Additional existing Brand, Calendar, Career Recovery and vehicle tests should be run after opening this branch. This document deliberately records incomplete items rather than implying gameplay parity from catalog names alone.
