# Shield health bubbles

CombatBike now has a ShieldBubbleEffectView connected to CollisionFeedbackView, with the existing Shield_Blue, Shield_Teal, Shield_Purple and Shield_Red prefabs assigned. Green is unused. Both feedback components support the bubble; if both feedback components are enabled on a bike, CollisionFeedbackView owns damage feedback to avoid duplicate effects/audio.

| Remaining shield / original capacity | Prefab |
| --- | --- |
| > 75% | Blue |
| > 50%, <= 75% | Teal |
| > 25%, <= 50% | Purple |
| > 0%, <= 25% | Red |
| Depleting hit | Red break pulse |

Thresholds, visible duration (0.65 seconds), break duration (0.45 seconds), Brightness Multiplier (3 by default), origin, local offset and scale are editable on ShieldBubbleEffectView. Capacity means BaseMaximum: energy loss must not make a weak shield appear healthy just because its current maximum has shrunk.

## Behavior

- Any damage event that absorbs shield triggers the whole bubble, regardless of impact side.
- Consecutive hits extend the pulse without restarting its particles. The color uses health after damage and also follows recharge/energy changes while visible.
- Colors switch immediately at thresholds. This version does not crossfade. It preserves the authored particle colors/materials and avoids relying on a generic opacity property across the different shaders and particle lights.
- Depletion plays red for the break duration alongside the existing break effect/audio. Later hull-only hits do not retrigger it. Destruction or disabling clears it immediately.
- The bubble disappears after its timer, rather than waiting for the authored 100-second particle lifetimes. The outgoing state is stopped, cleared and disabled, including its particle light.
- Runtime particle instances use local simulation and scaled game time, so they follow the bike and pause with the race. Source prefabs are unchanged.
- Instances are created lazily and reused (at most four per bike, only one active). No new object is allocated for repeated hits within a band.
- Legacy directional fields remain as a fallback for other bike prefabs without a fully configured bubble component.

For additional bike prefabs, add ShieldBubbleEffectView beside RacerViewController, assign the four prefabs, and assign it to the feedback component's Bubble Shield field (same-object auto-discovery is also supported). Adjust Local Offset / Local Scale while checking cockpit and external views. Existing scene instances with overrides may need their prefab changes applied/reverted selectively.

## Unity Play Mode verification

1. Confirm no bubble is visible at race start. Hit a shield from all four sides; each should show the same whole-bike bubble.
2. Check health immediately above and at 75%, 50% and 25%, including a large hit that skips multiple bands.
3. Fire continuously: the bubble should remain visible without restarting on every hit, then disappear 0.4 seconds after the final hit.
4. Deplete a shield from any band: red should remain visible for 0.25 seconds, with one depletion sound/effect. Hull-only follow-up hits must not restart it.
5. Reduce energy capacity, and allow recharge during a pulse: colors should reflect original capacity, not the reduced maximum.
6. Watch at racing speed and while turning: particles and light should remain attached. Pause during a pulse; it should resume without expiring during the pause.
7. Destroy/disable the bike during a pulse, then respawn/re-enable: no stale bubble or light should remain.
8. Check player and AI bikes and cockpit visibility. Tune placement/size in the Inspector if necessary.

Repository wiring and serialized references were checked outside Unity. Unity compilation and visual Play Mode checks still need to run in the editor.

## Brightness

Bubble instances now multiply the material HDR tint/emission by 3 and raise dark constant particle colors to full value while preserving hue and alpha. Material property blocks leave the shared source materials intact. Adjust **Brightness Multiplier** on CombatBike → ShieldBubbleEffectView for further tuning. Hit and break flashes last 0.65 and 0.45 seconds respectively. Verify all four shield health colors from the cockpit and chase camera in Play Mode; final perceived brightness depends on exposure and bloom.
