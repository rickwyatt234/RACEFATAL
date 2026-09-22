using RaceFatal.Content.Career;
using RaceFatal.Presentation.FrontEnd;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NewCampaignWizardScaffoldBuilder
{
    private const string ScenePath =
        "Assets/Game/Scenes/03_MainMenu.unity";

    private const string DefaultsPath =
        "Assets/Game/Scripts/Content/Catalogs/NewCampaignDefaults.asset";

    [MenuItem(
        "RACE//FATAL/Front End/Build New Campaign Wizard")]
    public static void BuildNewCampaignWizard()
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

        GameObject systems =
            GameObject.Find(
                "FrontEndSystems");

        GameObject canvas =
            GameObject.Find(
                "FrontEndCanvas");

        if (systems == null ||
            canvas == null)
        {
            EditorUtility.DisplayDialog(
                "RACE//FATAL",
                "The base front-end scaffold was not found. Run " +
                "RACE//FATAL -> Front End -> Build Main Menu Scaffold first.",
                "OK");

            return;
        }

        FrontEndController frontEnd =
            systems.GetComponent<
                FrontEndController>();

        if (frontEnd == null)
        {
            EditorUtility.DisplayDialog(
                "RACE//FATAL",
                "FrontEndSystems has no FrontEndController.",
                "OK");

            return;
        }

        Transform teamRoot =
            canvas.transform.Find(
                "TeamCreationRoot");

        Transform racerRoot =
            canvas.transform.Find(
                "RacerCreationRoot");

        Transform reviewRoot =
            canvas.transform.Find(
                "CampaignReviewRoot");

        if (teamRoot == null ||
            racerRoot == null ||
            reviewRoot == null)
        {
            EditorUtility.DisplayDialog(
                "RACE//FATAL",
                "The new-campaign screen roots are missing. Rebuild the base main-menu scaffold first.",
                "OK");

            return;
        }

        TeamCreationController teamController =
            GetOrAdd<TeamCreationController>(
                systems);

        RacerCreationController racerController =
            GetOrAdd<RacerCreationController>(
                systems);

        CampaignReviewController reviewController =
            GetOrAdd<CampaignReviewController>(
                systems);

        BuildTeamScreen(
            teamRoot,
            teamController);

        BuildRacerScreen(
            racerRoot,
            racerController);

        BuildReviewScreen(
            reviewRoot,
            reviewController);

        ConfigureFrontEnd(
            frontEnd,
            teamController,
            racerController,
            reviewController);

        teamRoot.gameObject.SetActive(
            false);

        racerRoot.gameObject.SetActive(
            false);

        reviewRoot.gameObject.SetActive(
            false);

        EditorSceneManager.MarkSceneDirty(
            scene);

        EditorSceneManager.SaveScene(
            scene);

        Selection.activeGameObject =
            systems;

        Debug.Log(
            "RACE//FATAL new-campaign wizard scaffold built in 03_MainMenu.");
    }

    private static void BuildTeamScreen(
        Transform root,
        TeamCreationController controller)
    {
        ClearChildren(
            root);

        CreateTitle(
            root,
            "NEW CAMPAIGN // TEAM CREATION");

        TMP_InputField teamName =
            CreateLabeledInput(
                root,
                "TEAM NAME",
                "Enter team name",
                -235f);

        TMP_InputField primaryColor =
            CreateLabeledInput(
                root,
                "PRIMARY COLOR",
                "#FFFFFF",
                -390f);

        Image primaryPreview =
            CreateColorPreview(
                root,
                "PrimaryColorPreview",
                new Vector2(930f, -405f),
                Color.white);

        TMP_InputField secondaryColor =
            CreateLabeledInput(
                root,
                "SECONDARY COLOR",
                "#202020",
                -545f);

        Image secondaryPreview =
            CreateColorPreview(
                root,
                "SecondaryColorPreview",
                new Vector2(930f, -560f),
                new Color32(32, 32, 32, 255));

        TMP_Text status =
            CreateText(
                root,
                "TeamCreationStatus",
                string.Empty,
                22f,
                TextAlignmentOptions.Left,
                new Vector2(80f, 150f),
                new Vector2(1200f, 48f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f));

        Button back =
            CreateButton(
                root,
                "BackButton",
                "BACK",
                new Vector2(80f, 70f),
                new Vector2(250f, 64f),
                new Vector2(0f, 0f));

        Button continueButton =
            CreateButton(
                root,
                "ContinueButton",
                "CONTINUE",
                new Vector2(-80f, 70f),
                new Vector2(320f, 64f),
                new Vector2(1f, 0f));

        SerializedObject serialized =
            new SerializedObject(
                controller);

        SetReference(
            serialized,
            "teamNameInput",
            teamName);

        SetReference(
            serialized,
            "primaryColorInput",
            primaryColor);

        SetReference(
            serialized,
            "secondaryColorInput",
            secondaryColor);

        SetReference(
            serialized,
            "primaryColorPreview",
            primaryPreview);

        SetReference(
            serialized,
            "secondaryColorPreview",
            secondaryPreview);

        SetReference(
            serialized,
            "continueButton",
            continueButton);

        SetReference(
            serialized,
            "backButton",
            back);

        SetReference(
            serialized,
            "statusText",
            status);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildRacerScreen(
        Transform root,
        RacerCreationController controller)
    {
        ClearChildren(
            root);

        CreateTitle(
            root,
            "NEW CAMPAIGN // RACER CREATION");

        TMP_InputField racerName =
            CreateLabeledInput(
                root,
                "RACER NAME",
                "Enter racer name",
                -260f);

        CreateText(
            root,
            "RacerInfo",
            "This racer begins the new career as the player character.",
            24f,
            TextAlignmentOptions.TopLeft,
            new Vector2(80f, -410f),
            new Vector2(1100f, 100f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        TMP_Text status =
            CreateText(
                root,
                "RacerCreationStatus",
                string.Empty,
                22f,
                TextAlignmentOptions.Left,
                new Vector2(80f, 150f),
                new Vector2(1200f, 48f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f));

        Button back =
            CreateButton(
                root,
                "BackButton",
                "BACK",
                new Vector2(80f, 70f),
                new Vector2(250f, 64f),
                new Vector2(0f, 0f));

        Button continueButton =
            CreateButton(
                root,
                "ContinueButton",
                "CONTINUE",
                new Vector2(-80f, 70f),
                new Vector2(320f, 64f),
                new Vector2(1f, 0f));

        SerializedObject serialized =
            new SerializedObject(
                controller);

        SetReference(
            serialized,
            "racerNameInput",
            racerName);

        SetReference(
            serialized,
            "continueButton",
            continueButton);

        SetReference(
            serialized,
            "backButton",
            back);

        SetReference(
            serialized,
            "statusText",
            status);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildReviewScreen(
        Transform root,
        CampaignReviewController controller)
    {
        ClearChildren(
            root);

        CreateTitle(
            root,
            "NEW CAMPAIGN // REVIEW");

        TMP_Text slotText =
            CreateText(
                root,
                "SlotText",
                "SAVE 01",
                24f,
                TextAlignmentOptions.TopLeft,
                new Vector2(80f, -220f),
                new Vector2(800f, 44f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

        TMP_Text teamText =
            CreateText(
                root,
                "TeamNameText",
                "TEAM",
                42f,
                TextAlignmentOptions.TopLeft,
                new Vector2(80f, -290f),
                new Vector2(1200f, 60f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

        TMP_Text racerText =
            CreateText(
                root,
                "RacerNameText",
                "RACER",
                30f,
                TextAlignmentOptions.TopLeft,
                new Vector2(80f, -375f),
                new Vector2(1200f, 50f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

        TMP_Text colorsText =
            CreateText(
                root,
                "ColorsText",
                "PRIMARY #FFFFFF  //  SECONDARY #202020",
                24f,
                TextAlignmentOptions.TopLeft,
                new Vector2(80f, -455f),
                new Vector2(1200f, 44f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

        Image primaryPreview =
            CreateColorPreview(
                root,
                "PrimaryColorPreview",
                new Vector2(80f, -535f),
                Color.white);

        Image secondaryPreview =
            CreateColorPreview(
                root,
                "SecondaryColorPreview",
                new Vector2(160f, -535f),
                new Color32(32, 32, 32, 255));

        CreateText(
            root,
            "StartingContentInfo",
            "Starter partner and bike builds are supplied by the authored NewCampaignDefaults asset.",
            22f,
            TextAlignmentOptions.TopLeft,
            new Vector2(80f, -625f),
            new Vector2(1300f, 80f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        TMP_Text status =
            CreateText(
                root,
                "CampaignReviewStatus",
                string.Empty,
                22f,
                TextAlignmentOptions.Left,
                new Vector2(80f, 150f),
                new Vector2(1200f, 48f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f));

        Button back =
            CreateButton(
                root,
                "BackButton",
                "BACK",
                new Vector2(80f, 70f),
                new Vector2(250f, 64f),
                new Vector2(0f, 0f));

        Button confirm =
            CreateButton(
                root,
                "ConfirmButton",
                "BEGIN CAREER",
                new Vector2(-80f, 70f),
                new Vector2(360f, 64f),
                new Vector2(1f, 0f));

        SerializedObject serialized =
            new SerializedObject(
                controller);

        SetReference(
            serialized,
            "slotText",
            slotText);

        SetReference(
            serialized,
            "teamNameText",
            teamText);

        SetReference(
            serialized,
            "racerNameText",
            racerText);

        SetReference(
            serialized,
            "colorsText",
            colorsText);

        SetReference(
            serialized,
            "primaryColorPreview",
            primaryPreview);

        SetReference(
            serialized,
            "secondaryColorPreview",
            secondaryPreview);

        SetReference(
            serialized,
            "confirmButton",
            confirm);

        SetReference(
            serialized,
            "backButton",
            back);

        SetReference(
            serialized,
            "statusText",
            status);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureFrontEnd(
        FrontEndController frontEnd,
        TeamCreationController teamController,
        RacerCreationController racerController,
        CampaignReviewController reviewController)
    {
        SerializedObject serialized =
            new SerializedObject(
                frontEnd);

        SetReference(
            serialized,
            "teamCreationController",
            teamController);

        SetReference(
            serialized,
            "racerCreationController",
            racerController);

        SetReference(
            serialized,
            "campaignReviewController",
            reviewController);

        NewCampaignDefaultsSO defaults =
            AssetDatabase.LoadAssetAtPath<
                NewCampaignDefaultsSO>(
                    DefaultsPath);

        SetReference(
            serialized,
            "newCampaignDefaults",
            defaults);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetOrAdd<T>(
        GameObject gameObject)
        where T : Component
    {
        T component =
            gameObject.GetComponent<T>();

        if (component == null)
        {
            component =
                gameObject.AddComponent<T>();
        }

        return component;
    }

    private static void ClearChildren(
        Transform root)
    {
        for (int i =
                 root.childCount - 1;
             i >= 0;
             i--)
        {
            Object.DestroyImmediate(
                root.GetChild(i).gameObject);
        }
    }

    private static TMP_InputField CreateLabeledInput(
        Transform parent,
        string label,
        string placeholder,
        float y)
    {
        CreateText(
            parent,
            label.Replace(" ", string.Empty) + "Label",
            label,
            22f,
            TextAlignmentOptions.TopLeft,
            new Vector2(80f, y + 48f),
            new Vector2(760f, 36f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        GameObject root =
            new GameObject(
                label.Replace(" ", string.Empty) + "Input",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(TMP_InputField));

        root.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            root.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0f, 1f);

        rect.anchoredPosition =
            new Vector2(80f, y);

        rect.sizeDelta =
            new Vector2(800f, 72f);

        Image image =
            root.GetComponent<Image>();

        image.color =
            new Color(
                0.08f,
                0.09f,
                0.11f,
                0.96f);

        GameObject viewportObject =
            new GameObject(
                "Text Area",
                typeof(RectTransform),
                typeof(RectMask2D));

        viewportObject.transform.SetParent(
            root.transform,
            false);

        RectTransform viewport =
            viewportObject.GetComponent<
                RectTransform>();

        viewport.anchorMin =
            Vector2.zero;

        viewport.anchorMax =
            Vector2.one;

        viewport.offsetMin =
            new Vector2(18f, 10f);

        viewport.offsetMax =
            new Vector2(-18f, -10f);

        TMP_Text placeholderText =
            CreateText(
                viewportObject.transform,
                "Placeholder",
                placeholder,
                26f,
                TextAlignmentOptions.Left,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f));

        placeholderText.color =
            new Color(1f, 1f, 1f, 0.35f);

        SetStretch(
            placeholderText.rectTransform);

        TMP_Text inputText =
            CreateText(
                viewportObject.transform,
                "Text",
                string.Empty,
                26f,
                TextAlignmentOptions.Left,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f));

        SetStretch(
            inputText.rectTransform);

        TMP_InputField input =
            root.GetComponent<TMP_InputField>();

        input.textViewport =
            viewport;

        input.textComponent =
            inputText;

        input.placeholder =
            placeholderText;

        input.lineType =
            TMP_InputField.LineType.SingleLine;

        return input;
    }

    private static Image CreateColorPreview(
        Transform parent,
        string name,
        Vector2 position,
        Color color)
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
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0f, 1f);

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            new Vector2(72f, 72f);

        Image image =
            root.GetComponent<Image>();

        image.color =
            color;

        return image;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size,
        Vector2 anchor)
    {
        GameObject root =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

        root.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            root.GetComponent<RectTransform>();

        rect.anchorMin =
            anchor;

        rect.anchorMax =
            anchor;

        rect.pivot =
            anchor;

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;

        Image image =
            root.GetComponent<Image>();

        image.color =
            new Color(
                0.12f,
                0.13f,
                0.16f,
                1f);

        TMP_Text text =
            CreateText(
                root.transform,
                "Label",
                label,
                24f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f));

        SetStretch(
            text.rectTransform);

        return root.GetComponent<Button>();
    }

    private static void CreateTitle(
        Transform parent,
        string title)
    {
        CreateText(
            parent,
            "Title",
            title,
            42f,
            TextAlignmentOptions.TopLeft,
            new Vector2(80f, -80f),
            new Vector2(1500f, 70f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        GameObject root =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

        root.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            root.GetComponent<RectTransform>();

        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.pivot =
            pivot;

        rect.anchoredPosition =
            anchoredPosition;

        rect.sizeDelta =
            size;

        TextMeshProUGUI text =
            root.GetComponent<TextMeshProUGUI>();

        text.text =
            value;

        text.fontSize =
            fontSize;

        text.alignment =
            alignment;

        text.color =
            Color.white;

        text.enableWordWrapping =
            true;

        return text;
    }

    private static void SetStretch(
        RectTransform rect)
    {
        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }

    private static void SetReference(
        SerializedObject serialized,
        string propertyName,
        Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            Debug.LogError(
                $"Could not find serialized property '{propertyName}' on {serialized.targetObject.name}.");

            return;
        }

        property.objectReferenceValue =
            value;
    }
}
