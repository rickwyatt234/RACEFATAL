# Weapon activation cooldown regressions

Run `dotnet run --project Tests/WeaponCooldowns/WeaponCooldowns.csproj` using .NET 8.

Compiles the actual Domain/Shared source and checks press spam, idle recovery, release/repress, unselected recovery, weapon switching, cancellation, pause, permission toggles, charged firing, passive cadence, automatic tick-size invariance and shot-event reentry. Unity input/HUD behavior requires Play Mode verification.
