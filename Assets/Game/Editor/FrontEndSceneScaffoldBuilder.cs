using System.Collections.Generic;
using RaceFatal.Presentation.FrontEnd;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FrontEndSceneScaffoldBuilder
{
    private const string ScenePath =
        "Assets/Game/Scenes/03_MainMenu.unity";

    [MenuItem(
        "RACE//FATAL/Front End/Build Main Menu Scaffold")]
    public static void BuildMainMenuScaffold()
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
            "FrontEndSystems");

        DestroyIfPresent(
            "FrontEndCanvas");

        DestroyIfPresent(
            "EventSystem");

        CreateEventSystem();

        Canvas canvas =
            CreateCanvas();

        GameObject systems =
            new GameObject(
                "FrontEndSystems");

        FrontEndController frontEnd =
            systems.AddComponent<
                FrontEndController>();

        CampaignSelectController
            campaignSelect =
                systems.AddComponent<
                    CampaignSelectController>();

        GameObject bootRoot =
            CreatePanel(
                canvas.transform,
                "BootRoot");

        CreateCenteredText(
            bootRoot.transform,
            "RACE//FATAL\nSYSTEM BOOT",
            56f);

        GameObject pressRoot =
            CreatePanel(
                canvas.transform,
                "PressAnyInputRoot");

        CreateCenteredText(
            pressRoot.transform,
            "RACE//FATAL\n\nPRESS ANY INPUT",
            48f);

        GameObject mainRoot =
            CreatePanel(
                canvas.transform,
                "MainMenuRoot");

        CreateTitle(
            mainRoot.transform,
            "RACE//FATAL");

        Button raceFatalButton =
            CreateMenuButton(
                mainRoot.transform,
                "RACE//FATAL",
                0);

        Button optionsButton =
            CreateMenuButton(
                mainRoot.transform,
                "OPTIONS",
                1);

        Button creditsButton =
            CreateMenuButton(
                mainRoot.transform,
                "CREDITS",
                2);

        Button disconnectButton =
            CreateMenuButton(
                mainRoot.transform,
                "DISCONNECT",
                3);

        GameObject campaignRoot =
            CreatePanel(
                canvas.transform,
                "CampaignSelectRoot");

        CreateTitle(
            campaignRoot.transform,
            "CAMPAIGN SELECT");

        var slotViews =
            new List<SaveSlotView>();

        for (int i = 0;
             i < 3;
             i++)
        {
            slotViews.Add(
                CreateSaveSlot(
                    campaignRoot.transform,
                    i + 1,
                    i));
        }

        TMP_Text campaignStatus =
            CreateText(
                campaignRoot.transform,
                "CampaignStatus",
                string.Empty,
                22f,
                TextAlignmentOptions.Left,
                new Vector2(80f, 82f),
                new Vector2(1500f, 50f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f));

        Button campaignBackButton =
            CreateBackButton(
                campaignRoot.transform);

        GameObject teamRoot =
            CreatePanel(
                canvas.transform,
                "TeamCreationRoot");

        CreateTitle(
            teamRoot.transform,
            "NEW CAMPAIGN // TEAM CREATION");

        CreateText(
            teamRoot.transform,
            "TeamCreationPlaceholder",
            "Team creation UI is reserved for the next implementation step.\nThe selected save slot is already tracked by FrontEndController.",
            28f,
            TextAlignmentOptions.TopLeft,
            new Vector2(80f, -240f),
            new Vector2(1300f, 220f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        Button teamBackButton =
            CreateBackButton(
                teamRoot.transform);

        GameObject racerRoot =
            CreatePlaceholderPanel(
                canvas.transform,
                "RacerCreationRoot",
                "RACER CREATION");

        GameObject reviewRoot =
            CreatePlaceholderPanel(
                canvas.transform,
                "CampaignReviewRoot",
                "CAMPAIGN REVIEW");

        GameObject optionsRoot =
            CreatePanel(
                canvas.transform,
                "OptionsRoot");

        CreateTitle(
            optionsRoot.transform,
            "OPTIONS");

        CreateText(
            optionsRoot.transform,
            "OptionsPlaceholder",
            "Options presentation will be connected here.",
            28f,
            TextAlignmentOptions.TopLeft,
            new Vector2(80f, -240f),
            new Vector2(1300f, 160f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        Button optionsBackButton =
            CreateBackButton(
                optionsRoot.transform);

        GameObject creditsRoot =
            CreatePanel(
                canvas.transform,
                "CreditsRoot");

        CreateTitle(
            creditsRoot.transform,
            "CREDITS");

        CreateText(
            creditsRoot.transform,
            "CreditsPlaceholder",
            "RACE//FATAL",
            32f,
            TextAlignmentOptions.TopLeft,
            new Vector2(80f, -240f),
            new Vector2(1300f, 160f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        Button creditsBackButton =
            CreateBackButton(
                creditsRoot.transform);

        TMP_Text errorText =
            CreateText(
                canvas.transform,
                "FrontEndErrorText",
                string.Empty,
                22f,
                TextAlignmentOptions.Left,
                new Vector2(80f, 28f),
                new Vector2(1760f, 44f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f));

        ConfigureCampaignSelect(
            campaignSelect,
            slotViews,
            campaignStatus);

        ConfigureFrontEnd(
            frontEnd,
            bootRoot,
            pressRoot,
            mainRoot,
            campaignRoot,
            teamRoot,
            racerRoot,
            reviewRoot,
            optionsRoot,
            creditsRoot,
            raceFatalButton,
            optionsButton,
            creditsButton,
            disconnectButton,
            campaignBackButton,
            teamBackButton,
            optionsBackButton,
            creditsBackButton,
            campaignSelect,
            errorText);

        pressRoot.SetActive(false);
        mainRoot.SetActive(false);
        campaignRoot.SetActive(false);
        teamRoot.SetActive(false);
        racerRoot.SetActive(false);
        reviewRoot.SetActive(false);
        optionsRoot.SetActive(false);
        creditsRoot.SetActive(false);

        EditorSceneManager.MarkSceneDirty(
            scene);

        EditorSceneManager.SaveScene(
            scene);

        Selection.activeGameObject =
            systems;

        Debug.Log(
            "RACE//FATAL front-end scaffold built in 03_MainMenu.");
    }

    private static void ConfigureCampaignSelect(
        CampaignSelectController controller,
        List<SaveSlotView> slots,
        TMP_Text statusText)
    {
        SerializedObject serialized =
            new SerializedObject(
                controller);

        SerializedProperty slotProperty =
            serialized.FindProperty(
                "slotViews");

        slotProperty.arraySize =
            slots.Count;

        for (int i = 0;
             i < slots.Count;
             i++)
        {
            slotProperty
                .GetArrayElementAtIndex(i)
                .objectReferenceValue =
                    slots[i];
        }

        serialized
            .FindProperty("statusText")
            .objectReferenceValue =
                statusText;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureFrontEnd(
        FrontEndController controller,
        GameObject bootRoot,
        GameObject pressRoot,
        GameObject mainRoot,
        GameObject campaignRoot,
        GameObject teamRoot,
        GameObject racerRoot,
        GameObject reviewRoot,
        GameObject optionsRoot,
        GameObject creditsRoot,
        Button raceFatalButton,
        Button optionsButton,
        Button creditsButton,
        Button disconnectButton,
        Button campaignBackButton,
        Button teamBackButton,
        Button optionsBackButton,
        Button creditsBackButton,
        CampaignSelectController campaignSelect,
        TMP_Text errorText)
    {
        SerializedObject serialized =
            new SerializedObject(
                controller);

        SetReference(serialized, "bootRoot", bootRoot);
        SetReference(serialized, "pressAnyInputRoot", pressRoot);
        SetReference(serialized, "mainMenuRoot", mainRoot);
        SetReference(serialized, "campaignSelectRoot", campaignRoot);
        SetReference(serialized, "teamCreationRoot", teamRoot);
        SetReference(serialized, "racerCreationRoot", racerRoot);
        SetReference(serialized, "campaignReviewRoot", reviewRoot);
        SetReference(serialized, "optionsRoot", optionsRoot);
        SetReference(serialized, "creditsRoot", creditsRoot);

        SetReference(serialized, "raceFatalButton", raceFatalButton);
        SetReference(serialized, "optionsButton", optionsButton);
        SetReference(serialized, "creditsButton", creditsButton);
        SetReference(serialized, "disconnectButton", disconnectButton);

        SetReference(serialized, "campaignBackButton", campaignBackButton);
        SetReference(serialized, "teamCreationBackButton", teamBackButton);
        SetReference(serialized, "optionsBackButton", optionsBackButton);
        SetReference(serialized, "creditsBackButton", creditsBackButton);

        SetReference(
            serialized,
            "campaignSelectController",
            campaignSelect);

        SetReference(
            serialized,
            "errorText",
            errorText);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SaveSlotView CreateSaveSlot(
        Transform parent,
        int slotIndex,
        int row)
    {
        GameObject root =
            new GameObject(
                $"SaveSlot_{slotIndex:00}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(SaveSlotView));

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
            new Vector2(
                80f,
                -210f - row * 225f);

        rect.sizeDelta =
            new Vector2(
                1500f,
                185f);

        Image image =
            root.GetComponent<Image>();

        image.color =
            new Color(
                0.08f,
                0.09f,
                0.11f,
                0.96f);

        TMP_Text slotLabel =
            CreateText(
                root.transform,
                "SlotLabel",
                $"SAVE {slotIndex:00}",
                24f,
                TextAlignmentOptions.TopLeft,
                new Vector2(24f, -18f),
                new Vector2(160f, 42f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

        TMP_Text teamName =
            CreateText(
                root.transform,
                "TeamName",
                "EMPTY SLOT",
                32f,
                TextAlignmentOptions.TopLeft,
                new Vector2(190f, -18f),
                new Vector2(560f, 48f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

        TMP_Text racerName =
            CreateText(
                root.transform,
                "RacerName",
                "CREATE NEW CAMPAIGN",
                23f,
                TextAlignmentOptions.TopLeft,
                new Vector2(190f, -70f),
                new Vector2(560f, 40f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

        TMP_Text resources =
            CreateText(
                root.transform,
                "Resources",
                string.Empty,
                21f,
                TextAlignmentOptions.TopLeft,
                new Vector2(780f, -24f),
                new Vector2(650f, 42f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

        TMP_Text lastSaved =
            CreateText(
                root.transform,
                "LastSaved",
                string.Empty,
                18f,
                TextAlignmentOptions.TopLeft,
                new Vector2(780f, -72f),
                new Vector2(650f, 36f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

        TMP_Text status =
            CreateText(
                root.transform,
                "Status",
                "AVAILABLE",
                18f,
                TextAlignmentOptions.BottomRight,
                new Vector2(-24f, 18f),
                new Vector2(460f, 36f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f));

        SaveSlotView view =
            root.GetComponent<SaveSlotView>();

        SerializedObject serialized =
            new SerializedObject(
                view);

        serialized
            .FindProperty("slotIndex")
            .intValue =
                slotIndex;

        SetReference(
            serialized,
            "selectButton",
            root.GetComponent<Button>());

        SetReference(serialized, "slotLabel", slotLabel);
        SetReference(serialized, "teamNameText", teamName);
        SetReference(serialized, "racerNameText", racerName);
        SetReference(serialized, "resourcesText", resources);
        SetReference(serialized, "lastSavedText", lastSaved);
        SetReference(serialized, "statusText", status);

        serialized.ApplyModifiedPropertiesWithoutUndo();

        return view;
    }

    private static GameObject CreatePlaceholderPanel(
        Transform parent,
        string name,
        string title)
    {
        GameObject root =
            CreatePanel(
                parent,
                name);

        CreateTitle(
            root.transform,
            title);

        return root;
    }

    private static GameObject CreatePanel(
        Transform parent,
        string name)
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

        Image image =
            root.GetComponent<Image>();

        image.color =
            new Color(
                0.025f,
                0.03f,
                0.04f,
                0.98f);

        return root;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject =
            new GameObject(
                "FrontEndCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(
                1920f,
                1080f);

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

    private static void CreateTitle(
        Transform parent,
        string text)
    {
        CreateText(
            parent,
            "Title",
            text,
            54f,
            TextAlignmentOptions.TopLeft,
            new Vector2(80f, -70f),
            new Vector2(1300f, 90f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));
    }

    private static void CreateCenteredText(
        Transform parent,
        string text,
        float fontSize)
    {
        CreateText(
            parent,
            "CenteredText",
            text,
            fontSize,
            TextAlignmentOptions.Center,
            Vector2.zero,
            new Vector2(1200f, 400f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));
    }

    private static Button CreateMenuButton(
        Transform parent,
        string label,
        int index)
    {
        return CreateButton(
            parent,
            label,
            new Vector2(
                80f,
                -235f - index * 92f),
            new Vector2(
                430f,
                70f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));
    }

    private static Button CreateBackButton(
        Transform parent)
    {
        return CreateButton(
            parent,
            "BACK",
            new Vector2(80f, 70f),
            new Vector2(240f, 60f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f));
    }

    private static Button CreateButton(
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        GameObject root =
            new GameObject(
                label + "Button",
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
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.pivot =
            pivot;

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;

        Image image =
            root.GetComponent<Image>();

        image.color =
            new Color(
                0.1f,
                0.12f,
                0.15f,
                1f);

        TMP_Text text =
            CreateText(
                root.transform,
                "Label",
                label,
                25f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f));

        RectTransform textRect =
            text.rectTransform;

        textRect.offsetMin =
            Vector2.zero;

        textRect.offsetMax =
            Vector2.zero;

        return root.GetComponent<Button>();
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 position,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        GameObject textObject =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

        textObject.transform.SetParent(
            parent,
            false);

        TextMeshProUGUI text =
            textObject.GetComponent<
                TextMeshProUGUI>();

        text.text =
            value;

        text.fontSize =
            fontSize;

        text.alignment =
            alignment;

        text.raycastTarget =
            false;

        RectTransform rect =
            text.rectTransform;

        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.pivot =
            pivot;

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;

        return text;
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
        string objectName)
    {
        GameObject existing =
            GameObject.Find(
                objectName);

        if (existing != null)
        {
            Object.DestroyImmediate(
                existing);
        }
    }
}
