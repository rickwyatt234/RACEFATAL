using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public enum BrandCategory { Shields, Chassis, Ballistics, EnergyWeapons, Explosives, Utility, Propulsion, Bikes }
    public enum BrandObjective { DestroyRacers, FinishRaces, WinRaces, PodiumFinishes, ConsecutivePodiums }
    public enum BrandRewardKind { Credits, PurchaseAccess, FreeItem, TeamPerk }

    [Serializable]
    public sealed class BrandProduct
    {
        public ShopItemKind Kind;
        public string DefinitionId;
        public bool BasicStock;
        public string Key => Kind + ":" + DefinitionId;
    }

    [Serializable]
    public sealed class BrandContractTemplate
    {
        public string Id, Title;
        public BrandObjective Objective;
        public int Target = 3;
        public string RaceId, TrackId;
        public int MinimumLifetimeFame;
        public int RequiredStanding;
        public int StandingReward = 1;
        public BrandRewardKind RewardKind;
        public int Credits = 2000;
        public ShopItemKind ItemKind;
        public string ItemId;
        public BrandContractTemplate Copy() => (BrandContractTemplate)MemberwiseClone();
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Title) || Target < 1 || MinimumLifetimeFame < 0 || RequiredStanding < 0 || StandingReward < 0 || Credits < 0 ||
                !Enum.IsDefined(typeof(BrandObjective), Objective) || !Enum.IsDefined(typeof(BrandRewardKind), RewardKind) ||
                (RewardKind != BrandRewardKind.Credits && (string.IsNullOrWhiteSpace(ItemId) || (RewardKind != BrandRewardKind.TeamPerk && !Enum.IsDefined(typeof(ShopItemKind), ItemKind)))))
                throw new ArgumentException("Invalid brand contract: " + Id);
        }
    }

    public sealed class BrandDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Tagline { get; }
        public string ColorHex { get; }
        public BrandCategory Category { get; }
        public TeamPhilosophy Philosophy { get; }
        public IReadOnlyList<BrandProduct> Products { get; }
        public IReadOnlyList<BrandContractTemplate> Contracts { get; }
        public BrandDefinition(string id, string name, string tagline, string color, BrandCategory category, TeamPhilosophy philosophy, IReadOnlyList<BrandProduct> products, IReadOnlyList<BrandContractTemplate> contracts)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Brand ID and name are required.");
            Id = id; DisplayName = name; Tagline = tagline; ColorHex = color; Category = category; Philosophy = philosophy;
            Products = products ?? Array.Empty<BrandProduct>(); Contracts = contracts ?? Array.Empty<BrandContractTemplate>();
            if (!Enum.IsDefined(typeof(BrandCategory), category) || !Enum.IsDefined(typeof(TeamPhilosophy), philosophy)) throw new ArgumentException("Invalid brand category or philosophy.");
            if (Products.Any(p => p == null || string.IsNullOrWhiteSpace(p.DefinitionId) || !Enum.IsDefined(typeof(ShopItemKind), p.Kind))) throw new ArgumentException("Invalid brand product.");
            if (Products.Select(p => p.Key).Distinct().Count() != Products.Count) throw new ArgumentException("Duplicate brand product.");
            foreach (var contract in Contracts) { if (contract == null) throw new ArgumentException("Missing contract."); contract.Validate(); }
        }
    }
}
