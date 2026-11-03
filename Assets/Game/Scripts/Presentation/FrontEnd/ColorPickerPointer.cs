using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace RaceFatal.Presentation.FrontEnd
{
    public sealed class ColorPickerPointer : Selectable, IDragHandler, ISubmitHandler
    {
        public Action<Vector2> Changed;
        public Func<Vector2> Read;
        public Action Submit;
        public void OnSubmit(BaseEventData e) { Submit?.Invoke(); e.Use(); }
        public override void OnPointerDown(PointerEventData e) { base.OnPointerDown(e); Select(); OnDrag(e); }
        public override void OnMove(AxisEventData e)
        {
            if (Read == null) return;
            var point = Read() + e.moveVector * .02f;
            Changed?.Invoke(new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y)));
            e.Use();
        }
        public void OnDrag(PointerEventData e)
        {
            var rect=(RectTransform)transform;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,e.position,e.pressEventCamera,out var p))
                Changed?.Invoke(new Vector2(Mathf.Clamp01((p.x-rect.rect.xMin)/rect.rect.width),Mathf.Clamp01((p.y-rect.rect.yMin)/rect.rect.height)));
        }
    }
}
