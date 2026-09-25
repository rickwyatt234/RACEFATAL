using System;
using System.Collections.Generic;
using UnityEngine;
namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(menuName = "RaceFatal/Career/Research Tree Layout")]
    public class ResearchTreeLayoutSO : ScriptableObject
    {
        [Serializable]
        public class Node
        {
            public TechnologyDefinitionSO technology;
            public Vector2 position;
        }
        [SerializeField] private List<Node> nodes = new List<Node>();
        public IReadOnlyList<Node> Nodes => nodes;
        public Vector2 PositionFor(string id, Vector2 fallback)
        {
            foreach (var node in nodes)
                if (node != null && node.technology != null && node.technology.Id == id) return node.position;
            return fallback;
        }
    }
}
