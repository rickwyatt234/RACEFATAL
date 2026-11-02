using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    // Stable starter catalog; authored ScriptableObjects override it by using their own IDs.
    public static class BrandDefaults
    {
        private static readonly string[] Names = { "AEGIS SIGNAL", "VEILWORK", "SAINT STATIC", "MONOFORGE", "FERROTYPE", "GRAVEMETAL", "BRASS CHOIR", "KINETIC SAINT", "BLACK RECEIPT", "LUX DIVISION", "PRISM HUNGER", "GHOST CURRENT", "AFTERSHOCK", "CINDER INDEX", "RED NOTICE", "NULL VECTOR", "PROXY ANGEL", "SECOND SIGHT", "ION VOW", "OVERDRIVE SOCIAL", "VACUUM CULT", "CHROME HEIR", "DEATH ESTATE", "PARADISE MOTORS" };
        public static void Ensure(GameDatabase database)
        {
            if (database.BrandDefinitions.Count != 0) return;
            var offers = new ShopService(database).GetOffers();
            var assigned = new Dictionary<string, List<BrandProduct>>();
            for (int i = 0; i < Names.Length; i++) assigned.Add(Id(i), new List<BrandProduct>());
            foreach (var offer in offers.OrderBy(o => o.DefinitionId, StringComparer.Ordinal))
            {
                BrandCategory category = Category(database, offer);
                int index = (int)category * 3 + StableHash(offer.DefinitionId) % 3;
                assigned[Id(index)].Add(new BrandProduct { Kind = offer.Kind, DefinitionId = offer.DefinitionId, BasicStock = false });
            }
            for (int i = 0; i < Names.Length; i++)
            {
                var products = assigned[Id(i)];
                // Cheapest product per brand is its entry-level stock.
                var basic = products.OrderBy(p => offers.First(o => o.Kind == p.Kind && o.DefinitionId == p.DefinitionId).CreditCost).ThenBy(p => p.DefinitionId, StringComparer.Ordinal).FirstOrDefault();
                if (basic != null) basic.BasicStock = true;
                var templates = new List<BrandContractTemplate> {
                    new BrandContractTemplate { Id = "finish", Title = "Keep our signal on air", Objective = BrandObjective.FinishRaces, Target = 3, Credits = 2500 },
                    new BrandContractTemplate { Id = "destroy", Title = "Make the highlight reel", Objective = BrandObjective.DestroyRacers, Target = 3, Credits = 4000 },
                    new BrandContractTemplate { Id = "podium", Title = "Own the broadcast", Objective = BrandObjective.ConsecutivePodiums, Target = 3, Credits = 6500, StandingReward = 2 },
                    new BrandContractTemplate { Id = "wins", Title = "Become the headline", Objective = BrandObjective.WinRaces, Target = 2, Credits = 5000, StandingReward = 2 }
                };
                foreach (var product in products)
                    templates.Add(new BrandContractTemplate { Id = "stock_" + product.Key, Title = "Product endorsement / " + offers.First(o => o.Kind == product.Kind && o.DefinitionId == product.DefinitionId).DisplayName, Objective = BrandObjective.PodiumFinishes, Target = product.BasicStock ? 2 : 4, RequiredStanding = product.BasicStock ? 0 : 2, MinimumLifetimeFame = product.BasicStock ? 0 : 300, StandingReward = 2, RewardKind = product.BasicStock ? BrandRewardKind.FreeItem : BrandRewardKind.PurchaseAccess, ItemKind = product.Kind, ItemId = product.DefinitionId });
                var races = database.RaceDefinitions.Values.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();
                if (races.Count > 0)
                {
                    var race = races[i % races.Count];
                    templates.Add(new BrandContractTemplate { Id = "venue", Title = "Headline " + race.DisplayName, Objective = BrandObjective.WinRaces, Target = 1, TrackId = race.TrackId, Credits = 4500, StandingReward = 2 });
                }
                database.AddBrandDefinition(new BrandDefinition(Id(i), Names[i], "YOUR PERFORMANCE. OUR PROPERTY.", i % 3 == 0 ? "#65FFE1" : i % 3 == 1 ? "#BC8BFF" : "#FF678D", (BrandCategory)(i / 3), (TeamPhilosophy)(i % 7), products, templates));
            }
        }
        private static string Id(int i) => "RF_BRAND_" + i.ToString("D2");
        private static int StableHash(string text) { uint value = 2166136261; foreach (char c in text) value = unchecked((value ^ c) * 16777619); return (int)(value & 0x7fffffff); }
        private static BrandCategory Category(GameDatabase database, ShopOffer offer)
        {
            if (offer.Kind == ShopItemKind.Engine) return BrandCategory.Propulsion;
            if (offer.Kind == ShopItemKind.Chassis) return BrandCategory.Chassis;
            if (offer.Kind == ShopItemKind.Bike) return BrandCategory.Bikes;
            var item = database.GetEquipmentDefinition(offer.DefinitionId);
            if (item is ShieldDefinition) return BrandCategory.Shields;
            if (item is BoosterDefinition) return BrandCategory.Propulsion;
            if (item is WeaponDefinition weapon)
            {
                if (weapon.DeliveryMode == WeaponDeliveryMode.GuidedProjectile || weapon.DeliveryMode == WeaponDeliveryMode.Dropped) return BrandCategory.Explosives;
                if (weapon.DeliveryMode == WeaponDeliveryMode.FlameCone || offer.DefinitionId.IndexOf("laser", StringComparison.OrdinalIgnoreCase) >= 0) return BrandCategory.EnergyWeapons;
                return BrandCategory.Ballistics;
            }
            return BrandCategory.Utility;
        }
    }
}
