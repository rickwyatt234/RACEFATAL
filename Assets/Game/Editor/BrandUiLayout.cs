using System.Collections.Generic;
using RaceFatal.Presentation.Career;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Called exclusively by the editor command. Every row and modal exists before Play Mode.
public static class BrandUiLayout
{
    private sealed class Layout
    {
        public readonly List<CareerBrandsView.UiSlot> slots = new List<CareerBrandsView.UiSlot>();
        public int page = -1;
        public RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var r = obj.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        private void Register(string kind, Transform parent, RectTransform rect, float x, float y, float w, float h, TMP_Text text = null, Button button = null)
        {
            slots.Add(new CareerBrandsView.UiSlot { page = page, parent = parent, key = CareerBrandsView.SlotKey(kind, x, y, w, h), rect = rect, text = text, button = button });
        }
        public RectTransform Card(Transform parent, string name, float x, float y, float w, float h)
        {
            var r = Rect(parent, name, x, y, w, h); r.gameObject.AddComponent<Image>().color = new Color(.055f, .07f, .105f);
            Register("card", parent, r, x, y, w, h); return r;
        }
        public TMP_Text Text(Transform parent, string name, string value, float x, float y, float w, float h, float size)
        {
            var r = Rect(parent, name, x, y, w, h); var label = r.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value; label.fontSize = size; label.color = new Color(.84f, .95f, .97f); label.richText = false; label.raycastTarget = false;
            Register("text", parent, r, x, y, w, h, label); return label;
        }
        public Button Button(Transform parent, string name, string value, float x, float y, float w, float h)
        {
            var r = Rect(parent, name, x, y, w, h); var image = r.gameObject.AddComponent<Image>(); image.color = new Color(.11f, .26f, .29f);
            var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            // Button label is part of its authored slot, rather than a separate binding.
            var labelRect = Rect(r, "Label", 0, 0, w, h); var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value; label.fontSize = 21; label.color = new Color(.84f, .95f, .97f); label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            Register("button", parent, r, x, y, w, h, label, button); return button;
        }
        public void Pager(Transform parent, int width)
        {
            Button(parent, "PreviousPage", "PREVIOUS", 0, 502, 180, 40);
            Text(parent, "PageCounter", "PAGE 1 / 1", 198, 510, width - 400, 30, 18);
            Button(parent, "NextPage", "NEXT", width - 180, 502, 180, 40);
        }
    }
    public static void Build(Transform root, CareerBrandsView view)
    {
        var b = new Layout();
        var terminal = b.Rect(root, "BrandsUI", 0, 0, 1420, 900);
        terminal.anchorMin = terminal.anchorMax = new Vector2(1, .5f); terminal.pivot = new Vector2(1, .5f); terminal.anchoredPosition = new Vector2(-35, 0);
        terminal.gameObject.AddComponent<Image>().color = new Color(.025f, .03f, .055f);
        var interaction = terminal.gameObject.AddComponent<CanvasGroup>();
        b.Text(terminal, "NetworkHeading", "RF / CORPORATE RELATIONS                         ASSOCIATION NETWORK // LIVE", 30, 25, 1360, 28, 16);
        b.Text(terminal, "Heading", "PERFORMANCE IS CURRENCY.", 30, 62, 1360, 64, 44);
        b.Text(terminal, "Balances", "AVAILABLE FAME  0 / LIFETIME FAME  0 / ACTIVE CONTRACTS  0 / 3", 30, 139, 1360, 35, 23);
        string[] names = { "Overview", "Directory", "Contracts", "BrandBoxes", "Association" };
        string[] captions = { "OVERVIEW", "DIRECTORY", "CONTRACTS", "BRAND BOXES", "RF ASSOCIATION" };
        for (int i = 0; i < 5; i++) b.Button(terminal, names[i] + "Tab", captions[i], 30 + i * 273, 193, 260, 48);
        b.Text(terminal, "Feedback", "", 30, 825, 1120, 57, 18);
        b.Button(terminal, "SaveRetry", "SAVE / RETRY", 1180, 830, 210, 45);
        var pages = new RectTransform[5];
        for (int i = 0; i < 5; i++) pages[i] = b.Rect(terminal, names[i] + "Page", 30, 262, 1360, 548);
        b.page = 0;
        var p = pages[0];
        b.Text(p, "Title", "YOUR CORPORATE FOOTPRINT", 0, 0, 1360, 40, 29);
        b.Text(p, "Totals", "00 BRANDS DISCOVERED / 00 REWARDS READY / 000 CONTRACTS ACQUIRED", 0, 55, 1360, 38, 23);
        b.Text(p, "Introduction", "Earn fame on the circuit. Spend it on championship entries, brand boxes and Association permits.\nBoxes are the only source of new contracts. Activate up to three; only your racer earns progress.\nYour brand standing and acquired contracts stay with the team when a racer dies.", 0, 109, 1300, 105, 22);
        for (int i = 0; i < 3; i++)
        {
            var row = b.Card(p, "ActiveContract" + (i + 1), 0, 241 + i * 94, 1360, 82);
            b.Text(row, "Title", "BRAND / CONTRACT TITLE", 18, 10, 1100, 29, 22);
            b.Text(row, "Progress", "0 / 3 — OBJECTIVE", 18, 44, 1100, 25, 18);
        }
        b.Text(p, "EmptyState", "NO ACTIVE CONTRACTS / OPEN A BOX TO MAKE YOUR FIRST CONTACT", 0, 250, 1300, 60, 25).gameObject.SetActive(false);
        b.Button(p, "BrowseContracts", "BROWSE CONTRACTS", 1030, 494, 330, 44);
        b.page = 1; p = pages[1];
        b.Button(p, "CategoryFilter", "ALL CATEGORIES", 0, 0, 270, 42);
        var inputRoot = b.Rect(p, "BrandSearch", 285, 0, 330, 42); inputRoot.gameObject.AddComponent<Image>().color = new Color(.055f, .07f, .105f);
        var input = inputRoot.gameObject.AddComponent<TMP_InputField>(); input.textViewport = inputRoot;
        input.textComponent = b.Text(inputRoot, "Text", "", 12, 5, 306, 32, 20);
        input.placeholder = b.Text(inputRoot, "Placeholder", "Search brands…", 12, 5, 306, 32, 20);
        // The input owns its children; runtime page clearing must leave them enabled.
        b.slots.RemoveAll(slot => slot.parent == inputRoot);
        for (int i = 0; i < 6; i++) b.Button(p, "BrandRow" + (i + 1), "BRAND NAME\nRELATIONSHIP / CATEGORY", 0, 58 + i * 73, 615, 67);
        b.Pager(p, 615);
        var detail = b.Card(p, "BrandDetail", 645, 0, 715, 548);
        b.Text(detail, "NoSelection", "SELECT A BRAND\n\nEvery logo represents a different route through the circuit.", 25, 25, 665, 250, 29);
        b.Text(detail, "BrandName", "BRAND NAME", 25, 20, 665, 52, 34);
        var scrollRoot = b.Rect(detail, "CatalogViewport", 25, 90, 665, 425);
        scrollRoot.gameObject.AddComponent<Image>().color = new Color(.035f, .065f, .085f); scrollRoot.gameObject.AddComponent<RectMask2D>();
        var catalog = b.Text(scrollRoot, "CatalogText", "BRAND CATALOG / PRODUCT / PRICE / ACCESS", 0, 0, 647, 0, 21);
        catalog.rectTransform.anchorMax = new Vector2(1, 1); catalog.rectTransform.sizeDelta = new Vector2(-18, 0);
        catalog.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.viewport = scrollRoot; scroll.content = catalog.rectTransform;
        scroll.horizontal = false; scroll.scrollSensitivity = 35; scroll.movementType = ScrollRect.MovementType.Clamped;
        // Bind the viewport and its content through one slot; never reconstruct a ScrollRect.
        b.slots.RemoveAt(b.slots.Count - 1);
        b.slots.Add(new CareerBrandsView.UiSlot { page = 1, parent = detail, key = CareerBrandsView.SlotKey("scroll", 25, 90, 665, 425), rect = scrollRoot, text = catalog });
        b.page = 2; p = pages[2];
        string[] filters = { "AVAILABLE", "ACTIVE", "REWARDS READY", "ARCHIVE" };
        for (int i = 0; i < 4; i++) b.Button(p, "Filter" + i, filters[i], i * 345, 0, 325, 42);
        for (int i = 0; i < 3; i++)
        {
            var row = b.Card(p, "ContractRow" + (i + 1), 0, 57 + i * 148, 1360, 140);
            b.Text(row, "Title", "BRAND / CONTRACT TITLE", 17, 10, 1060, 30, 23);
            b.Text(row, "Objective", "OBJECTIVE / PLAYER ONLY [0/3]", 17, 46, 1060, 42, 20);
            b.Text(row, "Reward", "REWARD / STANDING", 17, 96, 1060, 34, 18);
            b.Button(row, "Action", "ACTIVATE", 1110, 42, 230, 55);
        }
        b.Text(p, "EmptyState", "NO CONTRACTS IN THIS VIEW. NEW CONTRACTS COME FROM BRAND BOXES ONLY.", 0, 100, 1360, 100, 25).gameObject.SetActive(false);
        b.Pager(p, 1360);
        b.page = 3; p = pages[3];
        var box = b.Card(p, "BoxOffer", 0, 0, 1360, 548);
        b.Text(box, "Title", "THE NEXT NAME ON YOUR BIKE.", 36, 25, 1280, 65, 42);
        b.Text(box, "Contents", "BRAND CONTACT / THREE CONTRACTS / ONE OPENING", 36, 112, 1280, 38, 25);
        b.Text(box, "Description", "Every eligible brand has an equal chance within the selected pool.\nA new brand reveals its catalog and basic stock. A duplicate adds three more contracts.\nContract offers depend on lifetime fame and your standing with the drawn brand.\nNo equipment is awarded merely for opening a box.", 36, 175, 1240, 140, 23);
        b.Button(box, "CategoryFilter", "RANDOM / ALL CATEGORIES", 36, 337, 480, 55);
        b.Text(box, "PermitStatus", "POOL AVAILABLE", 550, 347, 770, 55, 21);
        b.Button(box, "OpenBox", "OPEN BOX / 150 FAME", 36, 441, 1280, 70);
        b.page = 4; p = pages[4];
        b.Text(p, "Title", "RF ASSOCIATION / DISTRIBUTION NEGOTIATIONS", 0, 0, 1360, 40, 28);
        b.Text(p, "Description", "Permanent category-filter permission: 500 fame each. Box price stays 150. Lifetime fame is preserved.", 0, 51, 1360, 60, 22);
        for (int i = 0; i < 8; i++)
        {
            var row = b.Card(p, "CategoryPermit" + i, (i % 2) * 690, 125 + (i / 2) * 102, 670, 88);
            b.Text(row, "Category", ((RaceFatal.Career.BrandCategory)i).ToString().ToUpperInvariant(), 18, 14, 370, 33, 23);
            b.Text(row, "Status", "DIRECT YOUR NEXT OPENING", 18, 52, 370, 24, 16);
            b.Button(row, "Negotiate", "NEGOTIATE", 420, 19, 230, 50);
        }
        b.page = 5;
        var overlay = b.Rect(terminal, "TransactionOverlay", 0, 0, 1420, 900);
        overlay.gameObject.AddComponent<Image>().color = new Color(.012f, .015f, .03f, .98f);
        overlay.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups = true;
        var panel = b.Card(overlay, "DialogPanel", 260, 200, 900, 500);
        b.Text(panel, "ConfirmationTitle", "CONFIRM TRANSACTION", 35, 35, 830, 60, 32);
        b.Text(panel, "ConfirmationBody", "TRANSACTION DETAILS", 35, 119, 830, 250, 25);
        b.Button(panel, "Cancel", "CANCEL", 35, 410, 390, 60);
        b.Button(panel, "Confirm", "CONFIRM", 475, 410, 390, 60);
        b.Text(panel, "RevealEyebrow", "RF ASSOCIATION // DECRYPTING BRAND CONTACT", 35, 28, 830, 40, 18);
        b.Text(panel, "RevealBrand", "SIGNAL ACQUIRED", 35, 105, 830, 110, 48).alignment = TextAlignmentOptions.Center;
        b.Text(panel, "RevealContracts", "THREE CONTRACTS ACQUIRED", 35, 238, 830, 145, 23).alignment = TextAlignmentOptions.Center;
        b.Button(panel, "SkipOrContinue", "SKIP REVEAL", 260, 414, 380, 58);
        overlay.gameObject.SetActive(false);
        view.ConfigureEditorLayout(terminal, pages, b.slots.ToArray(), input, overlay.gameObject, interaction);
        view.PreviewEditorPage(0);
    }
}
