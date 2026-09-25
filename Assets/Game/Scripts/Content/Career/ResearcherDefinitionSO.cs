using RaceFatal.Career;
using UnityEngine;
namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(menuName = "RaceFatal/Career/Researcher Contract")]
    public class ResearcherDefinitionSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Min(0)] [SerializeField] private int creditCost = 2000;
        [Min(1)] [SerializeField] private int pointsPerRace = 20;
        [Min(1)] [SerializeField] private int duration = 5;
        public ResearcherDefinition CreateDefinition() => new ResearcherDefinition(id, displayName, creditCost, pointsPerRace, duration);
    }
}
