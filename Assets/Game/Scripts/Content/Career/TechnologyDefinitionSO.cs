using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(fileName = "TechnologyDefinition", menuName = "RaceFatal/Career/Technology")]
    public class TechnologyDefinitionSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(3, 8)] [SerializeField] private string description;
        [SerializeField] private ResearchField researchField;
        [Min(0)] [SerializeField] private int researchCost;
        [SerializeField] private List<string> prerequisiteTechnologyIds = new List<string>();
        [SerializeField] private int displayOrder;
        [Min(1)] [SerializeField] private int tier = 1;
        [Tooltip("Legacy assets retain their explicit cost. Turn off to use the catalog tier table.")]
        [SerializeField] private bool overrideTierCost = true;
        public string Id => id;
        public string DisplayName => displayName;
        public ResearchField Field => researchField;
        public int Tier => tier;
        public IReadOnlyList<string> Prerequisites => prerequisiteTechnologyIds;
        public int Cost(ResearchProgressionSO progression) => overrideTierCost ? researchCost
            : progression != null ? progression.CostForTier(tier)
            : throw new System.InvalidOperationException("Assign Research Progression to the catalog to use tier costs.");

        public TechnologyDefinition CreateDefinition(ResearchProgressionSO progression = null)
        {
            return new TechnologyDefinition(id, displayName, description, researchField,
                Cost(progression), prerequisiteTechnologyIds, displayOrder, tier);
        }
    }
}
