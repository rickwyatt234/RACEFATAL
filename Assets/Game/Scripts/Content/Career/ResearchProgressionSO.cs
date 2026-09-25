using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(menuName = "RaceFatal/Career/Research Progression")]
    public class ResearchProgressionSO : ScriptableObject
    {
        [SerializeField] private List<int> tierCosts = new List<int> { 50, 125, 250, 450, 750 };
        public int CostForTier(int tier)
        {
            if (tier < 1 || tier > tierCosts.Count) throw new ArgumentOutOfRangeException(nameof(tier), "Research tier is outside the configured cost table.");
            return tierCosts[tier - 1];
        }
        public string ValidateCosts()
        {
            int previous = -1;
            foreach (int cost in tierCosts)
            {
                if (cost < 0 || cost <= previous) return "Tier costs must be nonnegative and strictly increasing.";
                previous = cost;
            }
            return tierCosts.Count == 0 ? "At least one tier is required." : null;
        }
    }
}
