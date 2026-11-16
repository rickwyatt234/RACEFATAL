# Race voiceover

Open `Assets/Game/Scenes/02_Race.unity`, select **RaceSystems**, and assign audio files to the matching fields on **Race Voiceover Controller**. The component is authored in the scene; RaceRuntimeController also requires it for other race setups.

| Clip | Trigger |
| --- | --- |
| Energy Low | Player energy falls below 20% of starting capacity (adjustable, includes boost strain). |
| Final Lap | Player starts their final lap, once per lap-based race; a one-lap race announces after GO. Never in deathmatch. |
| Hostile Lock | An incoming lock reaches full/hard lock on the player. Lock acquisition alone does not trigger it. |
| Integrity Critical | Player integrity falls strictly below 10%. |
| Lead Acquired / Lead Lost | Player transitions into / out of first place; initial grid position is a silent baseline. |
| Propulsion Activated | Boost becomes legal after GO, retaining the existing start-sequence timing. |
| Race Complete | Player finishes, or the whole event ends if they have not already finished; once only. |
| Racer Eliminated | Another racer is destroyed, including a teammate. No announcements after the player's own race resolves. |
| Shields Critical | An equipped shield falls strictly below 10%, including depletion. |
| Shields Restored | An equipped shield reaches exactly 100% after damage; never on spawn. |
| Sponsor Objective Complete | An active player contract meets its goal; kill objectives can trigger mid-race, finish-based goals wait for an actual finish. |
| Tactical Mode | Player enters tactical mode. |
| Weapons Activated | Weapons become legal after GO, retaining the existing start-sequence timing. |

The two existing unlock clips remain assigned on **Race Start Sequence Controller**. Leave the voiceover component's corresponding override fields empty to keep those references. Each old 0.75 gain is raised to 1.0 through the new playback path. Assigning an override replaces, rather than duplicates, the old sound. Missing clips are silently skipped.

Voice plays on a dedicated 2D source at full voice volume with highest audio-source priority. It keeps normal pitch during focus/tactical slow motion and honors AudioListener pause. While speaking, other listener audio fades to 25% in 0.12 seconds, then returns to its previous volume. The voice source bypasses listener attenuation but explicitly applies the captured original listener volume, so a muted listener remains muted. Tune **Voice Volume**, **Background Volume While Speaking**, **Duck Fade Seconds**, and **Gap Between Lines** on the component. This assumes one active race voiceover controller and that AudioListener.volume is not concurrently controlled by another system during a line; a future master-volume/mixer system should coordinate this duck there.

Lines play one at a time, with critical integrity/lock/shield warnings ahead of routine notifications. Pending warnings are discarded if their condition has already cleared; routine lines expire after ten active real-time seconds. Pause freezes queue aging. Race completion interrupts the current line and removes stale race warnings. Low-resource warnings rearm after recovery to 15% (integrity/shield) or threshold + 5 percentage points (energy). Repeated cue types have a six-second cooldown; simultaneous contract completions share a notification.

Contract announcements use a read-only projection of the same objective calculation as settlement. They do not pay rewards, modify active slots or save intermediate progress. Actual progress/rewards retain the existing finalized, receipt-protected settlement flow. The current catalog supports destruction, finish, win, podium and consecutive-podium objectives; later objective types must extend the shared calculation.

## Validation

`dotnet run --project Tests/RaceVoiceover/RaceVoiceover.csproj` checks warning edges and hysteresis, full shield recharge, lead and lap transitions, live contract projection, and production playback/mix cleanup using presentation doubles. `Tests/Brands` checks the existing contract settlement behavior.

In Unity, assign the clips and run a race: verify unlock lines, final lap, lead changes, incoming full lock, tactical entry, resource thresholds, full shield restoration, another racer destruction, active contract completion and player finish. Pause/resume during a line; confirm no overlap and that background volume returns after playback, disable or scene exit. Audition the mix with actual imported clips and adjust gain if the recordings themselves are quiet.
