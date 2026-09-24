using RaceFatal.Presentation.Career;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CareerShopScaffoldBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/01_Career.unity";

    [MenuItem("RACE//FATAL/Career/Build Shop UI")]
    public static void BuildShop()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before building the Shop UI.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject systems = GameObject.Find("CareerSystems");
        GameObject canvas = GameObject.Find("CareerCanvas");
        Transform shopRoot = canvas != null ? canvas.transform.Find("ShopRoot") : null;
        CareerController career = systems != null ? systems.GetComponent<CareerController>() : null;
        if (career == null || shopRoot == null)
        {
            EditorUtility.DisplayDialog("RACE//FATAL",
                "CareerSystems or ShopRoot is missing. Build the Career Hub scaffold first.", "OK");
            return;
        }

        // Rebuilding only replaces this tool's panels and the original placeholder.
        DestroyChild(shopRoot, "ShopUI");
        DestroyChild(shopRoot, "ShopConfirmation");
        DestroyChild(shopRoot, "Placeholder");
        CareerShopView view = shopRoot.GetComponent<CareerShopView>();
        if (view == null) view = shopRoot.gameObject.AddComponent<CareerShopView>();
        RectTransform ui = CreateRect(shopRoot, "ShopUI", Vector2.zero, Vector2.zero, true);
        CreateText(ui, "CategoriesHeading", "CATEGORIES", 28, new Vector2(410, -160), new Vector2(300, 42));
        CreateText(ui, "StockHeading", "AVAILABLE STOCK", 28, new Vector2(730, -160), new Vector2(520, 42));
        TMP_Text credits = CreateText(ui, "Credits", "CREDITS  --", 28, new Vector2(1290, -160), new Vector2(510, 42));
        RectTransform categories = CreateScroller(ui, "Categories", new Vector2(410, -225), new Vector2(290, 625));
        RectTransform items = CreateScroller(ui, "Stock", new Vector2(730, -225), new Vector2(530, 625));
        TMP_Text details = CreateText(ui, "ItemDetails", "SELECT AN ITEM.", 23, new Vector2(1290, -225), new Vector2(500, 530));
        Button purchase = CreateButton(ui, "Purchase", "BUY COMPONENT", new Vector2(1290, -785), new Vector2(500, 64));
        Button garage = CreateButton(ui, "OpenGarage", "OPEN GARAGE", new Vector2(1100, -875), new Vector2(330, 64));
        Button save = CreateButton(ui, "SaveShop", "SAVE CAMPAIGN", new Vector2(1460, -875), new Vector2(330, 64));
        TMP_Text feedback = CreateText(ui, "ShopFeedback", "BROWSE STOCK // PURCHASE COMPONENTS // INSTALL IN GARAGE", 23,
            new Vector2(410, -977), new Vector2(1380, 70));
        // The inactive template must not live under the list content.
        GameObject templateRoot =
            new GameObject("ShopTemplates", typeof(RectTransform));
        templateRoot.transform.SetParent(ui, false);

        GameObject template = new GameObject(
            "ShopOptionTemplate",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement),
            typeof(CareerGarageOptionView));

        template.transform.SetParent(templateRoot.transform, false);

        RectTransform templateRect =
            template.GetComponent<RectTransform>();
        templateRect.sizeDelta = new Vector2(0f, 66f);

        Image background = template.GetComponent<Image>();
        background.color = new Color(0.10f, 0.16f, 0.20f, 1f);

        Button templateButton = template.GetComponent<Button>();
        templateButton.targetGraphic = background;

        LayoutElement layout = template.GetComponent<LayoutElement>();
        layout.preferredHeight = 68f;
        layout.minHeight = 68f;

        TMP_Text optionLabel = CreateStretchText(
            template.transform, "OptionLabel", 20f);

        CareerGarageOptionView option =
            template.GetComponent<CareerGarageOptionView>();

        SerializedObject optionSerialized = new SerializedObject(option);
        SetReference(optionSerialized, "button", templateButton);
        SetReference(optionSerialized, "labelText", optionLabel);
        SetReference(optionSerialized, "background", background);
        optionSerialized.ApplyModifiedPropertiesWithoutUndo();

        templateRoot.SetActive(false);

        RectTransform modal = CreateRect(shopRoot, "ShopConfirmation", Vector2.zero, Vector2.zero, true);
        Image dimmer = modal.gameObject.AddComponent<Image>();
        dimmer.color = new Color(0, 0, 0, 0.92f);
        TMP_Text confirmation = CreateText(modal, "ConfirmationText", "", 28,
            new Vector2(620, -330), new Vector2(850, 280));
        Button confirm = CreateButton(modal, "ConfirmPurchase", "CONFIRM PURCHASE", new Vector2(620, -650), new Vector2(400, 70));
        Button cancel = CreateButton(modal, "CancelPurchase", "CANCEL", new Vector2(1050, -650), new Vector2(400, 70));
        modal.gameObject.SetActive(false);

        SerializedObject serialized = new SerializedObject(view);
        SetReference(serialized, "categoryList", categories);
        SetReference(serialized, "itemList", items);
        SetReference(serialized, "optionTemplate", option);
        SetReference(serialized, "creditsText", credits);
        SetReference(serialized, "detailsText", details);
        SetReference(serialized, "feedbackText", feedback);
        SetReference(serialized, "purchaseButton", purchase);
        SetReference(serialized, "saveButton", save);
        SetReference(serialized, "garageButton", garage);
        SetReference(serialized, "confirmationRoot", modal.gameObject);
        SetReference(serialized, "confirmationText", confirmation);
        SetReference(serialized, "confirmButton", confirm);
        SetReference(serialized, "cancelButton", cancel);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject controller = new SerializedObject(career);
        SetReference(controller, "shopView", view);
        controller.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = shopRoot.gameObject;
        Debug.Log("RACE//FATAL Shop UI built in 01_Career. Load a campaign from 00_Bootstrap to shop.");
    }

    private static RectTransform CreateScroller(
        Transform parent, string name, Vector2 position, Vector2 size)
    {
        RectTransform root = CreateRect(parent, name, position, size);

        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.055f, 0.085f, 0.11f, 0.96f);

        ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 22f;

        RectTransform viewport = CreateRect(
            root, "Viewport", Vector2.zero, Vector2.zero, true);

        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);

        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = CreateRect(
            viewport, "Content", Vector2.zero, Vector2.zero, true);

        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout =
            content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter =
            content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        return content;
    }

    private static RectTransform CreateRect(
        Transform parent, string name,
        Vector2 position, Vector2 size, bool stretch = false)
    {
        GameObject obj = new GameObject(
            name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();

        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        return rect;
    }

    private static TMP_Text CreateText(
        Transform parent, string name, string value,
        float size, Vector2 position, Vector2 dimensions)
    {
        RectTransform rect = CreateRect(parent, name, position, dimensions);
        TextMeshProUGUI label =
            rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.color = new Color(0.84f, 0.95f, 0.97f, 1f);
        return label;
    }

    private static TMP_Text CreateStretchText(
        Transform parent, string name, float size)
    {
        RectTransform rect = CreateRect(
            parent, name, Vector2.zero, Vector2.zero, true);
        rect.offsetMin = new Vector2(12, 4);
        rect.offsetMax = new Vector2(-12, -4);

        TextMeshProUGUI label =
            rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = string.Empty;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        return label;
    }

    private static Button CreateButton(
        Transform parent, string name, string label,
        Vector2 position, Vector2 dimensions)
    {
        RectTransform rect = CreateRect(
            parent, name, position, dimensions);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.11f, 0.26f, 0.29f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text =
            CreateStretchText(rect, "Label", 22f);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private static void SetReference(
        SerializedObject obj, string propertyName, Object value)
    {
        SerializedProperty property = obj.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            Object.DestroyImmediate(child.gameObject);
    }
}