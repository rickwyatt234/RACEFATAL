# Career content review

The active GameContentCatalog includes the authored assets directly. Start a new campaign from Main Menu. On campaign review, comma and period cycle bike models and square brackets cycle the starting perk. Confirm to save the chosen setup.

## Catalog and playtest scope

| System | Contents |
| --- | --- |
| Frames | Nine models across three weight classes with authored S/M/L mounts and nonpositive platform modifiers. All nine currently use the HARBINGER presentation prefab. |
| Hardware | Six four-generation engine technologies, 11 chassis archetypes, 11 boosters, 26 weapon archetypes, 11 shields and 19 utility archetypes. Shared products have small differences by manufacturer. Eight exclusive products are included. |
| Manufacturers | Fourteen editable manufacturer profiles with competing product lines, individual standing, gated contracts and two permanent Team Perks per brand. |
| Racers | Thirty authored Player Perks. New racers choose a free starter and spend personal Fame for subsequent purchases. Twenty-one opposing teams start with 42 distinct racers and signature builds. Individual rivalries, racer histories and deaths persist. |
| Calendar | Sixty-nine competition templates across circuits, sprints, endurance, high stakes, elimination, championships and invitationals. New event occurrences select directed layouts using the saved seed and date. Championship entries record every selected round. |
| Circuits | Seven greybox courses, each with forward and reverse grids, road collision, walls, progress path and lap trigger. Environment art and distinct bike meshes remain production work. |
| Career | Total equipment loss, free swapping, no resale or upkeep, cash-funded recovery, career incidents, a bankruptcy summary and championship succession choices. Incident narrative and outcome balance are provisional. |

Manual utility defaults are G for Plasma Siphon and H for Repulsion Generator. R activates the universal lateral ram while steering. Manufacturer stat differences, costs, incident outcomes and race rewards are first-pass tuning.

## Verification

The .NET test projects exercise domain systems and selected presentation behavior using test doubles. The ContentLoading Python audit checks catalog track and round links, team rosters, and each opponent build's equipment mount sizes and indices. Use Unity Play Mode for end-to-end verification of HDRP rendering, all grid starts, physical mounts, AI navigation, combat feedback and race completion in both directions. Temporary Career Incident modifiers, audio production, and UI polish were deferred.
