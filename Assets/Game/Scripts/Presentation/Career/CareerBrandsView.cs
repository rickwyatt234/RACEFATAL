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
        private RectTransform surface, body;
        private TMP_Text balances, feedback;
        private TMP_InputField search;
        private GameObject modal;
        private CanvasGroup bodyInteraction, terminalInteraction;
        private int tab, page, contractFilter;
        private BrandCategory? category, boxCategory;
        private string selectedBrand;
        private bool saveBlocked, revealing, skip;
        private string message = "";
        private TeamState Team => context?.Sessions.Current?.PlayerTeam;
        private static readonly Color Accent = new Color(.39f, 1f, .88f);
        private static readonly Color Panel = new Color(.055f, .07f, .105f);

        public static CareerBrandsView Create(GameObject root, CareerController owner, GameContext context)
        {
            if (root == null) return null;
            foreach (Transform child in root.transform) child.gameObject.SetActive(false);
            var view = root.GetComponent<CareerBrandsView>() ?? root.AddComponent<CareerBrandsView>();
            view.owner = owner; view.context = context; view.service = new BrandService(context.Database);
            view.Build(); return view;
        }
        private void Build()
        {
            if (surface != null) return;
            surface = CareerRuntimeUi.Rect(transform, "BrandTerminal", Vector2.zero, new Vector2(1420, 900));
            surface.anchorMin = surface.anchorMax = new Vector2(1, .5f); surface.pivot = new Vector2(1, .5f);
            terminalInteraction = surface.gameObject.AddComponent<CanvasGroup>();
            surface.gameObject.AddComponent<Image>().color = new Color(.025f, .03f, .055f);
            Label(surface, "RF / CORPORATE RELATIONS                         ASSOCIATION NETWORK // LIVE", 30, 25, 1360, 28, 16).color = Accent;
            Label(surface, "PERFORMANCE IS CURRENCY.", 30, 62, 1360, 64, 44);
            balances = Label(surface, "", 30, 139, 1360, 35, 23);
            string[] tabs = { "OVERVIEW", "DIRECTORY", "CONTRACTS", "BRAND BOXES", "RF ASSOCIATION" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int target = i;
                Button(surface, tabs[i], 30 + i * 273, 193, 260, 48, () => { tab = target; page = 0; Refresh(); });
            }
            body = CareerRuntimeUi.Rect(surface, "Content", new Vector2(30, -262), new Vector2(1360, 548));
            bodyInteraction = body.gameObject.AddComponent<CanvasGroup>();
            feedback = Label(surface, "", 30, 825, 1120, 57, 18);
            Button(surface, "SAVE / RETRY", 1180, 830, 210, 45, () => { Persist(); Refresh(); });
            Fit();
        }
        private void LateUpdate() => Fit();
        private void Fit()
        {
            if (surface == null) return;
            var parent = transform as RectTransform;
            float scale = Mathf.Min(Mathf.Max(1, parent.rect.width - 380) / 1420f, Mathf.Max(1, parent.rect.height - 80) / 900f);
            surface.localScale = Vector3.one * scale;
            surface.anchoredPosition = new Vector2(-35, 0);
        }
        private void OnDisable()
        {
            StopAllCoroutines(); revealing = false;
            if (modal != null) { Destroy(modal); modal = null; }
            if (bodyInteraction != null) bodyInteraction.interactable = true;
            if (terminalInteraction != null) terminalInteraction.interactable = true;
        }
        public void FocusBrand(string id) { selectedBrand = id; tab = 1; page = 0; Refresh(); }
        public void Refresh()
        {
            if (surface == null || Team == null) return;
            balances.text = $"AVAILABLE FAME  {Team.Fame:N0}     /     LIFETIME FAME  {Team.LifetimeFame:N0}     /     ACTIVE CONTRACTS  {Team.Brands.contracts.Count(c => c.active)} / 3";
            foreach (Transform child in body) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            feedback.text = message;
            switch (tab) { case 0: Overview(); break; case 1: Directory(); break; case 2: Contracts(); break; case 3: Boxes(); break; case 4: Association(); break; }
        }
        private void Overview()
        {
            Label(body, "YOUR CORPORATE FOOTPRINT", 0, 0, 1360, 40, 29).color = Accent;
            Label(body, $"{Team.Brands.relationships.Count:D2} BRANDS DISCOVERED       {Team.Brands.contracts.Count(c => c.completed && !c.claimed):D2} REWARDS READY       {Team.Brands.contracts.Count:D3} CONTRACTS ACQUIRED", 0, 55, 1360, 38, 23);
            Label(body, "Earn fame on the circuit. Spend it on championship entries, brand boxes and Association permits.\nBoxes are the only source of new contracts. Activate up to three; only your racer earns progress.\nYour brand standing and acquired contracts stay with the team when a racer dies.", 0, 109, 1300, 105, 22);
            int y = 241;
            foreach (var c in Team.Brands.contracts.Where(c => c.active).Take(3))
            {
                var row = Card(body, 0, y, 1360, 82);
                Label(row, BrandName(c.brandId) + "  //  " + c.terms.Title, 18, 10, 1100, 29, 22);
                Label(row, $"{c.progress} / {c.terms.Target}   —   {ObjectiveText(c.terms)}", 18, 44, 1100, 25, 18).color = Accent;
                y += 94;
            }
            if (!Team.Brands.contracts.Any(c => c.active)) Label(body, "NO ACTIVE CONTRACTS / OPEN A BOX TO MAKE YOUR FIRST CONTACT", 0, 250, 1300, 60, 25);
            Button(body, "BROWSE CONTRACTS", 1030, 494, 330, 44, () => { tab = 2; Refresh(); });
        }
        private string query = "";
        private void Directory()
        {
            Button(body, category.HasValue ? category.ToString().ToUpperInvariant() : "ALL CATEGORIES", 0, 0, 270, 42, () => { CycleCategory(); page = 0; Refresh(); });
            var searchRect = CareerRuntimeUi.Rect(body, "BrandSearch", new Vector2(285, 0), new Vector2(330, 42));
            searchRect.gameObject.AddComponent<Image>().color = Panel;
            search = searchRect.gameObject.AddComponent<TMP_InputField>();
            search.textViewport = searchRect; search.textComponent = Label(searchRect, "", 12, 5, 306, 32, 20);
            search.placeholder = Label(searchRect, "Search brands…", 12, 5, 306, 32, 20); search.text = query;
            search.onEndEdit.AddListener(value => { query = value; page = 0; Refresh(); });
            var brands = context.Database.BrandDefinitions.Values.Where(b => (!category.HasValue || b.Category == category) && (string.IsNullOrWhiteSpace(query) || b.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).OrderBy(b => b.DisplayName).ToList();
            page = Mathf.Clamp(page, 0, Mathf.Max(0, (brands.Count - 1) / 6));
            int y = 58;
            foreach (var brand in brands.Skip(page * 6).Take(6))
            {
                var relation = Team.Brands.Find(brand.Id);
                var button = Button(body, brand.DisplayName + "\n" + (relation == null ? "UNDISCOVERED / " + brand.Category : relation.TierName + " / " + relation.standing + " STANDING"), 0, y, 615, 67, () => { selectedBrand = brand.Id; Refresh(); });
                button.GetComponent<Image>().color = selectedBrand == brand.Id ? new Color(.15f, .2f, .28f) : Panel;
                y += 73;
            }
            Pager(brands.Count, 6, 615);
            var detail = Card(body, 645, 0, 715, 548);
            if (selectedBrand == null || !context.Database.BrandDefinitions.TryGetValue(selectedBrand, out var chosen))
            { Label(detail, "SELECT A BRAND\n\nEvery logo represents a different route through the circuit.", 25, 25, 665, 250, 29); return; }
            var standing = Team.Brands.Find(chosen.Id);
            Label(detail, chosen.DisplayName, 25, 20, 665, 52, 34).color = BrandColor(chosen);
            var text = CareerRuntimeUi.Scroll(detail, "Catalog", new Vector2(25, -90), new Vector2(665, 425)); text.fontSize = 21;
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
            for (int i = 0; i < filters.Length; i++) { int value = i; Button(body, filters[i], i * 345, 0, 325, 42, () => { contractFilter = value; page = 0; Refresh(); }); }
            var items = Team.Brands.contracts.Where(c => contractFilter == 0 ? !c.active && !c.completed : contractFilter == 1 ? c.active : contractFilter == 2 ? c.completed && !c.claimed : c.claimed).Reverse().ToList();
            page = Mathf.Clamp(page, 0, Mathf.Max(0, (items.Count - 1) / 3));
            int y = 57;
            foreach (var c in items.Skip(page * 3).Take(3))
            {
                var card = Card(body, 0, y, 1360, 140);
                Label(card, BrandName(c.brandId) + "  //  " + c.terms.Title, 17, 10, 1060, 30, 23).color = Accent;
                Label(card, ObjectiveText(c.terms) + $"    [{c.progress}/{c.terms.Target}]", 17, 46, 1060, 42, 20);
                Label(card, BrandService.RewardText(c.terms) + " / +" + c.terms.StandingReward + " STANDING", 17, 96, 1060, 34, 18);
                if (!c.claimed)
                    Button(card, c.completed ? "CLAIM REWARD" : c.active ? "DEACTIVATE" : "ACTIVATE", 1110, 42, 230, 55, () => {
                        if (c.active && c.terms.Objective == BrandObjective.ConsecutivePodiums) Confirm("DEACTIVATE CONTRACT?\nThis resets its consecutive podium streak.", () => Mutate(() => service.SetActive(Team, c.instanceId, false)));
                        else Mutate(() => c.completed ? service.Claim(Team, c.instanceId) : service.SetActive(Team, c.instanceId, !c.active));
                    });
                y += 148;
            }
            if (items.Count == 0) Label(body, "NO CONTRACTS IN THIS VIEW. NEW CONTRACTS COME FROM BRAND BOXES ONLY.", 0, 100, 1360, 100, 25);
            Pager(items.Count, 3, 1360);
        }
        private void Boxes()
        {
            var card = Card(body, 0, 0, 1360, 548);
            Label(card, "THE NEXT NAME ON YOUR BIKE.", 36, 25, 1280, 65, 42).color = Accent;
            Label(card, "BRAND CONTACT / THREE CONTRACTS / ONE OPENING", 36, 112, 1280, 38, 25);
            Label(card, "Every eligible brand has an equal chance within the selected pool.\nA new brand reveals its catalog and basic stock. A duplicate adds three more contracts.\nContract offers depend on lifetime fame and your standing with the drawn brand.\nNo equipment is awarded merely for opening a box.", 36, 175, 1240, 140, 23);
            string filter = boxCategory.HasValue ? boxCategory.ToString().ToUpperInvariant() : "RANDOM / ALL CATEGORIES";
            Button(card, filter, 36, 337, 480, 55, () => { CycleBoxCategory(); Refresh(); });
            bool allowed = !boxCategory.HasValue || Team.Brands.categoryPermits.Contains(boxCategory.Value);
            Label(card, allowed ? "POOL AVAILABLE" : "CATEGORY PERMIT REQUIRED / VISIT RF ASSOCIATION", 550, 347, 770, 55, 21);
            Button(card, Team.Brands.pendingReveal != null ? "RESUME OPENING" : "OPEN BOX / " + BrandService.BoxCost + " FAME", 36, 441, 1280, 70, () => {
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
            Label(body, "RF ASSOCIATION / DISTRIBUTION NEGOTIATIONS", 0, 0, 1360, 40, 28).color = Accent;
            Label(body, $"Permanent category-filter permission: {BrandService.CategoryPermitCost} fame each. Box price stays {BrandService.BoxCost}. Lifetime fame is preserved.", 0, 51, 1360, 60, 22);
            int i = 0;
            foreach (BrandCategory value in Enum.GetValues(typeof(BrandCategory)))
            {
                bool owned = Team.Brands.categoryPermits.Contains(value);
                var card = Card(body, (i % 2) * 690, 125 + (i / 2) * 102, 670, 88);
                Label(card, value.ToString().ToUpperInvariant(), 18, 14, 370, 33, 23);
                Label(card, owned ? "PERMANENT PERMIT / OWNED" : "DIRECT YOUR NEXT OPENING", 18, 52, 370, 24, 16);
                var button = Button(card, owned ? "OWNED" : "NEGOTIATE", 420, 19, 230, 50, () => Confirm($"BUY {value.ToString().ToUpperInvariant()} PERMIT?\nCost: {BrandService.CategoryPermitCost} available fame.\nUnlocks filtering; does not include a box.", () => Mutate(() => service.BuyPermit(Team, value))));
                button.interactable = !owned; i++;
            }
        }
        private void CycleBoxCategory() { boxCategory = !boxCategory.HasValue ? BrandCategory.Shields : (int)boxCategory.Value == 7 ? (BrandCategory?)null : (BrandCategory)((int)boxCategory.Value + 1); }
        private void CycleCategory() { category = !category.HasValue ? BrandCategory.Shields : (int)category.Value == Enum.GetValues(typeof(BrandCategory)).Length - 1 ? (BrandCategory?)null : (BrandCategory)((int)category.Value + 1); }
        private void Pager(int count, int size, int width)
        {
            Button(body, "PREVIOUS", 0, 502, 180, 40, () => { page = Mathf.Max(0, page - 1); Refresh(); });
            Label(body, $"PAGE {page + 1} / {Mathf.Max(1, (count + size - 1) / size)}", 198, 510, width - 400, 30, 18);
            Button(body, "NEXT", width - 180, 502, 180, 40, () => { if ((page + 1) * size < count) page++; Refresh(); });
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
            if (modal != null || saveBlocked) return;
            var panel = Modal();
            Label(panel, "CONFIRM TRANSACTION", 35, 35, 830, 60, 32).color = Accent;
            Label(panel, text, 35, 119, 830, 250, 25);
            Button(panel, "CANCEL", 35, 410, 390, 60, CloseModal);
            var confirm = Button(panel, "CONFIRM", 475, 410, 390, 60, () => { CloseModal(); action(); });
            EventSystem.current?.SetSelectedGameObject(confirm.gameObject);
        }
        private RectTransform Modal()
        {
            var overlay = Card(surface, 0, 0, 1420, 900); overlay.name = "TransactionOverlay"; modal = overlay.gameObject;
            overlay.GetComponent<Image>().color = new Color(.012f, .015f, .03f, .98f);
            bodyInteraction.interactable = false; terminalInteraction.interactable = false;
            overlay.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups = true;
            return Card(overlay, 260, 200, 900, 500);
        }
        private void CloseModal()
        {
            if (modal != null) { modal.SetActive(false); Destroy(modal); modal = null; }
            bodyInteraction.interactable = true; terminalInteraction.interactable = true;
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
            var eyebrow = Label(panel, "RF ASSOCIATION // DECRYPTING BRAND CONTACT", 35, 28, 830, 40, 18);
            var name = Label(panel, "SIGNAL ACQUIRED", 35, 105, 830, 110, 48); name.alignment = TextAlignmentOptions.Center;
            var details = Label(panel, "", 35, 238, 830, 145, 23); details.alignment = TextAlignmentOptions.Center;
            var skipButton = Button(panel, "SKIP REVEAL", 260, 414, 380, 58, () => skip = true);
            float elapsed = 0;
            while (elapsed < 3.2f && !skip)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 3.2f);
                name.rectTransform.localScale = Vector3.one * (1.12f - .12f * t);
                name.color = Color.Lerp(new Color(.2f, .25f, .4f), Accent, t);
                details.text = "CONNECTING " + new string('▰', Mathf.Clamp((int)(t * 16), 0, 16));
                yield return null;
            }
            context.Database.BrandDefinitions.TryGetValue(receipt.brandId, out var brand);
            name.rectTransform.localScale = Vector3.one; name.text = BrandName(receipt.brandId); name.color = brand != null ? BrandColor(brand) : Accent;
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
        private static RectTransform Card(Transform parent, float x, float y, float width, float height)
        {
            var rect = CareerRuntimeUi.Rect(parent, "Panel", new Vector2(x, -y), new Vector2(width, height)); rect.gameObject.AddComponent<Image>().color = Panel; return rect;
        }
        private static TMP_Text Label(Transform parent, string text, float x, float y, float width, float height, float size) => CareerRuntimeUi.Text(parent, "Label", text, new Vector2(x, -y), new Vector2(width, height), size);
        private static Button Button(Transform parent, string text, float x, float y, float width, float height, UnityEngine.Events.UnityAction action) => CareerRuntimeUi.Button(parent, text, new Vector2(x, -y), new Vector2(width, height), action);
    }
}
