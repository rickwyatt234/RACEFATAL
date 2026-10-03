using System;
using System.Collections.Generic;
using RaceFatal.Racing;
using RaceFatal.Shared;
using N=System.Numerics;

namespace UnityEngine
{
    public class Object { public string name; }
    public class ScriptableObject : Object { }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T:class => gameObject.GetComponent<T>();
        public T GetComponentInParent<T>() where T:class => GetComponent<T>();
    }
    public class MonoBehaviour : Component
    {
        public bool enabled=true;
        public static void Destroy(Object item) { }
    }
    public class GameObject : Object
    {
        readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
        public Transform transform;
        public bool activeSelf=true;
        public GameObject(string name="",params Type[] types)
        {
            this.name=name;
            transform=Array.IndexOf(types,typeof(RectTransform))>=0 ? new RectTransform() : new Transform();
            transform.gameObject=this; components[transform.GetType()]=transform;
            foreach(var type in types) if(type!=typeof(RectTransform)) {var c=(Component)Activator.CreateInstance(type);c.gameObject=this;components[type]=c;}
        }
        public T Add<T>(T component) where T:Component {component.gameObject=this;components[typeof(T)]=component;return component;}
        public T GetComponent<T>() where T:class => components.TryGetValue(typeof(T),out var c)?c as T:null;
        public void SetActive(bool value)=>activeSelf=value;
    }
    public class Transform : Component
    {
        public Vector3 position,localPosition;
        public Quaternion rotation=Quaternion.identity,localRotation=Quaternion.identity;
        public Vector3 forward=>rotation*Vector3.forward;
        public Vector3 up=>rotation*Vector3.up;
        public Transform parent;
        public void SetParent(Transform value,bool worldPositionStays)=>parent=value;
    }
    public class RectTransform : Transform { public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta,offsetMin,offsetMax; public Rect rect; }
    public struct Rect {public Vector2 center;}
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 zero=>new Vector2(0,0);public static Vector2 one=>new Vector2(1,1);
        public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);
        public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
        public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y);
    }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 up=>new Vector3(0,1,0);
        public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized=>this/MathF.Max(.000001f,magnitude);
        public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator/(Vector3 a,float b)=>new Vector3(a.x/b,a.y/b,a.z/b);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
        public static Vector3 ProjectOnPlane(Vector3 v,Vector3 n)=>v-n*Dot(v,n);
        public static float SignedAngle(Vector3 from,Vector3 to,Vector3 axis)
        {
            var cross=N.Vector3.Cross(ToN(from),ToN(to));
            return MathF.Atan2(N.Vector3.Dot(cross,ToN(axis)),Dot(from,to))*Mathf.Rad2Deg;
        }
        public static N.Vector3 ToN(Vector3 v)=>new N.Vector3(v.x,v.y,v.z);
        public static Vector3 FromN(N.Vector3 v)=>new Vector3(v.X,v.Y,v.Z);
    }
    public struct Quaternion
    {
        public N.Quaternion value;
        public static Quaternion identity=>new Quaternion{value=N.Quaternion.Identity};
        public static Quaternion AngleAxis(float angle,Vector3 axis)=>new Quaternion{value=N.Quaternion.CreateFromAxisAngle(Vector3.ToN(axis),angle*Mathf.Deg2Rad)};
        public static Quaternion operator*(Quaternion a,Quaternion b)=>new Quaternion{value=a.value*b.value};
        public static Vector3 operator*(Quaternion a,Vector3 v)=>Vector3.FromN(N.Vector3.Transform(Vector3.ToN(v),a.value));
    }
    public static class Mathf
    {
        public const float Deg2Rad=MathF.PI/180f,Rad2Deg=180f/MathF.PI;
        public static float Max(float a,float b)=>MathF.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Clamp(float v,float min,float max)=>Math.Clamp(v,min,max);public static float Clamp01(float v)=>Clamp(v,0,1);
        public static float InverseLerp(float a,float b,float v)=>Clamp01((v-a)/(b-a));
        public static float Atan(float v)=>MathF.Atan(v);public static float Exp(float v)=>MathF.Exp(v);public static float Abs(float v)=>MathF.Abs(v);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
    }
    public static class Time {public static float deltaTime=.02f;}
    public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public class Sprite:Object { }
    public class AudioClip:Object { }
    public enum AudioRolloffMode {Logarithmic}
    public enum RenderMode {ScreenSpaceOverlay,ScreenSpaceCamera,WorldSpace}
    public class Canvas:Component {public bool enabled=true;public Camera worldCamera;public RenderMode renderMode;}
    public class Camera:Component
    {
        public bool enabled=true;public float fieldOfView=75f,aspect=16f/9f;
        public Vector3 WorldToViewportPoint(Vector3 point)
        {
            var local=Vector3.FromN(N.Vector3.Transform(Vector3.ToN(point-transform.position),N.Quaternion.Inverse(transform.rotation.value)));
            float y=MathF.Tan(fieldOfView*Mathf.Deg2Rad/2);
            return new Vector3(.5f+local.x/(2*local.z*y*aspect),.5f+local.y/(2*local.z*y),local.z);
        }
        public Ray ViewportPointToRay(Vector3 point)
        {
            float y=MathF.Tan(fieldOfView*Mathf.Deg2Rad/2);
            return new Ray(transform.position,transform.rotation*new Vector3((point.x-.5f)*2*y*aspect,(point.y-.5f)*2*y,1).normalized);
        }
        public Vector3 ViewportToScreenPoint(Vector3 point)=>new Vector3(point.x*1920,point.y*1080,point.z);
    }
    public static class RectTransformUtility
    {public static bool ScreenPointToLocalPointInRectangle(RectTransform rect,Vector2 screen,Camera camera,out Vector2 local){local=screen;return true;}}
    public struct Ray {public Vector3 origin,direction;public Ray(Vector3 o,Vector3 d){origin=o;direction=d;}}
    public struct LayerMask {public int value;public static implicit operator LayerMask(int value)=>new LayerMask{value=value};}
    public enum QueryTriggerInteraction {Ignore}
    public class Collider:Component {public RaceFatal.Presentation.Racing.RacerViewController owner;public new T GetComponentInParent<T>() where T:class=>owner as T;}
    public struct RaycastHit {public Collider collider;public Vector3 point;public float distance;}
    public static class Physics
    {
        public static RaycastHit[] Hits=Array.Empty<RaycastHit>();
        public static int RaycastNonAlloc(Ray ray,RaycastHit[] buffer,float range,LayerMask mask,QueryTriggerInteraction trigger)
        {int count=Math.Min(Hits.Length,buffer.Length);Array.Copy(Hits,buffer,count);return count;}
    }
    public class SerializeField:Attribute{}
    public class Header:Attribute{public Header(string s){}}
    public class Tooltip:Attribute{public Tooltip(string s){}}
    public class Min:Attribute{public Min(float f){}}
    public class Range:Attribute{public Range(float a,float b){}}
    public class DefaultExecutionOrder:Attribute{public DefaultExecutionOrder(int i){}}
    public class RequireComponent:Attribute{public RequireComponent(Type t){}}
    public class CreateAssetMenu:Attribute{public string fileName,menuName;}
}
namespace UnityEngine.UI
{
    public class Image:UnityEngine.Component {public UnityEngine.Sprite sprite;public UnityEngine.Color color;public bool enabled=true,raycastTarget,preserveAspect;}
}
namespace RaceFatal.Presentation.Racing
{
    public static class RacePauseController {public static bool IsGameplayBlocked;}
    public class RacerViewController:UnityEngine.Component
    {
        public TestParticipant Participant;public bool IsInitialized=>Participant!=null;
        public RaceFatal.Presentation.Combat.BikeEquipmentMountBinding Mount;
        public bool TryGetEquipmentMount(string id,out RaceFatal.Presentation.Combat.BikeEquipmentMountBinding mount){mount=Mount;return mount!=null;}
    }
    public class TestParticipant {public RaceParticipantRole Role;public TestVehicle Vehicle=new TestVehicle();}
    public class TestVehicle {public TestEquipment EquipmentSystem=new TestEquipment();public bool IsDestroyed;}
    public class TestEquipment {public TestWeapon SelectedWeaponDefinition;public string SelectedEquipmentId="gun";}
    public class TestWeapon {public string Id="gun";public WeaponAimMode AimMode;public WeaponDeliveryMode DeliveryMode;public float Range=300;}
}
namespace RaceFatal.Presentation.Vehicles
{
    public class BikeMotor:UnityEngine.Component {public float SteeringInput,SpeedMetersPerSecond,LeanResponseMultiplier=1;}
    public class PlayerCockpitView:UnityEngine.Component {public UnityEngine.Camera CockpitCamera;public UnityEngine.Canvas ReticleCanvas;public bool IsActivePlayerView=true,IsDeathViewActive;}
}
namespace RaceFatal.Presentation.Combat
{
    public class ProjectileView:UnityEngine.Component{}
    public class BikeEquipmentMountBinding {public UnityEngine.Transform EquipmentOrigin;}
    public class RaceWeaponPresenter {public WeaponPresentationProfile Profile;public bool TryGetPresentationProfile(string id,out WeaponPresentationProfile profile){profile=Profile;return profile!=null;}}
}
