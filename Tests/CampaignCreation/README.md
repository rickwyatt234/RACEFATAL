Run `dotnet run --project Tests/CampaignCreation/CampaignCreation.csproj`.

Compiles the production domain, save mapper/service, and new-campaign draft. Uses an in-memory repository to verify draft isolation, starter fidelity, save rollback and retries, existing-slot protection, and save/load behavior. Does not test Unity UI or GPU rendering.
