using System;

namespace RaceFatal.Career
{
    public class ShopOffer
    {
        public ShopItemKind Kind { get; }
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public int CreditCost { get; }
        public string RequiredTechnologyId { get; }

        public ShopOffer(
            ShopItemKind kind,
            string definitionId,
            string displayName,
            int creditCost,
            string requiredTechnologyId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new ArgumentException(
                    "A shop offer requires a stable definition ID.",
                    nameof(definitionId));

            if (creditCost <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(creditCost),
                    "Only positively priced content may be sold.");

            Kind = kind;
            DefinitionId = definitionId;
            DisplayName = displayName;
            CreditCost = creditCost;
            RequiredTechnologyId = requiredTechnologyId;
        }

        public bool IsUnlockedFor(TeamState team)
        {
            return team != null &&
                (string.IsNullOrWhiteSpace(RequiredTechnologyId) ||
                 team.HasTechnology(RequiredTechnologyId));
        }
    }
}
