using System;
using System.Collections;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Infrastructure;
using RaceFatal.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public sealed class CareerBrandsView : MonoBehaviour
    {
        private GameContext context;
        private CareerController owner;
        private BrandService service;
        [Serializable]
        public sealed class UiSlot
        {
            public int page;
            public Transform parent;
            public string key;
            public RectTransform rect;
            public TMP_Text text;
            public Button button;
        }
        [SerializeField] private RectTransform surface;
        [SerializeField] private RectTransform[] pages;
        [SerializeField] private UiSlot[] slots;
        [SerializeField] private TMP_InputField search;
        [SerializeField] private GameObject modal;
        [SerializeField] private CanvasGroup terminalInteraction;
        private RectTransform body;
        private bool initialized;
        public bool HasAuthoredLayout => surface != null && pages != null && pages.Length == 5 && slots != null && slots.Length > 0;
        public void ConfigureEditorLayout(RectTransform terminal, RectTransform[] pageRoots, UiSlot[] authoredSlots, TMP_InputField input, GameObject overlay, CanvasGroup interaction)
        {
            surface = terminal; pages = pageRoots; slots = authoredSlots; search = input; modal = overlay; terminalInteraction = interaction;
        }
        public void PreviewEditorPage(int index)
        {
            if (pages == null) return;
            for (int i = 0; i < pages.Length; i++) pages[i].gameObject.SetActive(i == index);
            if (modal != null) modal.SetActive(index >= 5);
            if (index >= 5 && slots != null)
                foreach (var slot in slots.Where(x => x.page == 5))
                {
                    string n = slot.rect.gameObject.name;
                    bool revealControl = n.StartsWith("Reveal", StringComparison.Ordinal) || n == "SkipOrContinue";
                    slot.rect.gameObject.SetActive(n == "DialogPanel" || revealControl == (index == 6));
                }
        }
        private TMP_Text balances, feedback;
        private int tab, page, contractFilter;
        private BrandCategory? category, boxCategory;
        private string selectedBrand;
        private bool saveBlocked, revealing, skip;
        private RectTransform animatedLabel;
        private Vector3 authoredRevealScale;
        private string message = "";
        private TeamState Team => context?.Sessions.Current?.PlayerTeam;
        private static readonly Color Accent = new Color(.39f, 1f, .88f);
        private static readonly Color Panel = new Color(.055f, .07f, .105f);

        public static CareerBrandsView InitializeExisting(GameObject root, CareerController owner, GameContext context)
        {
            var view = root != null ? root.GetComponent<CareerBrandsView>() : null;
            if (view == null || !view.HasAuthoredLayout)
            {
                Debug.LogError("Brands UI is not authored. Exit Play Mode and run RACE//FATAL > Career > Build Brands UI.");
                return null;
            }
            view.owner = owner; view.context = context; view.service = new BrandService(context.Database);
            view.InitializeBindings(); return view;
        }
        private void InitializeBindings()
        {
            if (initialized) return;
            initialized = true;
            balances = UseLabel(surface, "", 30, 139, 1360, 35, 23);
            feedback = UseLabel(surface, "", 30, 825, 1120, 57, 18);
            string[] tabs = { "OVERVIEW", "DIRECTORY", "CONTRACTS", "BRAND BOXES", "RF ASSOCIATION" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int target = i;
                UseButton(surface, tabs[i], 30 + i * 273, 193, 260, 48, () => { tab = target; page = 0; Refresh(); });
            }
            UseButton(surface, "SAVE / RETRY", 1180, 830, 210, 45, () => { Persist(); Refresh(); });
            search.onEndEdit.AddListener(value => { query = value; page = 0; Refresh(); });
            CloseModal();
        }
        private void OnDisable()
        {
            StopAllCoroutines(); revealing = false;
            if (animatedLabel != null) animatedLabel.localScale = authoredRevealScale;
            if (modal != null) modal.SetActive(false);
            if (terminalInteraction != null) terminalInteraction.interactable = true;
        }
        public void FocusBrand(string id) { selectedBrand = id; tab = 1; page = 0; Refresh(); }
        public void Refresh()
        {
            if (!initialized || Team == null) return;
            balances.text = $"AVAILABLE FAME  {Team.Fame:N0}     /     LIFETIME FAME  {Team.LifetimeFame:N0}     /     ACTIVE CONTRACTS  {Team.Brands.contracts.Count(c => c.active)} / 3";
            for (int i = 0; i < pages.Length; i++) pages[i].gameObject.SetActive(i == tab);
            body = pages[tab];
            foreach (var slot in slots.Where(x => x.page >= 0 && x.page < 5)) slot.rect.gameObject.SetActive(false);
            feedback.text = message;
            switch (tab) { case 0: Overview(); break; case 1: Directory(); break; case 2: Contracts(); break; case 3: Boxes(); break; case 4: Association(); break; }
        }
        private void Overview()
        {
            UseLabel(body, "YOUR CORPORATE FOOTPRINT", 0, 0, 1360, 40, 29).color = Accent;
            UseLabel(body, $"{Team.Brands.relationships.Count:D2} BRANDS DISCOVERED       {Team.Brands.contracts.Count(c => c.completed && !c.claimed):D2} REWARDS READY       {Team.Brands.contracts.Count:D3} CONTRACTS ACQUIRED", 0, 55, 1360, 38, 23);
            UseLabel(body, "Earn fame on the circuit. Spend it on championship entries, brand boxes and Association permits.\nBoxes are the only source of new contracts. Activate up to three; only your racer earns progress.\nYour brand standing and acquired contracts stay with the team when a racer dies.", 0, 109, 1300, 105, 22);
            int y = 241;
            foreach (var c in Team.Brands.contracts.Where(c => c.active).Take(3))
            {
                var row = UseCard(body, 0, y, 1360, 82);
                UseLabel(row, BrandName(c.brandId) + "  //  " + c.terms.Title, 18, 10, 1100, 29, 22);
                UseLabel(row, $"{c.progress} / {c.terms.Target}   —   {ObjectiveText(c.terms)}", 18, 44, 1100, 25, 18).color = Accent;
                y += 94;
            }
            if (!Team.Brands.contracts.Any(c => c.active)) UseLabel(body, "NO ACTIVE CONTRACTS / OPEN A BOX TO MAKE YOUR FIRST CONTACT", 0, 250, 1300, 60, 25);
            UseButton(body, "BROWSE CONTRACTS", 1030, 494, 330, 44, () => { tab = 2; Refresh(); });
        }
        private string query = "";
        private void Directory()
        {
            UseButton(body, category.HasValue ? category.ToString().ToUpperInvariant() : "ALL CATEGORIES", 0, 0, 270, 42, () => { CycleCategory(); page = 0; Refresh(); });
            var brands = context.Database.BrandDefinitions.Values.Where(b => (!category.HasValue || b.Category == category) && (string.IsNullOrWhiteSpace(query) || b.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).OrderBy(b => b.DisplayName).ToList();
            page = Mathf.Clamp(page, 0, Mathf.Max(0, (brands.Count - 1) / 6));
            int y = 58;
            foreach (var brand in brands.Skip(page * 6).Take(6))
            {
                var relation = Team.Brands.Find(brand.Id);
                var button = UseButton(body, brand.DisplayName + "\n" + (relation == null ? "UNDISCOVERED / " + brand.Category : relation.TierName + " / " + relation.standing + " STANDING"), 0, y, 615, 67, () => { selectedBrand = brand.Id; Refresh(); });
                button.GetComponent<Image>().color = selectedBrand == brand.Id ? new Color(.15f, .2f, .28f) : Panel;
                y += 73;
            }
            Pager(brands.Count, 6, 615);
            var detail = UseCard(body, 645, 0, 715, 548);
            if (selectedBrand == null || !context.Database.BrandDefinitions.TryGetValue(selectedBrand, out var chosen))
            { UseLabel(detail, "SELECT A BRAND\n\nEvery logo represents a different route through the circuit.", 25, 25, 665, 250, 29); return; }
            var standing = Team.Brands.Find(chosen.Id);
            UseLabel(detail, chosen.DisplayName, 25, 20, 665, 52, 34).color = BrandColor(chosen);
            var text = Find(detail, "scroll", 25, 90, 665, 425).text;
            string catalog = standing == null ? "CATALOG LOCKED / DISCOVER THIS BRAND IN A BOX" : string.Join("\n\n", chosen.Products.Select(p => {
                var offer = new ShopService(context.Database).FindOffer(p.Kind, p.DefinitionId);
                return (offer?.DisplayName ?? p.DefinitionId) + "\n" + (Team.Brands.CanBuy(chosen.Id, p) ? "PURCHASABLE" : "CONTRACT UNLOCK") + " / " + (offer?.CreditCost.ToString("N0") ?? "?") + " CR";
            }));
            if (standing != null && chosen.Products.Count == 0) catalog = "No hardware is currently listed. This brand offers credits contracts.";
            text.text = chosen.Tagline + "\n\n" + chosen.Category + "\n" + (standing == null ? "NO RELATIONSHIP" : standing.TierName + " / " + standing.standing + " STANDING\nTIERS: 0 / 2 / 6 / 15") + "\n\n" + catalog;
        }
        private void Contracts()
        {
            string[] filters = { "AVAILABLE", "ACTIVE", "REWARDS READY", "ARCHIVE" };
            for (int i = 0; i < filters.Length; i++) { int value = i; UseButton(body, filters[i], i * 345, 0, 325, 42, () => { contractFilter = value; page = 0; Refresh(); }); }
            var items = Team.Brands.contracts.Where(c => contractFilter == 0 ? !c.active && !c.completed : contractFilter == 1 ? c.active : contractFilter == 2 ? c.completed && !c.claimed : c.claimed).Reverse().ToList();
            page = Mathf.Clamp(page, 0, Mathf.Max(0, (items.Count - 1) / 3));
            int y = 57;
            foreach (var c in items.Skip(page * 3).Take(3))
            {
                var card = UseCard(body, 0, y, 1360, 140);
                UseLabel(card, BrandName(c.brandId) + "  //  " + c.terms.Title, 17, 10, 1060, 30, 23).color = Accent;
                UseLabel(card, ObjectiveText(c.terms) + $"    [{c.progress}/{c.terms.Target}]", 17, 46, 1060, 42, 20);
                UseLabel(card, BrandService.RewardText(c.terms) + " / +" + c.terms.StandingReward + " STANDING", 17, 96, 1060, 34, 18);
                if (!c.claimed)
                    UseButton(card, c.completed ? "CLAIM REWARD" : c.active ? "DEACTIVATE" : "ACTIVATE", 1110, 42, 230, 55, () => {
                        if (c.active && c.terms.Objective == BrandObjective.ConsecutivePodiums) Confirm("DEACTIVATE CONTRACT?\nThis resets its consecutive podium streak.", () => Mutate(() => service.SetActive(Team, c.instanceId, false)));
                        else Mutate(() => c.completed ? service.Claim(Team, c.instanceId) : service.SetActive(Team, c.instanceId, !c.active));
                    });
                y += 148;
            }
            if (items.Count == 0) UseLabel(body, "NO CONTRACTS IN THIS VIEW. NEW CONTRACTS COME FROM BRAND BOXES ONLY.", 0, 100, 1360, 100, 25);
            Pager(items.Count, 3, 1360);
        }
        private void Boxes()
        {
            var card = UseCard(body, 0, 0, 1360, 548);
            UseLabel(card, "THE NEXT NAME ON YOUR BIKE.", 36, 25, 1280, 65, 42).color = Accent;
            UseLabel(card, "BRAND CONTACT / THREE CONTRACTS / ONE OPENING", 36, 112, 1280, 38, 25);
            UseLabel(card, "Every eligible brand has an equal chance within the selected pool.\nA new brand reveals its catalog and basic stock. A duplicate adds three more contracts.\nContract offers depend on lifetime fame and your standing with the drawn brand.\nNo equipment is awarded merely for opening a box.", 36, 175, 1240, 140, 23);
            string filter = boxCategory.HasValue ? boxCategory.ToString().ToUpperInvariant() : "RANDOM / ALL CATEGORIES";
            UseButton(card, filter, 36, 337, 480, 55, () => { CycleBoxCategory(); Refresh(); });
            bool allowed = !boxCategory.HasValue || Team.Brands.categoryPermits.Contains(boxCategory.Value);
            UseLabel(card, allowed ? "POOL AVAILABLE" : "CATEGORY PERMIT REQUIRED / VISIT RF ASSOCIATION", 550, 347, 770, 55, 21);
            UseButton(card, Team.Brands.pendingReveal != null ? "RESUME OPENING" : "OPEN BOX / " + BrandService.BoxCost + " FAME", 36, 441, 1280, 70, () => {
                if (saveBlocked) { message = "RETRY SAVING BEFORE ANOTHER TRANSACTION."; Refresh(); return; }
                if (Team.Brands.pendingReveal != null) { StartReveal(); return; }
                Confirm($"OPEN A BRAND BOX FOR {BrandService.BoxCost} FAME?\nAvailable after purchase: {Team.Fame - BrandService.BoxCost:N0}\nPool: {filter}\nThe result is saved before the reveal begins.", () => {
                    var opened = service.OpenBox(Team, boxCategory);
                    if (!opened.IsSuccess) { message = opened.ErrorMessage; Refresh(); return; }
                    if (Persist()) StartReveal();
                    Refresh();
                });
            });
        }
        private void Association()
        {
            UseLabel(body, "RF ASSOCIATION / DISTRIBUTION NEGOTIATIONS", 0, 0, 1360, 40, 28).color = Accent;
            UseLabel(body, $"Permanent category-filter permission: {BrandService.CategoryPermitCost} fame each. Box price stays {BrandService.BoxCost}. Lifetime fame is preserved.", 0, 51, 1360, 60, 22);
            int i = 0;
            foreach (BrandCategory value in Enum.GetValues(typeof(BrandCategory)))
            {
                bool owned = Team.Brands.categoryPermits.Contains(value);
                var card = UseCard(body, (i % 2) * 690, 125 + (i / 2) * 102, 670, 88);
                UseLabel(card, value.ToString().ToUpperInvariant(), 18, 14, 370, 33, 23);
                UseLabel(card, owned ? "PERMANENT PERMIT / OWNED" : "DIRECT YOUR NEXT OPENING", 18, 52, 370, 24, 16);
                var button = UseButton(card, owned ? "OWNED" : "NEGOTIATE", 420, 19, 230, 50, () => Confirm($"BUY {value.ToString().ToUpperInvariant()} PERMIT?\nCost: {BrandService.CategoryPermitCost} available fame.\nUnlocks filtering; does not include a box.", () => Mutate(() => service.BuyPermit(Team, value))));
                button.interactable = !owned; i++;
            }
        }
        private void CycleBoxCategory() { boxCategory = !boxCategory.HasValue ? BrandCategory.Shields : (int)boxCategory.Value == 7 ? (BrandCategory?)null : (BrandCategory)((int)boxCategory.Value + 1); }
        private void CycleCategory() { category = !category.HasValue ? BrandCategory.Shields : (int)category.Value == Enum.GetValues(typeof(BrandCategory)).Length - 1 ? (BrandCategory?)null : (BrandCategory)((int)category.Value + 1); }
        private void Pager(int count, int size, int width)
        {
            UseButton(body, "PREVIOUS", 0, 502, 180, 40, () => { page = Mathf.Max(0, page - 1); Refresh(); });
            UseLabel(body, $"PAGE {page + 1} / {Mathf.Max(1, (count + size - 1) / size)}", 198, 510, width - 400, 30, 18);
            UseButton(body, "NEXT", width - 180, 502, 180, 40, () => { if ((page + 1) * size < count) page++; Refresh(); });
        }
        private void Mutate(Func<Result> action)
        {
            if (saveBlocked) { message = "RETRY SAVING BEFORE ANOTHER TRANSACTION."; Refresh(); return; }
            var result = action();
            if (!result.IsSuccess) message = result.ErrorMessage;
            else Persist();
            Refresh();
        }
        private bool Persist()
        {
            var saved = context.Saves.SaveCurrentCampaign(); saveBlocked = !saved.IsSuccess;
            message = saved.IsSuccess ? "CAMPAIGN SAVED." : "SAVE FAILED. CHANGES REMAIN IN MEMORY. USE SAVE / RETRY BEFORE QUITTING. " + saved.ErrorMessage;
            if (feedback != null) feedback.text = message;
            return saved.IsSuccess;
        }
        private void Confirm(string text, Action action)
        {
            if (modal.activeSelf || saveBlocked) return;
            var panel = Modal();
            UseLabel(panel, "CONFIRM TRANSACTION", 35, 35, 830, 60, 32).color = Accent;
            UseLabel(panel, text, 35, 119, 830, 250, 25);
            UseButton(panel, "CANCEL", 35, 410, 390, 60, CloseModal);
            var confirm = UseButton(panel, "CONFIRM", 475, 410, 390, 60, () => { CloseModal(); action(); });
            EventSystem.current?.SetSelectedGameObject(confirm.gameObject);
        }
        private RectTransform Modal()
        {
            foreach (var slot in slots.Where(x => x.page == 5)) slot.rect.gameObject.SetActive(false);
            modal.SetActive(true);
            terminalInteraction.interactable = false;
            return UseCard(modal.transform, 260, 200, 900, 500);
        }
        private void CloseModal()
        {
            if (modal != null) modal.SetActive(false);
            if (terminalInteraction != null) terminalInteraction.interactable = true;
        }
        private void StartReveal()
        {
            if (revealing || Team.Brands.pendingReveal == null) return;
            StartCoroutine(Reveal());
        }
        private IEnumerator Reveal()
        {
            revealing = true; skip = false;
            var receipt = Team.Brands.pendingReveal;
            var panel = Modal();
            var eyebrow = UseLabel(panel, "RF ASSOCIATION // DECRYPTING BRAND CONTACT", 35, 28, 830, 40, 18);
            var name = UseLabel(panel, "SIGNAL ACQUIRED", 35, 105, 830, 110, 48); name.alignment = TextAlignmentOptions.Center;
            animatedLabel = name.rectTransform; authoredRevealScale = animatedLabel.localScale;
            var details = UseLabel(panel, "", 35, 238, 830, 145, 23); details.alignment = TextAlignmentOptions.Center;
            var skipButton = UseButton(panel, "SKIP REVEAL", 260, 414, 380, 58, () => skip = true);
            float elapsed = 0;
            while (elapsed < 3.2f && !skip)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 3.2f);
                name.rectTransform.localScale = authoredRevealScale * (1.12f - .12f * t);
                name.color = Color.Lerp(new Color(.2f, .25f, .4f), Accent, t);
                details.text = "CONNECTING " + new string('▰', Mathf.Clamp((int)(t * 16), 0, 16));
                yield return null;
            }
            context.Database.BrandDefinitions.TryGetValue(receipt.brandId, out var brand);
            name.rectTransform.localScale = authoredRevealScale; name.text = BrandName(receipt.brandId); name.color = brand != null ? BrandColor(brand) : Accent;
            eyebrow.text = receipt.newBrand ? "NEW BRAND DISCOVERED / BASIC STOCK AVAILABLE" : "EXISTING BRAND / NEW CONTRACTS ACQUIRED";
            details.text = string.Join("\n", receipt.contractIds.Select(id => Team.Brands.contracts.First(c => c.instanceId == id).terms.Title));
            skipButton.onClick.RemoveAllListeners(); skipButton.GetComponentInChildren<TMP_Text>().text = "ADD TO CONTRACT COLLECTION";
            skipButton.onClick.AddListener(() => {
                Team.Brands.pendingReveal = null;
                if (!Persist()) Team.Brands.pendingReveal = receipt;
                revealing = false; CloseModal(); tab = 2; contractFilter = 0; page = 0; Refresh();
            });
            EventSystem.current?.SetSelectedGameObject(skipButton.gameObject);
        }
        private string BrandName(string id) => context.Database.BrandDefinitions.TryGetValue(id, out var b) ? b.DisplayName : id;
        private string ObjectiveText(BrandContractTemplate t)
        {
            string objective = t.Objective == BrandObjective.DestroyRacers ? "Destroy racers" : t.Objective == BrandObjective.FinishRaces ? "Finish races" : t.Objective == BrandObjective.WinRaces ? "Finish first" : t.Objective == BrandObjective.PodiumFinishes ? "Finish on the podium" : "Consecutive podium finishes";
            if (!string.IsNullOrEmpty(t.RaceId)) objective += " / " + (context.Database.GetRaceDefinition(t.RaceId)?.DisplayName ?? t.RaceId);
            if (!string.IsNullOrEmpty(t.TrackId)) objective += " / TRACK: " + (context.Database.GetTrackDefinition(t.TrackId)?.DisplayName ?? t.TrackId);
            return objective + " / PLAYER ONLY";
        }
        private static Color BrandColor(BrandDefinition b) => ColorUtility.TryParseHtmlString(b.ColorHex, out var color) ? color : Accent;
        public static string SlotKey(string kind, float x, float y, float width, float height) =>
            string.Join("|", kind, x.ToString(System.Globalization.CultureInfo.InvariantCulture), y.ToString(System.Globalization.CultureInfo.InvariantCulture), width.ToString(System.Globalization.CultureInfo.InvariantCulture), height.ToString(System.Globalization.CultureInfo.InvariantCulture));
        private UiSlot Find(Transform parent, string kind, float x, float y, float width, float height)
        {
            string key = SlotKey(kind, x, y, width, height);
            var slot = slots.FirstOrDefault(item => item.parent == parent && item.key == key);
            if (slot == null) throw new InvalidOperationException("Missing authored Brands UI slot: " + key + ". Run Build Brands UI outside Play Mode.");
            slot.rect.gameObject.SetActive(true);
            return slot;
        }
        private RectTransform UseCard(Transform parent, float x, float y, float width, float height) => Find(parent, "card", x, y, width, height).rect;
        private TMP_Text UseLabel(Transform parent, string value, float x, float y, float width, float height, float unusedDefaultSize)
        {
            var label = Find(parent, "text", x, y, width, height).text;
            label.text = value; return label;
        }
        private Button UseButton(Transform parent, string value, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var slot = Find(parent, "button", x, y, width, height);
            slot.text.text = value;
            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(action);
            return slot.button;
        }
    }
}
