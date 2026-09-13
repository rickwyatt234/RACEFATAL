using System;

namespace RaceFatal.Career
{
    [Serializable]
    public class RacerDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public float Pace { get; }
        public float Aggression { get; }
        public float OvertakingSkill { get; }
        public float DefensiveSkill { get; }
        public float WeaponAggression { get; }

        public RacerDefinition(string id, string displayName, float pace, float aggression, float overtakingSkill, float defensiveSkill, float weaponAggression)
        {
            Id = id;
            DisplayName = displayName;
            Pace = pace;
            Aggression = aggression;
            OvertakingSkill = overtakingSkill;
            DefensiveSkill = defensiveSkill;
            WeaponAggression = weaponAggression;
        }
    }
}