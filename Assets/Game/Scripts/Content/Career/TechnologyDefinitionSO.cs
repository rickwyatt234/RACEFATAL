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
        public string Id => id;

        public TechnologyDefinition CreateDefinition()
        {
            return new TechnologyDefinition(id, displayName, description, researchField,
                researchCost, prerequisiteTechnologyIds, displayOrder);
        }
    }
}
