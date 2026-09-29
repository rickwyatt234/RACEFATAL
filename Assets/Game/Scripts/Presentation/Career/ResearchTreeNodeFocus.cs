using UnityEngine;
using UnityEngine.EventSystems;

namespace RaceFatal.Presentation.Career
{
    public class ResearchTreeNodeFocus : MonoBehaviour, ISelectHandler
    {
        public ResearchTreeViewport Viewport { get; set; }
        public Vector2 Position { get; set; }

        public void OnSelect(BaseEventData data)
        {
            if (!(data is PointerEventData))
                Viewport?.Focus(Position);
        }
    }
}
