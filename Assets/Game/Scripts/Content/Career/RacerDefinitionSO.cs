using RaceFatal.Career;
using UnityEngine;

namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(
        fileName = "RacerDefinition",
        menuName = "RaceFatal/Career/Racer")]
    public class RacerDefinitionSO :
        ScriptableObject
    {
        [Header("Identity")]

        [SerializeField]
        private string id;

        [SerializeField]
        private string displayName;

        public string Id =>
            id;

        public RacerDefinition CreateDefinition()
        {
            return new RacerDefinition(
                id,
                displayName);
        }
    }
}