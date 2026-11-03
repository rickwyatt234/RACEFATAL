# Authored UI regression checks

Run `dotnet run --project Tests/UiAuthoring/UiAuthoring.csproj` from the repository root with .NET 8.

The headless hierarchy adapter exercises the production `AuthoredPage`, `UiBindings`, and `RacePauseView` runtime code: 10,001 entries remain reachable through three saved rows; empty, shrinking and partial pages work; repeated binding preserves styles and avoids duplicate listeners; pause/resume/hide/confirmation callbacks reuse saved controls. It counts GameObjects/components to detect runtime allocations.

Roslyn then parses all first-party C# sources with and without `UNITY_EDITOR`, checks the UI construction call allowlist, and checks that only the two Brands UI `MenuItem` attributes remain. The reported check count includes per-file parsing and per-call static checks, not thousands of independent gameplay cases.

This is not a replacement for Unity Editor compilation, serialized prefab/scene reload, or input/rendering tests. See `Docs/EditorAuthoredUI.md` for the mandatory migration and acceptance flow.
