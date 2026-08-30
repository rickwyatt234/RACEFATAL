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

        public float Handling { get; }

        public float Mass { get; }

        public BikePerformance(
            float topSpeedMPH,
            float acceleration,
            float handling,
            float mass)
        {
            TopSpeedMPH = topSpeedMPH;
            Acceleration = acceleration;
            Handling = handling;
            Mass = mass;
        }
    }
}