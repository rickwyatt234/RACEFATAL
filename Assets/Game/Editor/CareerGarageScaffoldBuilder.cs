using RaceFatal.Presentation.Career;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CareerGarageScaffoldBuilder
{
    private const string ScenePath =
        "Assets/Game/Scenes/01_Career.unity";

    [MenuItem("RACE//FATAL/Career/Build Garage UI")]
    public static void BuildGarage()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(
            ScenePath, OpenSceneMode.Single);

        GameObject systems = GameObject.Find("CareerSystems");
        GameObject canvas = GameObject.Find("CareerCanvas");

        Transform garageRoot = canvas != null
            ? canvas.transform.Find("GarageRoot")
            : null;

        CareerController career =
            systems != null
                ? systems.GetComponent<CareerController>()
                : null;

        if (career == null || garageRoot == null)
        {
            EditorUtility.DisplayDialog(
                "RACE//FATAL",
                "CareerSystems or GarageRoot is missing. " +
                "Build the Career Hub scaffold before upgrading the Garage.",
                "OK");
            return;
        }

        // This builder only replaces its own generated panel and the
        // original placeholder. Other career screens are untouched.
        DestroyChild(garageRoot, "GarageUI");
        DestroyChild(garageRoot, "Placeholder");

        CareerGarageView view =
            garageRoot.GetComponent<CareerGarageView>();

        if (view == null)
            view = garageRoot.gameObject.AddComponent<CareerGarageView>();

        RectTransform ui = CreateRect(
            garageRoot, "GarageUI", Vector2.zero, Vector2.zero,
            true);

        CreateText(ui, "BikeListHeading", "OWNED BIKES", 28,
            new Vector2(410, -160), new Vector2(420, 42));

        CreateText(ui, "SlotListHeading", "BIKE COMPONENT SLOTS", 28,
            new Vector2(850, -160), new Vector2(430, 42));

        TMP_Text inventoryHeading = CreateText(
            ui, "InventoryHeading", "OWNED INVENTORY", 28,
            new Vector2(1310, -160), new Vector2(485, 42));

        RectTransform bikes = CreateScroller(
            ui, "OwnedBikes", new Vector2(410, -225),
            new Vector2(410, 410));

        RectTransform slots = CreateScroller(
            ui, "ComponentSlots", new Vector2(850, -225),
            new Vector2(420, 410));

        RectTransform inventory = CreateScroller(
            ui, "OwnedInventory", new Vector2(1310, -225),
            new Vector2(480, 410));

        TMP_Text bikeDetails = CreateText(
            ui, "BikeDetails", string.Empty, 23,
            new Vector2(410, -662), new Vector2(1380, 76));

        TMP_Text slotDetails = CreateText(
            ui, "SlotDetails", string.Empty, 23,
            new Vector2(410, -744), new Vector2(850, 44));

        TMP_Text assignments = CreateText(
            ui, "Assignments", string.Empty, 23,
            new Vector2(410, -802), new Vector2(630, 100));

        Button assignPlayer = CreateButton(
            ui, "AssignPlayer", "ASSIGN TO PLAYER",
            new Vector2(1100, -785), new Vector2(320, 64));

        Button assignPartner = CreateButton(
            ui, "AssignPartner", "ASSIGN TO PARTNER",
            new Vector2(1450, -785), new Vector2(320, 64));

        Button remove = CreateButton(
            ui, "RemoveComponent", "REMOVE SELECTED COMPONENT",
            new Vector2(1100, -865), new Vector2(320, 64));

        Button save = CreateButton(
            ui, "SaveGarage", "SAVE GARAGE",
            new Vector2(1450, -865), new Vector2(320, 64));

        TMP_Text feedback = CreateText(
            ui, "GarageFeedback",
            "SELECT A BIKE, THEN A SLOT TO VIEW OWNED COMPONENTS.",
            23, new Vector2(410, -977), new Vector2(1370, 55));

        // The inactive template must not live under the list content.
        GameObject templateRoot =
            new GameObject("GarageTemplates", typeof(RectTransform));
        templateRoot.transform.SetParent(ui, false);

        GameObject template = new GameObject(
            "GarageOptionTemplate",
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

        SerializedObject viewSerialized = new SerializedObject(view);
        SetReference(viewSerialized, "bikeList", bikes);
        SetReference(viewSerialized, "slotList", slots);
        SetReference(viewSerialized, "inventoryList", inventory);
        SetReference(viewSerialized, "optionTemplate", option);
        SetReference(viewSerialized, "bikeDetailsText", bikeDetails);
        SetReference(viewSerialized, "slotDetailsText", slotDetails);
        SetReference(viewSerialized, "assignmentText", assignments);
        SetReference(viewSerialized, "inventoryHeadingText", inventoryHeading);
        SetReference(viewSerialized, "feedbackText", feedback);
        SetReference(viewSerialized, "assignPlayerButton", assignPlayer);
        SetReference(viewSerialized, "assignPartnerButton", assignPartner);
        SetReference(viewSerialized, "removeButton", remove);
        SetReference(viewSerialized, "saveButton", save);
        viewSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject careerSerialized = new SerializedObject(career);
        SetReference(careerSerialized, "garageView", view);
        careerSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = garageRoot.gameObject;

        Debug.Log(
            "RACE//FATAL Garage UI built in 01_Career. " +
            "Other career screens were preserved.");
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