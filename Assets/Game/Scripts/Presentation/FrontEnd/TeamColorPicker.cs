using System;
using RaceFatal.Presentation.Career;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RaceFatal.Presentation.FrontEnd
{
    public sealed class TeamColorPicker : MonoBehaviour
    {
        private GameObject overlay;
        private Texture2D gradient, hueGradient;
        private RawImage field;
        private RectTransform marker;
        private Slider hue;
        private TMP_Text value;
        private Image swatch;
        private Action<Color> apply;
        private Button returnFocus;
        private float saturation, brightness;

        public static TeamColorPicker Create(Transform parent)
        {
            var host = new GameObject("TeamColorPicker", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var picker = host.AddComponent<TeamColorPicker>();
            picker.Build();
            return picker;
        }
        private void Build()
        {
            var panel = CareerRuntimeUi.Modal(transform, "ColorPickerOverlay", new Vector2(620, 650), out overlay);
            // The host fills the screen so the modal blocks the underlying wizard.
            var host = (RectTransform)transform;
            host.anchorMin = Vector2.zero; host.anchorMax = Vector2.one;
            host.offsetMin = host.offsetMax = Vector2.zero;
            CareerRuntimeUi.Text(panel, "Title", "SELECT TEAM COLOR", new Vector2(30,-25), new Vector2(560,50), 30);
            var area = CareerRuntimeUi.Rect(panel,"SaturationBrightness",new Vector2(30,-85),new Vector2(560,360));
            field = area.gameObject.AddComponent<RawImage>();
            var pointer = area.gameObject.AddComponent<ColorPickerPointer>();
            pointer.Changed = SelectPoint;
            pointer.Read = () => new Vector2(saturation, brightness);
            gradient = new Texture2D(128,128,TextureFormat.RGB24,false);
            gradient.wrapMode = TextureWrapMode.Clamp;
            field.texture = gradient;
            marker = CareerRuntimeUi.Rect(area,"Selection",Vector2.zero,new Vector2(14,14));
            marker.pivot = new Vector2(.5f,.5f);
            marker.gameObject.AddComponent<Image>().raycastTarget = false;
            hue = MakeSlider(panel,"Hue",new Vector2(30,-470), new Vector2(560,30));
            hueGradient = new Texture2D(128, 1, TextureFormat.RGB24, false);
            hueGradient.wrapMode = TextureWrapMode.Clamp;
            for (int x=0; x<128; x++) hueGradient.SetPixel(x, 0, Color.HSVToRGB(x/127f, 1, 1));
            hueGradient.Apply();
            var strip = CareerRuntimeUi.Rect(hue.transform, "HueSpectrum", Vector2.zero, new Vector2(560,30));
            strip.SetAsFirstSibling();
            var spectrum = strip.gameObject.AddComponent<RawImage>(); spectrum.texture = hueGradient; spectrum.raycastTarget = false;
            hue.onValueChanged.AddListener(_ => { RefreshGradient(); RefreshSelection(); });
            value = CareerRuntimeUi.Text(panel,"Value","",new Vector2(30,-520),new Vector2(450,45));
            swatch = CareerRuntimeUi.Rect(panel,"Preview",new Vector2(510,-515),new Vector2(80,45)).gameObject.AddComponent<Image>();
            var cancel = CareerRuntimeUi.Button(panel,"CANCEL",new Vector2(30,-585),new Vector2(260,45),Close);
            var accept = CareerRuntimeUi.Button(panel,"APPLY",new Vector2(330,-585),new Vector2(260,45),()=> { apply?.Invoke(Current); Close(); });
            pointer.Submit = () => hue.Select();
            hue.navigation = new Navigation { mode=Navigation.Mode.Explicit, selectOnUp=pointer, selectOnDown=cancel };
            cancel.navigation = new Navigation { mode=Navigation.Mode.Explicit, selectOnUp=hue, selectOnRight=accept, selectOnDown=pointer };
            accept.navigation = new Navigation { mode=Navigation.Mode.Explicit, selectOnUp=hue, selectOnLeft=cancel, selectOnDown=pointer };
            overlay.SetActive(false);
        }
        private Slider MakeSlider(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var root = CareerRuntimeUi.Rect(parent,name,position,size);
            root.gameObject.AddComponent<Image>().color = new Color(.2f,.3f,.35f);
            var slider = root.gameObject.AddComponent<Slider>();
            var handle = CareerRuntimeUi.Rect(root,"Handle",Vector2.zero,new Vector2(20,40));
            var graphic = handle.gameObject.AddComponent<Image>();
            slider.handleRect = handle; slider.targetGraphic = graphic;
            return slider;
        }
        public void Open(Color color, Action<Color> callback, Button source)
        {
            apply = callback; returnFocus = source;
            Color.RGBToHSV(color,out var h,out saturation,out brightness);
            hue.SetValueWithoutNotify(h);
            transform.SetAsLastSibling(); overlay.SetActive(true);
            RefreshGradient(); RefreshSelection(); hue.Select();
        }
        private Color Current => Color.HSVToRGB(hue.value,saturation,brightness);
        private void SelectPoint(Vector2 point)
        {
            saturation = point.x; brightness = point.y; RefreshSelection();
        }
        private void RefreshGradient()
        {
            var pixels = new Color[128*128];
            for (int y=0;y<128;y++) for(int x=0;x<128;x++) pixels[y*128+x]=Color.HSVToRGB(hue.value,x/127f,y/127f);
            gradient.SetPixels(pixels); gradient.Apply();
        }
        private void RefreshSelection()
        {
            marker.anchorMin = marker.anchorMax = new Vector2(saturation,brightness);
            marker.anchoredPosition = Vector2.zero;
            value.text = "#" + ColorUtility.ToHtmlStringRGB(Current);
            swatch.color = Current;
        }
        public void Close() { overlay.SetActive(false); returnFocus?.Select(); }
        private void Update()
        {
            if (!overlay.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            // Fine adjustment while the hue slider is not being changed.
            float x = (Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
            float y = (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
            if (x!=0 || y!=0) SelectPoint(new Vector2(Mathf.Clamp01(saturation+x*Time.unscaledDeltaTime*.4f),Mathf.Clamp01(brightness+y*Time.unscaledDeltaTime*.4f)));
        }
        private void OnDestroy() { if(gradient!=null) Destroy(gradient); if(hueGradient!=null) Destroy(hueGradient); }
    }
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
