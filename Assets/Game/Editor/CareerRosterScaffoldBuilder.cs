using RaceFatal.Presentation.Career;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CareerRosterScaffoldBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/01_Career.unity";

    [MenuItem("RACE//FATAL/Career/Build Roster UI")]
    public static void BuildRoster()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before building the Roster UI.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject systems = GameObject.Find("CareerSystems");
        GameObject canvas = GameObject.Find("CareerCanvas");
        Transform rosterRoot = canvas != null ? canvas.transform.Find("RosterRoot") : null;
        CareerController career = systems != null ? systems.GetComponent<CareerController>() : null;
        if (career == null || rosterRoot == null)
        {
            EditorUtility.DisplayDialog("RACE//FATAL", "CareerSystems or RosterRoot is missing. Build the Career Hub scaffold first.", "OK");
            return;
        }
        DestroyChild(rosterRoot, "RosterUI");
        DestroyChild(rosterRoot, "RosterConfirmation");
        DestroyChild(rosterRoot, "Placeholder");
        DestroyChild(rosterRoot, "RosterTemplates");
        CareerRosterView view = rosterRoot.GetComponent<CareerRosterView>();
        if (view == null) view = rosterRoot.gameObject.AddComponent<CareerRosterView>();
        RectTransform ui = CreateRect(rosterRoot, "RosterUI", Vector2.zero, Vector2.zero, true);
        CreateText(ui, "RosterHeading", "TEAM ROSTER", 28, new Vector2(410, -160), new Vector2(345, 42));
        CreateText(ui, "PerkHeading", "RACER PERKS", 28, new Vector2(810, -160), new Vector2(475, 42));
        CreateText(ui, "RecruitHeading", "RECRUITMENT", 28, new Vector2(1330, -160), new Vector2(450, 42));
        RectTransform racers = CreateScroller(ui, "TeamRacers", new Vector2(410, -225), new Vector2(355, 405));
        RectTransform perks = CreateScroller(ui, "Perks", new Vector2(810, -225), new Vector2(475, 405));
        RectTransform recruits = CreateScroller(ui, "Recruitment", new Vector2(1330, -225), new Vector2(450, 405));
        TMP_Text profile = CreateText(ui, "RacerProfile", "SELECT A RACER", 21, new Vector2(410, -648), new Vector2(1370, 160));
        TMP_Text funds = CreateText(ui, "RosterResources", "CREDITS -- // CHARACTER FAME --", 23, new Vector2(410, -835), new Vector2(1370, 38));
        Button assign = CreateButton(ui, "AssignPartner", "ASSIGN PARTNER", new Vector2(410, -885), new Vector2(310, 64));
        Button buy = CreateButton(ui, "BuyPerk", "BUY PERK", new Vector2(752, -885), new Vector2(310, 64));
        Button recruit = CreateButton(ui, "RecruitRacer", "RECRUIT RACER", new Vector2(1094, -885), new Vector2(310, 64));
        Button save = CreateButton(ui, "SaveRoster", "SAVE CAMPAIGN", new Vector2(1436, -885), new Vector2(344, 64));
        TMP_Text feedback = CreateText(ui, "RosterFeedback", "VIEW RACERS // ASSIGN PARTNER // BUY PERKS // RECRUIT", 22,
            new Vector2(410, -975), new Vector2(1370, 60));

        var templateRoot = new GameObject("RosterTemplates", typeof(RectTransform));
        templateRoot.transform.SetParent(rosterRoot, false);
        var template = new GameObject("RosterOptionTemplate", typeof(RectTransform), typeof(Image), typeof(Button),
            typeof(LayoutElement), typeof(CareerGarageOptionView));
        template.transform.SetParent(templateRoot.transform, false);
        template.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 68);
        Image background = template.GetComponent<Image>(); background.color = new Color(.10f, .16f, .20f, 1f);
        Button templateButton = template.GetComponent<Button>(); templateButton.targetGraphic = background;
        LayoutElement layout = template.GetComponent<LayoutElement>(); layout.preferredHeight = 72; layout.minHeight = 72;
        TMP_Text optionLabel = CreateStretchText(template.transform, "OptionLabel", 19f);
        var option = template.GetComponent<CareerGarageOptionView>();
        var optionSerialized = new SerializedObject(option);
        SetReference(optionSerialized, "button", templateButton);
        SetReference(optionSerialized, "labelText", optionLabel);
        SetReference(optionSerialized, "background", background);
        optionSerialized.ApplyModifiedPropertiesWithoutUndo();
        templateRoot.SetActive(false);

        RectTransform modal = CreateRect(rosterRoot, "RosterConfirmation", Vector2.zero, Vector2.zero, true);
        modal.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .92f);
        TMP_Text confirmation = CreateText(modal, "ConfirmationText", "", 27, new Vector2(620, -325), new Vector2(850, 300));
        Button confirm = CreateButton(modal, "ConfirmRoster", "CONFIRM", new Vector2(620, -650), new Vector2(400, 70));
        Button cancel = CreateButton(modal, "CancelRoster", "CANCEL", new Vector2(1050, -650), new Vector2(400, 70));
        modal.gameObject.SetActive(false);

        var serialized = new SerializedObject(view);
        SetReference(serialized, "racerList", racers);
        SetReference(serialized, "perkList", perks);
        SetReference(serialized, "recruitList", recruits);
        SetReference(serialized, "optionTemplate", option);
        SetReference(serialized, "profileText", profile);
        SetReference(serialized, "feedbackText", feedback);
        SetReference(serialized, "fundsText", funds);
        SetReference(serialized, "assignButton", assign);
        SetReference(serialized, "buyPerkButton", buy);
        SetReference(serialized, "recruitButton", recruit);
        SetReference(serialized, "saveButton", save);
        SetReference(serialized, "confirmationRoot", modal.gameObject);
        SetReference(serialized, "confirmationText", confirmation);
        SetReference(serialized, "confirmButton", confirm);
        SetReference(serialized, "cancelButton", cancel);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var controller = new SerializedObject(career);
        SetReference(controller, "rosterView", view);
        controller.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = rosterRoot.gameObject;
        Debug.Log("RACE//FATAL Roster UI built in 01_Career. Load a campaign from 00_Bootstrap.");
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
