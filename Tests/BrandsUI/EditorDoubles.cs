using System;
namespace UnityEngine {public static class GUILayout {public static bool Button(string text)=>false;}}
namespace UnityEngine.SceneManagement {public struct Scene {}}
namespace UnityEditor
{
 public class MenuItem:Attribute {public MenuItem(string name){}}
 public class CustomEditor:Attribute {public CustomEditor(Type type){}}
 public class Editor {protected UnityEngine.Object target;public virtual void OnInspectorGUI(){}protected void DrawDefaultInspector(){}}
 public static class EditorApplication {public static bool isPlayingOrWillChangePlaymode;}
 public static class Selection {public static UnityEngine.GameObject activeGameObject;}
 public static class EditorUtility {public static bool Confirm=true;public static bool DisplayDialog(string title,string text,string ok,string cancel)=>Confirm;public static void SetDirty(UnityEngine.Object obj){}}
 public static class Undo {public static void RegisterFullObjectHierarchyUndo(UnityEngine.Object obj,string text){}public static void DestroyObjectImmediate(UnityEngine.Object obj)=>throw new Exception("Unexpected overwrite");public static T AddComponent<T>(UnityEngine.GameObject obj) where T:UnityEngine.Component,new()=>obj.AddComponent<T>();}
 public class SerializedObject {public SerializedObject(object value){}public SerializedProperty FindProperty(string name)=>new SerializedProperty();public void ApplyModifiedProperties(){}}
 public class SerializedProperty {public UnityEngine.Object objectReferenceValue;}
 public enum MessageType {Info}
 public static class EditorGUILayout {public static void HelpBox(string text,MessageType type){}public static void LabelField(string text,object style){}}
 public static class EditorStyles {public static object boldLabel;}
}
namespace UnityEditor.SceneManagement
{
 public enum OpenSceneMode {Single}
 public static class EditorSceneManager {public static bool SaveCurrentModifiedScenesIfUserWantsTo()=>true;public static UnityEngine.SceneManagement.Scene OpenScene(string path,OpenSceneMode mode)=>default;public static void MarkSceneDirty(UnityEngine.SceneManagement.Scene scene){}public static void SaveScene(UnityEngine.SceneManagement.Scene scene){}}
}
