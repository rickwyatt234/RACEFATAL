# Audience regression checks

Run `dotnet run --project Tests/Audience/Audience.csproj` with .NET 8. No Unity or external test packages are needed. The executable compiles the production Domain, Shared and save-mapping code directly.

Checks cover neutral/start state, grace and idle decay, time weighting and frame-size independence, floor clamping, invalid values, burst budgets, sustained fire, shield breaks, collisions, rams, destruction, friendly fire, pass hysteresis/cooldowns/start-line wrap, finalized results, saved and legacy receipts, outcome freezing, individual deathmatches and fame-only reward scaling.

The windshield graphic and audio need Unity play-mode validation.
