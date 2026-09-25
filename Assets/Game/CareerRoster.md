# Career Roster

The Roster screen shows owned racers, race statistics, Character Fame, perks, and recruitable racers. It lets the player assign an active teammate as the race partner. This selection is saved with the campaign; older saves fall back to the original partner.

## One-time Unity setup

1. Select the `GameContentCatalogSO` asset used by `00_Bootstrap`. Run **RACE//FATAL → Career → Create Roster Development Perks** to register four sample perks, or author `RacerPerkDefinitionSO` assets and add them to the catalog's **Roster Perks** list. Each racer definition in the catalog that is not already on the team becomes recruitable.
2. Outside Play Mode, run **RACE//FATAL → Career → Build Roster UI**. This saves `01_Career`, wires the Roster view and confirmation panel, and may be rerun after updating the Career Hub scene. Commit the resulting scene and catalog changes when authoring project assets.
3. Run **RACE//FATAL → Career → Validate Roster Progression**. Start the game from `00_Bootstrap` and open Career → Roster for a visual smoke test.

Recruitment costs 5,000 team credits per racer definition. Perks spend that racer's own Character Fame; the player and participating teammate earn Fame according to their individual race finishes. Reserve Cell adds 20 maximum energy to the racer's race bike. Apex Reader, Evasion Protocol, and Predator Routine improve the AI teammate's pace, defense, and weapon aggression. AI skill perks cannot be bought for the player. Perks take effect when the next race is prepared. The selected partner must be alive and active to enter a race.

To exercise the complete flow, earn credits, recruit a catalog racer, assign them as partner, race to earn their Fame, buy an eligible perk, race again, save, quit, and reload. The selection, roster, Fame, and purchased perk IDs should persist. If a partner dies, select a different active teammate before racing again.
