using UnityEngine;
using UnityEngine.EventSystems;
namespace RaceFatal.Presentation.Career
{
    // Keep off-screen nodes reachable through keyboard/controller navigation.
    public class ResearchTreeNodeFocus : MonoBehaviour, ISelectHandler
    {
        public ResearchTreeViewport Viewport { get; set; }
        public Vector2 Position { get; set; }
        public void OnSelect(BaseEventData data)
        {
            if (!(data is PointerEventData)) Viewport?.Focus(Position);
        }
    }
}
