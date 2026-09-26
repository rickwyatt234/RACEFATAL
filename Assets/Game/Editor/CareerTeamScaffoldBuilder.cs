using RaceFatal.Career;
using RaceFatal.Presentation.Career;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CareerTeamScaffoldBuilder
{
    [MenuItem("RACE//FATAL/Career/Build Team UI")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before building the Team UI.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/01_Career.unity", OpenSceneMode.Single);
        var canvas = GameObject.Find("CareerCanvas");
        var systems = GameObject.Find("CareerSystems");
        var root = canvas != null ? canvas.transform.Find("TeamRoot") : null;
        var controller = systems != null ? systems.GetComponent<CareerController>() : null;
        if (root == null || controller == null)
        {
            EditorUtility.DisplayDialog("RACE//FATAL", "Build the Career Hub scaffold first; TeamRoot or CareerSystems is missing.", "OK");
            return;
        }
        foreach (string childName in new[] { "Placeholder", "TeamUI" })
        {
            var child = root.Find(childName);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }
        var view = root.GetComponent<CareerTeamView>();
        if (view == null) view = root.gameObject.AddComponent<CareerTeamView>();
        var ui = Rect(root, "TeamUI", Vector2.zero, Vector2.zero, true);
        Text(ui, "IdentityHeading", "TEAM IDENTITY", 28, new Vector2(410, -165), new Vector2(410, 44));
        Text(ui, "OverviewHeading", "TEAM OPERATIONS", 28, new Vector2(870, -165), new Vector2(440, 44));
        Text(ui, "ProgressHeading", "TEAM PROGRESSION", 28, new Vector2(1350, -165), new Vector2(450, 44));
        var name = Input(ui, "TeamName", "TEAM NAME", new Vector2(410, -265), 410, TeamManagementService.MaximumNameLength);
        var primary = Input(ui, "PrimaryColor", "PRIMARY COLOR (#RRGGBB)", new Vector2(410, -390), 330, 9);
        var secondary = Input(ui, "SecondaryColor", "SECONDARY COLOR (#RRGGBB)", new Vector2(410, -515), 330, 9);
        var primaryPreview = Rect(ui, "PrimaryPreview", new Vector2(756, -390), new Vector2(64, 64)).gameObject.AddComponent<Image>();
        var secondaryPreview = Rect(ui, "SecondaryPreview", new Vector2(756, -515), new Vector2(64, 64)).gameObject.AddComponent<Image>();
        var repaintRoot = Rect(ui, "RepaintOwnedBikes", new Vector2(410, -610), new Vector2(410, 48));
        var repaint = repaintRoot.gameObject.AddComponent<Toggle>();
        var box = Rect(repaintRoot, "Box", Vector2.zero, new Vector2(34, 34)).gameObject.AddComponent<Image>();
        box.color = new Color(.12f, .20f, .25f);
        var check = Rect(box.transform, "Checkmark", new Vector2(6, -6), new Vector2(22, 22)).gameObject.AddComponent<Image>();
        check.color = new Color(.2f, 1f, .8f);
        repaint.targetGraphic = box;
        repaint.graphic = check;
        repaint.isOn = false;
        Text(repaintRoot, "Label", "APPLY COLORS TO ALL\nOWNED BIKES", 19, new Vector2(48, 2), new Vector2(362, 48));
        var apply = Button(ui, "ApplyIdentity", "APPLY & SAVE", new Vector2(410, -690), new Vector2(410, 62));
        var revert = Button(ui, "RevertIdentity", "RESET FIELDS", new Vector2(410, -770), new Vector2(410, 62));
        var save = Button(ui, "SaveCampaign", "SAVE CAMPAIGN", new Vector2(410, -850), new Vector2(410, 62));
        var overview = ScrollText(ui, "Overview", new Vector2(870, -225), new Vector2(440, 680));
        var progress = ScrollText(ui, "Progression", new Vector2(1350, -225), new Vector2(450, 680));
        var roster = Button(ui, "OpenRoster", "MANAGE ROSTER", new Vector2(410, -945), new Vector2(410, 62));
        var garage = Button(ui, "OpenGarage", "OPEN GARAGE", new Vector2(870, -945), new Vector2(440, 62));
        var research = Button(ui, "OpenResearch", "MANAGE RESEARCH", new Vector2(1350, -945), new Vector2(450, 62));
        var feedback = Text(ui, "Feedback", "EDIT TEAM IDENTITY, THEN APPLY & SAVE.", 20, new Vector2(410, -1020), new Vector2(1390, 55));

        var serialized = new SerializedObject(view);
        Reference(serialized, "nameInput", name);
        Reference(serialized, "primaryInput", primary);
        Reference(serialized, "secondaryInput", secondary);
        Reference(serialized, "primaryPreview", primaryPreview);
        Reference(serialized, "secondaryPreview", secondaryPreview);
        Reference(serialized, "repaintToggle", repaint);
        Reference(serialized, "overviewText", overview);
        Reference(serialized, "progressionText", progress);
        Reference(serialized, "feedbackText", feedback);
        Reference(serialized, "applyButton", apply);
        Reference(serialized, "revertButton", revert);
        Reference(serialized, "saveButton", save);
        Reference(serialized, "rosterButton", roster);
        Reference(serialized, "garageButton", garage);
        Reference(serialized, "researchButton", research);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var controllerData = new SerializedObject(controller);
        Reference(controllerData, "teamView", view);
        controllerData.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("Team UI built in 01_Career. Start from 00_Bootstrap and load a campaign.");
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size, bool stretch = false)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = stretch ? Vector2.zero : new Vector2(0, 1);
        rect.anchorMax = stretch ? Vector2.one : new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        if (stretch) { rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
        else { rect.anchoredPosition = position; rect.sizeDelta = size; }
        return rect;
    }
    private static TMP_Text Text(Transform parent, string name, string value, float size, Vector2 position, Vector2 dimensions)
    {
        var label = Rect(parent, name, position, dimensions).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = size;
        label.color = new Color(.84f, .95f, .97f);
        label.richText = false;
        label.raycastTarget = false;
        return label;
    }
    private static Button Button(Transform parent, string name, string value, Vector2 position, Vector2 size)
    {
        var rect = Rect(parent, name, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.11f, .26f, .29f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var label = Text(rect, "Label", value, 23, Vector2.zero, size);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }
    private static TMP_InputField Input(Transform parent, string name, string label, Vector2 position, float width, int limit)
    {
        Text(parent, name + "Label", label, 21, position + new Vector2(0, 40), new Vector2(410, 34));
        var rect = Rect(parent, name, position, new Vector2(width, 64));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.055f, .085f, .11f);
        var input = rect.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        var viewport = Rect(rect, "TextArea", Vector2.zero, Vector2.zero, true);
        viewport.offsetMin = new Vector2(12, 8);
        viewport.offsetMax = new Vector2(-12, -8);
        viewport.gameObject.AddComponent<RectMask2D>();
        var text = Text(viewport, "Text", "", 23, Vector2.zero, Vector2.zero);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        input.textViewport = viewport;
        input.textComponent = text;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = limit;
        return input;
    }
    private static TMP_Text ScrollText(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var root = Rect(parent, name, position, size);
        root.gameObject.AddComponent<Image>().color = new Color(.055f, .085f, .11f);
        var scroll = root.gameObject.AddComponent<ScrollRect>();
        var viewport = Rect(root, "Viewport", Vector2.zero, Vector2.zero, true);
        viewport.offsetMin = new Vector2(14, 12);
        viewport.offsetMax = new Vector2(-14, -12);
        viewport.gameObject.AddComponent<RectMask2D>();
        var text = Text(viewport, "Content", "", 22, Vector2.zero, new Vector2(0, 0));
        text.rectTransform.anchorMin = new Vector2(0, 1);
        text.rectTransform.anchorMax = new Vector2(1, 1);
        text.rectTransform.pivot = new Vector2(0, 1);
        text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = text.rectTransform;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30;
        return text;
    }
    private static void Reference(SerializedObject data, string name, Object value)
    {
        var property = data.FindProperty(name);
        if (property == null) throw new System.InvalidOperationException("Missing serialized field: " + name);
        property.objectReferenceValue = value;
    }
}
