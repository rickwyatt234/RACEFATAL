using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace RaceFatal.Presentation.Career
{
    public class ResearchTreeViewport : ScrollRect
    {
        public override void OnScroll(PointerEventData data)
        {
            if (!IsActive() || content == null) return;
            SetZoom(content.localScale.x * Mathf.Pow(1.15f, data.scrollDelta.y));
            data.Use();
        }
        public void SetZoom(float scale)
        {
            if (content == null) return;
            scale = Mathf.Clamp(scale, .3f, 1.4f);
            float old = content.localScale.x;
            content.anchoredPosition *= scale / old;
            content.localScale = Vector3.one * scale;
            StopMovement();
        }
        public void Focus(Vector2 point)
        {
            StopMovement(); content.anchoredPosition = -point * content.localScale.x;
        }
    }
}
