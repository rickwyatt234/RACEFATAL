# Opponent team roster randomizer

Select your GameContentCatalog asset and expand **Randomize Opponent Team Rosters** at the bottom of its Inspector.

1. Register the opponent teams you want to populate in the catalog's Opponent Team Definitions list.
2. Enable **Use all project racers** to include RacerDefinition assets under Assets, or disable it to use only registered catalog racers.
3. Choose **Distribute all eligible racers**, or turn it off and set **Racers per team** (default: 2).
4. Add exclusions for any racers you want to reserve. Partners referenced by NewCampaignDefaults assets are automatically excluded.
5. Use **Preview assignments** for the current seed or **New shuffle** for a new seed. Inspect the team rosters shown below.
6. Click **Apply preview to team assets**, then use Unity's **Save Project** command. Undo restores both rosters and catalog registrations.

Each racer is assigned at most once. When distributing everyone, team sizes differ by at most one; which teams receive the extra racers is also shuffled. Fixed-size mode requires enough unique racers to fill every team and leaves excess racers unassigned. Applying replaces all catalog opponent team racer lists, including their old assignments.

Newly assigned project racers are added to the catalog automatically. Blank or duplicate IDs are rejected. No racer assets are modified, and team philosophies, colors and bike builds stay as authored. This is an authoring shuffle for new campaigns, not a per-campaign runtime shuffle or a personality randomizer. Existing saved campaign rosters are unaffected.

The tool does not create extra bikes. Review each team's starting garage before assigning more race entrants. Teams outside the selected catalog are not edited or considered part of its uniqueness scope.

Validation: editor source syntax and patch whitespace checked. Unity editor execution remains required: preview, apply, Undo/Redo, Save Project, and start a fresh campaign to verify generated rosters.
