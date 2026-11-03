using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using RaceFatal.Presentation.Career;
using RaceFatal.Presentation.FrontEnd;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tactics;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Presentation.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Object = UnityEngine.Object;

// Deliberately an inspector action: the only project toolbar commands are Brands UI.
public sealed class UiAuthoringMigration : ScriptableObject
{
    [SerializeField, HideInInspector] private int version;
    public bool IsComplete => version == 1;
    public const string AssetPath = "Assets/Game/Editor/UIAuthoring.asset";

    public void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        int saved = 0;
        try
        {
            // Only assets containing our presentation scripts, not vendor demos.
            var names = new[] { "CareerController", "CareerHomeView", "CareerShopView", "CareerGarageView", "CareerRosterView", "CareerRacesView", "TeamCreationController", "CampaignReviewController", "RaceRuntimeController", "RaceStandingsRowView", "RaceOutcomeController", "PlayerCockpitHUD", "PlayerCockpitTargetingHUD", "PlayerWeaponAim" };
            var guids = names.Select(n => AssetDatabase.AssetPathToGUID("Assets/Game/Scripts/Presentation/" + ScriptFolder(n) + "/" + n + ".cs")).Where(g => !string.IsNullOrEmpty(g)).ToArray();
            var prefabs = AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase) && ContainsScript(p,guids)).OrderBy(p => AssetDatabase.GetDependencies(p,true).Length).ToArray();
            foreach(var path in prefabs)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { Author(new[] {root}); PrefabUtility.SaveAsPrefabAsset(root,path); saved++; }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var scenes = AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath).Where(p => ContainsScript(p,guids)).ToArray();
            foreach(var path in scenes)
            {
                var scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                Author(scene.GetRootGameObjects());
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + path);
                saved++;
            }
            version=1; EditorUtility.SetDirty(this); AssetDatabase.SaveAssets();
            Debug.Log($"Authored UI in {saved} assets. Review and commit scene/prefab changes. Brands UI is unchanged; use its existing command separately.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
    private static string ScriptFolder(string name) => name.StartsWith("Career") ? "Career" : name == "TeamCreationController" || name == "CampaignReviewController" ? "FrontEnd" : name.StartsWith("Race") ? "Racing" : name == "PlayerWeaponAim" ? "Combat" : "Vehicles";
    private static bool ContainsScript(string path,string[] guids)
    {
        var text=File.ReadAllText(path);
        return guids.Any(g=>text.Contains("guid: " + g));
    }
    private static IEnumerable<T> All<T>(GameObject[] roots) where T:Component => roots.SelectMany(r=>r.GetComponentsInChildren<T>(true));
    private static T Reference<T>(Object owner,string field) where T:Object => (T)new SerializedObject(owner).FindProperty(field).objectReferenceValue;
    private static void SetReference(Object owner,string field,Object value)
    {
        var so=new SerializedObject(owner); so.FindProperty(field).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static T Add<T>(GameObject obj) where T:Component => obj.GetComponent<T>() ?? obj.AddComponent<T>();
    private static T Child<T>(Transform parent,string name,Vector2 position,Vector2 size) where T:Component
    {
        var existing=parent.Find(name);
        if(existing!=null) return Add<T>(existing.gameObject);
        return Add<T>(EditorUiFactory.Rect(parent,name,position,size).gameObject);
    }
    private static void Author(GameObject[] roots)
    {
        foreach(var home in All<CareerHomeView>(roots))
        {
            home.EditorAuthor();
            Child<CareerHomeCalendarView>(home.transform,"HomeCalendar",new Vector2(410,-385),new Vector2(1380,450)).EditorAuthor();
        }
        foreach(var career in All<CareerController>(roots))
        {
            var home=Reference<GameObject>(career,"homeRoot");
            if(home==null) throw new InvalidOperationException("CareerController has no Home root.");
            var canvas=home.transform.parent;
            Child<CareerSuccessionView>(home.transform,"CareerSuccession",new Vector2(410,-855),new Vector2(1380,170)).EditorAuthor(canvas);
            Child<CareerPreparationView>(canvas,"TeamPreparationController",Vector2.zero,Vector2.zero).EditorAuthor(canvas,
                Reference<GameObject>(career,"racesRoot"),Reference<GameObject>(career,"rosterRoot"),Reference<GameObject>(career,"garageRoot"));
        }
        foreach(var shop in All<CareerShopView>(roots)) { shop.EditorAuthor(); AuthorOptions(shop); }
        foreach(var garage in All<CareerGarageView>(roots)) AuthorOptions(garage);
        foreach(var roster in All<CareerRosterView>(roots)) AuthorOptions(roster);
        foreach(var races in All<CareerRacesView>(roots))
            AuthorPage(Reference<RectTransform>(races,"cardContainer"),Reference<CareerRaceCardView>(races,"cardTemplate"),3,190);
        foreach(var review in All<CampaignReviewController>(roots)) review.EditorAuthor();
        foreach(var team in All<TeamCreationController>(roots))
        {
            foreach(var field in new[]{"primaryColorPreview","secondaryColorPreview"})
            {
                var preview=Reference<Image>(team,field);
                if(preview==null) throw new InvalidOperationException("Missing team color preview.");
                Add<Button>(preview.gameObject).targetGraphic=preview; preview.raycastTarget=true;
                var canvas=preview.GetComponentInParent<Canvas>(true);
                Child<TeamColorPicker>(canvas.transform,"TeamColorPicker",Vector2.zero,Vector2.zero).EditorAuthor();
            }
        }
        foreach(var runtime in All<RaceRuntimeController>(roots))
        {
            var pause=Add<RacePauseController>(runtime.gameObject);
            var view=Child<RacePauseView>(runtime.transform,"RacePauseUI",Vector2.zero,Vector2.zero);
            var so=new SerializedObject(pause);
            view.EditorAuthor(pause,(KeyCode)so.FindProperty("pauseKey").intValue,(KeyCode)so.FindProperty("hideMenuKey").intValue);
            SetReference(pause,"view",view);
            Add<TacticalRaceController>(runtime.gameObject);
            Add<TacticalRaceUI>(runtime.gameObject).BuildUI();
            if(!All<EventSystem>(roots).Any())
            {
                var events=new GameObject("RaceEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
                events.transform.SetParent(runtime.transform,false);
            }
        }
        foreach(var row in All<RaceStandingsRowView>(roots)) row.EditorAuthor();
        foreach(var outcome in All<RaceOutcomeController>(roots)) outcome.EditorAuthor();
        foreach(var hud in All<PlayerCockpitHUD>(roots)) hud.EditorAuthor();
        foreach(var targeting in All<PlayerCockpitTargetingHUD>(roots)) targeting.EditorAuthor();
        foreach(var aim in All<PlayerWeaponAim>(roots))
        {
            var cockpit=aim.GetComponent<PlayerCockpitView>();
            if(cockpit?.ReticleCanvas!=null) aim.EditorAuthor(cockpit.ReticleCanvas);
            else throw new InvalidOperationException("Assign the cockpit reticle canvas before authoring UI: " + aim.name);
        }
        foreach(var component in All<MonoBehaviour>(roots))
            if(component!=null) { EditorUtility.SetDirty(component); if(PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
    }
    private static void AuthorOptions(Component view)
    {
        var template=Reference<CareerGarageOptionView>(view,"optionTemplate");
        var so=new SerializedObject(view); var property=so.GetIterator();
        while(property.NextVisible(true))
            if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceValue is RectTransform rect && property.name.EndsWith("List",StringComparison.Ordinal))
                AuthorPage(rect,template,6,88);
    }
    private static void AuthorPage(RectTransform container,Component template,int count,float height)
    {
        if(container==null || template==null) throw new InvalidOperationException("Missing list container or template.");
        if(container.GetComponent<AuthoredPage>()!=null) return;
        foreach(var layout in container.GetComponents<LayoutGroup>()) Object.DestroyImmediate(layout);
        foreach(var fitter in container.GetComponents<ContentSizeFitter>()) Object.DestroyImmediate(fitter);
        container.sizeDelta=new Vector2(container.sizeDelta.x,65+count*height);
        var previous=EditorUiFactory.Button(container,"<",Vector2.zero,new Vector2(45,42),()=>{});
        var next=EditorUiFactory.Button(container,">",new Vector2(50,0),new Vector2(45,42),()=>{});
        var label=EditorUiFactory.Text(container,"Page","",new Vector2(105,0),new Vector2(350,42),18);
        label.rectTransform.anchorMax=new Vector2(1,1);
        label.rectTransform.sizeDelta=new Vector2(-110,42);
        label.enableAutoSizing=true; label.fontSizeMin=12; label.fontSizeMax=18;
        var slots=new Component[count];
        for(int i=0;i<count;i++)
        {
            var copy=Object.Instantiate(template,container); copy.name="Row " + (i+1);
            var rect=(RectTransform)copy.transform;
            rect.anchorMin=new Vector2(0,1); rect.anchorMax=new Vector2(1,1); rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=new Vector2(0,-55-i*height); rect.sizeDelta=new Vector2(0,height-8);
            slots[i]=copy;
        }
        var page=container.gameObject.AddComponent<AuthoredPage>();
        page.EditorConfigure(slots,previous,next,label);
    }
}

[CustomEditor(typeof(UiAuthoringMigration))]
public sealed class UiAuthoringMigrationInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Bake the former runtime UI into scenes and bike prefabs. Save open scenes first. Existing authored layouts are retained. Brands uses its own toolbar command. Commit the resulting assets after review.",MessageType.Info);
        GUI.enabled=!EditorApplication.isPlayingOrWillChangePlaymode;
        if(GUILayout.Button("Author missing UI in scenes and prefabs")) ((UiAuthoringMigration)target).Migrate();
        GUI.enabled=true;
        EditorGUILayout.LabelField(((UiAuthoringMigration)target).IsComplete ? "Migration completed" : "Migration required before Play or Build");
    }
}

[InitializeOnLoad]
public sealed class UiAuthoringBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;
    static UiAuthoringBuildGuard()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if(state==PlayModeStateChange.ExitingEditMode && !Ready())
            {
                EditorApplication.isPlaying=false;
                Debug.LogError("Select Assets/Game/Editor/UIAuthoring.asset and author the missing UI before entering Play Mode.");
            }
        };
    }
    private static bool Ready() => AssetDatabase.LoadAssetAtPath<UiAuthoringMigration>(UiAuthoringMigration.AssetPath)?.IsComplete == true;
    public void OnPreprocessBuild(BuildReport report)
    {
        if(!Ready()) throw new BuildFailedException("Author the UI using Assets/Game/Editor/UIAuthoring.asset before building.");
    }
}
