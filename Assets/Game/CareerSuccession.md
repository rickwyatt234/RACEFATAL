# Career succession and new racer summary

The existing Career scene automatically receives the new controls when it starts.
No scaffold rebuild is needed.

## Player flow

- A fatal race completes its existing results/reward/save flow first.
- Returning to Career presents **Create New Racer** or **Main Menu**.
- Main Menu keeps the ended run in the campaign. Loading it presents the choice again.
- Create New Racer accepts a name, saves the successor and opens the starting summary.
- The summary also appears after creating a brand-new campaign. It lists the starting perk, assigned bike, engine class, engine/chassis, installed equipment, partner readiness, team resources and calendar week.
- Garage, Roster and Career Home buttons acknowledge the summary and save that acknowledgement. Closing the game before acknowledgement shows it again without generating more rewards.
- Career Home has **Retire Player Racer**, with a separate confirmation. Retirement is permanent; former racers and their statistics remain in Roster.

## Continuity rules

Team credits, fame, research, technologies, staff contracts, owned inventory, opponent world, event unlocks and calendar survive succession. Character fame/statistics start afresh.

Death preserves the ended run reference. The race settlement closes an unfinished championship and advances the calendar exactly once. Retirement between championship rounds withdraws the event without a refund or final prize and advances a week. Retirement outside an event does not advance time. An interrupted paid entry must be resolved or explicitly withdrawn from Races before retirement.

The successor retains a ready assigned player bike. If it is unusable, an existing ready spare is assigned, excluding the partner's bike. If none exists, a fresh copy of the campaign's authored player starter build is supplied at no credit cost. Wrecks and destroyed upgrades are never repaired or revived. New campaigns save their configured starter build ID; older campaigns fall back to the existing `player_starter` definition. Keep that definition registered for older saves.

The starter perk is drawn once from the cheapest valid authored tier of player-compatible EnergyCapacity perks. It costs no Character Fame. If no eligible perk exists, creation still works and the summary says no starting perk is available. Register perks using the existing roster content tools or the content catalog. AI-only perks are excluded.

Only the player's bike receives this recovery allowance. Partner recruitment and partner bike readiness still use the existing Roster/Garage rules; the summary identifies anything that prevents entry.

Creation, recovery, starter perk and summary state are saved together. Failed writes restore the prior session, including failures during retirement or summary acknowledgement. Optional v1 save fields preserve compatibility with existing campaigns, including old saves with a null run pointer.

## Validation

Run **RACE//FATAL > Career > Validate Career Succession** in Unity. It covers death settlement, ended save summaries, permanent history, preserved resources, recovery kit, no duplicate grants, starter perk eligibility, retirement, championship withdrawal, legacy null-run saves, and returned/thrown save failures. Run **Validate Race Calendar** for the existing event regression suite.

Play Mode checks:

1. Create a new campaign and review the starting summary; enter Career Home and reload. The acknowledged summary stays dismissed.
2. Die during a championship race, finish Results, and return to Career. Choose Main Menu, reload the same slot, and confirm the choice reappears.
3. Create a successor. Review the starter perk, recovery bike/equipment and unchanged team resources; check that the previous racer and wreck remain in Roster/Garage.
4. Assign an active partner and compatible ready bikes, enter an event, finish it, then save/reload. The successor and rewards persist.
5. Cancel a retirement, then confirm retirement between rounds. Verify the career and championship ended once and a new racer can be created.
