using System;

namespace RaceFatal.Racing
{
    public enum RaceAnnouncement
    {
        EnergyLow, FinalLap, HostileLock, IntegrityCritical, LeadAcquired, LeadLost,
        PropulsionActivated, RaceComplete, RacerEliminated, ShieldsCritical,
        ShieldsRestored, SponsorObjectiveComplete, TacticalMode, WeaponsActivated
    }

    // Stateful edges, rather than per-frame conditions, prevent warning spam.
    public sealed class RaceAnnouncementState
    {
        private bool energyWarned, integrityWarned, shieldWarned, shieldDamaged;
        private bool locked, tactical, finalLap;
        private int previousPosition;

        public void Sample(float energyRatio, float integrityRatio, float shieldRatio, bool hasShield,
            int position, int completedLaps, int lapCount, bool lapRace, bool hardLock, bool tacticalOpen,
            float lowEnergyThreshold, Action<RaceAnnouncement> announce)
        {
            Warn(energyRatio, lowEnergyThreshold, Math.Min(1f, lowEnergyThreshold + .05f), ref energyWarned, RaceAnnouncement.EnergyLow, announce);
            Warn(integrityRatio, .1f, .15f, ref integrityWarned, RaceAnnouncement.IntegrityCritical, announce);
            if (hasShield)
            {
                Warn(shieldRatio, .1f, .15f, ref shieldWarned, RaceAnnouncement.ShieldsCritical, announce);
                if (shieldRatio < 1f) shieldDamaged = true;
                else if (shieldDamaged) { shieldDamaged = false; announce(RaceAnnouncement.ShieldsRestored); }
            }
            else { shieldWarned = false; shieldDamaged = false; }
            if (hardLock && !locked) announce(RaceAnnouncement.HostileLock);
            if (tacticalOpen && !tactical) announce(RaceAnnouncement.TacticalMode);
            locked = hardLock;
            tactical = tacticalOpen;
            // Grid order is only a baseline: spawning first does not mean an overtake.
            if (previousPosition > 0 && position > 0 && position != previousPosition)
            {
                if (position == 1) announce(RaceAnnouncement.LeadAcquired);
                else if (previousPosition == 1) announce(RaceAnnouncement.LeadLost);
            }
            previousPosition = position;
            // A one-lap event starts on its final lap; deathmatches have no final lap cue.
            if (lapRace && !finalLap && completedLaps == lapCount - 1)
            {
                finalLap = true;
                announce(RaceAnnouncement.FinalLap);
            }
        }

        private static void Warn(float value, float threshold, float reset, ref bool warned,
            RaceAnnouncement cue, Action<RaceAnnouncement> announce)
        {
            if (value >= reset) warned = false;
            if (value < threshold && !warned) { warned = true; announce(cue); }
        }
    }
}
