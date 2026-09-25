using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace RaceFatal.Presentation.Career
{
    public class ResearchTreeConnections : MaskableGraphic
    {
        public struct Edge { public Vector2 from, to; public Color tint; }
        public readonly List<Edge> Edges = new List<Edge>();
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            foreach (var edge in Edges)
            {
                Vector2 normal = new Vector2(-(edge.to - edge.from).y, (edge.to - edge.from).x).normalized * 2f;
                int start = helper.currentVertCount;
                helper.AddVert(edge.from - normal, edge.tint, Vector2.zero);
                helper.AddVert(edge.from + normal, edge.tint, Vector2.zero);
                helper.AddVert(edge.to + normal, edge.tint, Vector2.zero);
                helper.AddVert(edge.to - normal, edge.tint, Vector2.zero);
                helper.AddTriangle(start, start + 1, start + 2); helper.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
