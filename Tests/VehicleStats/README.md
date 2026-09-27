# Vehicle stat regression checks

Run `dotnet run --project Tests/VehicleStats/VehicleStats.csproj` with .NET 8.
This compiles the production Domain and Shared source directly, without Unity or test packages.
It covers chassis stat propagation, variable integrity, shield overflow, collision-only resistance,
weapon/environment damage, zero and multiple booster capacities, energy perks, handling composition,
and participant creation failures. Unity controller behavior still needs play-mode verification.
