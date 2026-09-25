using RaceFatal.Presentation.Career;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CareerResearchScaffoldBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/01_Career.unity";

    [MenuItem("RACE//FATAL/Career/Build Research UI")]
    public static void BuildResearch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before building the Research UI.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject systems = GameObject.Find("CareerSystems");
        GameObject canvas = GameObject.Find("CareerCanvas");
        Transform researchRoot = canvas != null ? canvas.transform.Find("ResearchRoot") : null;
        CareerController career = systems != null ? systems.GetComponent<CareerController>() : null;
        if (career == null || researchRoot == null)
        {
            EditorUtility.DisplayDialog("RACE//FATAL",
                "CareerSystems or ResearchRoot is missing. Build the Career Hub scaffold first.", "OK");
            return;
        }

        // Rebuilding only replaces this tool's panels and the original placeholder.
        DestroyChild(researchRoot, "ResearchUI");
        DestroyChild(researchRoot, "ResearchConfirmation");
        DestroyChild(researchRoot, "Placeholder");
        CareerResearchView view = researchRoot.GetComponent<CareerResearchView>();
        if (view == null) view = researchRoot.gameObject.AddComponent<CareerResearchView>();
        RectTransform ui = CreateRect(researchRoot, "ResearchUI", Vector2.zero, Vector2.zero, true);
        CreateText(ui, "CategoriesHeading", "RESEARCH FIELDS", 28, new Vector2(410, -160), new Vector2(260, 42));
        CreateText(ui, "TechnologyHeading", "TECHNOLOGIES", 28, new Vector2(710, -160), new Vector2(500, 42));
        TMP_Text points = CreateText(ui, "ResearchPoints", "RP -- // STAFF --", 22, new Vector2(1290, -160), new Vector2(510, 42));
        RectTransform categories = CreateScroller(ui, "Categories", new Vector2(410, -225), new Vector2(290, 625));
        RectTransform items = CreateScroller(ui, "Technologies", new Vector2(730, -225), new Vector2(530, 625));
        RectTransform detailContent = CreateScroller(ui, "TechnologyDetails", new Vector2(1290, -225), new Vector2(500, 530));
        TMP_Text details = CreateText(detailContent, "DetailsText", "SELECT A TECHNOLOGY.", 23, Vector2.zero, new Vector2(480, 530));
        details.gameObject.AddComponent<LayoutElement>().minHeight = 510;
        Button research = CreateButton(ui, "Research", "RESEARCH TECHNOLOGY", new Vector2(1290, -785), new Vector2(500, 64));
        Button shop = CreateButton(ui, "OpenShop", "OPEN SHOP", new Vector2(1100, -875), new Vector2(330, 64));
        Button save = CreateButton(ui, "SaveResearch", "SAVE CAMPAIGN", new Vector2(1460, -875), new Vector2(330, 64));
        RectTransform treeRoot = CreateRect(ui, "TechTree", new Vector2(710, -225), new Vector2(550, 625));
        treeRoot.gameObject.AddComponent<Image>().color = new Color(.025f, .045f, .06f);
        treeRoot.gameObject.AddComponent<RectMask2D>();
        var viewport = treeRoot.gameObject.AddComponent<ResearchTreeViewport>();
        viewport.horizontal = true; viewport.vertical = true; viewport.movementType = ScrollRect.MovementType.Unrestricted;
        viewport.inertia = false; viewport.viewport = treeRoot;
        RectTransform treeContent = CreateRect(treeRoot, "TreeContent", Vector2.zero, new Vector2(1500, 1200));
        treeContent.anchorMin = treeContent.anchorMax = treeContent.pivot = new Vector2(.5f, .5f);
        viewport.content = treeContent;
        var tree = treeRoot.gameObject.AddComponent<CareerResearchTreeView>();
        var treeSerialized = new SerializedObject(tree);
        SetReference(treeSerialized, "viewport", viewport); SetReference(treeSerialized, "content", treeContent);
        treeSerialized.ApplyModifiedPropertiesWithoutUndo();
        Button recenter = CreateButton(ui, "RecenterTree", "CENTER", new Vector2(710, -865), new Vector2(220, 54));
        Button zoomIn = CreateButton(ui, "ZoomIn", "+", new Vector2(945, -865), new Vector2(65, 54));
        Button zoomOut = CreateButton(ui, "ZoomOut", "−", new Vector2(1020, -865), new Vector2(65, 54));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(recenter.onClick, tree.Recenter);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(zoomIn.onClick, tree.ZoomIn);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(zoomOut.onClick, tree.ZoomOut);
        TMP_Text feedback = CreateText(ui, "ResearchFeedback", "EARN RP // RESEARCH TECHNOLOGY // UNLOCK SHOP COMPONENTS", 23,
            new Vector2(410, -977), new Vector2(1380, 70));
        // The inactive template must not live under the list content.
        GameObject templateRoot =
            new GameObject("ResearchTemplates", typeof(RectTransform));
        templateRoot.transform.SetParent(ui, false);

        GameObject template = new GameObject(
            "ResearchOptionTemplate",
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

        RectTransform modal = CreateRect(researchRoot, "ResearchConfirmation", Vector2.zero, Vector2.zero, true);
        Image dimmer = modal.gameObject.AddComponent<Image>();
        dimmer.color = new Color(0, 0, 0, 0.92f);
        TMP_Text confirmation = CreateText(modal, "ConfirmationText", "", 28,
            new Vector2(620, -330), new Vector2(850, 280));
        Button confirm = CreateButton(modal, "ConfirmResearch", "CONFIRM RESEARCH", new Vector2(620, -650), new Vector2(400, 70));
        Button cancel = CreateButton(modal, "CancelResearch", "CANCEL", new Vector2(1050, -650), new Vector2(400, 70));
        modal.gameObject.SetActive(false);

        SerializedObject serialized = new SerializedObject(view);
        SetReference(serialized, "treeView", tree);
        SetReference(serialized, "staffListRoot", items.parent.parent.gameObject);
        SetReference(serialized, "categoryList", categories);
        SetReference(serialized, "itemList", items);
        SetReference(serialized, "optionTemplate", option);
        SetReference(serialized, "pointsText", points);
        SetReference(serialized, "detailsText", details);
        SetReference(serialized, "feedbackText", feedback);
        SetReference(serialized, "researchButton", research);
        SetReference(serialized, "saveButton", save);
        SetReference(serialized, "shopButton", shop);
        SetReference(serialized, "confirmationRoot", modal.gameObject);
        SetReference(serialized, "confirmationText", confirmation);
        SetReference(serialized, "confirmButton", confirm);
        SetReference(serialized, "cancelButton", cancel);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject controller = new SerializedObject(career);
        SetReference(controller, "researchView", view);
        controller.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = researchRoot.gameObject;
        Debug.Log("RACE//FATAL Research UI built in 01_Career. Load a campaign from 00_Bootstrap to research.");
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