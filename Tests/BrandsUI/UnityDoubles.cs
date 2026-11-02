// Compile-only adapter for the new UI's domain API usage. Unity rendering/input still requires Play Mode.
using System;
using System.Collections;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Shared;
namespace UnityEngine
{
 public class Object { public string name; public static void Destroy(Object value) {} }
 public class Component : Object { public GameObject gameObject; public Transform transform; public T GetComponent<T>() where T:Component => null; public T GetComponentInChildren<T>() where T:Component => null; }
 public class Behaviour : Component {}
 public class MonoBehaviour : Behaviour { protected void StartCoroutine(IEnumerator value){} protected void StopAllCoroutines(){} }
 public class ScriptableObject : Object {}
 public class GameObject : Object { public Transform transform; public GameObject(string name, params Type[] components){} public T AddComponent<T>() where T:Component,new() => new T(); public T GetComponent<T>() where T:Component => null; public void SetActive(bool active){} }
 public class Transform : Component,IEnumerable { public Vector3 localScale; public void SetParent(Transform parent,bool world){} public IEnumerator GetEnumerator()=>Array.Empty<Transform>().GetEnumerator(); }
 public class RectTransform : Transform {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta,offsetMin,offsetMax; public Rect rect; }
 public struct Rect {public float width,height;}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero,one;}
 public struct Vector3 {public static Vector3 one; public static Vector3 operator*(Vector3 v,float s)=>v;}
 public struct Color {public Color(float r,float g,float b,float a=1){}public static Color Lerp(Color a,Color b,float t)=>a;}
 public static class ColorUtility {public static bool TryParseHtmlString(string text,out Color color){color=default;return true;}}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b);public static float Clamp01(float v)=>Math.Clamp(v,0,1);}
 public static class Time {public static float unscaledDeltaTime;}
 public class CanvasGroup : Behaviour {public bool interactable,blocksRaycasts,ignoreParentGroups;}
 public class SerializeField:Attribute{}
 public class TextArea:Attribute{}
 public class CreateAssetMenu:Attribute{public string menuName,fileName;}
}
namespace UnityEngine.Events {public delegate void UnityAction();public class UnityEvent {public void AddListener(UnityAction action){}public void RemoveAllListeners(){}}public class UnityEvent<T> {public void AddListener(Action<T> action){}}}
namespace UnityEngine.UI
{
 public class Graphic:UnityEngine.Behaviour {public UnityEngine.Color color;public bool raycastTarget;}
 public class Image:Graphic{}
 public class Selectable:UnityEngine.Behaviour {public bool interactable;}
 public class Button:Selectable {public Graphic targetGraphic;public UnityEngine.Events.UnityEvent onClick=new UnityEngine.Events.UnityEvent();}
 public class RectMask2D:UnityEngine.Behaviour{}
 public class ContentSizeFitter:UnityEngine.Behaviour {public enum FitMode{PreferredSize}public FitMode verticalFit;}
 public class ScrollRect:UnityEngine.Behaviour {public UnityEngine.RectTransform viewport,content;public bool horizontal;public float scrollSensitivity;public enum MovementType{Clamped}public MovementType movementType;}
}
namespace UnityEngine.EventSystems {public class EventSystem {public static EventSystem current;public void SetSelectedGameObject(UnityEngine.GameObject obj){}}}
namespace TMPro
{
 public enum TextAlignmentOptions{Center}
 public class TMP_Text:UnityEngine.UI.Graphic {public string text;public float fontSize;public bool richText;public UnityEngine.RectTransform rectTransform;public TextAlignmentOptions alignment;}
 public class TextMeshProUGUI:TMP_Text{}
 public class TMP_InputField:UnityEngine.UI.Selectable {public UnityEngine.RectTransform textViewport;public TMP_Text textComponent;public UnityEngine.UI.Graphic placeholder;public string text;public UnityEngine.Events.UnityEvent<string> onEndEdit=new UnityEngine.Events.UnityEvent<string>();}
}
namespace RaceFatal.Infrastructure
{
 public class GameContext {public GameDatabase Database;public GameSessionManager Sessions;public SaveService Saves;}
 public class SaveService {public Result SaveCurrentCampaign()=>Result.Success();}
}
namespace RaceFatal.Presentation.Career {public class CareerController {}}
