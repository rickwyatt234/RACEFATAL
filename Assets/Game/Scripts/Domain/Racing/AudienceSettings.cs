using System;

namespace RaceFatal.Racing
{
    [Serializable]
    public sealed class AudienceSettings
    {
        public float decayPerSecond = 1.5f;
        public float combatGraceSeconds = 4f;
        public float passFavor = 3f;
        public float passCooldownSeconds = 12f;
        public float passSeparationLaps = 0.002f;
        public float hitFavor = 2f;
        public float hitCooldownSeconds = 1f;
        public float favorPerDamage = 0.04f;
        public float damageFavorPerSecond = 5f;
        public float shieldBreakFavor = 12f;
        public float shieldBreakCooldownSeconds = 10f;
        public float sustainedFireFavor = 3f;
        public float sustainedFireSeconds = 2f;
        public float sustainedFireGapSeconds = 1.25f;
        public float collisionFavor = 5f;
        public float ramFavor = 10f;
        public float impactCooldownSeconds = 2f;
        public float destructionFavor = 30f;
        internal static float Safe(float value, float fallback = 0f) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(0f, value);
    }
}
