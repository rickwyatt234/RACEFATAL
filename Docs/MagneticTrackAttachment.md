# Magnetic track attachment

Bikes retain their dynamic Rigidbody, local magnetic spring/damper, steering,
braking, lateral grip and planar combat impulses. Global Rigidbody gravity stays
disabled. No WheelColliders or centreline-constrained driving are introduced.

## Contact and motion

`TrackSurfaceProbe` samples the centre plus front/rear offsets (0.8 m by default),
using the Rigidbody's physics pose. It selects the nearest valid hit per sample,
rejecting self hits and normals more than 70 degrees from expected road up.
Centre support has twice the weight of each end. Cast origins are lifted by the
sphere radius to avoid starting inside the road at low riding heights; sweep length
is extended equally to preserve capture range. Suspension height is measured
from the bike root to the averaged contact plane, not from spherecast travel.

The spatially averaged `ContactNormal` drives suspension, momentum transport and
collision response immediately. `SurfaceNormal` is separately filtered (50/s) for
alignment/AI consumers. Delaying the physical normal causes high-speed crest drift.
`LastSurfaceNormal` retains the last actual support normal for seam/air recovery.

On continuous nearby contact, `BikeMotor` carries velocity and chassis orientation
through the minimal rotation between consecutive support normals. It preserves
forward/lateral velocity instead of steering toward the track path. A replaceable
half-step normal velocity follows the curve's arc during discrete position updates;
the suspension and outward-speed limit exclude that intentional curvature component.
It is replaced, not accumulated, each tick. Changes larger than 45 degrees in one
step skip transport and use normal landing/suspension behavior.

`TrackRuntimeController` binds the existing progress path to every probe. The probe
tracks nearby segments and uses authored banking as a normal filter and alternate
cast direction if bike-down casts miss. A path never creates contact without a real
hit. Position-only paths continue to use local probing. At closely stacked parallel
roads with matching normals, normal filtering alone cannot identify collider ownership;
keep road separation beyond the probe reach or add section ownership if such layouts
are required. Authored banking must agree with road orientation.

## Loss and reset

A new spawn gets its separate initial 0.5-second acquisition grace. After actual
attachment, short misses retain driving, alignment and light attraction for up to
0.2 seconds. Longer loss releases surface driving and momentum transport. Airborne
gravity preserves the existing rule: 12 m/s² toward the last road's local down,
not world down. This grace intentionally bridges small gaps; designed jumps that
must release instantly would need an explicit detachment trigger.

Re-enable/grid placement resets contact/path/curvature history. New serialized
fields have working defaults, so existing bike prefabs require no new references.
Rigidbody interpolation is enabled; existing visual lean remains separate.

## Unity play testing

1. Test player and AI on the existing straight, up/down turn and twist pieces,
   first at low speed, then at maximum boost. Watch for bottoming out, hovering,
   missed contact and abrupt roll changes. Test reverse travel if supported.
2. Complete a vertical loop, a convex crest and a full corkscrew at several speeds.
   Stop on a wall/ceiling: attachment should persist without throttle.
3. Ram an opponent on a bank and through a loop. Sideways movement and barrier
   impacts should remain effective without throwing bikes away from the road.
4. Cross collider seams, then drive off a genuine edge. Short loss should recover;
   sustained loss should become airborne. Check intentional jumps with the grace rule.
5. Retry/reposition bikes on differently oriented grid slots and check crossings.
   Check camera/HUD separately: this change does not retune their follow behavior.
6. Compare ordinary cornering/braking and race resource timing with the baseline.

Select the probe to view three cast gizmos and the contact normal. Retain the existing
track layer mask. Adjust longitudinal offset to the bike wheelbase and mesh transitions;
keep probe reach local to avoid picking up another road. Adjust `normalResponse` for
visual alignment, not physical adhesion. Discontinuous geometry still needs authored
transitions; use shorter physics timesteps for exceptionally tight curves at high speed.

## Validation scope

`Tests/TrackAttachment` compiles the changed production classes and exercises control
flow, quaternion transport and numerical curved travel using scripted/analytic contacts.
It does not validate Unity/PhysX, actual colliders, serialized prefab overrides or camera
feel. Unity Editor compilation and the play checks above remain required before merging.
