using System;

namespace RaceFatal.Vehicles
{
    public class ChassisDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public float Mass { get; }
        public float MaxIntegrity { get; }
        public float HandlingMultiplier { get; }
        public float SteeringResponseMultiplier { get; }
        public float LeanResponseMultiplier { get; }
        public float StabilityMultiplier { get; }
        // Fraction of structural collision damage prevented; excludes weapons and fatal hazards.
        public float ImpactResistance { get; }
        public int CreditCost { get; }
        public string RequiredTechnologyId { get; }

        public ChassisDefinition(string id, string displayName, float mass,
            float handlingMultiplier, int creditCost, string requiredTechnologyId,
            float maxIntegrity = 100f, float steeringResponseMultiplier = 1f,
            float leanResponseMultiplier = 1f, float stabilityMultiplier = 1f,
            float impactResistance = 0f)
        {
            Id = id;
            DisplayName = displayName;
            Mass = Positive(mass, nameof(mass));
            MaxIntegrity = Positive(maxIntegrity, nameof(maxIntegrity));
            HandlingMultiplier = Positive(handlingMultiplier, nameof(handlingMultiplier));
            SteeringResponseMultiplier = Positive(steeringResponseMultiplier, nameof(steeringResponseMultiplier));
            LeanResponseMultiplier = Positive(leanResponseMultiplier, nameof(leanResponseMultiplier));
            StabilityMultiplier = Positive(stabilityMultiplier, nameof(stabilityMultiplier));
            if (float.IsNaN(impactResistance) || impactResistance < 0f || impactResistance > 0.9f)
                throw new ArgumentOutOfRangeException(nameof(impactResistance));
            ImpactResistance = impactResistance;
            CreditCost = creditCost;
            RequiredTechnologyId = requiredTechnologyId;
        }

        private static float Positive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }
}
