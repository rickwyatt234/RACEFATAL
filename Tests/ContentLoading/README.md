# Content loading regression checks

`dotnet run --project Tests/ContentLoading/ContentLoading.csproj` compiles the production race asset, track asset and database factory against the real Domain assembly and minimal Unity/catalog doubles. It checks missing tracks, invalid race rules, direct creation diagnostics, skipped invalid races, stale entries, unregistered tracks and valid race/deathmatch settings.

`python Tests/ContentLoading/audit_catalog.py` checks committed race, track and event GUID references. This checks serialized source assets, not Unity's imported state.

If a local race still has a missing Track reference, the Console now names the race asset. Select that asset and assign its intended TrackDefinition in **Track**. Register that track in the active catalog's **Track Definitions** list. Invalid races are excluded rather than preventing bootstrap from loading all valid content. Unity compilation and play-mode validation remain required.
