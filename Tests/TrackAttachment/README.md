# Track attachment regression harness

Run from the repository root with .NET 8:

```sh
dotnet run --project Tests/TrackAttachment/TrackAttachment.csproj
```

Compiles the production `BikeMotor`, `TrackSurfaceProbe`, `TrackProgressPath`,
`TrackRuntimeController`, and `BikePerformance` against small Unity/racing doubles.
Quaternion/vector operations use System.Numerics. Casts are scripted planar contacts
or analytic cylindrical surfaces; forces are integrated explicitly by the harness.
This does **not** run Unity, PhysX collision resolution, real track meshes, cameras,
or prefab serialization.

Coverage:

- Root height on floors, walls and ceilings with different sphere radii/start offsets.
- Front/rear support across a centre seam, normal filtering, self/wall/opposing-hit rejection.
- Physics pose sampling when an interpolated Transform lags behind the Rigidbody.
- Authored path-up fallback requires an actual collider hit; position-only paths remain supported.
- Full loop and bank-roll transport preserves physical speed and lateral ram velocity
  at 0.01/0.02/0.04-second timesteps, without centreline steering.
- Numerically integrated inner loops and outer crests at 75/135 m/s, radius 50 m,
  0.01/0.02-second timesteps, and 0.1/0.5 m suspension heights. Positions are never
  reset to the road during integration; height must stay positive and within 0.12 m
  of the configured target through a complete revolution.
- Short misses retain driving/adhesion; sustained misses release to local airborne gravity.
- Genuine landings/discontinuous normals do not transport old momentum.
- Grid placement/reuse clears contact, curvature and path history.

Unity play-test checklist and tuning notes: `Docs/MagneticTrackAttachment.md`.
