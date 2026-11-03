# Brands and contract progression

Research is retired: no RP, staff payouts, technology gates, tech tree or research tools remain. The Career navigation slot is now **Brands**. Existing component stats, owned inventory and race reward amounts other than RP are preserved.

## Playable loop

- **Lifetime fame** records positive team fame awards and determines event/contract eligibility. **Available fame** is spent on boxes, Association permits and championship entry. Spending and refunds do not change lifetime fame.
- A random box costs **150 fame** and adds **three individual contracts**. Every eligible brand has equal odds. Duplicate brands add contracts without replacing existing copies. There are no rotating/free offers: more contracts require another box.
- The RF Association sells permanent category-filter permits for **500 fame** each. Filtered openings still cost 150 fame. Ordinary openings remain completely random.
- Keep an unlimited collection; activate up to **three** contracts. Inactive contracts never progress. Several active contracts can progress in one race. There are no expiration dates or exclusivity rules.
- Only the player-controlled racer counts. Supported objectives: destructions, finished races, wins, podiums, consecutive podiums; optional race and/or track restrictions. Nonmatching races are ignored, including for restricted streaks. Matching non-podium finishes and DNFs reset a podium streak. Deactivation also resets a streak; cumulative progress remains when deactivating other objectives.
- Finalized DNF/destruction results retain valid combat accomplishments. Abandoned/nonfinal races grant no progress. Team-owned contracts, standing and purchasing access survive racer succession.
- Completion frees its active slot. Claim rewards in Brands: credits, purchase access, or one physical item/complete bike plus replacement purchase access. Claims grant permanent standing. Tiers are Contact / Recognized / Partner / Icon at 0 / 2 / 6 / 15 standing.
- Discovery reveals the brand's catalog and opens basic stock. Advanced stock requires contract access. Previously owned components remain purchasable, even when destroyed. Undiscovered brand stock is hidden from Shop unless already owned/unlocked.
- Existing championship credit charges remain; their separate fame fee is **150** on the current championship asset and configurable per event. Registration checks both currencies, charges once per event, and refunds both if the launch save fails. Paid entry retries do not charge again.

## UI

Run **RACE//FATAL > Career > Build Brands UI** outside Play Mode once after pulling this change. The command opens and saves `01_Career`, authors every Brands page and control, and wires the controller. All elements exist in the scene before Play Mode: header/navigation, fixed reusable row slots, search input, catalog scroll view, confirmation controls and box reveal. Runtime code binds existing elements, updates their text/visibility/listeners, and animates the reveal. It never creates or destroys Brands UI objects or components.

Select `CareerCanvas/BrandsRoot` and use the inspector's page-preview buttons to edit Overview, Directory, Contracts, Brand Boxes, RF Association, Confirmation or Box Reveal. Fonts, positions, dimensions and other visual properties remain authored in the scene. Pagination reuses six directory rows and three contract rows; it does not spawn rows. Save scene edits before entering Play Mode. The layout uses the Career canvas's reference resolution; adjust the saved BrandsUI RectTransform for your layout.

Running **Build Brands UI** again preserves a complete existing layout and repairs the controller reference. **Rebuild Brands UI (Reset Layout)** explicitly confirms replacing the generated BrandsUI subtree and its customizations. No automatic runtime fallback exists: missing authoring produces an actionable error asking for the Build command. Scene generation requires Unity Editor; it has not been executed in this container.

The terminal occupies the Career content area. Tabs: Overview, Directory, Contracts, Brand Boxes, RF Association. Directory supports category browsing, search, pagination and scrollable catalog details; contracts have available/active/reward/archive views and pagination. Purchase/access/free-item rewards are labeled explicitly. Home and Team show available and lifetime fame. Race results show updated/completed contract counts; individual progress is in Brands.

Box purchase is confirmed, resolved and saved **before** the 3.2-second reveal. Skip only skips the animation. A saved pending receipt can be resumed after leaving the screen or reloading. The acquisition happens once, independently of the animation. Failed saves retain changes in memory, show a retry message and block further transactions in Brands until saving succeeds. Save/retry before quitting after an error.

## Content authoring

An empty authored brand list loads **24 starter brands** across Shields, Chassis, Ballistics, EnergyWeapons, Explosives, Utility, Propulsion and Bikes. Stock is mapped deterministically by category and stable product ID; each brand's cheapest product is basic stock. Starter objectives and economy numbers are initial tuning values. Brands without hardware currently offer credits contracts; this is stated in their directory entry.

Select the Bootstrap **GameContentCatalog**, then **RACE//FATAL > Brands > Materialize Starter Brand Catalog** to turn the fallback catalog into editable assets. This never overwrites existing authored brands. Once materialized, the authored list is authoritative; new products must be assigned explicitly. Use **Validate Selected Catalog** after edits.

Create individual assets through **RaceFatal > Career > Brand**. Configure permanent ID, name, tagline, accent color, category, preferred team philosophy, products and contract templates. Product identity is item kind + definition ID. A product belongs to one brand. BasicStock opens on discovery. Each brand needs at least one introductory contract with zero fame/standing requirement. Contract fields include objective, target, optional race/track IDs, minimum lifetime fame, required standing, standing reward, reward kind and item/credits. Acquired contracts snapshot their terms; later template edits do not change paid copies. Do not rename released brand or item IDs.

Opponent teams acquire preferred brand relationships from their philosophy. Standing/access advances with the player's lifetime fame (one standing per 200 fame, capped at 15; advanced access at 6). This establishes their progression preferences without granting player contract progress or changing authored starting loadouts. Automatic equipment replacement is not introduced by this system.

## Saves and removed content

New saves use version 2; version 1 remains readable. Legacy available fame initializes lifetime fame. Old RP, technology and researcher fields are ignored and not re-saved; there is no compensation. Inventory and settlement receipts remain. Former development hardware lives under BrandDevelopment with its asset GUIDs and persisted item IDs retained for save compatibility; labels and gates are updated. These legacy IDs are identifiers, not research mechanics.

Each acquired contract has a unique instance ID. Box receipts, activation, progress, claims, standing, permits, access and both fame balances are saved. Captures detach brand state from live objects. Existing race settlement receipts prevent progress and payouts from being repeated, including after reload. Invalid progress, duplicate contract identities and inconsistent fame balances are rejected on load.

## Validation

Portable regression suites cover the domain and save mapper with a System.Text.Json adapter (not Unity JsonUtility). `Tests/Brands` covers opening, duplicates, term snapshots, maximum active contracts, player attribution, replay, DNFs, streaks, venue constraints, rewards, inventory, missing rewards, legacy saves, succession, opponent preference and championship fame-fee rollback. `Tests/BrandsUI` runs 20 headless hierarchy/event checks using the production layout authoring and binding code. It checks every page, populated/empty rows, confirmation/reveal reuse, absence of runtime objects/components, preserved font/position edits, repeat-build preservation, and Play Mode/reset guards. This adapter does not render Unity UI or validate native serialization.

Unity Editor compilation and Play Mode remain required: start from Bootstrap, run Build Brands UI outside Play Mode, customize and save a page, open all five tabs, use keyboard/controller navigation and text search, buy/skip/resume a box, complete and claim each objective/reward type, test the championship confirmation and interrupted launch, kill the player and create a successor, save/quit/reload, and check layout at supported resolutions. Validate native JsonUtility persistence and unavailable-disk error handling. No claim of Unity rendering or Play Mode verification is made by portable tests.
