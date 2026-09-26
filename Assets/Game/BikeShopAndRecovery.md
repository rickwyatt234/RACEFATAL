# Complete bikes, preparation and team recovery

## Setup and authoring

The Shop and preparation controls extend the existing Career scene at runtime. No scaffold rebuild is required.

- On a **Bike Build** asset, enable **Available In Shop**, set **Credit Cost**, and optionally set **Required Technology Id**. Register the build and all referenced definitions in GameContentCatalog as usual.
- A complete-bike offer includes its engine, chassis and equipment mounts. All included components' technology requirements are enforced as well as the build's own requirement.
- The existing player starter build is listed at a development price of **12,000 credits**. Other builds remain unlisted until enabled. Prices are editable balance values.
- On the underlying **Bike** definition, assign **Shop Preview** to supply a static preview sprite. Without one the Shop clearly labels the preview unavailable; the build details still list the full loadout and node counts.
- Long build details scroll. Confirmation lists the exact debit. All Shop purchases now automatically save; failed saves undo the purchase. The existing Save button remains available.
- Purchased bikes are distinct physical instances with their own installed components, painted in team colors. Assign them in Garage. The owned-frame count covers all loadouts of that frame, including destroyed bikes.

## Preparation

Races displays all player/partner/bike/engine-class checks beneath the selected event's details. **Preparation / Recovery** opens a larger checklist with Garage, Roster, Shop and Career Home shortcuts. The same button is available on Garage and Roster.

The checklist diagnoses roster and loadout problems; race launch still performs the full content, opponent-grid, fee and eligibility validation. Recovery restores a basic team and may require further upgrades for higher engine classes.

## Recovery policy

The confirmation shows the proposed number of bikes, whether a new partner is needed, replacement cost, team debit and assistance amount.

1. Reuse ready owned bikes and active reserve racers first. An existing compatible pair is preferred. If no pair exists, use compatible owned starter-class bikes and supply only the missing starter builds.
2. A missing partner receives a new identity using the original partner's AI content as a template. If that content is missing, use another registered racer template. Deceased racers remain dead; the fresh identity retains a saved content ID for race spawning.
3. New bikes cost the configured starter build price. A replacement partner costs the existing recruitment fee. Free reassignment does not charge.
4. Use available team credits toward replacement costs. Recovery assistance covers only the shortfall and adds no spendable currency.
5. Assistance is available once initially and then once after each newly settled race. Reloading, retrying a result and skipping weeks cannot renew it. Fully funded replacements and free reassignments remain available.
6. An active player career is required. An interrupted paid event must be finished or withdrawn before recovery. Neither recovery nor assistance awards fame/research, advances staff contracts, repairs wrecks or advances the calendar.
7. Purchase/recovery changes and their save are one transaction. A failed write restores the prior session, including credits, grants, roster and assistance receipt.

The assistance limit prevents repeat claims without racing. A player who dismantles a subsidized bike before their next race must rebuild from owned components in Garage. Existing succession recovery remains its own rule.

## Validation

Run **RACE//FATAL > Career > Validate Bike Shop and Recovery**. Also rerun the existing Shop, Succession and Calendar validators.

The new validator covers bundle technology gates, invalid mounts, unique physical component identities, exact debits, saved purchases, funded/subsidized recovery, permanent losses, reserve reuse, stale confirmations, save failures, persistent AI template identity, support renewal and the preparation checklist. Test saves use an in-memory JSON repository and do not touch real campaign slots.

Play Mode:

- Buy a starter bike, quit/reload, and assign it in Garage. Buy a second copy and verify separate component instances.
- Author a higher-class build with a locked component. Confirm the bundle remains locked until that component's technology is researched.
- Lose the partner and bike, return to Career, inspect preparation and confirm recovery. Check the new partner's identity and AI behavior on track.
- Repeat with insufficient credits; review the assistance amount, reload and confirm no duplicate grants.
- Check the preview sprite, long equipment lists, confirmation cancellation and navigation at your supported display resolutions.
