using System;
using System.Reflection;
using RaceFatal.Presentation.Combat;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using RaceFatal.Shared;
using UnityEngine;

static class Program
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static T Get<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void Invoke(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    static void Main()
    {
        var bike=new GameObject("Bike");
        var racer=bike.Add(new RacerViewController{Participant=new TestParticipant{Role=RaceParticipantRole.Player}});
        var camera=new GameObject("Camera").Add(new Camera());camera.transform.position=new Vector3(0,1,0);
        var canvas=new GameObject("Canvas",typeof(RectTransform)).Add(new Canvas{renderMode=RenderMode.ScreenSpaceCamera,worldCamera=camera});
        var cockpit=bike.Add(new PlayerCockpitView{CockpitCamera=camera,ReticleCanvas=canvas});
        var aim=bike.Add(new PlayerWeaponAim());
        var profile=new WeaponPresentationProfile();
        var presenter=new RaceWeaponPresenter{Profile=profile};aim.Initialize(presenter);
        var muzzle=new GameObject("Muzzle").transform;
        racer.Mount=new BikeEquipmentMountBinding{EquipmentOrigin=muzzle};
        racer.Participant.Vehicle.EquipmentSystem.SelectedWeaponDefinition=new TestWeapon{AimMode=WeaponAimMode.Forward,DeliveryMode=WeaponDeliveryMode.Projectile};
        camera.transform.rotation=Quaternion.AngleAxis(15,new Vector3(1,0,0));
        Check(aim.TryGetReticleAimDirection(muzzle,300,~0,profile,out var direction),"Player gun produces aim direction");
        Check(Vector3.Dot(direction,Vector3.forward)>.999999f,"Downward camera framing does not pitch forward gun aim");
        foreach(float pitch in new[]{0f,10f,25f})
            foreach(float roll in new[]{-15f,15f})
            {
                camera.transform.rotation=Quaternion.AngleAxis(roll,Vector3.forward)*Quaternion.AngleAxis(pitch,new Vector3(1,0,0));
                aim.TryGetReticleAimDirection(muzzle,300,~0,profile,out direction);
                Check(Vector3.Dot(direction,Vector3.forward)>.999999f,"Camera pitch and roll preserve chassis-forward gun aim");
            }
        camera.transform.rotation=Quaternion.AngleAxis(15,new Vector3(1,0,0));
        Check(aim.GetReticleViewport(muzzle,300,profile).y>.5f,"Bike-forward reticle moves above framed camera center");
        camera.fieldOfView=82;
        aim.TryGetReticleAimDirection(muzzle,300,~0,profile,out direction);
        Check(Vector3.Dot(direction,Vector3.forward)>.999999f,"Boost FOV preserves physical forward aim");
        Set(profile,"reticleViewportOffset",new Vector2(.07f,.08f));
        aim.TryGetReticleAimDirection(muzzle,300,~0,profile,out direction);
        Check(direction.x>.05f && direction.y>.05f,"Weapon offset moves both reticle and aim");
        Set(profile,"reticlePlacement",WeaponReticlePlacement.FixedViewport);
        Set(profile,"reticleViewportPosition",new Vector2(.4f,.65f));Set(profile,"reticleViewportOffset",Vector2.zero);
        var viewport=aim.GetReticleViewport(muzzle,300,profile);
        Check(MathF.Abs(viewport.x-.4f)<.00001f && MathF.Abs(viewport.y-.65f)<.00001f,"Fixed placement uses authored viewport point");
        aim.TryGetReticleAimDirection(muzzle,300,~0,profile,out direction);
        var ray=camera.ViewportPointToRay(new Vector3(.4f,.65f,0));
        var expected=(ray.origin+ray.direction*Vector3.Distance(ray.origin,muzzle.position+Vector3.forward*300)-muzzle.position).normalized;
        Check(Vector3.Dot(direction,expected)>.999999f,"Gun converges on center of fixed reticle");
        Set(profile,"reticlePlacement",WeaponReticlePlacement.BikeForward);
        aim.TryGetAimDirection(muzzle,300,~0,out direction);
        Check(direction.y<-.1f,"Legacy special/locking camera-centered path preserved");
        Physics.Hits=new[]{
            new RaycastHit{collider=new Collider{owner=racer},distance=1,point=new Vector3(0,0,1)},
            new RaycastHit{collider=new Collider(),distance=40,point=new Vector3(2,2,40)},
            new RaycastHit{collider=new Collider(),distance=20,point=new Vector3(1,1,20)}};
        aim.TryGetReticleAimDirection(muzzle,300,~0,profile,out direction);
        Check(Vector3.Dot(direction,new Vector3(1,1,20).normalized)>.999999f,"Nearest non-owner ray hit is convergence point");
        Physics.Hits=new[]{new RaycastHit{collider=new Collider(),distance=1,point=new Vector3(0,0,-1)}};
        aim.TryGetReticleAimDirection(muzzle,300,~0,profile,out direction);
        Check(direction.z>.99f,"Camera obstruction behind muzzle cannot reverse gun direction");Physics.Hits=Array.Empty<RaycastHit>();
        foreach(var mode in new[]{WeaponDeliveryMode.Hitscan,WeaponDeliveryMode.Projectile,WeaponDeliveryMode.ConeProjectile})
            Check(PlayerWeaponAim.UsesGunReticle(WeaponAimMode.Forward,mode),"Forward gun supports reticle");
        foreach(var mode in new[]{WeaponDeliveryMode.GuidedProjectile,WeaponDeliveryMode.Dropped,WeaponDeliveryMode.Ram,WeaponDeliveryMode.FlameCone})
            Check(!PlayerWeaponAim.UsesGunReticle(WeaponAimMode.Forward,mode),"Special delivery excluded from gun reticle");
        Check(!PlayerWeaponAim.UsesGunReticle(WeaponAimMode.RearTargeted,WeaponDeliveryMode.Projectile),"Rear turret has no forward reticle");
        Invoke(aim,"LateUpdate");var root=Get<RectTransform>(aim,"reticleRoot");
        Check(root.gameObject.activeSelf,"Selected forward gun shows reticle");
        Set(profile,"reticleSize",new Vector2(56,56));Set(profile,"reticleSprite",new Sprite());Invoke(aim,"LateUpdate");
        Check(root.sizeDelta.x==56 && Get<UnityEngine.UI.Image>(aim,"reticleImage").enabled && !Get<RectTransform>(aim,"fallbackRoot").gameObject.activeSelf,"Selected weapon applies custom sprite and size");
        racer.Participant.Vehicle.EquipmentSystem.SelectedWeaponDefinition.DeliveryMode=WeaponDeliveryMode.GuidedProjectile;
        Invoke(aim,"LateUpdate");Check(!root.gameObject.activeSelf,"Switching to missiles hides gun crosshair");
        racer.Participant.Vehicle.EquipmentSystem.SelectedWeaponDefinition.DeliveryMode=WeaponDeliveryMode.Projectile;
        canvas.enabled=false;Invoke(aim,"LateUpdate");Check(!root.gameObject.activeSelf,"Outcome canvas hiding is respected");canvas.enabled=true;
        cockpit.IsDeathViewActive=true;Check(!aim.TryGetReticleAimDirection(muzzle,300,~0,profile,out direction),"Death camera cannot aim guns");cockpit.IsDeathViewActive=false;
        racer.Participant.Role=RaceParticipantRole.Opponent;
        Check(!aim.TryGetReticleAimDirection(muzzle,300,~0,profile,out direction),"AI never uses player's reticle aim");

        var motor=bike.Add(new BikeMotor{SpeedMetersPerSecond=40,SteeringInput=.005f});
        var lean=bike.Add(new BikeLeanController());
        var visual=new GameObject("Visual").transform;
        var baseRotation=Quaternion.AngleAxis(8,Vector3.forward);visual.localRotation=baseRotation;
        Set(lean,"visualLeanRoot",visual);Invoke(lean,"Awake");
        for(int frame=0;frame<100;frame++)
        {bike.transform.rotation=Quaternion.AngleAxis((frame+1)*.5f,Vector3.up);Invoke(lean,"LateUpdate");}
        float angle=Get<float>(lean,"currentLean");
        Check(angle < -10 && angle >= -16.001f,"AI banks visibly with small steering and remains within configured limit");
        RacePauseController.IsGameplayBlocked=true;bike.transform.rotation=Quaternion.AngleAxis(80,Vector3.up);Invoke(lean,"LateUpdate");
        Check(Get<float>(lean,"currentLean")==angle,"Paused gameplay freezes visual lean");RacePauseController.IsGameplayBlocked=false;
        Invoke(lean,"OnDisable");Check(MathF.Abs(Get<float>(lean,"currentLean"))<.001f && Vector3.Dot(visual.localRotation*Vector3.up,baseRotation*Vector3.up)>.999999f,"Disable restores authored visual rotation");
        bike.transform.rotation=Quaternion.identity;Invoke(lean,"Awake");
        for(int frame=0;frame<100;frame++){bike.transform.rotation=Quaternion.AngleAxis(-(frame+1)*.5f,Vector3.up);Invoke(lean,"LateUpdate");}
        Check(Get<float>(lean,"currentLean")>10,"Left corner banks in the opposite direction");
        for(int frame=0;frame<100;frame++)Invoke(lean,"LateUpdate");
        Check(MathF.Abs(Get<float>(lean,"currentLean"))<.05f,"AI smoothly returns upright on straights");
        Invoke(lean,"OnDisable");bike.transform.rotation=Quaternion.identity;Invoke(lean,"Awake");motor.SpeedMetersPerSecond=0;
        for(int frame=0;frame<20;frame++){bike.transform.rotation=Quaternion.AngleAxis(frame,Vector3.up);Invoke(lean,"LateUpdate");}
        Check(MathF.Abs(Get<float>(lean,"currentLean"))<.001f,"Stopped AI stays upright");
        Invoke(lean,"OnDisable");racer.Participant.Role=RaceParticipantRole.Player;motor.SpeedMetersPerSecond=40;motor.SteeringInput=.5f;
        for(int frame=0;frame<100;frame++)Invoke(lean,"LateUpdate");
        Check(MathF.Abs(Get<float>(lean,"currentLean")+24)<.01f,"Player retains existing steering-based lean");
        Console.WriteLine($"PASS: {checks} weapon-reticle and AI-lean checks (presentation doubles)");
    }
}
