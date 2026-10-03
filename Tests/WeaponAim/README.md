# Gun reticle and AI lean regression checks

Run `dotnet run --project Tests/WeaponAim/WeaponAim.csproj`.

Compiles production `PlayerWeaponAim`, `WeaponPresentationProfile` and `BikeLeanController` against the real Domain assembly and small presentation doubles. Tests exercise camera framing independence, reticle/aim alignment, FOV/roll, collision convergence, selected weapon appearance/visibility and measured AI banking. System.Numerics supplies camera/quaternion math; physics hits are supplied deterministically.

The doubles do not replace Unity compilation, Canvas rendering or Play Mode collision verification. See `Docs/GunReticlesAndAILean.md` for authoring and smoke checks.
