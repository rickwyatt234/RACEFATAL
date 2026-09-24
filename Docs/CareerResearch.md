# Career Research

Research spends the team's existing Research Points and unlocks the technology IDs
already checked by the Shop. Engine, chassis and equipment requirements continue
using `RequiredTechnologyId`. Saves retain their existing schema and version.

## Unity setup

1. Pull this branch and let Unity compile.
2. Run **RACE//FATAL > Career > Build Research UI** outside Play Mode. This opens and
   saves `01_Career`, replacing only the generated Research panels and placeholder,
   and wiring `CareerResearchView` into the existing controller. Build the Career Hub
   first only if it does not exist. Rebuilding the whole Hub later replaces its screens.
3. Select the `GameContentCatalog` asset used by Bootstrap and optionally run
   **RACE//FATAL > Career > Create Research Development Content**. This registers three
   technologies and copies one engine and shield into `Assets/Game/ResearchDevelopment`.
   Existing starter items are unchanged. Repeat runs preserve generated assets and
   avoid registering the same asset twice. The copies have original performance stats
   and placeholder prices, so they are test stock, not balanced upgrades.
4. Run **RACE//FATAL > Career > Validate Research Progression**. This uses isolated test
   state and does not alter a campaign or write to save slots.
5. Start from `00_Bootstrap` and load a saved campaign. Earn RP through races.

Development content: Engine Calibration (50 RP) -> Advanced Propulsion (100 RP) ->
Development Research Engine (150 CR). Shield Modulation (75 RP) unlocks the
Development Research Shield (100 CR). Restart from Bootstrap after changing the catalog.

## Authoring real content

Create **RaceFatal > Career > Technology** assets. Set a unique, stable ID, display
name, description, existing Research Field, RP cost, prerequisite IDs and display
order. Add each asset to the catalog's Technology Definitions. Set components'
Required Technology ID to the corresponding ID. Empty component requirements remain
unlocked. Prerequisite IDs must exist in the catalog, and cycles are invalid.
Technology IDs are permanent save identifiers; do not rename released IDs.

The UI lists technologies by field with status, cost, prerequisites and matching Shop
unlocks. Confirmation revalidates before spending. Successful research automatically
saves; a failed save leaves the change in memory and displays an explicit warning.
Use **Save Campaign** to retry before quitting. Repeating research cannot charge again.
Only saved campaigns can research through the UI.

## Verification

The validation menu covers unknown IDs, missing teams, negative costs, missing and
cyclic prerequisites, multiple prerequisites, insufficient RP, zero costs, exact
balance, repeated research, Shop purchase, Garage inventory and save DTO JSON
roundtripping through `CampaignSaveMapper`, including a campaign with no research.

Manual Unity checks still required:

- Compile, run the validation command, and build the UI twice without duplicate panels.
- Verify scrolling, details, confirmation/cancel and keyboard/controller modal blocking.
- See the development item locked in Shop, research its prerequisites, and verify the
  debit and Shop availability. Buy, install in Garage, save, quit and reload.
- Verify credits, remaining RP, technology IDs and installed equipment persist.
- Check a pre-existing campaign and check the UI at supported resolutions.
- Force a save failure in a disposable test environment; confirm the warning and
  successful retry via Save Campaign without another RP debit.

Unity is not available in the implementation environment. Static checks are not a
substitute for Unity compilation or running the validation menu and Play Mode checks.
