using RaceFatal.Presentation.Career;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CareerHubScaffoldBuilder
{
    private const string ScenePath =
        "Assets/Game/Scenes/01_Career.unity";

    [MenuItem(
        "RACE//FATAL/Career/Build Career Hub Scaffold")]
    public static void BuildCareerHubScaffold()
    {
        if (!EditorSceneManager
            .SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene =
            EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);

        DestroyIfPresent(
            "CareerSystems");

        DestroyIfPresent(
            "CareerCanvas");

        DestroyIfPresent(
            "EventSystem");

        CreateEventSystem();

        Canvas canvas =
            CreateCanvas();

        GameObject systems =
            new GameObject(
                "CareerSystems");

        CareerController controller =
            systems.AddComponent<CareerController>();

        GameObject homeRoot =
            CreateScreen(
                canvas.transform,
                "HomeRoot",
                "CAREER // HOME");

        CareerHomeView homeView =
            homeRoot.AddComponent<CareerHomeView>();

        TMP_Text teamName =
            CreateText(
                homeRoot.transform,
                "TeamName",
                "TEAM NAME",
                46f,
                TextAlignmentOptions.TopLeft,
                new Vector2(410f, -205f),
                new Vector2(1100f, 64f));

        TMP_Text racerName =
            CreateText(
                homeRoot.transform,
                "RacerName",
                "RACER NAME",
                30f,
                TextAlignmentOptions.TopLeft,
                new Vector2(410f, -280f),
                new Vector2(900f, 52f));

        TMP_Text credits =
            CreateText(
                homeRoot.transform,
                "Credits",
                "CREDITS  0",
                28f,
                TextAlignmentOptions.TopLeft,
                new Vector2(410f, -400f),
                new Vector2(700f, 48f));

        TMP_Text fame =
            CreateText(
                homeRoot.transform,
                "TeamFame",
                "TEAM FAME  0",
                28f,
                TextAlignmentOptions.TopLeft,
                new Vector2(410f, -465f),
                new Vector2(700f, 48f));

        TMP_Text research =
            CreateText(
                homeRoot.transform,
                "ResearchPoints",
                "RESEARCH POINTS  0",
                28f,
                TextAlignmentOptions.TopLeft,
                new Vector2(410f, -530f),
                new Vector2(700f, 48f));

        TMP_Text bike =
            CreateText(
                homeRoot.transform,
                "CurrentBike",
                "CURRENT BIKE  NONE",
                26f,
                TextAlignmentOptions.TopLeft,
                new Vector2(1140f, -400f),
                new Vector2(650f, 48f));

        TMP_Text partner =
            CreateText(
                homeRoot.transform,
                "CurrentPartner",
                "CURRENT PARTNER  NONE",
                26f,
                TextAlignmentOptions.TopLeft,
                new Vector2(1140f, -465f),
                new Vector2(650f, 48f));

        TMP_Text careerStatus =
            CreateText(
                homeRoot.transform,
                "CareerStatus",
                "CAREER STATUS  ACTIVE",
                26f,
                TextAlignmentOptions.TopLeft,
                new Vector2(1140f, -530f),
                new Vector2(650f, 48f));

        ConfigureHomeView(
            homeView,
            teamName,
            racerName,
            credits,
            fame,
            research,
            bike,
            partner,
            careerStatus);

        GameObject racesRoot =
            CreatePlaceholderScreen(
                canvas.transform,
                "RacesRoot",
                "RACES // CHAMPIONSHIPS",
                "Race selection will be implemented next.");

        GameObject garageRoot =
            CreatePlaceholderScreen(
                canvas.transform,
                "GarageRoot",
                "GARAGE",
                "Owned bikes and loadout management will appear here.");

        GameObject researchRoot =
            CreatePlaceholderScreen(
                canvas.transform,
                "ResearchRoot",
                "RESEARCH",
                "Technology progression will appear here.");

        GameObject shopRoot =
            CreatePlaceholderScreen(
                canvas.transform,
                "ShopRoot",
                "SHOP",
                "Purchasable bikes, parts, and equipment will appear here.");

        GameObject rosterRoot =
            CreatePlaceholderScreen(
                canvas.transform,
                "RosterRoot",
                "ROSTER",
                "Team racers and career status will appear here.");

        GameObject teamRoot =
            CreatePlaceholderScreen(
                canvas.transform,
                "TeamRoot",
                "TEAM",
                "Team identity and progression will appear here.");

        GameObject navigation =
            CreateNavigationPanel(
                canvas.transform);

        CreateText(
            navigation.transform,
            "CareerLabel",
            "RACE//FATAL\nCAREER",
            34f,
            TextAlignmentOptions.TopLeft,
            new Vector2(30f, -35f),
            new Vector2(260f, 100f),
            new Vector2(0f, 1f));

        Button homeButton =
            CreateNavigationButton(
                navigation.transform,
                "HOME",
                0);

        Button racesButton =
            CreateNavigationButton(
                navigation.transform,
                "RACES",
                1);

        Button garageButton =
            CreateNavigationButton(
                navigation.transform,
                "GARAGE",
                2);

        Button researchButton =
            CreateNavigationButton(
                navigation.transform,
                "RESEARCH",
                3);

        Button shopButton =
            CreateNavigationButton(
                navigation.transform,
                "SHOP",
                4);

        Button rosterButton =
            CreateNavigationButton(
                navigation.transform,
                "ROSTER",
                5);

        Button teamButton =
            CreateNavigationButton(
                navigation.transform,
                "TEAM",
                6);

        Button saveAndReturnButton =
            CreateBottomButton(
                navigation.transform,
                "SAVE & RETURN");

        TMP_Text errorText =
            CreateText(
                canvas.transform,
                "CareerErrorText",
                string.Empty,
                20f,
                TextAlignmentOptions.Left,
                new Vector2(350f, 22f),
                new Vector2(1500f, 40f),
                new Vector2(0f, 0f));

        ConfigureCareerController(
            controller,
            homeRoot,
            racesRoot,
            garageRoot,
            researchRoot,
            shopRoot,
            rosterRoot,
            teamRoot,
            homeButton,
            racesButton,
            garageButton,
            researchButton,
            shopButton,
            rosterButton,
            teamButton,
            saveAndReturnButton,
            homeView,
            errorText);

        homeRoot.SetActive(true);
        racesRoot.SetActive(false);
        garageRoot.SetActive(false);
        researchRoot.SetActive(false);
        shopRoot.SetActive(false);
        rosterRoot.SetActive(false);
        teamRoot.SetActive(false);

        EditorSceneManager.MarkSceneDirty(
            scene);

        EditorSceneManager.SaveScene(
            scene);

        Selection.activeGameObject =
            systems;

        Debug.Log(
            "RACE//FATAL career hub scaffold built in 01_Career.");
    }

    private static void ConfigureHomeView(
        CareerHomeView view,
        TMP_Text teamName,
        TMP_Text racerName,
        TMP_Text credits,
        TMP_Text fame,
        TMP_Text research,
        TMP_Text bike,
        TMP_Text partner,
        TMP_Text careerStatus)
    {
        SerializedObject serialized =
            new SerializedObject(
                view);

        SetReference(serialized, "teamNameText", teamName);
        SetReference(serialized, "racerNameText", racerName);
        SetReference(serialized, "creditsText", credits);
        SetReference(serialized, "fameText", fame);
        SetReference(serialized, "researchPointsText", research);
        SetReference(serialized, "currentBikeText", bike);
        SetReference(serialized, "currentPartnerText", partner);
        SetReference(serialized, "careerStatusText", careerStatus);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCareerController(
        CareerController controller,
        GameObject homeRoot,
        GameObject racesRoot,
        GameObject garageRoot,
        GameObject researchRoot,
        GameObject shopRoot,
        GameObject rosterRoot,
        GameObject teamRoot,
        Button homeButton,
        Button racesButton,
        Button garageButton,
        Button researchButton,
        Button shopButton,
        Button rosterButton,
        Button teamButton,
        Button saveAndReturnButton,
        CareerHomeView homeView,
        TMP_Text errorText)
    {
        SerializedObject serialized =
            new SerializedObject(
                controller);

        SetReference(serialized, "homeRoot", homeRoot);
        SetReference(serialized, "racesRoot", racesRoot);
        SetReference(serialized, "garageRoot", garageRoot);
        SetReference(serialized, "researchRoot", researchRoot);
        SetReference(serialized, "shopRoot", shopRoot);
        SetReference(serialized, "rosterRoot", rosterRoot);
        SetReference(serialized, "teamRoot", teamRoot);

        SetReference(serialized, "homeButton", homeButton);
        SetReference(serialized, "racesButton", racesButton);
        SetReference(serialized, "garageButton", garageButton);
        SetReference(serialized, "researchButton", researchButton);
        SetReference(serialized, "shopButton", shopButton);
        SetReference(serialized, "rosterButton", rosterButton);
        SetReference(serialized, "teamButton", teamButton);
        SetReference(serialized, "saveAndReturnButton", saveAndReturnButton);

        SetReference(serialized, "homeView", homeView);
        SetReference(serialized, "errorText", errorText);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreatePlaceholderScreen(
        Transform parent,
        string name,
        string title,
        string body)
    {
        GameObject root =
            CreateScreen(
                parent,
                name,
                title);

        CreateText(
            root.transform,
            "Placeholder",
            body,
            28f,
            TextAlignmentOptions.TopLeft,
            new Vector2(410f, -240f),
            new Vector2(1200f, 180f));

        return root;
    }

    private static GameObject CreateScreen(
        Transform parent,
        string name,
        string title)
    {
        GameObject root =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

        root.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            root.GetComponent<RectTransform>();

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        root.GetComponent<Image>().color =
            new Color(
                0.035f,
                0.04f,
                0.055f,
                0.98f);

        CreateText(
            root.transform,
            "Title",
            title,
            42f,
            TextAlignmentOptions.TopLeft,
            new Vector2(410f, -85f),
            new Vector2(1300f, 70f));

        return root;
    }

    private static GameObject CreateNavigationPanel(
        Transform parent)
    {
        GameObject root =
            new GameObject(
                "Navigation",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

        root.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            root.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 0f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0f, 0.5f);

        rect.anchoredPosition =
            Vector2.zero;

        rect.sizeDelta =
            new Vector2(330f, 0f);

        root.GetComponent<Image>().color =
            new Color(
                0.06f,
                0.065f,
                0.08f,
                1f);

        return root;
    }

    private static Button CreateNavigationButton(
        Transform parent,
        string label,
        int row)
    {
        GameObject root =
            CreateButtonObject(
                parent,
                label);

        RectTransform rect =
            root.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0f, 1f);

        rect.anchoredPosition =
            new Vector2(
                28f,
                -165f - row * 82f);

        rect.sizeDelta =
            new Vector2(
                274f,
                62f);

        return root.GetComponent<Button>();
    }

    private static Button CreateBottomButton(
        Transform parent,
        string label)
    {
        GameObject root =
            CreateButtonObject(
                parent,
                label);

        RectTransform rect =
            root.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 0f);

        rect.anchorMax =
            new Vector2(0f, 0f);

        rect.pivot =
            new Vector2(0f, 0f);

        rect.anchoredPosition =
            new Vector2(28f, 36f);

        rect.sizeDelta =
            new Vector2(274f, 70f);

        return root.GetComponent<Button>();
    }

    private static GameObject CreateButtonObject(
        Transform parent,
        string label)
    {
        GameObject root =
            new GameObject(
                label.Replace(" ", string.Empty) + "Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

        root.transform.SetParent(
            parent,
            false);

        root.GetComponent<Image>().color =
            new Color(
                0.12f,
                0.13f,
                0.16f,
                1f);

        CreateStretchText(
            root.transform,
            label,
            22f);

        return root;
    }

    private static TMP_Text CreateStretchText(
        Transform parent,
        string value,
        float size)
    {
        GameObject root =
            new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        root.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            root.GetComponent<RectTransform>();

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            new Vector2(16f, 4f);

        rect.offsetMax =
            new Vector2(-16f, -4f);

        TMP_Text text =
            root.GetComponent<TMP_Text>();

        text.text =
            value;

        text.fontSize =
            size;

        text.alignment =
            TextAlignmentOptions.MidlineLeft;

        return text;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float size,
        TextAlignmentOptions alignment,
        Vector2 position,
        Vector2 dimensions,
        Vector2? anchor = null)
    {
        GameObject root =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        root.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            root.GetComponent<RectTransform>();

        Vector2 resolvedAnchor =
            anchor ?? new Vector2(0f, 1f);

        rect.anchorMin =
            resolvedAnchor;

        rect.anchorMax =
            resolvedAnchor;

        rect.pivot =
            resolvedAnchor;

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            dimensions;

        TMP_Text text =
            root.GetComponent<TMP_Text>();

        text.text =
            value;

        text.fontSize =
            size;

        text.alignment =
            alignment;

        return text;
    }

    private static Canvas CreateCanvas()
    {
        GameObject root =
            new GameObject(
                "CareerCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

        Canvas canvas =
            root.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            root.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.matchWidthOrHeight =
            0.5f;

        return canvas;
    }

    private static void CreateEventSystem()
    {
        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
    }

    private static void SetReference(
        SerializedObject serialized,
        string propertyName,
        Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property != null)
        {
            property.objectReferenceValue =
                value;
        }
    }

    private static void DestroyIfPresent(
        string name)
    {
        GameObject existing =
            GameObject.Find(
                name);

        if (existing != null)
        {
            Object.DestroyImmediate(
                existing);
        }
    }
}
