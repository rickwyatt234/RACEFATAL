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
                "CareerSystems");

        GameObject canvas =
            GameObject.Find(
                "CareerCanvas");

        Transform shopRoot =
            canvas != null
                ? canvas.transform.Find(
                    "ShopRoot")
                : null;

        CareerController career =
            systems != null
                ? systems.GetComponent<
                    CareerController>()
                : null;

        if (career == null ||
            shopRoot == null)
        {
            EditorUtility.DisplayDialog(
                "RACE//FATAL",
                "CareerSystems or ShopRoot is missing. " +
                "Build the Career Hub scaffold before upgrading the Shop.",
                "OK");

            return;
        }

        DestroyChild(
            shopRoot,
            "ShopUI");

        DestroyChild(
            shopRoot,
            "Placeholder");

        CareerShopView view =
            shopRoot.GetComponent<
                CareerShopView>();

        if (view == null)
        {
            view =
                shopRoot.gameObject
                    .AddComponent<
                        CareerShopView>();
        }

        RectTransform ui =
            CreateRect(
                shopRoot,
                "ShopUI",
                Vector2.zero,
                Vector2.zero,
                true);

        TMP_Text credits =
            CreateText(
                ui,
                "Credits",
                "CREDITS  0",
                30f,
                new Vector2(410f, -145f),
                new Vector2(520f, 48f));

        Button bikesButton =
            CreateButton(
                ui,
                "BikesCategory",
                "BIKES",
                new Vector2(410f, -215f),
                new Vector2(240f, 58f));

        Button enginesButton =
            CreateButton(
                ui,
                "EnginesCategory",
                "ENGINES",
                new Vector2(665f, -215f),
                new Vector2(240f, 58f));

        Button chassisButton =
            CreateButton(
                ui,
                "ChassisCategory",
                "CHASSIS",
                new Vector2(920f, -215f),
                new Vector2(240f, 58f));

        Button equipmentButton =
            CreateButton(
                ui,
                "EquipmentCategory",
                "EQUIPMENT",
                new Vector2(1175f, -215f),
                new Vector2(290f, 58f));

        TMP_Text categoryText =
            CreateText(
                ui,
                "CategoryHeading",
                "ENGINES",
                28f,
                new Vector2(410f, -305f),
                new Vector2(560f, 44f));

        RectTransform itemList =
            CreateScroller(
                ui,
                "ShopItems",
                new Vector2(410f, -365f),
                new Vector2(600f, 505f));

        TMP_Text details =
            CreateText(
                ui,
                "ItemDetails",
                "SELECT AN ITEM.",
                25f,
                new Vector2(1050f, -365f),
                new Vector2(715f, 290f));

        TMP_Text ownership =
            CreateText(
                ui,
                "Ownership",
                "OWNED INSTANCES  0",
                24f,
                new Vector2(1050f, -675f),
                new Vector2(715f, 48f));

        Button purchase =
            CreateButton(
                ui,
                "Purchase",
                "PURCHASE",
                new Vector2(1050f, -760f),
                new Vector2(715f, 72f));

        TMP_Text purchaseText =
            purchase.GetComponentInChildren<
                TMP_Text>();

        TMP_Text feedback =
            CreateText(
                ui,
                "ShopFeedback",
                "SELECT AN ITEM TO VIEW PURCHASE REQUIREMENTS.",
                23f,
                new Vector2(410f, -915f),
                new Vector2(1355f, 70f));

        GameObject templateRoot =
            new GameObject(
                "ShopTemplates",
                typeof(RectTransform));

        templateRoot.transform.SetParent(
            ui,
            false);

        GameObject template =
            new GameObject(
                "ShopItemTemplate",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement),
                typeof(CareerShopItemView));

        template.transform.SetParent(
            templateRoot.transform,
            false);

        RectTransform templateRect =
            template.GetComponent<
                RectTransform>();

        templateRect.sizeDelta =
            new Vector2(
                0f,
                78f);

        Image background =
            template.GetComponent<
                Image>();

        background.color =
            new Color(
                0.10f,
                0.16f,
                0.20f,
                1f);

        Button templateButton =
            template.GetComponent<
                Button>();

        templateButton.targetGraphic =
            background;

        LayoutElement layout =
            template.GetComponent<
                LayoutElement>();

        layout.preferredHeight =
            80f;

        layout.minHeight =
            80f;

        TMP_Text optionLabel =
            CreateStretchText(
                template.transform,
                "OptionLabel",
                20f);

        CareerShopItemView option =
            template.GetComponent<
                CareerShopItemView>();

        SerializedObject optionSerialized =
            new SerializedObject(
                option);

        SetReference(
            optionSerialized,
            "button",
            templateButton);

        SetReference(
            optionSerialized,
            "labelText",
            optionLabel);

        SetReference(
            optionSerialized,
            "background",
            background);

        optionSerialized
            .ApplyModifiedPropertiesWithoutUndo();

        templateRoot.SetActive(
            false);

        SerializedObject viewSerialized =
            new SerializedObject(
                view);

        SetReference(
            viewSerialized,
            "bikesButton",
            bikesButton);

        SetReference(
            viewSerialized,
            "enginesButton",
            enginesButton);

        SetReference(
            viewSerialized,
            "chassisButton",
            chassisButton);

        SetReference(
            viewSerialized,
            "equipmentButton",
            equipmentButton);

        SetReference(
            viewSerialized,
            "itemList",
            itemList);

        SetReference(
            viewSerialized,
            "itemTemplate",
            option);

        SetReference(
            viewSerialized,
            "creditsText",
            credits);

        SetReference(
            viewSerialized,
            "categoryText",
            categoryText);

        SetReference(
            viewSerialized,
            "detailsText",
            details);

        SetReference(
            viewSerialized,
            "ownershipText",
            ownership);

        SetReference(
            viewSerialized,
            "feedbackText",
            feedback);

        SetReference(
            viewSerialized,
            "purchaseButton",
            purchase);

        SetReference(
            viewSerialized,
            "purchaseButtonText",
            purchaseText);

        viewSerialized
            .ApplyModifiedPropertiesWithoutUndo();

        SerializedObject careerSerialized =
            new SerializedObject(
                career);

        SetReference(
            careerSerialized,
            "shopView",
            view);

        careerSerialized
            .ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(
            scene);

        EditorSceneManager.SaveScene(
            scene);

        Selection.activeGameObject =
            shopRoot.gameObject;

        Debug.Log(
            "RACE//FATAL Shop UI built in 01_Career. " +
            "Other career screens were preserved.");
    }

    private static RectTransform CreateScroller(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size)
    {
        RectTransform root =
            CreateRect(
                parent,
                name,
                position,
                size);

        Image background =
            root.gameObject
                .AddComponent<
                    Image>();

        background.color =
            new Color(
                0.055f,
                0.085f,
                0.11f,
                0.96f);

        ScrollRect scroll =
            root.gameObject
                .AddComponent<
                    ScrollRect>();

        scroll.horizontal =
            false;

        scroll.vertical =
            true;

        scroll.movementType =
            ScrollRect.MovementType.Clamped;

        scroll.scrollSensitivity =
            22f;

        RectTransform viewport =
            CreateRect(
                root,
                "Viewport",
                Vector2.zero,
                Vector2.zero,
                true);

        Image viewportImage =
            viewport.gameObject
                .AddComponent<
                    Image>();

        viewportImage.color =
            new Color(
                0f,
                0f,
                0f,
                0.01f);

        Mask mask =
            viewport.gameObject
                .AddComponent<
                    Mask>();

        mask.showMaskGraphic =
            false;

        RectTransform content =
            CreateRect(
                viewport,
                "Content",
                Vector2.zero,
                Vector2.zero,
                true);

        content.anchorMin =
            new Vector2(
                0f,
                1f);

        content.anchorMax =
            new Vector2(
                1f,
                1f);

        content.pivot =
            new Vector2(
                0.5f,
                1f);

        content.anchoredPosition =
            Vector2.zero;

        VerticalLayoutGroup listLayout =
            content.gameObject
                .AddComponent<
                    VerticalLayoutGroup>();

        listLayout.spacing =
            6f;

        listLayout.padding =
            new RectOffset(
                8,
                8,
                8,
                8);

        listLayout.childAlignment =
            TextAnchor.UpperCenter;

        listLayout.childControlHeight =
            true;

        listLayout.childControlWidth =
            true;

        listLayout.childForceExpandHeight =
            false;

        listLayout.childForceExpandWidth =
            true;

        ContentSizeFitter fitter =
            content.gameObject
                .AddComponent<
                    ContentSizeFitter>();

        fitter.verticalFit =
            ContentSizeFitter
                .FitMode
                .PreferredSize;

        scroll.viewport =
            viewport;

        scroll.content =
            content;

        return content;
    }

    private static RectTransform CreateRect(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        bool stretch = false)
    {
        GameObject obj =
            new GameObject(
                name,
                typeof(RectTransform));

        obj.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            obj.GetComponent<
                RectTransform>();

        if (stretch)
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
        else
        {
            rect.anchorMin =
                new Vector2(
                    0f,
                    1f);

            rect.anchorMax =
                new Vector2(
                    0f,
                    1f);

            rect.pivot =
                new Vector2(
                    0f,
                    1f);

            rect.anchoredPosition =
                position;

            rect.sizeDelta =
                size;
        }

        return rect;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float size,
        Vector2 position,
        Vector2 dimensions)
    {
        RectTransform rect =
            CreateRect(
                parent,
                name,
                position,
                dimensions);

        TextMeshProUGUI label =
            rect.gameObject
                .AddComponent<
                    TextMeshProUGUI>();

        label.text =
            value;

        label.fontSize =
            size;

        label.alignment =
            TextAlignmentOptions.TopLeft;

        label.color =
            new Color(
                0.84f,
                0.95f,
                0.97f,
                1f);

        return label;
    }

    private static TMP_Text CreateStretchText(
        Transform parent,
        string name,
        float size)
    {
        RectTransform rect =
            CreateRect(
                parent,
                name,
                Vector2.zero,
                Vector2.zero,
                true);

        rect.offsetMin =
            new Vector2(
                12f,
                4f);

        rect.offsetMax =
            new Vector2(
                -12f,
                -4f);

        TextMeshProUGUI label =
            rect.gameObject
                .AddComponent<
                    TextMeshProUGUI>();

        label.text =
            string.Empty;

        label.fontSize =
            size;

        label.alignment =
            TextAlignmentOptions.MidlineLeft;

        label.color =
            Color.white;

        return label;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 dimensions)
    {
        RectTransform rect =
            CreateRect(
                parent,
                name,
                position,
                dimensions);

        Image image =
            rect.gameObject
                .AddComponent<
                    Image>();

        image.color =
            new Color(
                0.11f,
                0.26f,
                0.29f,
                1f);

        Button button =
            rect.gameObject
                .AddComponent<
                    Button>();

        button.targetGraphic =
            image;

        TMP_Text text =
            CreateStretchText(
                rect,
                "Label",
                22f);

        text.text =
            label;

        text.alignment =
            TextAlignmentOptions.Center;

        return button;
    }

    private static void SetReference(
        SerializedObject obj,
        string propertyName,
        Object value)
    {
        SerializedProperty property =
            obj.FindProperty(
                propertyName);

        if (property != null)
        {
            property.objectReferenceValue =
                value;
        }
    }

    private static void DestroyChild(
        Transform parent,
        string name)
    {
        Transform child =
            parent.Find(
                name);

        if (child != null)
        {
            Object.DestroyImmediate(
                child.gameObject);
        }
    }
}
