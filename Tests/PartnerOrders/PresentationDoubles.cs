using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class MonoBehaviour { }
    public class Transform
    {
        public Vector3 position;
        public Vector3 forward = new Vector3(0,0,1);
        public Vector3 right = new Vector3(1,0,0);
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
        public Vector3 normalized=>magnitude>0?this*(1/magnitude):new Vector3();
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 ProjectOnPlane(Vector3 a,Vector3 n)=>a-n*Dot(a,n);
    }
    public static class Mathf
    {
        public static float Abs(float a)=>Math.Abs(a);
        public static float Sign(float a)=>a>=0?1:-1;
        public static float Clamp(float a,float min,float max)=>Math.Clamp(a,min,max);
        public static float Repeat(float a,float length)=>a-(float)Math.Floor(a/length)*length;
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
    }
}
namespace RaceFatal.Presentation.Racing
{
    public class RacerViewController
    {
        public RaceFatal.Racing.RaceParticipant Participant;
        public string RacerId=>Participant.RacerId;
        public UnityEngine.Transform transform=new();
    }
}
namespace RaceFatal.Presentation.Tracks
{
    public class TrackProgressPath
    {
        public float TotalLength=>1000;
        public int SegmentCount=>100;
        public float GetProgress(UnityEngine.Vector3 p,int previous,int radius,out int segment){segment=0;return p.z/TotalLength;}
        public UnityEngine.Vector3 GetPositionAtProgress(float p)=>new(0,0,p*TotalLength);
        public UnityEngine.Vector3 GetForwardAtProgress(float p)=>new(0,0,1);
    }
}
namespace RaceFatal.Presentation.Vehicles
{
    public class AIRacerSensor
    {
        public static List<AIRacerSensor> ActiveSensors=new();
        public RaceFatal.Presentation.Racing.RacerViewController View;
        public T GetComponentInParent<T>() where T:class=>View as T;
    }
}
