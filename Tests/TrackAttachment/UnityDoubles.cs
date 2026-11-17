// Deterministic controller harness. Numerics implements rotation math; scripted
// casts supply contacts. This is not a substitute for Unity/PhysX play testing.
using System;
using System.Collections.Generic;
using N = System.Numerics;
namespace UnityEngine
{
    public class Object { public string name; }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
    }
    public class MonoBehaviour : Component { }
    public class GameObject : Object
    {
        readonly Dictionary<Type, Component> components = new();
        public Transform transform;
        public GameObject(string name = "") { this.name = name; transform = Add(new Transform()); }
        public T Add<T>(T component) where T : Component { component.gameObject = this; components[typeof(T)] = component; return component; }
        public T GetComponent<T>() where T : class => components.TryGetValue(typeof(T), out var c) ? c as T : null;
    }
    public class Transform : Component
    {
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 forward => rotation * Vector3.forward;
        public Vector3 up => rotation * Vector3.up;
        public Vector3 right => rotation * Vector3.right;
        public Vector3 InverseTransformPoint(Vector3 point) => Quaternion.Inverse(rotation) * (point - position);
        public void SetPositionAndRotation(Vector3 p, Quaternion q) { position = p; rotation = q; }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new(0, 0, 0);
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 right => new(1, 0, 0);
        public static Vector3 forward => new(0, 0, 1);
        public float sqrMagnitude => x*x + y*y + z*z;
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized => magnitude < 1e-8f ? zero : this / magnitude;
        public void Normalize() { this = normalized; }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x+b.x, a.y+b.y, a.z+b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x, a.y-b.y, a.z-b.z);
        public static Vector3 operator -(Vector3 a) => a * -1;
        public static Vector3 operator *(Vector3 a, float b) => new(a.x*b, a.y*b, a.z*b);
        public static Vector3 operator *(float a, Vector3 b) => b * a;
        public static Vector3 operator /(Vector3 a, float b) => a * (1/b);
        public static float Dot(Vector3 a, Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => FromN(N.Vector3.Cross(ToN(a), ToN(b)));
        public static float Distance(Vector3 a, Vector3 b) => (a-b).magnitude;
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) => v-n*Dot(v,n)/n.sqrMagnitude;
        public static float Angle(Vector3 a, Vector3 b) => MathF.Acos(Math.Clamp(Dot(a.normalized,b.normalized),-1,1))*Mathf.Rad2Deg;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a+(b-a)*Mathf.Clamp01(t);
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) => Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(a,b),t)*a;
        public static N.Vector3 ToN(Vector3 v) => new(v.x,v.y,v.z);
        public static Vector3 FromN(N.Vector3 v) => new(v.X,v.Y,v.Z);
    }
    public struct Quaternion
    {
        public N.Quaternion value;
        public static Quaternion identity => new() { value = N.Quaternion.Identity };
        public static Quaternion AngleAxis(float angle, Vector3 axis) => new() { value = N.Quaternion.CreateFromAxisAngle(Vector3.ToN(axis.normalized),angle*Mathf.Deg2Rad) };
        public static Quaternion operator *(Quaternion a, Quaternion b) => new() { value = N.Quaternion.Normalize(a.value*b.value) };
        public static Vector3 operator *(Quaternion q, Vector3 v) => Vector3.FromN(N.Vector3.Transform(Vector3.ToN(v),q.value));
        public static Quaternion Inverse(Quaternion q) => new() { value = N.Quaternion.Inverse(q.value) };
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => new() { value = N.Quaternion.Slerp(a.value,b.value,Mathf.Clamp01(t)) };
        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            from.Normalize(); to.Normalize(); float dot = Vector3.Dot(from,to);
            if(dot > .9999999f) return identity;
            if(dot < -.999999f) return AngleAxis(180,Vector3.Cross(from,MathF.Abs(from.x)<.9f?Vector3.right:Vector3.up));
            var cross = Vector3.Cross(from,to);
            return new Quaternion { value = N.Quaternion.Normalize(new N.Quaternion(cross.x,cross.y,cross.z,1+dot)) };
        }
        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            forward.Normalize(); var right = Vector3.Cross(up,forward).normalized; up=Vector3.Cross(forward,right);
            return new Quaternion { value = N.Quaternion.CreateFromRotationMatrix(new N.Matrix4x4(
                right.x,right.y,right.z,0,up.x,up.y,up.z,0,forward.x,forward.y,forward.z,0,0,0,0,1)) };
        }
    }
    public static class Mathf
    {
        public const float PI=MathF.PI, Deg2Rad=PI/180, Rad2Deg=180/PI;
        public static float Max(float a,float b)=>MathF.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b); public static float Clamp01(float v)=>Clamp(v,0,1);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float Exp(float v)=>MathF.Exp(v); public static float Abs(float v)=>MathF.Abs(v);
        public static float Cos(float v)=>MathF.Cos(v); public static float Sign(float v)=>MathF.Sign(v);
        public static float Repeat(float v,float length)=>v-MathF.Floor(v/length)*length;
        public static int FloorToInt(float v)=>(int)MathF.Floor(v); public static float Floor(float v)=>MathF.Floor(v);
    }
    public enum ForceMode { Acceleration, VelocityChange }
    public enum RigidbodyInterpolation { None, Interpolate }
    public class Rigidbody : Component
    {
        public Vector3 position, linearVelocity, angularVelocity, centerOfMass, acceleration;
        public Quaternion rotation=Quaternion.identity;
        public bool useGravity=true;
        public float mass,maxDepenetrationVelocity;
        public RigidbodyInterpolation interpolation;
        public void AddForce(Vector3 force,ForceMode mode) { if(mode==ForceMode.Acceleration) acceleration+=force; else linearVelocity+=force; }
        public void MoveRotation(Quaternion q) { rotation=q; }
    }
    public class Collision { }
    public class Collider : Component { public Rigidbody attachedRigidbody; }
    public struct RaycastHit { public Collider collider; public Vector3 point,normal; public float distance; }
    public struct LayerMask
    {
        public int value;
        public static implicit operator LayerMask(int v)=>new(){value=v};
        public static int GetMask(string name)=>8;
    }
    public enum QueryTriggerInteraction { Ignore }
    public static class Physics
    {
        public static Func<Vector3,float,Vector3,float,RaycastHit[]> Cast = (o,r,d,l)=>Array.Empty<RaycastHit>();
        public static int SphereCastNonAlloc(Vector3 o,float r,Vector3 d,RaycastHit[] buffer,float length,LayerMask mask,QueryTriggerInteraction trigger)
        { var hits=Cast(o,r,d,length); int count=Math.Min(buffer.Length,hits.Length); Array.Copy(hits,buffer,count); return count; }
        public static bool Raycast(Vector3 o,Vector3 d,out RaycastHit hit,float distance,int mask,QueryTriggerInteraction trigger=QueryTriggerInteraction.Ignore) {hit=default;return false;}
    }
    public static class Time {public static float fixedDeltaTime=.02f;}
    public static class Debug {public static void LogWarning(string text,Object context){} public static void LogError(string text,Object context){} }
    public struct Color {public static Color yellow,green;}
    public static class Gizmos {public static Color color;public static void DrawRay(Vector3 o,Vector3 d){} public static void DrawWireSphere(Vector3 o,float r){} public static void DrawLine(Vector3 a,Vector3 b){} }
    public class SerializeField:Attribute{}
    public class Header:Attribute{public Header(string s){}}
    public class Tooltip:Attribute{public Tooltip(string s){}}
    public class Min:Attribute{public Min(float v){}}
    public class Range:Attribute{public Range(float a,float b){}}
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true)] public class RequireComponent:Attribute{public RequireComponent(Type t){}}
}
namespace RaceFatal.Career
{
    public enum RacerPerkEffect {SlipstreamAcceleration,IronWill,LateBraking,CorneringStability,DescentStability}
}
namespace RaceFatal.Presentation.Racing
{
    public class RacerViewController:UnityEngine.Component {public TestParticipant Participant;}
    public class TestParticipant {public TestPerks Perks=new();public TestVehicle Vehicle=new();}
    public class TestPerks {public float Bonus(RaceFatal.Career.RacerPerkEffect effect)=>0;public bool Has(RaceFatal.Career.RacerPerkEffect effect)=>false;}
    public class TestVehicle {public TestDamage Damage=new();}
    public class TestDamage {public float CurrentIntegrity=100;}
    public class RaceRuntimeController { }
    public class LapLineTrigger:UnityEngine.Component {public void Initialize(RaceRuntimeController runtime){} }
    public static class RaceStartupTrace {public static void Mark(string text,UnityEngine.Object context){} }
}
namespace RaceFatal.Presentation.Tracks
{
    public class TrackPathBuilder:UnityEngine.Component {public bool Build()=>true;}
    public class TrackProgressReporter:UnityEngine.Component
    {public void Initialize(RaceFatal.Presentation.Racing.RaceRuntimeController runtime,TrackProgressPath path){} public void ResetTracking(){} }
}
