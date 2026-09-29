using System;
using System.Collections.Generic;
using RaceFatal.Combat;

namespace RaceFatal.Racing
{
    public sealed class RaceAudience
    {
        private sealed class Rival
        {
            public bool HasOrder, WasAhead;
            public double LastPass = double.NegativeInfinity, LastShield = double.NegativeInfinity;
            public double LastWeaponHit = double.NegativeInfinity, FireStarted, LastSustain;
        }

        private readonly Dictionary<string, Rival> rivals = new Dictionary<string, Rival>();
        private AudienceSettings settings = new AudienceSettings();
        private double elapsed, favorIntegral, lastCombat, lastHit = double.NegativeInfinity;
        private double lastImpact = double.NegativeInfinity, damageWindow;
        private float damageFavorInWindow;
        private double previousPlayerProgress;
        private bool hasPlayerProgress;
        public float Favor { get; private set; } = 100f;
        public float AverageFavor => elapsed > 0 ? ClampFavor((float)(favorIntegral / elapsed)) : 100f;
        public float FameMultiplier => MultiplierFor(AverageFavor);
        public float ActiveSeconds => (float)elapsed;
        public string LastAction { get; private set; } = "AUDIENCE READY";

        internal void Configure(AudienceSettings value) => settings = value ?? new AudienceSettings();
        public static float ClampFavor(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 100f : Math.Max(0f, Math.Min(200f, value));
        public static float MultiplierFor(float averageFavor) => 0.5f + ClampFavor(averageFavor) / 200f;
        internal void Tick(float deltaTime, RaceState state, RaceParticipant player)
        {
            if (player == null || player.Status != RaceParticipantStatus.Racing || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                return;
            double graceEnd = lastCombat + AudienceSettings.Safe(settings.combatGraceSeconds);
            double heldSeconds = Math.Min(deltaTime, Math.Max(0, graceEnd - elapsed));
            double decaySeconds = deltaTime - heldSeconds;
            double rate = AudienceSettings.Safe(settings.decayPerSecond);
            double fallingSeconds = rate > 0 ? Math.Min(decaySeconds, Favor / rate) : decaySeconds;
            favorIntegral += Favor * heldSeconds + Favor * fallingSeconds - 0.5 * rate * fallingSeconds * fallingSeconds;
            Favor = ClampFavor((float)(Favor - rate * fallingSeconds));
            elapsed += deltaTime;
            TrackPasses(state, player);
        }

        internal void RecordDamage(DamageEvent hit, bool ramAttack)
        {
            float damage = hit.ShieldAbsorbed + hit.BikeDamage;
            if (damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
                return;
            lastCombat = elapsed;
            Rival rival = GetRival(hit.VictimRacerId);
            if (elapsed - damageWindow >= 1)
            {
                damageWindow = elapsed;
                damageFavorInWindow = 0f;
            }

            float damageFavor = Math.Min(damage * AudienceSettings.Safe(settings.favorPerDamage), Math.Max(0f, AudienceSettings.Safe(settings.damageFavorPerSecond) - damageFavorInWindow));
            damageFavorInWindow += damageFavor;
            AddFavor(damageFavor, "DAMAGE");
            bool impact = ramAttack || hit.Cause == DamageCause.Collision;
            if (impact && elapsed - lastImpact >= Math.Max(0.1f, AudienceSettings.Safe(settings.impactCooldownSeconds)))
            {
                lastImpact = elapsed;
                AddFavor(ramAttack ? settings.ramFavor : settings.collisionFavor, ramAttack ? "RAM HIT" : "BIKE IMPACT");
            }

            if (!impact && hit.Cause == DamageCause.Weapon)
            {
                if (elapsed - lastHit >= Math.Max(0.1f, AudienceSettings.Safe(settings.hitCooldownSeconds)))
                {
                    lastHit = elapsed;
                    AddFavor(settings.hitFavor, "WEAPON HIT");
                }

                if (elapsed - rival.LastWeaponHit > AudienceSettings.Safe(settings.sustainedFireGapSeconds))
                    rival.FireStarted = rival.LastSustain = elapsed;
                rival.LastWeaponHit = elapsed;
                float interval = Math.Max(0.1f, AudienceSettings.Safe(settings.sustainedFireSeconds));
                if (elapsed - rival.FireStarted >= interval && elapsed - rival.LastSustain >= interval)
                {
                    rival.LastSustain = elapsed;
                    AddFavor(settings.sustainedFireFavor, "SUSTAINED FIRE");
                }
            }

            if (hit.ShieldDepleted && elapsed - rival.LastShield >= Math.Max(0.1f, AudienceSettings.Safe(settings.shieldBreakCooldownSeconds)))
            {
                rival.LastShield = elapsed;
                AddFavor(settings.shieldBreakFavor, "SHIELD BROKEN");
            }

            if (hit.CausedDestruction)
                AddFavor(settings.destructionFavor, "RACER DESTROYED");
        }

        private void TrackPasses(RaceState state, RaceParticipant player)
        {
            if (state.Deathmatch != null || !player.HasCourseSample)
                return;
            double progress = player.AudienceCourseProgress;
            bool movingForward = hasPlayerProgress && progress > previousPlayerProgress;
            float separation = Math.Max(0.0001f, AudienceSettings.Safe(settings.passSeparationLaps));
            foreach (RaceParticipant other in state.Participants)
            {
                if (other == player || other.TeamId == player.TeamId)
                    continue;
                Rival rival = GetRival(other.RacerId);
                if (other.Status != RaceParticipantStatus.Racing || !other.HasCourseSample)
                {
                    rival.HasOrder = false;
                    continue;
                }

                double gap = other.AudienceCourseProgress - progress;
                gap -= Math.Floor(gap + 0.5);
                if (Math.Abs(gap) < separation)
                    continue;
                bool ahead = gap > 0;
                if (rival.HasOrder && rival.WasAhead && !ahead && movingForward && elapsed - rival.LastPass >= Math.Max(0.1f, AudienceSettings.Safe(settings.passCooldownSeconds)))
                {
                    rival.LastPass = elapsed;
                    AddFavor(settings.passFavor, "OVERTAKE");
                }

                rival.WasAhead = ahead;
                rival.HasOrder = true;
            }

            previousPlayerProgress = progress;
            hasPlayerProgress = true;
        }

        private Rival GetRival(string racerId)
        {
            if (!rivals.TryGetValue(racerId, out Rival rival))
                rivals.Add(racerId, rival = new Rival());
            return rival;
        }

        private void AddFavor(float amount, string action)
        {
            amount = AudienceSettings.Safe(amount);
            if (amount <= 0f)
                return;
            Favor = ClampFavor(Favor + amount);
            LastAction = action;
        }
    }
}
