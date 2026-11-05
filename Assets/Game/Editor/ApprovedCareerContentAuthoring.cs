using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Content;
using RaceFatal.Content.Vehicles;
using RaceFatal.Content.Equipment;
using RaceFatal.Shared;
using UnityEditor;
using UnityEngine;

namespace RaceFatal.Editor
{
    /// <summary>
    /// Opt-in authoring for approved vehicle/equipment archetypes. Preserves any already
    /// registered definition with the same ID and never overwrites its tuning or artwork.
    /// All generated numbers are provisional. Track art and advanced utility logic are separate.
    /// </summary>
    public static class ApprovedCareerContentAuthoring
    {
        private const string Root = "Assets/Game/GeneratedApprovedContent";
        [MenuItem("RACE//FATAL/Career/Generate Approved Vehicle and Equipment Definitions")]
        public static void Generate()
        {
            var catalog = Selection.activeObject as GameContentCatalogSO;
            if (catalog == null)
            {
                EditorUtility.DisplayDialog("RACE//FATAL", "Select the GameContentCatalogSO asset first. Generation will not change an unidentified catalog.", "OK");
                return;
            }
            int count=0;
            var fallbackPrefab = catalog.BikeDefinitions.FirstOrDefault(b=>b!=null && b.BikePrefab!=null)?.BikePrefab;
            var bikes = new (string name,int small,int medium,int large)[]
            {
                ("HARBINGER",1,2,2),("STELLA",2,2,1),("CASTLE",2,2,2),
                ("WIRE",3,2,1),("SERAPH",2,3,0),("KATANA",1,3,1),
                ("CAFE",2,2,0),("GOBLIN",3,2,0),("DEMEANOR",2,1,1)
            };
            foreach(var b in bikes)
            {
                string id="rf_bike_"+b.name.ToLowerInvariant();
                count+=Author<BikeDefinitionSO>(catalog,"bikeDefinitions","Bikes",id,b.name,so=>{
                    Put(so,"smallNodeCount",b.small);Put(so,"mediumNodeCount",b.medium);Put(so,"largeNodeCount",b.large);
                    Put(so,"bikePrefab",fallbackPrefab);
                });
            }

            string[] engines={"Combustion","Gaussian","Plasma","Flux","Turbine","Atomic"};
            foreach(var family in engines)
            for(int mk=1;mk<=4;mk++)
            {
                int generation=mk;
                string id="rf_engine_"+family.ToLowerInvariant()+"_mk"+generation;
                float speed=new[]{0f,165f,205f,250f,300f}[generation]*(family=="Turbine"?1.07f:family=="Atomic"?1.04f:1f);
                float acceleration=new[]{0f,13f,17f,22f,28f}[generation]*(family=="Gaussian"?1.05f:family=="Combustion"?1.02f:1f);
                int cost=new[]{0,2200,5500,12000,24000}[generation];
                count+=Author<EngineDefinitionSO>(catalog,"engineDefinitions","Engines",id,family+" Mk "+generation,so=>{
                    Put(so,"engineClass",generation==1?EngineClass.Class5:generation==2?EngineClass.Class3:generation==3?EngineClass.Class2:EngineClass.Class1);
                    Put(so,"topSpeed",speed);Put(so,"acceleration",acceleration);Put(so,"creditCost",cost);
                });
            }

            var chassis = new [] { "Composite Mk I","Composite Mk II","Monofilament Mk I","Monofilament Mk II",
                "Amorphous Alloy Mk I","Amorphous Alloy Mk II","Magnetically Braced Mk I",
                "Magnetically Braced Mk II","Non-Newtonian","Phase-Adaptive Experimental","Self-Bonding Nanocomposite Experimental" };
            for(int i=0;i<chassis.Length;i++)
            {
                int index=i;
                string id="rf_chassis_"+Slug(chassis[i]);
                count+=Author<ChassisDefinitionSO>(catalog,"chassisDefinitions","Chassis",id,chassis[i],so=>{
                    Put(so,"mass",Mathf.Max(120f,275f-index*12f));
                    Put(so,"maxIntegrity",100f+index*9f);
                    Put(so,"handlingMultiplier",1f+(index%3)*.03f);
                    Put(so,"steeringResponseMultiplier",1f+(index%4)*.03f);
                    Put(so,"leanResponseMultiplier",1f+(index%3)*.03f);
                    Put(so,"stabilityMultiplier",1f+(index%4)*.02f);
                    Put(so,"impactResistance",Mathf.Min(.3f,.04f+index*.018f));
                    Put(so,"creditCost",1200+index*1200);
                });
            }

            string[] boosters={"Chemical Mk I","Chemical Mk II","Plasma Mk I","Plasma Mk II",
                "Ion Mk I","Ion Mk II","Turbine Mk I","Turbine Mk II",
                "Magnetic Mk I","Magnetic Mk II","Fusion Experimental"};
            for(int i=0;i<boosters.Length;i++)
            {
                int n=i;string id="rf_booster_"+Slug(boosters[i]);
                bool upgraded=i%2==1, experimental=i==10;
                count+=Author<BoosterDefinitionSO>(catalog,"equipmentDefinitions","Boosters",id,boosters[i],so=>{
                    Put(so,"requiredNodeSize",experimental?NodeSize.Large:NodeSize.Medium);
                    Put(so,"energyCapacity",experimental?1650f:upgraded?1250f:1000f);
                    Put(so,"energyPerSecond",experimental?150f:upgraded?125f:95f);
                    Put(so,"capacityStrainPerSecond",experimental?10f:8f);
                    Put(so,"speedMultiplier",experimental?1.25f:upgraded?1.19f:1.14f);
                    Put(so,"accelerationMultiplier",experimental?1.42f:upgraded?1.32f:1.25f);
                    Put(so,"creditCost",900+n*900);
                });
            }

            string[] shields={"Standard Mk I","Standard Mk II","Rapid-Cycle Mk I","Rapid-Cycle Mk II",
                "High-Capacity Mk I","High-Capacity Mk II","Regenerative Mk I","Regenerative Mk II",
                "Frequency-Adaptive Experimental","Ablative Mk I","Ablative Mk II"};
            for(int i=0;i<shields.Length;i++)
            {
                int n=i;bool ablative=i>=9;
                string id="rf_shield_"+Slug(shields[i]);
                count+=Author<ShieldDefinitionSO>(catalog,"equipmentDefinitions","Shields",id,shields[i],so=>{
                    Put(so,"requiredNodeSize",NodeSize.Medium);
                    Put(so,"capacity",ablative?400f+n*15f:120f+n*25f);
                    Put(so,"rechargePerSecond",ablative?0f:12f+n*2f);
                    Put(so,"rechargeDelay",ablative?999999f:i==2||i==3?2f:4f);
                    Put(so,"creditCost",1100+n*900);
                });
            }

            var weapons = new (string name, WeaponAimMode aim, WeaponDeliveryMode delivery,NodeSize node,EquipmentActivationMode activation)[]
            {
                ("Ballistic Machine Gun Mk I",WeaponAimMode.Forward,WeaponDeliveryMode.Hitscan,NodeSize.Small,EquipmentActivationMode.Hold),
                ("Ballistic Machine Gun Mk II",WeaponAimMode.Forward,WeaponDeliveryMode.Hitscan,NodeSize.Small,EquipmentActivationMode.Hold),
                ("Electromagnetic Machine Gun",WeaponAimMode.Forward,WeaponDeliveryMode.Hitscan,NodeSize.Medium,EquipmentActivationMode.Hold),
                ("Flechette Machine Gun",WeaponAimMode.Forward,WeaponDeliveryMode.ConeProjectile,NodeSize.Medium,EquipmentActivationMode.Hold),
                ("Ballistic Shotgun Mk I",WeaponAimMode.Forward,WeaponDeliveryMode.ConeProjectile,NodeSize.Medium,EquipmentActivationMode.Press),
                ("Ballistic Shotgun Mk II",WeaponAimMode.Forward,WeaponDeliveryMode.ConeProjectile,NodeSize.Medium,EquipmentActivationMode.Press),
                ("Plasma Shotgun",WeaponAimMode.Forward,WeaponDeliveryMode.ConeProjectile,NodeSize.Medium,EquipmentActivationMode.Press),
                ("Ballistic Railgun Mk I",WeaponAimMode.Forward,WeaponDeliveryMode.Hitscan,NodeSize.Large,EquipmentActivationMode.ChargeRelease),
                ("Ballistic Railgun Mk II",WeaponAimMode.Forward,WeaponDeliveryMode.Hitscan,NodeSize.Large,EquipmentActivationMode.ChargeRelease),
                ("Particle Railgun",WeaponAimMode.Forward,WeaponDeliveryMode.Hitscan,NodeSize.Large,EquipmentActivationMode.ChargeRelease),
                ("Guided Rocket Mk I",WeaponAimMode.Targeted,WeaponDeliveryMode.GuidedProjectile,NodeSize.Large,EquipmentActivationMode.Press),
                ("Guided Rocket Mk II",WeaponAimMode.Targeted,WeaponDeliveryMode.GuidedProjectile,NodeSize.Large,EquipmentActivationMode.Press),
                ("Cluster Rocket",WeaponAimMode.Targeted,WeaponDeliveryMode.GuidedProjectile,NodeSize.Large,EquipmentActivationMode.Press),
                ("Penetrator Rocket",WeaponAimMode.Targeted,WeaponDeliveryMode.GuidedProjectile,NodeSize.Large,EquipmentActivationMode.Press),
                ("Ballistic Mine Mk I",WeaponAimMode.RearDrop,WeaponDeliveryMode.Dropped,NodeSize.Small,EquipmentActivationMode.Press),
                ("Ballistic Mine Mk II",WeaponAimMode.RearDrop,WeaponDeliveryMode.Dropped,NodeSize.Small,EquipmentActivationMode.Press),
                ("Incendiary Mine",WeaponAimMode.RearDrop,WeaponDeliveryMode.Dropped,NodeSize.Medium,EquipmentActivationMode.Press),
                ("EMP Mine",WeaponAimMode.RearDrop,WeaponDeliveryMode.Dropped,NodeSize.Medium,EquipmentActivationMode.Press),
                ("Rear Auto Turret Mk I",WeaponAimMode.RearTargeted,WeaponDeliveryMode.Hitscan,NodeSize.Medium,EquipmentActivationMode.Passive),
                ("Rear Auto Turret Mk II",WeaponAimMode.RearTargeted,WeaponDeliveryMode.Hitscan,NodeSize.Medium,EquipmentActivationMode.Passive),
                ("Electromagnetic Rear Turret",WeaponAimMode.RearTargeted,WeaponDeliveryMode.Hitscan,NodeSize.Medium,EquipmentActivationMode.Passive),
                ("Laser Rear Turret",WeaponAimMode.RearTargeted,WeaponDeliveryMode.Hitscan,NodeSize.Medium,EquipmentActivationMode.Passive),
                ("Chemical Flamethrower Mk I",WeaponAimMode.ForwardAndSideways,WeaponDeliveryMode.FlameCone,NodeSize.Medium,EquipmentActivationMode.Hold),
                ("Chemical Flamethrower Mk II",WeaponAimMode.ForwardAndSideways,WeaponDeliveryMode.FlameCone,NodeSize.Medium,EquipmentActivationMode.Hold),
                ("Plasma Flamethrower",WeaponAimMode.ForwardAndSideways,WeaponDeliveryMode.FlameCone,NodeSize.Large,EquipmentActivationMode.Hold),
                ("Corrosive Projector",WeaponAimMode.ForwardAndSideways,WeaponDeliveryMode.FlameCone,NodeSize.Large,EquipmentActivationMode.Hold)
            };
            for(int i=0;i<weapons.Length;i++)
            {
                var w=weapons[i];int n=i;
                count+=Author<WeaponDefinitionSO>(catalog,"equipmentDefinitions","Weapons","rf_weapon_"+Slug(w.name),w.name,so=>{
                    Put(so,"requiredNodeSize",w.node);Put(so,"activationMode",w.activation);
                    Put(so,"aimMode",w.aim);Put(so,"deliveryMode",w.delivery);
                    Put(so,"range",w.delivery==WeaponDeliveryMode.FlameCone?28f:w.delivery==WeaponDeliveryMode.Dropped?8f:150f);
                    Put(so,"damage",w.delivery==WeaponDeliveryMode.Hitscan?10f:22f+n*.7f);
                    Put(so,"projectileSpeed",w.delivery==WeaponDeliveryMode.GuidedProjectile?100f:180f);
                    Put(so,"energyPerActivation",w.activation==EquipmentActivationMode.Hold?2f:25f);
                    Put(so,"fireInterval",w.activation==EquipmentActivationMode.Hold?.13f:1.2f);
                    Put(so,"chargeDuration",w.activation==EquipmentActivationMode.ChargeRelease?1f:0f);
                    Put(so,"targetLockDuration",w.delivery==WeaponDeliveryMode.GuidedProjectile?.8f:0f);
                    Put(so,"projectileCount",w.delivery==WeaponDeliveryMode.ConeProjectile?7:1);
                    Put(so,"spreadAngle",w.delivery==WeaponDeliveryMode.ConeProjectile?25f:0f);
                    Put(so,"explosionRadius",w.delivery==WeaponDeliveryMode.Dropped?6f:2f);
                    Put(so,"armingDelay",w.delivery==WeaponDeliveryMode.Dropped?.5f:0f);
                    Put(so,"exposureDuration",w.delivery==WeaponDeliveryMode.FlameCone?.8f:0f);
                    Put(so,"effectDuration",w.delivery==WeaponDeliveryMode.FlameCone?3f:0f);
                    Put(so,"statusDamagePerSecond",w.delivery==WeaponDeliveryMode.FlameCone?6f:0f);
                    Put(so,"creditCost",1000+n*400);
                });
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("RACE//FATAL", "Added "+count+" missing approved definitions. Existing IDs and assets were preserved. Generated numbers and generic bike prefab references are placeholders. Advanced utilities, tracks and teams need their respective gameplay/scene implementation.", "OK");
        }

        private static string Slug(string name) => new string(name.ToLowerInvariant().Select(c=>char.IsLetterOrDigit(c)?c:'_').ToArray()).Trim('_');
        private static int Author<T>(GameContentCatalogSO catalog,string collection,string folder,string id,string title,Action<SerializedObject> configure) where T:ScriptableObject
        {
            var serializedCatalog=new SerializedObject(catalog);
            var list=serializedCatalog.FindProperty(collection);
            for(int i=0;i<list.arraySize;i++)
            {
                var existing=list.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableObject;
                if(existing!=null)
                {
                    var stored = new SerializedObject(existing);
                    var existingName=stored.FindProperty("displayName");
                    if((string)existing.GetType().GetProperty("Id")?.GetValue(existing)==id || (existingName!=null && existingName.stringValue==title))return 0;
                }
            }
            string directory=Root+"/"+folder;
            if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/Game","GeneratedApprovedContent");
            if(!AssetDatabase.IsValidFolder(directory))AssetDatabase.CreateFolder(Root,folder);
            string assetPath=directory+"/"+id+".asset";
            var asset=AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if(asset==null)
            {
                asset=ScriptableObject.CreateInstance<T>();
                asset.name=title;
                AssetDatabase.CreateAsset(asset,assetPath);
            }
            var so=new SerializedObject(asset);
            Put(so,"id",id);Put(so,"displayName",title);configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize-1).objectReferenceValue=asset;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }
        private static void Put(SerializedObject obj,string name,string value){var p=obj.FindProperty(name);if(p!=null)p.stringValue=value;}
        private static void Put(SerializedObject obj,string name,int value){var p=obj.FindProperty(name);if(p!=null)p.intValue=value;}
        private static void Put(SerializedObject obj,string name,float value){var p=obj.FindProperty(name);if(p!=null)p.floatValue=value;}
        private static void Put(SerializedObject obj,string name,Enum value){var p=obj.FindProperty(name);if(p!=null)p.enumValueIndex=Convert.ToInt32(value);}
        private static void Put(SerializedObject obj,string name,UnityEngine.Object value){var p=obj.FindProperty(name);if(p!=null)p.objectReferenceValue=value;}
    }
}
