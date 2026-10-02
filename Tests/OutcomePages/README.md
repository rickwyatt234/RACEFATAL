Run `dotnet run --project Tests/OutcomePages/OutcomePages.csproj`.

Compiles the production RaceOutcomeController against minimal presentation doubles. Checks exclusive standings/payout visibility across live, final, and payout pages, reward/completion callback ordering, and return navigation. Does not verify Unity rendering, prefab references, or real scene loading.
