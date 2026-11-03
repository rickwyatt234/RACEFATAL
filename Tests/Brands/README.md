# Brand progression regression suite

Run `dotnet run --project Tests/Brands/Brands.csproj` with .NET 8. This compiles production domain and save code and runs brand contracts plus shop/team/recovery checks. The fields serialization adapter does not substitute for Unity JsonUtility or Play Mode verification. See `Docs/BrandContracts.md` for the full acceptance pass.

`dotnet run --project Tests/BrandsUI/BrandsUI.csproj` runs 20 authored-hierarchy, binding, style preservation, toolbar and reveal checks against a headless Unity adapter. It does not render the screen.
