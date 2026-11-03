# Developer resource tools

Select `Assets/Game/Editor/DeveloperTools.asset` and keep its inspector open (lock the inspector if useful). Enter Play Mode from Bootstrap, load a campaign, and open Career.

- Credits: +1,000, +10,000, +100,000.
- Fame: +150, +1,000, +10,000. Both available and lifetime team fame increase.
- Custom credits/fame: grant either or both in one operation. Negative amounts, empty grants, and integer overflow are rejected before either balance changes.
- Save after grant: optional, initially off. Normal future campaign saves still persist granted resources.
- Save campaign now / retry save: explicitly persist current state. If saving fails, the grant stays applied; retry saving rather than granting again.
- Session log: recent grants, rejections and save results appear here and in the Unity Console.

Balances in the inspector update live. Reopen the current Career tab to refresh its labels and offers. Controls are disabled outside Play Mode, without a saved campaign, outside Career, and during pending races. No toolbar command or gameplay UI is added. All tooling resides in the Editor assembly and is excluded from player builds.

Validation: `dotnet run --project Tests/DeveloperResources/DeveloperResources.csproj` checks grants, lifetime history and atomic validation. Native Unity inspector/input acceptance remains required.
