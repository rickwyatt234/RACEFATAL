using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public class EngineDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }

        public EngineClass EngineClass { get; }

        public float TopSpeed { get; }
        public float Acceleration { get; }

        public int CreditCost { get; }

        public string RequiredTechnologyId { get; }

        public EngineDefinition(
            string id,
            string displayName,
            EngineClass engineClass,
            float topSpeed,
            float acceleration,
            int creditCost,
            string requiredTechnologyId)
        {
            Id = id;
            DisplayName = displayName;
            EngineClass = engineClass;

            TopSpeed = topSpeed;
            Acceleration = acceleration;

            CreditCost = creditCost;

            RequiredTechnologyId =
                requiredTechnologyId;
        }
    }
}