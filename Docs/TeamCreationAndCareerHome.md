# Team creation, team paint, and career home

## Behavior

- Click either team color square to open an HSV picker. Drag the saturation/value gradient and rainbow hue slider, then Apply or Cancel. Hex entry remains optional. The gradient supports arrow adjustments when selected; WASD also makes fine adjustments. Escape cancels.
- Review creates an isolated, unsaved session containing the actual player and partner bikes, installed parts/equipment, starter perk, resources, and week. Begin Career commits that exact session. Back/forward without edits retains the roll; changing identity or colors rebuilds the draft. Cancel does not activate a session or occupy a slot.
- New campaigns acknowledged on review do not open the starter-summary popup on career home. The separate successor-racer flow keeps its existing introduction.
- Home now uses dated recurring events from a fixed campaign event bank. Entry advances to the event date; completion preserves it. See `Docs/DatedCareerCalendar.md` for scheduling, migration, and post-race page behavior.

## Paint

`RacerViewController.Initialize` applies saved bike colors to the existing `M_Metal` material slot on player and opponent bike renderers. `Motorbike_ LOD_0` references this material. No prefab rebuild is required.

`Assets/Game/Resources/BikeTeamPaintSettings.asset` references the existing paint mask, upper metal material, and a compositor shader. Its white mask region receives primary paint; a narrow UV-space stripe within that region receives secondary paint. Black mask areas retain the source base color. The supplied mask is grayscale, so it cannot specify independent artist-authored primary and secondary regions. The stripe is a configurable initial accent, not a panel-accurate livery; adjust `accentStart`/`accentWidth` after seeing it on the mesh (width zero disables it). A future two-region mask can replace the stripe.

The compositor replaces only the base-color texture through a per-material property block. HDRP metallic/AO/smoothness, normal, lower-metal emissive, and glass remain unchanged. Textures are generated once per distinct team color pair, shared between bikes, and released when the last user is destroyed. Default paint resolution is 1024 to keep 12-bike memory reasonable; the settings asset can raise it to 2048.

## Verification

Run `dotnet run --project Tests/CampaignCreation/CampaignCreation.csproj` for isolated preview, unchanged-roll navigation, invalid/occupied slots, simulated failed writes, retry, save/load fidelity, and legacy session creation.

Unity editor checks still required:
1. Start from Bootstrap; create a team using both swatches. Verify gradient drag, hue, Apply/Cancel, and optional hex edits.
2. Review the scrollable starting equipment, go back without editing, and confirm the perk is unchanged. Begin Career and verify there is no initial popup.
3. Inspect the calendar at 1920x1080 and the game's supported aspect ratios. Select dates/events, browse four-week blocks, return to NOW, and confirm browsing does not advance the saved date.
4. Enter a race with contrasting team colors. Check player/partner and opponent paint, stripe placement, preserved unpainted metal/glass, and shader behavior in a built HDRP player.

Existing scenes receive these UI additions at runtime; do not rerun destructive scene scaffold builders for this update.
