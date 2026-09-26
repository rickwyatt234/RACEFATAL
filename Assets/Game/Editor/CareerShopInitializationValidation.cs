using System;
using RaceFatal.Presentation.Career;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CareerShopInitializationValidation
{
    [MenuItem("RACE//FATAL/Career/Validate Inactive Shop Initialization")]
    public static void Run()
    {
        var root = new GameObject("InactiveShopTest", typeof(RectTransform));
        root.SetActive(false);
        try
        {
            var view = root.AddComponent<CareerShopView>();
            var textObject = new GameObject("ItemDetails", typeof(RectTransform));
            textObject.transform.SetParent(root.transform, false);
            var originalText = textObject.AddComponent<TextMeshProUGUI>();
            originalText.rectTransform.sizeDelta = new Vector2(500, 530);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("detailsText").objectReferenceValue = originalText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Match CareerController.Start: initialize a hidden tab before selecting it.
            view.Initialize(null, null);
            var scrolls = root.GetComponentsInChildren<ScrollRect>(true);
            Require(scrolls.Length == 1 && scrolls[0].content != null && scrolls[0].viewport != null,
                "Hidden Shop must build a complete scroll panel without throwing.");
            Require(!originalText.gameObject.activeSelf, "Original details label should be replaced.");
            Require(!root.activeSelf, "Initialization must not activate the hidden tab.");
            view.Initialize(null, null);
            Require(root.GetComponentsInChildren<ScrollRect>(true).Length == 1,
                "Repeated initialization must reuse the panel.");
            Debug.Log("Inactive Shop initialization validation passed.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
