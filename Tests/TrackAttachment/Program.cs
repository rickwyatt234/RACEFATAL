using System;
using System.Collections.Generic;
using System.Reflection;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Vehicles;
using UnityEngine;

static class Program
{
    static int checks;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition,string message) { checks++;if(!condition)throw new Exception(message); }
    static void Near(float actual,float expected,string message,float tolerance=.002f)=>Check(MathF.Abs(actual-expected)<tolerance,message+": "+actual+" expected "+expected);
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,Private).SetValue(target,value);
    static T Get<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
    static void Call(object target,string method)=>target.GetType().GetMethod(method,Private).Invoke(target,null);
    static Collider road=new GameObject("road").Add(new Collider());
    static (GameObject go,Rigidbody body,TrackSurfaceProbe probe,BikeMotor motor) Bike()
    {
        var go=new GameObject("bike"); var body=go.Add(new Rigidbody());var probe=go.Add(new TrackSurfaceProbe());var motor=go.Add(new BikeMotor());
        Call(probe,"Awake");Call(probe,"OnEnable");Call(motor,"Awake");Call(motor,"OnEnable");
        Set(probe,"trackMask",(LayerMask)8);Set(probe,"normalResponse",0f);
        motor.SetPerformance(new BikePerformance(300,20,1,200));
        return (go,body,probe,motor);
    }
    static RaycastHit[] Plane(Vector3 origin,float radius,Vector3 direction,float length,Vector3 up)
    {
        float denominator=Vector3.Dot(direction,up);
        if(denominator>=-.001f)return Array.Empty<RaycastHit>();
        float travel=(radius-Vector3.Dot(origin,up))/denominator;
        if(travel<0||travel>length)return Array.Empty<RaycastHit>();
        return new[]{new RaycastHit{collider=road,normal=up,distance=travel,point=origin+direction*travel-up*radius}};
    }
    static void Contact(Vector3 up)=>Physics.Cast=(o,r,d,l)=>Plane(o,r,d,l,up);
    static void Tick(Rigidbody body,BikeMotor motor){body.acceleration=Vector3.zero;Call(motor,"FixedUpdate");}
    static void Main()
    {
        ProbeHeights(); ProbeFilteringAndSeams(); PathGuidance(); Momentum(); CurvedTravel(); LossAndReset(); RuntimeBinding();
        Console.WriteLine($"Track attachment: {checks} checks passed (scripted contacts; no PhysX simulation).");
    }
    static void ProbeHeights()
    {
        var b=Bike();
        foreach(float angle in new[]{0f,90f,180f,270f})
        foreach(float radius in new[]{.1f,.25f,.4f})
        {
            var rotation=Quaternion.AngleAxis(angle,Vector3.forward);var up=rotation*Vector3.up;
            b.body.rotation=rotation;b.body.position=up*.5f;b.probe.ResetTracking();Contact(up);
            Set(b.probe,"sphereRadius",radius);Set(b.probe,"startOffset",.6f);
            Check(b.probe.Sample(),"Floor/wall/ceiling detected");
            Near(b.probe.SurfaceDistance,.5f,"Height independent of sphere travel/radius/offset");
            Near(Vector3.Dot(b.probe.SurfaceNormal,up),1,"Correct local road up");
            b.motor.ResetSurfaceTracking();Tick(b.body,b.motor);
            Near(b.body.acceleration.magnitude,0,"Stopped bikes remain at suspension equilibrium on walls and ceilings");
        }
        b.body.rotation=Quaternion.identity;b.body.position=Vector3.up*.5f;b.probe.ResetTracking();
        Set(b.probe,"sphereRadius",.25f);Set(b.probe,"startOffset",.25f);
        // Transform deliberately lags behind the physics pose, as with interpolation.
        b.go.transform.position=new Vector3(100,100,100);Contact(Vector3.up);
        Check(b.probe.Sample(),"Probe uses Rigidbody pose, not interpolated Transform");
        Near(b.probe.SurfaceDistance,.5f,"Physics height remains correct");
    }
    static void ProbeFilteringAndSeams()
    {
        var b=Bike();b.body.position=Vector3.up*.5f;
        Physics.Cast=(o,r,d,l)=>MathF.Abs(o.z)<.3f?Array.Empty<RaycastHit>():Plane(o,r,d,l,Vector3.up);
        Check(b.probe.Sample(),"Front and rear samples bridge centre seam");Near(b.probe.SurfaceDistance,.5f,"Seam retains root height");
        var self=new Collider{attachedRigidbody=b.body};
        Physics.Cast=(o,r,d,l)=>new[]{new RaycastHit{collider=road,normal=-Vector3.up,distance=.01f},new RaycastHit{collider=self,normal=Vector3.up,distance=.02f},new RaycastHit{collider=road,normal=Vector3.right,distance=.03f},Plane(o,r,d,l,Vector3.up)[0]};
        Check(b.probe.Sample(),"Opposing road, self and wall rejected before choosing road");Near(b.probe.SurfaceDistance,.5f,"Nearest valid contact selected");
        Set(b.probe,"normalResponse",20f);var tilted=Quaternion.AngleAxis(10,Vector3.forward)*Vector3.up;Contact(tilted);
        Check(b.probe.Sample(),"Tilt detected");float angle=Vector3.Angle(Vector3.up,b.probe.SurfaceNormal);
        Check(angle>0&&angle<10,"Small normal changes are filtered");
        Physics.Cast=(o,r,d,l)=>Array.Empty<RaycastHit>();Check(!b.probe.Sample(),"No imaginary surface over a gap");
        Near(Vector3.Dot(b.probe.LastSurfaceNormal,tilted),1,"Miss retains last valid support normal");
    }
    static TrackProgressPath Path(Quaternion frame)
    {
        var path=new GameObject("path").Add(new TrackProgressPath());
        var points=new List<Vector3>{new(-10,0,-10),new(-10,0,10),new(10,0,10),new(10,0,-10)};
        path.SetControlPoints(points,new[]{frame,frame,frame,frame});Check(path.Rebuild(),"Guidance path rebuilds");return path;
    }
    static void PathGuidance()
    {
        var b=Bike();var frame=Quaternion.AngleAxis(60,Vector3.forward);var up=frame*Vector3.up;var path=Path(frame);
        b.body.position=up*.5f;b.probe.Initialize(path);int fallbackCalls=0;
        Physics.Cast=(o,r,d,l)=>{if(Vector3.Dot(d,-up)>.999f){fallbackCalls++;return Plane(o,r,d,l,up);}return Array.Empty<RaycastHit>();};
        Check(b.probe.Sample(),"Authored banking reacquires when bike-down cast misses");Check(fallbackCalls==3,"Fallback samples real geometry");
        Near(Vector3.Dot(b.probe.SurfaceNormal,up),1,"Authored fallback gets correct normal");
        Physics.Cast=(o,r,d,l)=>Array.Empty<RaycastHit>();Check(!b.probe.Sample(),"Path cannot create ground across a gap");
        path.SetControlPoints(new List<Vector3>{new(-10,0,-10),new(-10,0,10),new(10,0,10),new(10,0,-10)});path.Rebuild();
        Check(!path.HasAuthoredOrientations,"Position-only path does not invent banking");
        b.body.rotation=frame;b.probe.ResetTracking();Contact(up);Check(b.probe.Sample(),"Position-only path leaves surface probing functional");
    }
    static void Momentum()
    {
        foreach(float step in new[]{.01f,.02f,.04f})
        foreach(var axis in new[]{Vector3.right,Vector3.forward})
        {
            Time.fixedDeltaTime=step;var b=Bike();
            Set(b.motor,"lateralGrip",0f);Set(b.motor,"surfaceSpringStrength",0f);Set(b.motor,"surfaceSpringDamping",0f);
            b.body.position=Vector3.up*.5f;b.body.linearVelocity=Vector3.forward*100;Contact(Vector3.up);Tick(b.body,b.motor);
            b.motor.ApplyExternalVelocityChange(Vector3.right,7);
            float speed=b.body.linearVelocity.magnitude;
            // Full vertical loop or bank roll, including 90/180-degree orientations.
            // Smaller timesteps traverse the same course at the same angular rate.
            int steps=(int)MathF.Round(360/(100*step));float increment=360f/steps;
            for(int i=1;i<=steps;i++)
            {
                var frame=Quaternion.AngleAxis(i*increment,axis);var normal=frame*Vector3.up;
                b.body.position=normal*.5f;Contact(normal);Tick(b.body,b.motor);
                var physicalVelocity=b.body.linearVelocity-normal*Get<float>(b.motor,"curvatureNormalSpeed");
                Near(physicalVelocity.magnitude,speed,"Transport preserves physical speed",.03f);
                Near(Vector3.Dot(physicalVelocity,normal),0,"Physical motion remains tangent",.03f);
                Near(Vector3.Dot(b.body.linearVelocity,frame*Vector3.right),7,"Lateral ram velocity survives tilt/bank",.03f);
                Near(Vector3.Dot(b.body.rotation*Vector3.up,normal),1,"Chassis follows changing road up",.003f);
            }
        }
        Time.fixedDeltaTime=.02f;
        var landing=Bike();Set(landing.motor,"maximumSurfaceSeparationSpeed",1000f);landing.body.position=Vector3.up*.5f;landing.body.linearVelocity=Vector3.forward*50;Contact(Vector3.up);Tick(landing.body,landing.motor);
        var normal60=Quaternion.AngleAxis(60,Vector3.right)*Vector3.up;landing.body.position=normal60*.5f;Contact(normal60);Tick(landing.body,landing.motor);
        Near(Vector3.Dot(landing.body.linearVelocity,Vector3.forward),50,"Abrupt discontinuity does not transport momentum");
    }
    static void CurvedTravel()
    {
        // Analytic cylindrical road contacts plus semi-implicit integration. Unlike
        // the rotation tests, positions are never reset to the road each tick.
        foreach(bool inside in new[]{false,true})
        foreach(float speed in new[]{75f,135f})
        foreach(float dt in new[]{.01f,.02f})
        foreach(float hoverHeight in new[]{.1f,.5f})
        {
            Time.fixedDeltaTime=dt;const float radius=50f;var b=Bike();
            Set(b.probe,"normalResponse",50f);Set(b.motor,"desiredSurfaceDistance",hoverHeight);
            b.body.position=Vector3.up*(inside?-(radius-hoverHeight):radius+hoverHeight);
            b.body.linearVelocity=Vector3.forward*speed;
            Physics.Cast=(origin,sphere,direction,length)=>
            {
                float target=inside?radius-sphere:radius+sphere;
                float a=direction.y*direction.y+direction.z*direction.z;
                float halfB=origin.y*direction.y+origin.z*direction.z;
                float c=origin.y*origin.y+origin.z*origin.z-target*target;
                float disc=halfB*halfB-a*c;
                if(a<1e-6f||disc<0)return Array.Empty<RaycastHit>();
                float travel=(-halfB+(inside?1:-1)*MathF.Sqrt(disc))/a;
                if(travel<0||travel>length)return Array.Empty<RaycastHit>();
                var centre=origin+direction*travel;
                var normal=new Vector3(0,centre.y,centre.z).normalized*(inside?-1:1);
                return new[]{new RaycastHit{collider=road,normal=normal,distance=travel,point=centre-normal*sphere}};
            };
            float minimumHeight=float.MaxValue,maximumHeight=0;
            int steps=(int)(2*MathF.PI*radius/speed/dt)+20;
            for(int i=0;i<steps;i++)
            {
                if(i==steps/2)b.motor.ApplyCollisionRebound(Vector3.right,2);
                Tick(b.body,b.motor);Check(b.motor.HasSurface,"Continuous contact around integrated crest/loop");
                b.body.linearVelocity+=b.body.acceleration*dt;b.body.position+=b.body.linearVelocity*dt;
                float radial=MathF.Sqrt(b.body.position.y*b.body.position.y+b.body.position.z*b.body.position.z);
                float height=inside?radius-radial:radial-radius;
                minimumHeight=MathF.Min(minimumHeight,height);maximumHeight=MathF.Max(maximumHeight,height);
                Check(height>0&&MathF.Abs(height-hoverHeight)<.12f,"Integrated curved motion stays near configured hover height: "+height+" at step "+i);
            }
            Console.WriteLine($"  {(inside?"Inner loop":"Outer crest")}, {speed} m/s, dt={dt}, target={hoverHeight}: height {minimumHeight:F3}..{maximumHeight:F3} m");
        }
        Time.fixedDeltaTime=.02f;
    }
    static void LossAndReset()
    {
        var b=Bike();b.body.position=Vector3.up*.5f;b.motor.SetControls(1,0,0);Contact(Vector3.up);Tick(b.body,b.motor);
        Check(!b.body.useGravity&&b.body.interpolation==RigidbodyInterpolation.Interpolate,"Local physics and interpolation configured");
        Physics.Cast=(o,r,d,l)=>Array.Empty<RaycastHit>();Tick(b.body,b.motor);
        Check(b.body.acceleration.z>0,"Brief loss retains drive instead of spawn grace");Near(b.body.acceleration.y,-25,"Brief loss retains seam adhesion");
        for(int i=0;i<15;i++)Tick(b.body,b.motor);
        Near(b.body.acceleration.z,0,"Sustained loss releases drive");Near(b.body.acceleration.y,-12,"Air gravity follows last surface down");
        // Landing after genuine flight must not rotate airborne momentum with an old road frame.
        var frame=Quaternion.AngleAxis(20,Vector3.right);var up=frame*Vector3.up;
        Set(b.motor,"maximumSurfaceSeparationSpeed",1000f);b.body.linearVelocity=Vector3.forward*50;b.body.position=up*.5f;Contact(up);Tick(b.body,b.motor);
        Near(b.body.linearVelocity.z,50,"Landing does not transport old momentum");
        b.body.rotation=Quaternion.AngleAxis(180,Vector3.forward);b.motor.ResetSurfaceTracking();
        Check(!Get<bool>(b.motor,"hasAcquiredSurface")&&!b.probe.HasSurface,"Reset clears attachment history");
        Near(Vector3.Dot(b.probe.LastSurfaceNormal,-Vector3.up),1,"Reset uses new spawn orientation");
        Physics.Cast=(o,r,d,l)=>Array.Empty<RaycastHit>();Tick(b.body,b.motor);
        Near(b.body.acceleration.magnitude,0,"New spawn grace is separate from seam recovery");
        for(int i=0;i<26;i++)Tick(b.body,b.motor);
        Near(b.body.acceleration.y,12,"Unattached spawn eventually receives local air gravity");
        var seam=Bike();seam.body.position=Vector3.up*.5f;seam.body.linearVelocity=Vector3.forward*50;Contact(Vector3.up);Tick(seam.body,seam.motor);
        Physics.Cast=(o,r,d,l)=>Array.Empty<RaycastHit>();Tick(seam.body,seam.motor);
        seam.body.position=up*.5f;Contact(up);Tick(seam.body,seam.motor);
        Near(Vector3.Dot(seam.body.linearVelocity,up)-Get<float>(seam.motor,"curvatureNormalSpeed"),0,"Brief seam reacquisition transports motion");
    }
    static void RuntimeBinding()
    {
        var b=Bike();var racer=b.go.Add(new RacerViewController());var path=Path(Quaternion.identity);
        var track=new GameObject("track").Add(new TrackRuntimeController());
        Set(track,"progressPath",path);track.BindRacer(racer);
        Check(Get<TrackProgressPath>(b.probe,"path")==path,"Race binding initializes guidance even without progress reporter");
        var slot=new GameObject("slot").transform;slot.position=new Vector3(5,6,7);slot.rotation=Quaternion.AngleAxis(90,Vector3.forward);
        Set(track,"gridSlots",new List<Transform>{slot});Check(track.PlaceRacerAtGridSlot(racer,0),"Grid placement succeeds");
        Check(!b.probe.HasSurface&&!Get<bool>(b.motor,"hasAcquiredSurface"),"Grid placement resets both attachment states");
        Near(Vector3.Dot(b.probe.LastSurfaceNormal,slot.up),1,"Grid reset uses Rigidbody orientation");
    }
}
