namespace RaceFatal.Career
{
    public class ShopPurchaseReceipt
    {
        public ShopItemKind Kind { get; }
        public string DefinitionId { get; }
        public string InstanceId { get; }
        public string DisplayName { get; }
        public int CreditsSpent { get; }

        public ShopPurchaseReceipt(
            ShopOffer offer,
            string instanceId)
        {
            Kind = offer.Kind;
            DefinitionId = offer.DefinitionId;
            InstanceId = instanceId;
            DisplayName = offer.DisplayName;
            CreditsSpent = offer.CreditCost;
        }
    }
}
