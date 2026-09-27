/*
    CALCULATED BASE PERFORMANCE OF ONE CONFIGURED BIKE
    DOES NOT INCLUDE TEMPORARY RACE EFFECTS SUCH AS BOOSTS OR DEBUFFS/BUFFS
*/


namespace RaceFatal.Vehicles
{
    public class BikePerformance
    {
        public float TopSpeedMPH { get; }
        public float TopSpeedKPH => TopSpeedMPH * 1.60934f;
        public float TopSpeedMetersPerSecond => TopSpeedMPH * 0.44704f;
        public float TopSpeedFeetPerSecond => TopSpeedMPH * 1.46667f;

        public float Acceleration { get; }

        public float BaseHandling { get; }
        public float ConfiguredHandling { get; }
        public float Handling => ConfiguredHandling;
        public float EnergyCapacity { get; }
        public float MaxIntegrity { get; }
        public float SteeringResponseMultiplier { get; }
        public float LeanResponseMultiplier { get; }
        public float StabilityMultiplier { get; }
        public float ImpactResistance { get; }

        public float Mass { get; }

        public BikePerformance(
            float topSpeedMPH,
            float acceleration,
            float handling,
            float mass, float maxIntegrity = 100f,
            float steeringResponseMultiplier = 1f, float leanResponseMultiplier = 1f,
            float stabilityMultiplier = 1f, float impactResistance = 0f,
            float passiveHandlingMultiplier = 1f, float energyCapacity = 0f)
        {
            TopSpeedMPH = topSpeedMPH;
            Acceleration = acceleration;
            BaseHandling = handling;
            ConfiguredHandling = handling * passiveHandlingMultiplier;
            MaxIntegrity = maxIntegrity;
            SteeringResponseMultiplier = steeringResponseMultiplier;
            LeanResponseMultiplier = leanResponseMultiplier;
            StabilityMultiplier = stabilityMultiplier;
            ImpactResistance = impactResistance;
            EnergyCapacity = energyCapacity;
            Mass = mass;
        }
    }
}