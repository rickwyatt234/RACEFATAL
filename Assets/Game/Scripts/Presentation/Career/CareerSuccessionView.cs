using System.Linq;
using System.Text;
using RaceFatal.Career;
using RaceFatal.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    // Added to existing Career scenes at runtime, so upgrading does not require
    // rebuilding (and replacing) a player's customized Career scaffold.
    public sealed class CareerSuccessionView : MonoBehaviour
    {
        private CareerController owner;
        private GameContext context;
        private GameObject modal;
        private TMP_Text title, summary, feedback, homeLabel;
        private TMP_InputField nameInput;
        private Button primary, secondary, roster, homeAction;
        private ScrollRect introductionScroll;
        private TMP_Text introductionText;
        private TMP_Text primaryLabel, secondaryLabel;
        private int page; // 0: ended, 1: name, 2: retirement, 3: new racer summary
        private bool busy;
        private GameSessionState Session => context?.Sessions?.Current;

        public static void Create(CareerController owner, GameContext context, Transform home)
        {
            var root = Rect(home, "CareerSuccession", new Vector2(410, -650), new Vector2(1380, 320));
            var view = root.gameObject.AddComponent<CareerSuccessionView>();
            view.owner = owner;
            view.context = context;
            view.Build(home.parent);
            view.RefreshHome();
            if (context.Sessions.Current?.CareerRun?.IsActive != true) view.Show(0);
            else if (context.Sessions.Current.CareerRun.NeedsIntroduction) view.Show(3);
        }

        private void Build(Transform canvas)
        {
            homeLabel = Text(transform, "Summary", "", 23, Vector2.zero, new Vector2(1380, 160));
            homeAction = Button(transform, "CareerAction", "", new Vector2(0, -175), new Vector2(430, 58), out var actionLabel);
            homeAction.onClick.AddListener(() => Show(Session?.CareerRun?.IsActive == true ? 2 : 0));
            // Keep the action label accessible without relying on object names in scenes.
            homeActionLabel = actionLabel;

            var overlay = Rect(canvas, "CareerSuccessionModal", Vector2.zero, Vector2.zero);
            modal = overlay.gameObject;
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            modal.AddComponent<Image>().color = new Color(0, 0, 0, .9f);
            var panel = Rect(overlay, "Panel", Vector2.zero, new Vector2(1100, 690));
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.gameObject.AddComponent<Image>().color = new Color(.035f, .065f, .085f);
            title = Text(panel, "Title", "", 36, new Vector2(40, -35), new Vector2(1020, 55));
            summary = Text(panel, "Summary", "", 25, new Vector2(40, -110), new Vector2(1020, 250));
            nameInput = Input(panel, new Vector2(40, -360), new Vector2(1020, 64));
            feedback = Text(panel, "Feedback", "", 22, new Vector2(40, -445), new Vector2(1020, 100));
            primary = Button(panel, "Primary", "", new Vector2(40, -575), new Vector2(490, 70), out primaryLabel);
            secondary = Button(panel, "Secondary", "", new Vector2(570, -575), new Vector2(490, 70), out secondaryLabel);
            roster = Button(panel, "Roster", "ROSTER", new Vector2(393, -575), new Vector2(314, 70), out var rosterLabel);
            roster.onClick.AddListener(() => FinishIntroduction(1));
            var scrollRoot = Rect(panel, "Introduction", new Vector2(40, -105), new Vector2(1020, 325));
            scrollRoot.gameObject.AddComponent<Image>().color = new Color(.035f, .065f, .085f);
            introductionScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRoot.gameObject.AddComponent<RectMask2D>();
            introductionText = Text(scrollRoot, "Content", "", 24, Vector2.zero, new Vector2(990, 0));
            introductionText.rectTransform.anchorMin = new Vector2(0, 1);
            introductionText.rectTransform.anchorMax = new Vector2(1, 1);
            introductionText.rectTransform.sizeDelta = new Vector2(-24, 0);
            introductionText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            introductionScroll.content = introductionText.rectTransform;
            introductionScroll.viewport = scrollRoot;
            introductionScroll.horizontal = false;
            introductionScroll.scrollSensitivity = 35;
            introductionScroll.movementType = ScrollRect.MovementType.Clamped;
            primary.onClick.AddListener(Confirm);
            secondary.onClick.AddListener(Back);
            modal.SetActive(false);
        }

        private TMP_Text homeActionLabel;
        private void OnEnable() { if (context != null) RefreshHome(); }
        private void RefreshHome()
        {
            var session = Session;
            if (session == null) return;
            bool active = session.CareerRun?.IsActive == true;
            var playerBike = session.PlayerTeam.Garage.FindBike(session.SelectedPlayerBikeId);
            var partnerBike = session.PlayerTeam.Garage.FindBike(session.SelectedPartnerBikeId);
            var partner = session.PlayerTeam.Roster.FindRacer(session.SelectedPartnerRacerId);
            homeActionLabel.text = active ? "RETIRE PLAYER RACER" : "CONTINUE TEAM";
            homeLabel.text = active
                ? "TEAM CONTINUITY\nRetirement permanently ends this racer's career. Your team can continue with a new racer."
                : "CAREER ENDED\nCreate a new player racer to continue this team. Previous racers and their statistics remain in the Roster.";
            if (active && (playerBike?.IsRaceReady != true || partnerBike?.IsRaceReady != true || partner?.CanRace != true))
                homeLabel.text += "\nPREPARATION REQUIRED: assign two ready bikes in Garage and an active partner in Roster.";
        }

        private void Show(int nextPage)
        {
            page = nextPage;
            modal.SetActive(true);
            modal.transform.SetAsLastSibling();
            feedback.text = "";
            nameInput.gameObject.SetActive(page == 1);
            summary.gameObject.SetActive(page != 3);
            introductionScroll.gameObject.SetActive(page == 3);
            roster.gameObject.SetActive(page == 3);
            ((RectTransform)primary.transform).sizeDelta = new Vector2(page == 3 ? 314 : 490, 70);
            ((RectTransform)secondary.transform).sizeDelta = new Vector2(page == 3 ? 314 : 490, 70);
            ((RectTransform)secondary.transform).anchoredPosition = new Vector2(page == 3 ? 746 : 570, -575);
            primaryLabel.rectTransform.sizeDelta = ((RectTransform)primary.transform).sizeDelta;
            secondaryLabel.rectTransform.sizeDelta = ((RectTransform)secondary.transform).sizeDelta;
            if (page == 3)
            {
                title.text = "NEW RACER // STARTING SUMMARY";
                introductionText.text = BuildIntroduction();
                Canvas.ForceUpdateCanvases();
                introductionScroll.verticalNormalizedPosition = 1;
                primaryLabel.text = "GARAGE";
                secondaryLabel.text = "CAREER HOME";
                feedback.text = "CAMPAIGN SAVED. Scroll to review your equipment and team resources.";
                ConfigureNavigation();
                primary.Select();
                return;
            }
            var racer = Session?.CareerRun?.Player ?? Session?.PlayerTeam.Roster.Racers.LastOrDefault(r => r.IsPlayerCharacter);
            string record = racer == null ? "" : $"{racer.Name} // {racer.Status.ToString().ToUpperInvariant()}\n{racer.RacesEntered} STARTS · {racer.RacesWon} WINS · {racer.Podiums} PODIUMS\n\n";
            title.text = page == 2 ? "CONFIRM RETIREMENT" : page == 1 ? "CREATE NEW RACER" : "CAREER ENDED // TEAM CONTINUES";
            summary.text = page == 2
                ? "Permanently retire your current racer? Their statistics will remain in the Roster.\n\nAn unfinished championship will be withdrawn without a refund or final prize. Your team can continue with a new racer."
                : record + (page == 1
                    ? "Enter your new racer's name (1–40 characters). They begin with fresh personal progression. Your team's equipment carries forward. If your player bike is unusable, a ready spare or fresh starter bike will be assigned. Wrecks and lost upgrades stay destroyed."
                    : "Would you like to create a new racer or return to the Main Menu?\n\nYour credits, research, fame, inventory and calendar remain with this team.");
            primaryLabel.text = page == 2 ? "CONFIRM RETIREMENT" : page == 1 ? "CREATE & SAVE" : "CREATE NEW RACER";
            secondaryLabel.text = page == 2 ? "CANCEL" : page == 1 ? "BACK" : "MAIN MENU";
            ConfigureNavigation();
            if (page == 1) { nameInput.SetTextWithoutNotify(""); nameInput.ActivateInputField(); }
            else primary.Select();
        }

        private bool CanChange()
        {
            if (busy) return false;
            if (context.RaceLaunch?.HasPendingRace == true)
            { feedback.text = "Finish or leave the pending race before changing racers."; return false; }
            return true;
        }

        private void Confirm()
        {
            if (!CanChange()) return;
            if (page == 0) { Show(1); return; }
            if (page == 3) { FinishIntroduction(0); return; }
            busy = true;
            var result = page == 2 ? context.Saves.RetirePlayer() : context.Saves.StartSuccessor(nameInput.text);
            busy = false;
            RefreshHome();
            owner.RefreshHome();
            if (!result.IsSuccess) { feedback.text = result.ErrorMessage; return; }
            if (page == 2) { Show(0); return; }
            Show(3);
        }

        private void Back()
        {
            if (busy) return;
            if (page == 3) { FinishIntroduction(2); return; }
            if (page == 1) { Show(0); return; }
            if (page == 2) { modal.SetActive(false); homeAction.Select(); return; }
            owner.SaveAndReturnToMainMenu();
            // If scene loading/save failed, keep the decision available for retry.
            feedback.text = owner.LastError ?? "Returning to Main Menu...";
        }

        private void FinishIntroduction(int destination)
        {
            if (!CanChange()) return;
            busy = true;
            var saved = context.Saves.AcknowledgeNewRacer();
            busy = false;
            if (!saved.IsSuccess) { feedback.text = saved.ErrorMessage; return; }
            modal.SetActive(false);
            if (destination == 0) owner.ShowGarage();
            else if (destination == 1) owner.ShowRoster();
            else owner.ShowHome();
        }

        private string BuildIntroduction()
        {
            var session = Session;
            var run = session.CareerRun;
            var team = session.PlayerTeam;
            var db = context.Database;
            var bike = team.Garage.FindBike(session.SelectedPlayerBikeId);
            var partner = team.Roster.FindRacer(session.SelectedPartnerRacerId);
            var partnerBike = team.Garage.FindBike(session.SelectedPartnerBikeId);
            var text = new StringBuilder();
            text.AppendLine($"{run.Player.Name} // {team.TeamName}\n");
            text.AppendLine($"CHARACTER FAME  {run.Player.Progression.Fame:N0}  ·  STARTS  {run.Player.RacesEntered}\n");
            text.AppendLine("STARTING PERK");
            var perk = db.GetRacerPerkDefinition(run.StartingPerkId);
            text.AppendLine(perk == null ? "No eligible player starter perk is configured."
                : $"{perk.DisplayName} — GRANTED FREE\n{perk.Description}");
            text.AppendLine($"\nASSIGNED BIKE\n{run.StartingBikeSource ?? "TEAM BIKE"}");
            if (bike == null) text.AppendLine("No bike assigned. Visit Garage.");
            else
            {
                text.AppendLine(db.GetBikeDefinition(bike.BikeDefinitionId)?.DisplayName ?? bike.BikeDefinitionId);
                text.AppendLine($"CONDITION  {(bike.IsDestroyed ? "DESTROYED" : bike.IsRaceReady ? "READY" : "INCOMPLETE")}  ·  ENGINE CLASS  {bike.EngineClass}");
                text.AppendLine("ENGINE  " + (bike.Loadout.Engine == null ? "NONE" :
                    db.GetEngineDefinition(bike.Loadout.Engine.EngineDefinitionId)?.DisplayName ?? bike.Loadout.Engine.EngineDefinitionId));
                text.AppendLine("CHASSIS  " + (bike.Loadout.Chassis == null ? "NONE" :
                    db.GetChassisDefinition(bike.Loadout.Chassis.ChassisDefinitionId)?.DisplayName ?? bike.Loadout.Chassis.ChassisDefinitionId));
                text.AppendLine("\nINSTALLED EQUIPMENT");
                foreach (var node in bike.Loadout.Nodes)
                {
                    var equipment = node.InstalledEquipment;
                    string item = equipment == null ? "EMPTY" : db.GetEquipmentDefinition(equipment.EquipmentDefinitionId)?.DisplayName ?? equipment.EquipmentDefinitionId;
                    text.AppendLine($"{node.NodeSize} {node.Index + 1}: {item}{(equipment?.IsDestroyed == true ? " (DESTROYED)" : "")}");
                }
                if (bike.Loadout.Nodes.Count == 0) text.AppendLine("NO EQUIPMENT NODES");
            }
            text.AppendLine($"\nPARTNER  {partner?.Name ?? "NONE"} — {(partner?.CanRace == true ? "ACTIVE" : "UNAVAILABLE")}");
            if (partner?.CanRace != true) text.AppendLine("Choose or recruit an active partner in Roster.");
            if (partnerBike?.IsRaceReady != true) text.AppendLine("Assign a ready partner bike in Garage before entering a race.");
            if (partnerBike?.IsRaceReady == true && bike?.IsRaceReady == true && partnerBike.EngineClass != bike.EngineClass)
                text.AppendLine("Player and partner engines need matching classes for team race entry.");
            text.AppendLine($"\nTEAM RESOURCES\nCREDITS  {team.Credits:N0}\nTEAM FAME  {team.Fame:N0}\nRESEARCH POINTS  {team.ResearchPoints:N0}\nCALENDAR WEEK  {team.Calendar.Week}");
            text.AppendLine("\nTeam resources and owned items remain with this campaign. Previous racers, wrecks and lost upgrades retain their permanent status.");
            return text.ToString();
        }

        private void ConfigureNavigation()
        {
            var controls = page == 1 ? new Selectable[] { nameInput, primary, secondary }
                : page == 3 ? new Selectable[] { primary, roster, secondary }
                : new Selectable[] { primary, secondary };
            for (int i = 0; i < controls.Length; i++)
            {
                var previous = controls[(i + controls.Length - 1) % controls.Length];
                var next = controls[(i + 1) % controls.Length];
                controls[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnLeft = previous, selectOnUp = previous, selectOnRight = next, selectOnDown = next };
            }
        }

        private void OnDestroy() { if (modal != null) Destroy(modal); }
        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
        private static TMP_Text Text(Transform parent, string name, string value, float size, Vector2 position, Vector2 dimensions)
        {
            var text = Rect(parent, name, position, dimensions).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = size; text.color = new Color(.84f, .95f, .97f);
            text.richText = false; text.raycastTarget = false;
            return text;
        }
        private static Button Button(Transform parent, string name, string label, Vector2 position, Vector2 size, out TMP_Text text)
        {
            var rect = Rect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.11f, .26f, .29f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            text = Text(rect, "Label", label, 24, Vector2.zero, size); text.alignment = TextAlignmentOptions.Center;
            return button;
        }
        private static TMP_InputField Input(Transform parent, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, "RacerName", position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.08f, .13f, .17f);
            var input = rect.gameObject.AddComponent<TMP_InputField>(); input.targetGraphic = image;
            var area = Rect(rect, "TextArea", new Vector2(14, -8), size - new Vector2(28, 16));
            area.gameObject.AddComponent<RectMask2D>();
            var text = Text(area, "Text", "", 27, Vector2.zero, area.sizeDelta);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            input.textViewport = area; input.textComponent = text;
            input.characterLimit = 40; input.lineType = TMP_InputField.LineType.SingleLine;
            input.navigation = new Navigation { mode = Navigation.Mode.None };
            return input;
        }
    }
}
