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
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(
            ScenePath,
            OpenSceneMode.Single);

        GameObject systems = GameObject.Find("CareerSystems");
        GameObject canvas = GameObject.Find("CareerCanvas");

        if (systems == null || canvas == null)
        {
            EditorUtility.DisplayDialog(
                "RACE//FATAL",
                "Build the Career Hub scaffold before building the Races screen.",
                "OK");
            return;
        }

        CareerController owner =
            systems.GetComponent<CareerController>();

        Transform racesRoot =
            canvas.transform.Find("RacesRoot");

        if (owner == null || racesRoot == null)
        {
            EditorUtility.DisplayDialog(
                "RACE//FATAL",
                "CareerController or RacesRoot is missing.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Rebuild Races Screen",
                "Replace the contents of RacesRoot? Other career screens " +
                "and all other scene objects will be left unchanged.",
                "Build",
                "Cancel"))
        {
            return;
        }

        CareerRacesView view =
            racesRoot.GetComponent<CareerRacesView>();

        if (view == null)
            view = racesRoot.gameObject.AddComponent<CareerRacesView>();

        ClearChildren(racesRoot);

        CreateText(
            racesRoot,
            "Title",
            "RACES // CHAMPIONSHIPS",
            40,
            new Vector2(410, -85),
            new Vector2(1250, 62));

        TMP_Text help = CreateText(
            racesRoot,
            "Instructions",
            "SELECT AN EVENT // DEFAULT TEAM AND BIKES",
            23,
            new Vector2(410, -160),
            new Vector2(780, 40));
        help.color = new Color(0.64f, 0.82f, 0.86f, 1f);

        RectTransform listPanel =
            CreatePanel(
                racesRoot,
                "RaceListPanel",
                new Vector2(405, -220),
                new Vector2(735, 646));

        RectTransform content = CreateScrollList(listPanel);

        CareerRaceCardView template =
            CreateCardTemplate(racesRoot);

        RectTransform detailsPanel =
            CreatePanel(
                racesRoot,
                "RaceDetailsPanel",
                new Vector2(1170, -220),
                new Vector2(650, 646));

        TMP_Text selectedName = CreateText(
            detailsPanel,
            "SelectedRaceName",
            "SELECT A RACE",
            36,
            new Vector2(24, -28),
            new Vector2(595, 100));

        TMP_Text selectedTrack = CreateText(
            detailsPanel,
            "SelectedTrack",
            "",
            26,
            new Vector2(24, -150),
            new Vector2(580, 50));

        TMP_Text requirements = CreateText(
            detailsPanel,
            "SelectedRequirements",
            "",
            25,
            new Vector2(24, -225),
            new Vector2(570, 205));

        TMP_Text selectedStatus = CreateText(
            detailsPanel,
            "SelectedStatus",
            "",
            22,
            new Vector2(24, -445),
            new Vector2(570, 95));

        Button enterButton = CreateButton(
            detailsPanel,
            "EnterRaceButton",
            "ENTER RACE",
            new Vector2(24, 28),
            new Vector2(600, 70));

        // This button anchors to the bottom-left of the details panel.
        RectTransform buttonRect =
            enterButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.zero;
        buttonRect.pivot = Vector2.zero;

        TMP_Text feedback = CreateText(
            racesRoot,
            "RaceStatus",
            "",
            21,
            new Vector2(410, -914),
            new Vector2(1330, 75));

        SetReference(view, "cardContainer", content);
        SetReference(view, "cardTemplate", template);
        SetReference(view, "selectedRaceNameText", selectedName);
        SetReference(view, "selectedTrackText", selectedTrack);
        SetReference(view, "selectedRequirementsText", requirements);
        SetReference(view, "selectedStatusText", selectedStatus);
        SetReference(view, "enterRaceButton", enterButton);
        SetReference(view, "feedbackText", feedback);
        SetReference(owner, "racesView", view);

        template.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = racesRoot.gameObject;

        Debug.Log(
            "RACE//FATAL: Career Races screen added. Other career screens preserved.");
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
            new Vector2(620, 45));

        TMP_Text track = CreateText(
            root.transform,
            "TrackName",
            "TRACK",
            21,
            new Vector2(18, -62),
            new Vector2(620, 34));

        TMP_Text requirements = CreateText(
            root.transform,
            "Requirements",
            "ENGINE // LAPS // RACERS",
            18,
            new Vector2(18, -98),
            new Vector2(640, 32));

        TMP_Text availability = CreateText(
            root.transform,
            "Availability",
            "AVAILABLE",
            17,
            new Vector2(18, -130),
            new Vector2(600, 28));

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
        button.interactable = false;

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
