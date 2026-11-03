using RaceFatal.Presentation.Career;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CareerBrandsScaffoldBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/01_Career.unity";
    [MenuItem("RACE//FATAL/Career/Build Brands UI")]
    public static void Build() => BuildScene(false);
    [MenuItem("RACE//FATAL/Career/Rebuild Brands UI (Reset Layout)")]
    public static void Rebuild() => BuildScene(true);
    private static void BuildScene(bool reset)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Exit Play Mode before building Brands UI."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var canvas = GameObject.Find("CareerCanvas");
        var systems = GameObject.Find("CareerSystems");
        var root = canvas != null ? canvas.transform.Find("BrandsRoot") : null;
        var controller = systems != null ? systems.GetComponent<CareerController>() : null;
        if (root == null || controller == null) { Debug.LogError("Build the Career Hub first. BrandsRoot and CareerSystems are required."); return; }
        var view = root.GetComponent<CareerBrandsView>();
        if (view != null && view.HasAuthoredLayout && !reset)
        {
            Wire(controller, view); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Existing Brands layout preserved. Edit BrandsUI in the Hierarchy. Use Rebuild Brands UI only to reset it."); return;
        }
        if (!reset && root.Find("BrandsUI") != null)
        {
            Selection.activeGameObject = root.gameObject;
            Debug.LogError("An existing BrandsUI hierarchy has incomplete bindings. Repair the view references or explicitly use Rebuild Brands UI (Reset Layout)."); return;
        }
        if (reset && !EditorUtility.DisplayDialog("Reset Brands UI?", "This replaces the generated BrandsUI hierarchy and its visual customizations. Campaign data is unaffected.", "Reset Layout", "Cancel")) return;
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Build Brands UI");
        var previous = root.Find("BrandsUI"); if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
        var placeholder = root.Find("Placeholder"); if (placeholder != null) Undo.DestroyObjectImmediate(placeholder.gameObject);
        if (view == null) view = Undo.AddComponent<CareerBrandsView>(root.gameObject);
        BrandUiLayout.Build(root, view); Wire(controller, view);
        EditorUtility.SetDirty(view); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("Brands UI saved in 01_Career. All pages, rows, search, confirmations and reveal controls are editable before Play Mode. Start from Bootstrap to bind campaign data.");
    }
    private static void Wire(CareerController controller, CareerBrandsView view)
    {
        var serialized = new SerializedObject(controller); serialized.FindProperty("brandsView").objectReferenceValue = view;
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(controller);
    }
}

[CustomEditor(typeof(CareerBrandsView))]
public sealed class CareerBrandsViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var view = (CareerBrandsView)target;
        if (!view.HasAuthoredLayout) { EditorGUILayout.HelpBox("Run RACE//FATAL > Career > Build Brands UI to author this screen.", MessageType.Info); return; }
        EditorGUILayout.LabelField("Preview authored page", EditorStyles.boldLabel);
        string[] names = { "Overview", "Directory", "Contracts", "Brand Boxes", "RF Association", "Confirmation", "Box Reveal" };
        for (int i = 0; i < names.Length; i++) if (GUILayout.Button(names[i]))
        {
            Undo.RegisterFullObjectHierarchyUndo(view.gameObject, "Preview Brands page");
            view.gameObject.SetActive(true); view.PreviewEditorPage(i);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }
    }
}
