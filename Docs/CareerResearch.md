# Research tree and staff

## Unity setup

1. Pull the PR branch and let Unity compile.
2. Select the `GameContentCatalog` used by Bootstrap and run **RACE//FATAL > Career > Create Research Development Content**. This optional tool adds five connected technologies, four copied Shop components, two researcher offers, a tier-cost configuration and a tree layout. Starter components and existing catalog settings remain intact.
3. Run **RACE//FATAL > Career > Build Research UI** outside Play Mode. It opens and saves `01_Career`, replacing only generated Research panels and the placeholder, and wiring the controller. Existing Garage and Shop panels are retained.
4. Open **RACE//FATAL > Research > Tech Tree Editor**, choose the same catalog, then **Validate** and **Save**.
5. Run **RACE//FATAL > Career > Validate Research Economy**. It includes the original research checks and uses isolated test state, never real save slots.
6. Start from Bootstrap and load a saved campaign. Restart from Bootstrap after changing catalog content.

## Authoring the tree

The editor can create technology assets, register them, arrange their nodes and connect prerequisites. Select an existing technology in the Project window and use Add Selected Technology Asset to reuse it. Add Missing Catalog Nodes fills layout omissions.

Drag nodes to position them. Middle-drag pans; the wheel zooms. Select a prerequisite, press **Connect FROM this prerequisite**, then click its dependent. All prerequisites must be researched. Remove prerequisite IDs in the inspector to disconnect edges. The connection tool rejects cycles; Validate also checks manually authored data.

The shared layout uses positive Y upward. Arrange foundations at the bottom and advanced technologies above them. The game reads the same positions. Moving a node does not change its tier, cost or prerequisites. Remove Node From Layout preserves the technology; unregister it through the catalog to remove it from the game. Missing positions get runtime fallbacks.

Technology properties: permanent ID, display name, description, research field, tier, explicit-cost override, prerequisite IDs and display order. The catalog's Research Progression asset defaults to 50 / 125 / 250 / 450 / 750 RP. New editor-created nodes use this table. Legacy explicit-cost assets retain their overrides; disable Override Tier Cost to use tier pricing. Table entries must increase, and validation flags nodes whose tier or cost does not increase above a prerequisite.

Validation checks duplicate/missing IDs, null registrations, missing layout nodes, cycles, unknown prerequisite/component gates, and researcher terms. IDs are saved identifiers: do not rename released technology IDs.

Set an engine, chassis or equipment asset's Required Technology ID to its gate. Several different component types can share a technology. Research unlocks purchasing; it does not grant or install the components. Locked items stay visible in Shop, where **View Required Technology** opens the correct node.

## Research staff and event rewards

Create **RaceFatal > Career > Researcher Contract** assets and register them under Researcher Definitions in the catalog. Set a unique ID, name, prepaid credit cost, RP per race and duration. Different researchers can work simultaneously, but each researcher ID can have only one active contract. Contracts do not auto-renew. Expired contracts can be hired again.

The contract snapshots output, duration and display name when hired. Editing or removing its content asset later does not change a purchased contract's remaining payouts.

Race assets have a new Research Point Bonus, default zero. It supplements the existing placement RP and pays on a finalized result, including a resolved DNF. Races previews the bonus; results distinguish race, event and staff RP.

## In-game controls

Choose a field or STAFF on the left. Drag the tree to pan, wheel to zoom, or use +/- and CENTER. Nodes distinguish researched, available, low-RP and locked states. Teal connections indicate researched prerequisites. Cross-field prerequisites remain visible and navigable. Keyboard/controller focus brings off-screen nodes into view. The detail panel scrolls separately.

STAFF shows offers and existing contracts with remaining races. The header forecasts staff output for the next race. Both hiring and research use confirmations, revalidate at confirmation, and automatically save. Save failures explicitly say that changes remain in memory: use Save Campaign to retry before quitting.

Results uses a compact two-line breakdown in the existing RP value. Designers can wire RaceOutcomeController.researchBreakdownText to a separate label for a larger layout.

## Settlement and persistence

Each RaceDirector gets a unique instance ID distinct from the event definition ID. Team save data records settled results. Reprocessing the same ID returns its saved payout without granting credits, fame or RP again or advancing contracts. Entering the same event anew creates another instance and earns another payout.

Finished, destroyed and retired finalized outcomes advance contracts once. Abandoned/non-final races do not. Receipts and contract state are captured with rewards in the existing campaign save. Save retries cannot duplicate payouts.

Optional v1 fields store contracts and settlement receipts. Old saves without those fields load empty collections. Mid-race resume is not introduced; quitting before results are saved retains the previous saved campaign state.

## Development content

- Engine Calibration (tier 1) -> Advanced Propulsion (tier 2).
- Shield Modulation (tier 1) -> Adaptive Shielding (tier 2).
- Both tier-2 nodes -> Integrated Combat Systems (tier 3, Energy field).
- Integrated Combat Systems unlocks an engine and shield; earlier nodes gate two additional test components.
- Contract Engineer: 2,000 CR prepaid, +20 RP per race for 5 races.
- Research Lab: 5,000 CR prepaid, +45 RP per race for 5 races.

Copied components retain source performance stats and placeholder prices; they are test stock, not balanced upgrades. Re-running development setup preserves existing generated assets and positions and avoids duplicate registrations. When upgrading earlier development content, review retained tier/cost overrides in the editor.

## Verification

The validation menus cover research costs/prerequisites/cycles, repeated spending, Shop/Garage integration, hiring/renewal/expiry, combined contracts, DNF/abandonment, repeated settlement before and after reload, invalid contracts/receipts, and legacy saves.

The domain and save mapper compile and the validation assertions pass in a portable .NET runner that substitutes System.Text.Json for Unity JsonUtility. This is not verification of Unity serialization, UI or editor APIs. Static checks cover C# syntax and builder field references.

Unity compilation, running the validation menus in Unity and Play Mode checks remain required:

- Build UI twice; arrange/connect nodes; save/reopen the editor; compare game layout.
- Edit tier pricing and explicit overrides; inspect validation warnings.
- Pan, zoom, recenter, navigate and cancel confirmations with mouse and controller.
- Shop locked item -> research node -> unlock -> buy -> Garage install.
- Hire both offers; finish races and a DNF; check payouts, expiry and renewal.
- Save, quit and reload: verify RP, unlocks, contracts, inventory and installations.
- In a disposable test environment, force a result-save failure and retry; ensure no duplicate payout.
- Check field labels, scrolling, Shop buttons and results text at supported resolutions.
