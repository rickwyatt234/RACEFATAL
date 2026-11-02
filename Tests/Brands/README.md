# Brand progression regression suite

Run `dotnet run --project Tests/Brands/Brands.csproj` with .NET 8. This compiles production domain and save code and runs brand contracts plus shop/team/recovery checks. The fields serialization adapter does not substitute for Unity JsonUtility or Play Mode verification. See `Docs/BrandContracts.md` for the full acceptance pass.

`dotnet build Tests/BrandsUI/BrandsUI.csproj` checks the new screen's API usage against a small Unity adapter; it does not render the screen.
