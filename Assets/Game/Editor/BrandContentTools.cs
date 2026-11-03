#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Content;
using RaceFatal.Content.Career;
using UnityEditor;
using UnityEngine;

public static class BrandContentTools
{
    [Serializable]
    private sealed class BrandAssetData
    {
        public string id, displayName, tagline, colorHex;
        public BrandCategory category;
        public RaceFatal.Shared.TeamPhilosophy philosophy;
        public List<BrandProduct> products;
        public List<BrandContractTemplate> contracts;
    }

    public static void Materialize()
    {
        var catalog = Selection.activeObject as GameContentCatalogSO;
        if (catalog == null) { Debug.LogError("Select the GameContentCatalog used by Bootstrap."); return; }
        if (catalog.BrandDefinitions.Count > 0) { Debug.LogError("This catalog already has authored brands. Edit those assets; materialization never overwrites them."); return; }
        var database = GameDatabaseFactory.CreateGameDatabase(catalog);
        const string folder = "Assets/Game/Brands";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Game", "Brands");
        var serialized = new SerializedObject(catalog);
        var entries = serialized.FindProperty("brandDefinitions");
        foreach (var brand in database.BrandDefinitions.Values.OrderBy(b => b.Id))
        {
            string path = folder + "/" + brand.Id + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<BrandDefinitionSO>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BrandDefinitionSO>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new BrandAssetData {
                    id = brand.Id, displayName = brand.DisplayName, tagline = brand.Tagline, colorHex = brand.ColorHex, category = brand.Category,
                    philosophy = brand.Philosophy, products = brand.Products.ToList(), contracts = brand.Contracts.Select(c => c.Copy()).ToList()
                }), asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            int index = entries.arraySize++; entries.GetArrayElementAtIndex(index).objectReferenceValue = asset;
        }
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        Debug.Log("Starter brands saved as editable assets. Restart from Bootstrap after editing content. Existing contract copies retain their original terms.");
    }

    public static void Validate()
    {
        var catalog = Selection.activeObject as GameContentCatalogSO;
        if (catalog == null) { Debug.LogError("Select the GameContentCatalog used by Bootstrap."); return; }
        try
        {
            var database = GameDatabaseFactory.CreateGameDatabase(catalog);
            var shop = new ShopService(database);
            var ownership = new HashSet<string>();
            foreach (var brand in database.BrandDefinitions.Values)
            {
                if (brand.Contracts.Count == 0 || !brand.Contracts.Any(c => c.MinimumLifetimeFame == 0 && c.RequiredStanding == 0)) throw new InvalidOperationException(brand.DisplayName + " needs an introductory contract.");
                if (brand.Contracts.Select(c => c.Id).Distinct().Count() != brand.Contracts.Count) throw new InvalidOperationException(brand.DisplayName + " has duplicate contract IDs.");
                foreach (var item in brand.Products)
                {
                    if (item == null || shop.FindOffer(item.Kind, item.DefinitionId) == null) throw new InvalidOperationException(brand.DisplayName + " references missing shop stock.");
                    if (!ownership.Add(item.Key)) throw new InvalidOperationException(item.Key + " belongs to more than one brand.");
                }
                foreach (var contract in brand.Contracts)
                {
                    contract.Validate();
                    if (!string.IsNullOrEmpty(contract.RaceId) && database.GetRaceDefinition(contract.RaceId) == null) throw new InvalidOperationException("Missing race: " + contract.RaceId);
                    if (!string.IsNullOrEmpty(contract.TrackId) && database.GetTrackDefinition(contract.TrackId) == null) throw new InvalidOperationException("Missing track: " + contract.TrackId);
                    if (contract.RewardKind != BrandRewardKind.Credits && (!brand.Products.Any(p => p.Kind == contract.ItemKind && p.DefinitionId == contract.ItemId) || shop.FindOffer(contract.ItemKind, contract.ItemId) == null)) throw new InvalidOperationException("Reward must refer to this brand's shop stock: " + contract.Id);
                }
            }
            Debug.Log($"Brand catalog valid: {database.BrandDefinitions.Count} brands, {ownership.Count} products.");
        }
        catch (Exception error) { Debug.LogError("Brand validation failed: " + error.Message); }
    }
}
#endif
