using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
 public class Object { public string name; }
 public class Component:Object {public T GetComponentInChildren<T>(bool includeInactive=false) where T:Component=>GetComponentsInChildren<T>(includeInactive).FirstOrDefault();public GameObject gameObject; public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>(); public T[] GetComponentsInChildren<T>(bool includeInactive=false) where T:Component=>gameObject.GetComponentsInChildren<T>(includeInactive); }
 public class MonoBehaviour:Component {}
 public class CanvasGroup:Component {public float alpha;public bool interactable;}
 public enum KeyCode {Escape,F1}
 public class GameObject:Object {
  public static int Created,ComponentsAdded; public bool activeSelf=true;public readonly List<GameObject> children=new();private readonly List<Component> components=new();
  public GameObject(string name){this.name=name;Created++;}
  public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this,name=name};components.Add(c);ComponentsAdded++;return c;}
  public T GetComponent<T>() where T:Component=>components.OfType<T>().FirstOrDefault();
  public T[] GetComponentsInChildren<T>(bool includeInactive=false) where T:Component=>components.OfType<T>().Concat(children.SelectMany(c=>c.GetComponentsInChildren<T>(includeInactive))).ToArray();
  public void SetActive(bool active){activeSelf=active;}
 }
 public class SerializeField:Attribute{}
}
namespace UnityEngine.Events {public delegate void UnityAction();public class UnityEvent {readonly List<UnityAction> actions=new();public void AddListener(UnityAction a)=>actions.Add(a);public void RemoveAllListeners()=>actions.Clear();public void Invoke(){foreach(var a in actions.ToArray())a();}public int Count=>actions.Count;}}
namespace UnityEngine.UI {public class Button:UnityEngine.Component{public bool interactable;public UnityEngine.Events.UnityEvent onClick=new();}}
namespace TMPro {public class TMP_Text:UnityEngine.Component{public string text;public float fontSize=23;}}

namespace UnityEngine.EventSystems {public class EventSystem {public static EventSystem current;public UnityEngine.GameObject selected;public void SetSelectedGameObject(UnityEngine.GameObject value)=>selected=value;}}
namespace RaceFatal.Presentation.Racing {public class RacePauseController {public int resumed,toggled,quit;public void Resume()=>resumed++;public void ToggleMenu()=>toggled++;public void QuitGame()=>quit++;}}
