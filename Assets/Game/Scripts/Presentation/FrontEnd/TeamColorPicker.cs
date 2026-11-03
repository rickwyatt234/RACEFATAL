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
        [SerializeField] private GameObject overlay;
        private Texture2D gradient, hueGradient;
        [SerializeField] private RawImage field;
        [SerializeField] private RectTransform marker;
        [SerializeField] private Slider hue;
        [SerializeField] private TMP_Text value;
        [SerializeField] private Image swatch;
        [SerializeField] private ColorPickerPointer pointer;
        [SerializeField] private RawImage spectrum;
        [SerializeField] private Button cancel, accept;
        private bool initialized;
        private Action<Color> apply;
        private Button returnFocus;
        private float saturation, brightness;

        public static TeamColorPicker Create(Transform parent)
        {
            var picker = parent.GetComponentInChildren<TeamColorPicker>(true);
            if (picker == null) throw new InvalidOperationException("Author the color picker in the Editor before playing.");
            picker.InitializeBindings();
            return picker;
        }
        private void InitializeBindings()
        {
            if (initialized) return;
            initialized = true;
            pointer.Changed = SelectPoint;
            pointer.Read = () => new Vector2(saturation, brightness);
            pointer.Submit = () => hue.Select();
            gradient = new Texture2D(128,128,TextureFormat.RGB24,false);
            gradient.wrapMode = TextureWrapMode.Clamp;
            field.texture = gradient;
            hueGradient = new Texture2D(128,1,TextureFormat.RGB24,false);
            hueGradient.wrapMode = TextureWrapMode.Clamp;
            for(int x=0;x<128;x++) hueGradient.SetPixel(x,0,Color.HSVToRGB(x/127f,1,1));
            hueGradient.Apply(); spectrum.texture = hueGradient;
            hue.onValueChanged.AddListener(_ => { RefreshGradient(); RefreshSelection(); });
            UiBindings.Bind(cancel, Close);
            UiBindings.Bind(accept, () => { apply?.Invoke(Current); Close(); });
            overlay.SetActive(false);
        }
#if UNITY_EDITOR
        public void EditorAuthor()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Editor only");
            if (overlay != null) return;
            var panel = EditorUiFactory.Modal(transform, "ColorPickerOverlay", new Vector2(620, 650), out overlay);
            // The host fills the screen so the modal blocks the underlying wizard.
            var host = (RectTransform)transform;
            host.anchorMin = Vector2.zero; host.anchorMax = Vector2.one;
            host.offsetMin = host.offsetMax = Vector2.zero;
            EditorUiFactory.Text(panel, "Title", "SELECT TEAM COLOR", new Vector2(30,-25), new Vector2(560,50), 30);
            var area = EditorUiFactory.Rect(panel,"SaturationBrightness",new Vector2(30,-85),new Vector2(560,360));
            field = area.gameObject.AddComponent<RawImage>();
            pointer = area.gameObject.AddComponent<ColorPickerPointer>();
            pointer.Changed = SelectPoint;
            pointer.Read = () => new Vector2(saturation, brightness);
            marker = EditorUiFactory.Rect(area,"Selection",Vector2.zero,new Vector2(14,14));
            marker.pivot = new Vector2(.5f,.5f);
            marker.gameObject.AddComponent<Image>().raycastTarget = false;
            hue = MakeSlider(panel,"Hue",new Vector2(30,-470), new Vector2(560,30));
            var strip = EditorUiFactory.Rect(hue.transform, "HueSpectrum", Vector2.zero, new Vector2(560,30));
            strip.SetAsFirstSibling();
            spectrum = strip.gameObject.AddComponent<RawImage>(); spectrum.raycastTarget = false;
            value = EditorUiFactory.Text(panel,"Value","",new Vector2(30,-520),new Vector2(450,45));
            swatch = EditorUiFactory.Rect(panel,"Preview",new Vector2(510,-515),new Vector2(80,45)).gameObject.AddComponent<Image>();
            cancel = EditorUiFactory.Button(panel,"CANCEL",new Vector2(30,-585),new Vector2(260,45),Close);
            accept = EditorUiFactory.Button(panel,"APPLY",new Vector2(330,-585),new Vector2(260,45),()=> { apply?.Invoke(Current); Close(); });
            pointer.Submit = () => hue.Select();
            hue.navigation = new Navigation { mode=Navigation.Mode.Explicit, selectOnUp=pointer, selectOnDown=cancel };
            cancel.navigation = new Navigation { mode=Navigation.Mode.Explicit, selectOnUp=hue, selectOnRight=accept, selectOnDown=pointer };
            accept.navigation = new Navigation { mode=Navigation.Mode.Explicit, selectOnUp=hue, selectOnLeft=cancel, selectOnDown=pointer };
            overlay.SetActive(false);
        }
#endif
#if UNITY_EDITOR
        private Slider MakeSlider(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var root = EditorUiFactory.Rect(parent,name,position,size);
            root.gameObject.AddComponent<Image>().color = new Color(.2f,.3f,.35f);
            var slider = root.gameObject.AddComponent<Slider>();
            var handle = EditorUiFactory.Rect(root,"Handle",Vector2.zero,new Vector2(20,40));
            var graphic = handle.gameObject.AddComponent<Image>();
            slider.handleRect = handle; slider.targetGraphic = graphic;
            return slider;
        }
#endif
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
            if (!initialized || !overlay.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            // Fine adjustment while the hue slider is not being changed.
            float x = (Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
            float y = (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
            if (x!=0 || y!=0) SelectPoint(new Vector2(Mathf.Clamp01(saturation+x*Time.unscaledDeltaTime*.4f),Mathf.Clamp01(brightness+y*Time.unscaledDeltaTime*.4f)));
        }
        private void OnDestroy() { if(gradient!=null) Destroy(gradient); if(hueGradient!=null) Destroy(hueGradient); }
    }
}
