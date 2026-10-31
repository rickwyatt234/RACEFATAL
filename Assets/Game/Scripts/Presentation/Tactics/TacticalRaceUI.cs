using System;
using System.Collections.Generic;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Racing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace RaceFatal.Presentation.Tactics
{
    public sealed class TacticalRaceUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [FormerlySerializedAs("radialRoot")] [SerializeField] private RectTransform actionListRoot;
        [SerializeField] private TMP_Text details;
        [SerializeField] private TMP_Text orders;
        private TacticalRaceController controller;
        private readonly List<GameObject> actionItems = new List<GameObject>();
        private readonly Dictionary<RacerViewController,Button> markers = new Dictionary<RacerViewController,Button>();
        private readonly Dictionary<EnergyStripAIAnchor,TMP_Text> strips = new Dictionary<EnergyStripAIAnchor,TMP_Text>();
        private RacerViewController actionSelection;
        private TMP_Text positionMarker;
        public void Initialize(TacticalRaceController value)
        {
            controller = value;
            if (canvas == null) BuildUI();
            ConfigureActionList();
            var controls = canvas.transform.Find("ControlsWindow");
            if (controls != null)
            {
                var label = controls.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "TACTICAL // RACE CONTROL\nSpace: race / tactical | W / S: pan | Scroll: zoom | MMB drag: orbit | Q: top-down | Tab: cycle | Enter: actions | Home: player";
            }
            if (Application.isPlaying && EventSystem.current == null)
                new GameObject("TacticalEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        }
        public void BuildUI()
        {
            if (canvas != null) return;
            var root = new GameObject("TacticalCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false); canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
            Text("TACTICAL // RACE CONTROL\nSpace: race / tactical | W / S: pan | Scroll: zoom | MMB drag: orbit | Q: top-down | Tab: cycle | Enter: actions | Home: player",Panel("ControlsWindow",root.transform,new Vector2(0,465),new Vector2(1750,95)),Vector2.zero,new Vector2(1700,90),23);
            details = Text("",Panel("ReconWindow",root.transform,new Vector2(-690,80),new Vector2(460,650)),Vector2.zero,new Vector2(430,620),22);
            orders = Text("",Panel("PartnerOrdersWindow",root.transform,new Vector2(0,-450),new Vector2(1650,110)),Vector2.zero,new Vector2(1600,100),23);
            ConfigureActionList();
            if (Application.isPlaying && EventSystem.current == null) new GameObject("TacticalEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            root.SetActive(false);
        }
        private void ConfigureActionList()
        {
            // Reuse the serialized wheel root so existing scenes need no UI rebuild.
            if (actionListRoot == null)
            {
                actionListRoot = new GameObject("ActionsWindow", typeof(RectTransform)).GetComponent<RectTransform>();
                actionListRoot.SetParent(canvas.transform, false);
            }
            actionListRoot.name = "ActionsWindow";
            actionListRoot.anchorMin = actionListRoot.anchorMax = new Vector2(1f, 0.5f);
            actionListRoot.pivot = new Vector2(1f, 0.5f);
            actionListRoot.anchoredPosition = new Vector2(-30f, 0f);
            actionListRoot.sizeDelta = new Vector2(340f, 560f);
            var background = actionListRoot.GetComponent<Image>() ?? actionListRoot.gameObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.03f, 0.06f, 0.96f);
            background.raycastTarget = true;
            if (actionListRoot.Find("ActionsHeader") == null)
            {
                var header = Text("RACER // ACTIONS", actionListRoot, new Vector2(0f, 245f), new Vector2(300f, 40f), 22f);
                header.name = "ActionsHeader";
            }
            actionListRoot.gameObject.SetActive(false);
        }
        private void Update()
        {
            if (controller == null || canvas == null) return;
            canvas.gameObject.SetActive(controller.IsOpen && !RacePauseController.IsGameplayBlocked);
            if (!controller.IsOpen || RacePauseController.IsGameplayBlocked) return;
            UpdateMarkers();
            if (positionMarker == null) positionMarker = Text("+ POSITION",canvas.transform,Vector2.zero,new Vector2(180,40),22);
            positionMarker.gameObject.SetActive(controller.PositionPreview.HasValue);
            if(controller.PositionPreview.HasValue) positionMarker.transform.position=controller.TacticalCamera.WorldToScreenPoint(controller.PositionPreview.Value);
            var selected = controller.Selected;
            if (selected != null)
            {
                var p = selected.Participant; var e = p.Vehicle.EnergyPool; var shield = p.Vehicle.EquipmentSystem.Shield;
                details.text = $"{p.Racer.Name}\n{p.TeamName}\n{p.SpeedKph:0} km/h";
                if (controller.Height < 130f) details.text += $"\nSHIELD {shield?.Current ?? 0f:0} / {shield?.Maximum ?? 0f:0}\nINTEGRITY {p.Vehicle.Damage.CurrentIntegrity:0} / {p.Vehicle.Damage.MaxIntegrity:0}";
                if (controller.Height < 65f)
                {
                    details.text += $"\nENERGY {e.CurrentEnergy:0} / {e.MaxEnergy:0}\nCAPACITY LOST {e.LostCapacity:0}\nWEAPON CD {p.Vehicle.EquipmentSystem.SelectedWeaponCooldownRemaining:0.0}s";
                    for (int i=0;i<p.Vehicle.EquipmentSystem.WeaponCount;i++) if (p.Vehicle.EquipmentSystem.TryGetWeaponSnapshot(i,out var weapon)) details.text += $"\n{weapon.Definition.DisplayName}: {weapon.Definition.EnergyPerActivation:0.#} E";
                }
            }
            else details.text = "Select a racer for recon and commands.";
            orders.text = controller.Partner != null ? $"PARTNER // {controller.Partner.Acknowledgment}\n{controller.Partner.Aggression} | {(controller.Partner.ConserveEnergy ? "CONSERVING" : "ENERGY AUTHORIZED")}" : "PARTNER // UNAVAILABLE";
            if (actionSelection != selected) { actionSelection = selected; RebuildActionList(); }
            actionListRoot.gameObject.SetActive(controller.WheelOpen && selected != null);
            // World-space racer markers must not intercept clicks on the command list.
            actionListRoot.SetAsLastSibling();
            if (!controller.WheelOpen || selected == null) controller.ClearPreview();
        }
        private void UpdateMarkers()
        {
            foreach (var view in controller.Racers)
            {
                if (view == null) continue;
                if (!markers.TryGetValue(view,out var button))
                {
                    var captured = view;
                    button = Button("",canvas.transform,Vector2.zero,new Vector2(195,45),() => controller.Select(captured));
                    markers[view] = button;
                }
                Vector3 screen = controller.TacticalCamera.WorldToScreenPoint(view.transform.position);
                bool visible = screen.z > 0f && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height && view.Participant.Status == RaceParticipantStatus.Racing;
                button.gameObject.SetActive(visible);
                button.transform.position = new Vector3(screen.x,screen.y + 25f,0f);
                button.interactable = view != controller.Player;
                button.GetComponent<Image>().color = view == controller.Selected ? new Color(0.5f,0.12f,0.55f,0.95f) : view == controller.Player ? new Color(0.05f,0.3f,0.4f,0.9f) : new Color(0.04f,0.07f,0.12f,0.9f);
                button.GetComponentInChildren<TMP_Text>().text = controller.Height > 130f && view != controller.Selected && view != controller.Player ? "+" : (view == controller.Player ? "PLAYER // " : controller.TaggedRacerId == view.RacerId ? "TAG // " : "") + view.Participant.Racer.Name;
            }
            foreach (var pair in markers) if (pair.Key == null || pair.Key.Participant.Status != RaceParticipantStatus.Racing) pair.Value.gameObject.SetActive(false);
            foreach (var strip in EnergyStripAIAnchor.ActiveAnchors)
            {
                if (!strips.TryGetValue(strip,out var label)) { label = Text("RECHARGE",canvas.transform,Vector2.zero,new Vector2(160,35),18); label.color = Color.cyan; strips[strip] = label; }
                Vector3 screen = controller.TacticalCamera.WorldToScreenPoint(strip.Position);
                label.gameObject.SetActive(screen.z > 0f); label.transform.position = screen;
            }
        }
        private void RebuildActionList()
        {
            foreach (var item in actionItems) { item.SetActive(false); Destroy(item); } actionItems.Clear();
            controller.ClearPreview();
            if (actionSelection == null) return;
            var actions = new List<(string,Action)>();
            bool partner = actionSelection.Participant.Role == RaceParticipantRole.PlayerPartner;
            if (partner)
            {
                actions.Add(("FORM UP",() => controller.Command(PartnerOrder.FormUp)));
                actions.Add(("COVER ME",() => controller.Command(PartnerOrder.Cover)));
                actions.Add(("DISENGAGE",() => controller.Command(PartnerOrder.Disengage)));
                actions.Add(("CONSERVE / SPEND",() => { var p=controller.Partner; if(p!=null)p.SetConservation(!p.ConserveEnergy); }));
                actions.Add(("AGGRESSION",() => {var p=controller.Partner;if(p!=null)p.SetAggression((PartnerAggression)(((int)p.Aggression+1)%3));}));
                actions.Add(("FORMATION LEFT",() => controller.Partner?.SetFormationSide(-1)));
                actions.Add(("FORMATION RIGHT",() => controller.Partner?.SetFormationSide(1)));
            }
            else
            {
                actions.Add(("TAG",controller.Tag));
                actions.Add(("TAIL / ASSIGN",() => controller.Command(PartnerOrder.Tail)));
                actions.Add(("ATTACK",() => controller.Command(PartnerOrder.Attack)));
                actions.Add(("PRESSURE LEFT",() => controller.Command(PartnerOrder.PressureLeft)));
                actions.Add(("PRESSURE RIGHT",() => controller.Command(PartnerOrder.PressureRight)));
                actions.Add(("RAM",() => controller.Command(PartnerOrder.Ram)));
            }
            for(int i=0;i<actions.Count;i++)
            {
                var action=actions[i];
                var button=Button(action.Item1,actionListRoot,new Vector2(0f, 180f - i * 66f),new Vector2(300f,56f),action.Item2);
                button.interactable = controller.Partner != null || action.Item1 == "TAG";
                actionItems.Add(button.gameObject);
                if(action.Item1.Contains("LEFT") || action.Item1.Contains("RIGHT"))
                {
                    int side=action.Item1.Contains("LEFT")?-1:1; var trigger=button.gameObject.AddComponent<EventTrigger>();
                    var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter}; enter.callback.AddListener(_=>controller.Preview(side)); trigger.triggers.Add(enter);
                    var leave=new EventTrigger.Entry{eventID=EventTriggerType.PointerExit}; leave.callback.AddListener(_=>controller.ClearPreview()); trigger.triggers.Add(leave);
                }
            }
        }
        private static Transform Panel(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);rect.anchoredPosition=position;rect.sizeDelta=size;
            var image=go.GetComponent<Image>();image.color=new Color(0.02f,0.03f,0.06f,0.9f);image.raycastTarget=false;
            return go.transform;
        }
        public static TMP_Text Text(string value,Transform parent,Vector2 position,Vector2 size,float fontSize)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);rect.anchoredPosition=position;rect.sizeDelta=size;
            var text=go.GetComponent<TextMeshProUGUI>();text.text=value;text.fontSize=fontSize;text.color=new Color(0.8f,0.95f,1f);text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
        }
        private static Button Button(string label,Transform parent,Vector2 position,Vector2 size,Action action)
        {
            var go=new GameObject(label.Length>0?label:"RacerMarker",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);rect.anchoredPosition=position;rect.sizeDelta=size;
            go.GetComponent<Image>().color=new Color(0.04f,0.07f,0.12f,0.94f);
            var button=go.GetComponent<Button>();button.onClick.AddListener(()=>action());Text(label,go.transform,Vector2.zero,size,18);return button;
        }
    }
}
