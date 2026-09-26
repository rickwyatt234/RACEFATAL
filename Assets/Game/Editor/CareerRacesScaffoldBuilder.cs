using RaceFatal.Presentation.Career;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CareerRacesScaffoldBuilder
{
    private const string ScenePath =
        "Assets/Game/Scenes/01_Career.unity";

    [MenuItem("RACE//FATAL/Career/Build Races Screen")]
    public static void BuildRacesScreen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Exit Play Mode before building Races UI."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var systems = GameObject.Find("CareerSystems");
        var canvas = GameObject.Find("CareerCanvas");
        var owner = systems != null ? systems.GetComponent<CareerController>() : null;
        var root = canvas != null ? canvas.transform.Find("RacesRoot") : null;
        if (owner == null || root == null)
        { EditorUtility.DisplayDialog("RACE//FATAL", "Build the Career Hub scaffold first.", "OK"); return; }
        var view = root.GetComponent<CareerRacesView>();
        if (view == null) view = root.gameObject.AddComponent<CareerRacesView>();
        ClearChildren(root);
        CreateText(root, "Title", "RACES // CHAMPIONSHIPS", 40, new Vector2(410, -85), new Vector2(1300, 62));
        var calendar = CreateButton(root, "Calendar", "THIS WEEK", new Vector2(410, -165), new Vector2(300, 52));
        var unlocked = CreateButton(root, "Unlocked", "UNLOCKED EVENTS", new Vector2(740, -165), new Vector2(340, 52));
        var fame = CreateButton(root, "Fame", "FAME MILESTONES", new Vector2(1110, -165), new Vector2(340, 52));
        var standings = CreateButton(root, "Standings", "STANDINGS", new Vector2(1480, -165), new Vector2(340, 52));
        var week = CreateText(root, "Week", "WEEK --", 24, new Vector2(410, -232), new Vector2(1400, 40));
        var listPanel = CreatePanel(root, "EventList", new Vector2(410, -280), new Vector2(645, 610));
        var content = CreateScrollList(listPanel);
        var template = CreateCardTemplate(root);
        var details = CreatePanel(root, "EventDetails", new Vector2(1080, -280), new Vector2(740, 610));
        var selectedName = CreateText(details, "EventName", "SELECT AN EVENT", 30, new Vector2(20, -20), new Vector2(430, 72));
        var selectedTrack = CreateText(details, "Track", "", 23, new Vector2(20, -100), new Vector2(425, 70));
        var diagramPanel = CreatePanel(details, "Diagram", new Vector2(475, -20), new Vector2(245, 145));
        var diagram = CreatePanel(diagramPanel, "TrackDiagram", Vector2.zero, new Vector2(245, 145)).GetComponent<Image>();
        diagram.preserveAspect = true; diagram.raycastTarget = false; diagram.color = Color.white;
        var placeholder = CreateText(diagramPanel, "Placeholder", "TRACK DIAGRAM\nNOT AVAILABLE", 20, new Vector2(15, -40), new Vector2(215, 90));
        var scrollRoot = CreatePanel(details, "DetailScroll", new Vector2(20, -185), new Vector2(700, 305));
        var viewport = CreatePanel(scrollRoot, "Viewport", Vector2.zero, new Vector2(700, 305));
        viewport.gameObject.AddComponent<RectMask2D>();
        var requirements = CreateText(viewport, "Details", "", 21, Vector2.zero, Vector2.zero);
        var textRect = requirements.rectTransform;
        textRect.anchorMin = new Vector2(0, 1); textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(8, 0); textRect.offsetMax = new Vector2(-8, 0);
        requirements.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = textRect; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
        var status = CreateText(details, "Status", "", 21, new Vector2(20, -515), new Vector2(700, 80));
        var advance = CreateButton(root, "AdvanceWeek", "SKIP WEEK", new Vector2(410, -915), new Vector2(645, 65));
        var enter = CreateButton(root, "EnterEvent", "ENTER / CONTINUE", new Vector2(1080, -915), new Vector2(740, 65));
        var feedback = CreateText(root, "Feedback", "", 21, new Vector2(410, -998), new Vector2(1410, 75));
        var modal = CreatePanel(root, "EventConfirmation", Vector2.zero, Vector2.zero); Stretch(modal, 0);
        modal.GetComponent<Image>().color = new Color(0, 0, 0, .96f);
        var confirmation = CreateText(modal, "Message", "", 28, new Vector2(600, -275), new Vector2(990, 360));
        var confirm = CreateButton(modal, "Confirm", "CONFIRM", new Vector2(620, -700), new Vector2(400, 70));
        var cancel = CreateButton(modal, "Cancel", "CANCEL", new Vector2(1100, -700), new Vector2(400, 70));
        modal.gameObject.SetActive(false);
        SetReference(view, "cardContainer", content); SetReference(view, "cardTemplate", template);
        SetReference(view, "weekText", week); SetReference(view, "selectedRaceNameText", selectedName);
        SetReference(view, "selectedTrackText", selectedTrack); SetReference(view, "selectedRequirementsText", requirements);
        SetReference(view, "selectedStatusText", status); SetReference(view, "feedbackText", feedback);
        SetReference(view, "trackDiagram", diagram); SetReference(view, "diagramPlaceholder", placeholder);
        SetReference(view, "enterRaceButton", enter); SetReference(view, "calendarButton", calendar);
        SetReference(view, "unlockedButton", unlocked); SetReference(view, "fameButton", fame);
        SetReference(view, "standingsButton", standings); SetReference(view, "advanceButton", advance);
        SetReference(view, "advanceLabel", advance.GetComponentInChildren<TMP_Text>());
        SetReference(view, "confirmationRoot", modal.gameObject); SetReference(view, "confirmationText", confirmation);
        SetReference(view, "confirmButton", confirm); SetReference(view, "cancelButton", cancel);
        SetReference(owner, "racesView", view);
        template.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("Races calendar UI built. Start from Bootstrap and load a campaign.");
    }

    private static RectTransform CreateScrollList(
        RectTransform parent)
    {
        GameObject scrollObject = new GameObject(
            "RaceScroll",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(ScrollRect));

        scrollObject.transform.SetParent(parent, false);

        RectTransform scrollRect =
            scrollObject.GetComponent<RectTransform>();
        Stretch(scrollRect, 12f);

        scrollObject.GetComponent<Image>().color =
            new Color(0.055f, 0.06f, 0.075f, 0.98f);

        GameObject viewport = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(RectMask2D));

        viewport.transform.SetParent(
            scrollObject.transform,
            false);

        RectTransform viewportRect =
            viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, 12f);

        GameObject content = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));

        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect =
            content.GetComponent<RectTransform>();

        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout =
            content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(5, 5, 7, 7);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter =
            content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit =
            ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll =
            scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType =
            ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        return contentRect;
    }

    private static CareerRaceCardView CreateCardTemplate(
        Transform parent)
    {
        GameObject root = new GameObject(
            "RaceCardTemplate",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement),
            typeof(CareerRaceCardView));

        root.transform.SetParent(parent, false);

        RectTransform rect =
            root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(685, 160);

        Image image = root.GetComponent<Image>();
        image.color = new Color(0.12f, 0.13f, 0.16f, 1f);

        LayoutElement layout =
            root.GetComponent<LayoutElement>();
        layout.preferredHeight = 160f;
        layout.minHeight = 160f;

        TMP_Text name = CreateText(
            root.transform,
            "RaceName",
            "RACE NAME",
            28,
            new Vector2(18, -13),
            new Vector2(550, 45));

        TMP_Text track = CreateText(
            root.transform,
            "TrackName",
            "TRACK",
            21,
            new Vector2(18, -62),
            new Vector2(550, 34));

        TMP_Text requirements = CreateText(
            root.transform,
            "Requirements",
            "ENGINE // LAPS // RACERS",
            18,
            new Vector2(18, -98),
            new Vector2(550, 32));

        TMP_Text availability = CreateText(
            root.transform,
            "Availability",
            "AVAILABLE",
            17,
            new Vector2(18, -130),
            new Vector2(550, 28));

        CareerRaceCardView view =
            root.GetComponent<CareerRaceCardView>();

        SetReference(view, "raceNameText", name);
        SetReference(view, "trackNameText", track);
        SetReference(view, "requirementsText", requirements);
        SetReference(view, "availabilityText", availability);
        SetReference(view, "selectButton",
            root.GetComponent<Button>());
        SetReference(view, "backgroundImage", image);

        return view;
    }

    private static RectTransform CreatePanel(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size)
    {
        GameObject root = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        root.transform.SetParent(parent, false);

        RectTransform rect =
            root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        root.GetComponent<Image>().color =
            new Color(0.08f, 0.09f, 0.115f, 0.97f);

        return rect;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string caption,
        Vector2 position,
        Vector2 size)
    {
        GameObject root = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));

        root.transform.SetParent(parent, false);

        RectTransform rect =
            root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = root.GetComponent<Image>();
        image.color = new Color(0.16f, 0.29f, 0.34f, 1f);

        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;
        button.interactable = true;

        TMP_Text label = CreateText(
            root.transform,
            "Label",
            caption,
            28,
            Vector2.zero,
            Vector2.zero);

        RectTransform labelRect =
            label.GetComponent<RectTransform>();
        Stretch(labelRect, 8f);
        label.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        Vector2 position,
        Vector2 size)
    {
        GameObject root = new GameObject(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI));

        root.transform.SetParent(parent, false);

        RectTransform rect =
            root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text label = root.GetComponent<TMP_Text>();
        label.text = value;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        label.richText = false;

        return label;
    }

    private static void Stretch(
        RectTransform rect,
        float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void SetReference(
        Object target,
        string property,
        Object reference)
    {
        SerializedObject serialized =
            new SerializedObject(target);

        SerializedProperty field =
            serialized.FindProperty(property);

        if (field == null)
            throw new System.InvalidOperationException(
                $"Serialized field '{property}' missing on '{target.name}'.");

        field.objectReferenceValue = reference;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ClearChildren(Transform root)
    {
        while (root.childCount > 0)
            Object.DestroyImmediate(
                root.GetChild(0).gameObject);
    }
}
