using RaceFatal.Presentation.Career;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CareerShopScaffoldBuilder
{
    private const string ScenePath =
        "Assets/Game/Scenes/01_Career.unity";

    [MenuItem("RACE//FATAL/Career/Build Shop UI")]
    public static void BuildShop()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(
            ScenePath, OpenSceneMode.Single);

        GameObject systems = GameObject.Find("CareerSystems");
        GameObject canvas = GameObject.Find("CareerCanvas");

        Transform shopRoot = canvas != null
            ? canvas.transform.Find("ShopRoot")
            : null;

        CareerController career = systems != null
            ? systems.GetComponent<CareerController>()
            : null;

        if (career == null || shopRoot == null)
        {
            EditorUtility.DisplayDialog(
                "RACE//FATAL",
                "CareerSystems or ShopRoot is missing. " +
                "Build or repair the Career Hub scene before upgrading the Shop.",
                "OK");
            return;
        }

        // Only replaces the generated Shop UI and original placeholder.
        // Do not rebuild Career Hub, Garage, or user-customized screens.
        DestroyChild(shopRoot, "ShopUI");
        DestroyChild(shopRoot, "Placeholder");

        CareerShopView shopView =
            shopRoot.GetComponent<CareerShopView>();

        if (shopView == null)
            shopView = shopRoot.gameObject.AddComponent<CareerShopView>();

        RectTransform ui = CreateRect(
            shopRoot, "ShopUI", Vector2.zero, Vector2.zero, true);

        TMP_Text balance = CreateText(
            ui, "ShopBalance", "CREDITS  --", 28f,
            new Vector2(1200, -158), new Vector2(540, 55));

        Button all = CreateButton(
            ui, "FilterAll", "ALL",
            new Vector2(410, -166), new Vector2(130, 64));

        Button bikes = CreateButton(
            ui, "FilterBikes", "BIKES",
            new Vector2(555, -166), new Vector2(130, 64));

        Button engines = CreateButton(
            ui, "FilterEngines", "ENGINES",
            new Vector2(700, -166), new Vector2(140, 64));

        Button chassis = CreateButton(
            ui, "FilterChassis", "CHASSIS",
            new Vector2(855, -166), new Vector2(140, 64));

        Button equipment = CreateButton(
            ui, "FilterEquipment", "EQUIPMENT",
            new Vector2(1010, -166), new Vector2(165, 64));

        RectTransform offers = CreateScroller(
            ui, "Offers", new Vector2(410, -255),
            new Vector2(765, 620));

        RectTransform detailPanel = CreateRect(
            ui, "Details", new Vector2(1200, -255),
            new Vector2(590, 620));

        Image panelBackground =
            detailPanel.gameObject.AddComponent<Image>();
        panelBackground.color =
            new Color(0.055f, 0.085f, 0.11f, 0.96f);

        TMP_Text itemName = CreateText(
            detailPanel, "ItemName", "SELECT AN ITEM",
            31f, new Vector2(24, -22), new Vector2(540, 88));

        TMP_Text details = CreateText(
            detailPanel, "ItemDetails", string.Empty,
            23f, new Vector2(24, -140), new Vector2(540, 270));

        TMP_Text requirements = CreateText(
            detailPanel, "Requirements", string.Empty,
            23f, new Vector2(24, -420), new Vector2(540, 66));

        Button purchase = CreateButton(
            detailPanel, "PurchaseButton", "PURCHASE",
            new Vector2(24, -516), new Vector2(540, 76));

        Button save = CreateButton(
            ui, "SaveShopButton", "SAVE CAMPAIGN",
            new Vector2(1200, -895), new Vector2(300, 64));

        TMP_Text feedback = CreateText(
            ui, "ShopFeedback",
            "SELECT AN ITEM TO INSPECT OR PURCHASE.",
            22f, new Vector2(410, -965), new Vector2(1380, 60));

        GameObject templateRoot =
            new GameObject("ShopTemplates", typeof(RectTransform));
        templateRoot.transform.SetParent(ui, false);

        RectTransform template = CreateRect(
            templateRoot.transform,
            "ShopOptionTemplate",
            Vector2.zero,
            new Vector2(0, 68));

        Image background = template.gameObject.AddComponent<Image>();
        background.color = new Color(0.10f, 0.16f, 0.20f, 1f);

        Button button = template.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        LayoutElement layout =
            template.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 76f;
        layout.minHeight = 76f;

        TMP_Text label = CreateStretchText(
            template, "OfferLabel", 19f);

        CareerGarageOptionView option =
            template.gameObject.AddComponent<CareerGarageOptionView>();

        SerializedObject optionSerialized = new SerializedObject(option);
        SetReference(optionSerialized, "button", button);
        SetReference(optionSerialized, "labelText", label);
        SetReference(optionSerialized, "background", background);
        optionSerialized.ApplyModifiedPropertiesWithoutUndo();

        templateRoot.SetActive(false);

        SerializedObject viewSerialized = new SerializedObject(shopView);
        SetReference(viewSerialized, "offersRoot", offers);
        SetReference(viewSerialized, "optionTemplate", option);
        SetReference(viewSerialized, "allButton", all);
        SetReference(viewSerialized, "bikesButton", bikes);
        SetReference(viewSerialized, "enginesButton", engines);
        SetReference(viewSerialized, "chassisButton", chassis);
        SetReference(viewSerialized, "equipmentButton", equipment);
        SetReference(viewSerialized, "balanceText", balance);
        SetReference(viewSerialized, "itemNameText", itemName);
        SetReference(viewSerialized, "itemDetailsText", details);
        SetReference(viewSerialized, "requirementText", requirements);
        SetReference(viewSerialized, "feedbackText", feedback);
        SetReference(viewSerialized, "purchaseButton", purchase);
        SetReference(viewSerialized, "saveButton", save);
        viewSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject careerSerialized = new SerializedObject(career);
        SetReference(careerSerialized, "shopView", shopView);
        careerSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = shopRoot.gameObject;

        Debug.Log(
            "RACE//FATAL Shop UI built in 01_Career. " +
            "All other Career screens are preserved.");
    }

    private static RectTransform CreateScroller(
        Transform parent, string name,
        Vector2 position, Vector2 size)
    {
        RectTransform root = CreateRect(parent, name, position, size);

        Image image = root.gameObject.AddComponent<Image>();
        image.color = new Color(0.055f, 0.085f, 0.11f, 0.96f);

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

        VerticalLayoutGroup group =
            content.gameObject.AddComponent<VerticalLayoutGroup>();

        group.spacing = 6f;
        group.padding = new RectOffset(8, 8, 8, 8);
        group.childAlignment = TextAnchor.UpperCenter;
        group.childControlHeight = true;
        group.childControlWidth = true;
        group.childForceExpandHeight = false;
        group.childForceExpandWidth = true;

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
        GameObject obj = new GameObject(name, typeof(RectTransform));
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
        RectTransform rect = CreateRect(
            parent, name, position, dimensions);

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

        TMP_Text text = CreateStretchText(rect, "Label", 20f);
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
