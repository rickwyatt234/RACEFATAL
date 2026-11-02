using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Racing;

namespace RaceFatal.Career
{
    [Serializable]
    public sealed class BrandRelationship
    {
        public string brandId;
        public int standing;
        public int Tier => standing >= 15 ? 3 : standing >= 6 ? 2 : standing >= 2 ? 1 : 0;
        public string TierName => new[] { "CONTACT", "RECOGNIZED", "PARTNER", "ICON" }[Tier];
    }

    [Serializable]
    public sealed class BrandContract
    {
        public string instanceId, brandId;
        public BrandContractTemplate terms;
        public int progress;
        public bool active, completed, claimed;
    }

    [Serializable]
    public sealed class BrandBoxReceipt
    {
        public string id, brandId;
        public bool newBrand;
        public List<string> contractIds = new List<string>();
    }

    [Serializable]
    public sealed class BrandProgress
    {
        public List<BrandRelationship> relationships = new List<BrandRelationship>();
        public List<BrandContract> contracts = new List<BrandContract>();
        public List<string> purchaseAccess = new List<string>();
        public List<BrandCategory> categoryPermits = new List<BrandCategory>();
        public List<string> preferredBrandIds = new List<string>();
        public BrandBoxReceipt pendingReveal;
        public int ActiveLimit => 3;
        public BrandRelationship Find(string id) => relationships.FirstOrDefault(b => b.brandId == id);
        public bool CanBuy(string brandId, BrandProduct item) => purchaseAccess.Contains(item.Key) || (Find(brandId) != null && item.BasicStock);
        public BrandRelationship Discover(string id)
        {
            var existing = Find(id);
            if (existing != null) return existing;
            var result = new BrandRelationship { brandId = id }; relationships.Add(result); return result;
        }
        public void Unlock(ShopItemKind kind, string id)
        {
            string key = kind + ":" + id;
            if (!purchaseAccess.Contains(key)) purchaseAccess.Add(key);
        }
        // Called only inside receipt-protected finalized settlement. Partners never enter here.
        public string ApplyRace(RaceResult race, RaceResultEntry player)
        {
            var messages = new List<string>();
            foreach (var contract in contracts.Where(c => c.active && !c.completed))
            {
                var t = contract.terms;
                if ((!string.IsNullOrEmpty(t.RaceId) && t.RaceId != race.RaceId) || (!string.IsNullOrEmpty(t.TrackId) && t.TrackId != race.TrackId)) continue;
                bool finish = player.Status == RaceParticipantStatus.Finished;
                int gain = 0;
                switch (t.Objective)
                {
                    case BrandObjective.DestroyRacers: gain = Math.Max(0, player.Eliminations); break;
                    case BrandObjective.FinishRaces: gain = finish ? 1 : 0; break;
                    case BrandObjective.WinRaces: gain = finish && player.Position == 1 ? 1 : 0; break;
                    case BrandObjective.PodiumFinishes: gain = finish && player.Position <= 3 ? 1 : 0; break;
                    case BrandObjective.ConsecutivePodiums:
                        if (!finish || player.Position > 3) contract.progress = 0;
                        else gain = 1;
                        break;
                }
                contract.progress = (int)Math.Min(t.Target, (long)contract.progress + gain);
                if (contract.progress >= t.Target) { contract.completed = true; contract.active = false; }
                messages.Add(t.Title + "  " + contract.progress + "/" + t.Target + (contract.completed ? " — COMPLETE; CLAIM IN BRANDS" : ""));
            }
            return string.Join("\n", messages);
        }
        public void Validate()
        {
            relationships = relationships ?? new List<BrandRelationship>(); contracts = contracts ?? new List<BrandContract>();
            purchaseAccess = purchaseAccess ?? new List<string>(); categoryPermits = categoryPermits ?? new List<BrandCategory>(); preferredBrandIds = preferredBrandIds ?? new List<string>();
            if (relationships.Any(x => x == null || string.IsNullOrWhiteSpace(x.brandId) || x.standing < 0) || relationships.Select(x => x.brandId).Distinct().Count() != relationships.Count)
                throw new ArgumentException("Invalid brand relationships.");
            if (contracts.Any(c => c == null || string.IsNullOrWhiteSpace(c.instanceId) || c.terms == null || Find(c.brandId) == null) || contracts.Select(c => c.instanceId).Distinct().Count() != contracts.Count || contracts.Count(c => c.active) > ActiveLimit)
                throw new ArgumentException("Invalid brand contracts.");
            foreach (var c in contracts)
            {
                c.terms.Validate();
                if (c.progress < 0 || c.progress > c.terms.Target || c.completed != (c.progress == c.terms.Target) || (c.claimed && !c.completed) || (c.active && c.completed)) throw new ArgumentException("Invalid contract progress.");
            }
            if (categoryPermits.Any(c => !Enum.IsDefined(typeof(BrandCategory), c)) || categoryPermits.Distinct().Count() != categoryPermits.Count || purchaseAccess.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Invalid brand unlocks.");
            if (pendingReveal != null && (string.IsNullOrWhiteSpace(pendingReveal.id) || Find(pendingReveal.brandId) == null || pendingReveal.contractIds == null || pendingReveal.contractIds.Count == 0 || pendingReveal.contractIds.Any(id => !contracts.Any(c => c.instanceId == id && c.brandId == pendingReveal.brandId)))) throw new ArgumentException("Invalid box receipt.");
        }
    }
}
