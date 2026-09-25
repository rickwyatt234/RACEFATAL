using RaceFatal.Career;
using UnityEngine;

namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(menuName = "RaceFatal/Career/Racer Perk")]
    public sealed class RacerPerkDefinitionSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 4)] [SerializeField] private string description;
        [Min(0)] [SerializeField] private int fameCost = 50;
        [SerializeField] private RacerPerkEffect effect;
        [Min(.001f)] [SerializeField] private float strength = .1f;
        public string Id => id;
        public RacerPerkDefinition CreateDefinition() =>
            new RacerPerkDefinition(id, displayName, description, fameCost, effect, strength);
    }
}
