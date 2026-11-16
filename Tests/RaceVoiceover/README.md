# Race voiceover checks

Run `dotnet run --project Tests/RaceVoiceover/RaceVoiceover.csproj`.

Compiles the production domain and voiceover controller against minimal Unity audio doubles. Checks strict thresholds, warning hysteresis, lock/tactical edges, lap and lead transitions, shield restoration, contract projection, and voice mix restoration. It does not replace in-editor audio auditioning or Unity scene validation.
