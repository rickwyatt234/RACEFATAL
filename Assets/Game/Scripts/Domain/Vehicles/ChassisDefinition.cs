namespace RaceFatal.Vehicles
{
    public class ChassisDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }

        public float MassModifier { get; }
        public float HandlingModifier { get; }

        public int CreditCost { get; }

        public string RequiredTechnologyId { get; }

        public ChassisDefinition(
            string id,
            string displayName,
            float massModifier,
            float handlingModifier,
            int creditCost,
            string requiredTechnologyId)
        {
            Id = id;
            DisplayName = displayName;

            MassModifier = massModifier;
            HandlingModifier = handlingModifier;

            CreditCost = creditCost;

            RequiredTechnologyId =
                requiredTechnologyId;
        }
    }
}