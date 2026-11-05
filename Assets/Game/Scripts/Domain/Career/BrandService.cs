using System;
using System.Linq;
using RaceFatal.Data;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public sealed class BrandService
    {
        public const int BoxCost = 150;
        public const int CategoryPermitCost = 500;
        public const int ContractsPerBox = 3;
        private readonly GameDatabase database;
        private readonly Random random;
        public BrandService(GameDatabase database, Random random = null) { this.database = database; this.random = random ?? new Random(); }
        public static BrandDefinition OwnerOf(GameDatabase database, ShopItemKind kind, string id) => database.BrandDefinitions.Values.FirstOrDefault(b => b.Products.Any(p => p.Kind == kind && p.DefinitionId == id));
        public Result BuyPermit(TeamState team, BrandCategory category)
        {
            if (team == null || !Enum.IsDefined(typeof(BrandCategory), category)) return Result.Failure("INVALID TEAM OR CATEGORY.");
            if (team.Brands.categoryPermits.Contains(category)) return Result.Failure("PERMIT ALREADY OWNED.");
            if (!team.TrySpendFame(CategoryPermitCost)) return Result.Failure("NOT ENOUGH AVAILABLE FAME.");
            team.Brands.categoryPermits.Add(category); return Result.Success();
        }
        public Result<BrandBoxReceipt> OpenBox(TeamState team, BrandCategory? filter = null)
        {
            if (team == null) return Result<BrandBoxReceipt>.Failure("NO TEAM LOADED.");
            if (team.Brands.pendingReveal != null) return Result<BrandBoxReceipt>.Failure("FINISH YOUR PREVIOUS REVEAL FIRST.");
            if (filter.HasValue && !team.Brands.categoryPermits.Contains(filter.Value)) return Result<BrandBoxReceipt>.Failure("RF ASSOCIATION CATEGORY PERMIT REQUIRED.");
            var pool = database.BrandDefinitions.Values.Where(b => (!filter.HasValue || b.Category == filter) && b.Contracts.Any(c => Eligible(team, b.Id, c))).OrderBy(b => b.Id, StringComparer.Ordinal).ToList();
            if (pool.Count == 0) return Result<BrandBoxReceipt>.Failure("NO ELIGIBLE BRANDS IN THIS CATEGORY.");
            if (team.Fame < BoxCost) return Result<BrandBoxReceipt>.Failure("NOT ENOUGH AVAILABLE FAME.");
            var brand = pool[random.Next(pool.Count)];
            var templates = brand.Contracts.Where(c => Eligible(team, brand.Id, c) && (c.RewardKind != BrandRewardKind.TeamPerk || !team.HasTeamPerk(c.ItemId))).ToList();
            var receipt = new BrandBoxReceipt { id = Guid.NewGuid().ToString("N"), brandId = brand.Id, newBrand = team.Brands.Find(brand.Id) == null };
            var acquired = new System.Collections.Generic.List<BrandContract>();
            // Prefer distinct templates within an opening. Each acquired copy has its own identity.
            var available = templates.ToList();
            for (int i = 0; i < ContractsPerBox; i++)
            {
                if (available.Count == 0) available.AddRange(templates.Where(t => t.RewardKind != BrandRewardKind.TeamPerk));
                if (available.Count == 0) break;
                int index = random.Next(available.Count); var template = available[index]; available.RemoveAt(index);
                var contract = new BrandContract { instanceId = Guid.NewGuid().ToString("N"), brandId = brand.Id, terms = template.Copy() };
                acquired.Add(contract); receipt.contractIds.Add(contract.instanceId);
            }
            if (!team.TrySpendFame(BoxCost)) return Result<BrandBoxReceipt>.Failure("NOT ENOUGH AVAILABLE FAME.");
            team.Brands.Discover(brand.Id); team.Brands.contracts.AddRange(acquired); team.Brands.pendingReveal = receipt;
            return Result<BrandBoxReceipt>.Success(receipt);
        }
        private static bool Eligible(TeamState team, string brandId, BrandContractTemplate terms) => (terms.RewardKind != BrandRewardKind.TeamPerk || !team.HasTeamPerk(terms.ItemId) && !team.Brands.contracts.Any(c => !c.claimed && c.terms.RewardKind == BrandRewardKind.TeamPerk && c.terms.ItemId == terms.ItemId)) && team.LifetimeFame >= terms.MinimumLifetimeFame && (team.Brands.Find(brandId)?.standing ?? 0) >= terms.RequiredStanding;
        public Result SetActive(TeamState team, string instanceId, bool active)
        {
            var c = team?.Brands.contracts.FirstOrDefault(x => x.instanceId == instanceId);
            if (c == null || c.completed) return Result.Failure("CONTRACT IS NOT AVAILABLE.");
            if (c.active == active) return Result.Success();
            if (active && team.Brands.contracts.Count(x => x.active) >= team.Brands.ActiveLimit) return Result.Failure("THREE CONTRACTS ARE ALREADY ACTIVE.");
            c.active = active;
            // Banking a streak while racing other contracts would defeat consecutive objectives.
            if (!active && c.terms.Objective == BrandObjective.ConsecutivePodiums) c.progress = 0;
            return Result.Success();
        }
        public Result Claim(TeamState team, string instanceId)
        {
            var c = team?.Brands.contracts.FirstOrDefault(x => x.instanceId == instanceId);
            if (c == null || !c.completed || c.claimed) return Result.Failure("NO UNCLAIMED REWARD.");
            var t = c.terms; var relationship = team.Brands.Find(c.brandId);
            if ((long)relationship.standing + t.StandingReward > int.MaxValue || (t.RewardKind == BrandRewardKind.Credits && (long)team.Credits + t.Credits > int.MaxValue)) return Result.Failure("REWARD EXCEEDS ACCOUNT LIMIT.");
            if (t.RewardKind == BrandRewardKind.TeamPerk)
            {
                var perk = TeamPerkCatalog.Find(t.ItemId);
                if (perk == null || !string.Equals(perk.Manufacturer, database.BrandDefinitions[c.brandId].DisplayName, StringComparison.OrdinalIgnoreCase))
                    return Result.Failure("TEAM PERK DOES NOT MATCH THE MANUFACTURER.");
                if (team.HasTeamPerk(perk.Id)) return Result.Failure("TEAM PERK ALREADY CLAIMED.");
                if (!team.GrantTeamPerk(perk.Id)) return Result.Failure("COULD NOT GRANT TEAM PERK.");
            }
            else if (t.RewardKind != BrandRewardKind.Credits)
            {
                var offer = new ShopService(database).FindOffer(t.ItemKind, t.ItemId);
                if (offer == null) return Result.Failure("REWARD CONTENT IS MISSING. CONTRACT IS RETAINED.");
                if (t.RewardKind == BrandRewardKind.FreeItem)
                {
                    var granted = new ShopService(database).Grant(team, t.ItemKind, t.ItemId);
                    if (!granted.IsSuccess) return granted;
                }
                team.Brands.Unlock(t.ItemKind, t.ItemId); // Free exclusives also retain replacement purchasing access.
            }
            else team.AddCredits(t.Credits);
            relationship.standing += t.StandingReward; c.claimed = true;
            return Result.Success();
        }
        public static string RewardText(BrandContractTemplate t) => t.RewardKind == BrandRewardKind.TeamPerk ? "PERMANENT TEAM PERK: " + (TeamPerkCatalog.Find(t.ItemId)?.Name ?? t.ItemId) : t.RewardKind == BrandRewardKind.Credits ? t.Credits.ToString("N0") + " CR" : t.RewardKind == BrandRewardKind.PurchaseAccess ? "PURCHASE ACCESS: " + t.ItemId : "ONE FREE ITEM + REPLACEMENT ACCESS: " + t.ItemId;
        public void ApplyOpponentPreferences(TeamState team, OpponentTeamDefinition definition, int lifetimeFame)
        {
            if (definition == null) return;
            var profile = ApprovedCareerBlueprint.Teams.FirstOrDefault(t => t.Id == definition.Id);
            var preferred = database.BrandDefinitions.Values.Where(b => profile == null
                ? b.Philosophy == definition.Philosophy
                : b.DisplayName == profile.ManufacturerA || b.DisplayName == profile.ManufacturerB).OrderBy(b => b.Id, StringComparer.Ordinal).ToList();
            team.Brands.preferredBrandIds = preferred.Select(b => b.Id).ToList();
            foreach (var brand in preferred)
            {
                var relation = team.Brands.Discover(brand.Id);
                relation.standing = Math.Max(relation.standing, Math.Min(1100, lifetimeFame / 2));
                if (relation.standing >= 650) foreach (var item in brand.Products) team.Brands.Unlock(item.Kind, item.DefinitionId);
            }
        }
    }
}
