# Career Team tab

Outside Play Mode, run **RACE//FATAL → Career → Build Team UI**. The builder opens and saves `01_Career`, replaces the Team placeholder and wires the Team view into the existing Career controller. It can be rerun; it replaces the generated `TeamUI` child. Commit the generated scene when authoring the project in Unity.

Run **RACE//FATAL → Career → Validate Team Management** for domain and JSON save checks, then start from `00_Bootstrap`, load a campaign and open **Team**.

## Features

- Edit the team name and primary/secondary hex colors with live color swatches. Apply & Save validates the complete edit before changing saved team state. Reset Fields discards unapplied edits. Leaving and reopening the Team tab also discards unapplied edits.
- Optionally apply colors to all owned bikes. The checkbox is off by default, so customized bike paint is preserved. New purchases use the team's current colors through the existing Shop flow. Visible on-bike colors depend on the existing presentation/material pipeline.
- Inspect shared credits, Team Fame and Research Points; current player/partner; roster totals and cumulative racer statistics; owned bikes, readiness and component counts. Racer starts/podiums count individual racers, so two team racers in one race count as two starts.
- Inspect research output per race, active/expired researcher contracts, unlocked technologies. Race and championship unlocks, weekly events and standings are managed in the Races screen.
- Open Roster, Garage or Research directly from the Team screen.

Identity, bike colors and progression use the existing campaign save fields. No save version change or development content generation is required. Save Campaign retries saving current team state after a failed write; unapplied input edits require Apply & Save.

## Unity smoke check

1. Build the screen twice and confirm one TeamUI exists. Start from Bootstrap and open Team.
2. Try an empty name and invalid secondary color. Verify neither the name nor the primary color changes.
3. Apply a valid name and colors with repaint off; verify Garage paint is preserved. Repeat with repaint on and verify owned bike color state updates.
4. Save, return to the menu, reload and confirm the team name, colors, resources and progression are retained. Check the campaign slot name and Career Home name also update.
5. Hire a researcher, complete a race, return to Team, and verify the remaining contract races and resources update. Test all three navigation shortcuts.

The screen builder and runtime layout require Unity Editor validation; source parsing alone does not verify rendering or compilation.
