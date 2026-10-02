using System.Collections.Generic;
using RaceFatal.Vehicles;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    public sealed class BikeTeamPaint : MonoBehaviour
    {
        private sealed class Entry { public RenderTexture texture; public int users; }
        private static readonly Dictionary<string,Entry> cache = new Dictionary<string,Entry>();
        private string key;
        private readonly List<(Renderer renderer, int index, MaterialPropertyBlock before)> bindings = new List<(Renderer,int,MaterialPropertyBlock)>();
        public void Apply(BikeState bike)
        {
            Release();
            var settings=Resources.Load<BikeTeamPaintSettings>("BikeTeamPaintSettings");
            if(settings==null || settings.sourceMaterial==null || settings.paintMask==null || settings.compositor==null) return;
            if(!ColorUtility.TryParseHtmlString(bike.PrimaryColor,out var primary)) primary=Color.white;
            if(!ColorUtility.TryParseHtmlString(bike.SecondaryColor,out var secondary)) secondary=Color.gray;
            var targets=new List<(Renderer renderer,int index)>();
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++) if(materials[i]==settings.sourceMaterial) targets.Add((renderer,i));
            }
            if(targets.Count==0) return;
            key=ColorUtility.ToHtmlStringRGB(primary)+ColorUtility.ToHtmlStringRGB(secondary);
            if(!cache.TryGetValue(key,out var entry))
            {
                var texture=new RenderTexture(settings.resolution,settings.resolution,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                texture.name="TeamPaint_"+key; texture.useMipMap=true; texture.autoGenerateMips=true;
                texture.wrapMode=TextureWrapMode.Repeat; texture.Create();
                var material=new Material(settings.compositor);
                material.SetTexture("_PaintMask",settings.paintMask);
                material.SetColor("_Primary",primary.linear); material.SetColor("_Secondary",secondary.linear);
                material.SetFloat("_AccentStart",settings.accentStart); material.SetFloat("_AccentWidth",settings.accentWidth);
                var previous=RenderTexture.active;
                bool previousSrgb=GL.sRGBWrite;
                GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;
                Graphics.Blit(settings.sourceMaterial.GetTexture("_BaseColorMap"),texture,material);
                GL.sRGBWrite=previousSrgb; RenderTexture.active=previous;
                Destroy(material);
                entry=new Entry { texture=texture }; cache.Add(key,entry);
            }
            entry.users++;
            foreach(var target in targets)
            {
                var before=new MaterialPropertyBlock(); target.renderer.GetPropertyBlock(before,target.index);
                var block=new MaterialPropertyBlock(); target.renderer.GetPropertyBlock(block,target.index);
                block.SetTexture("_BaseColorMap",entry.texture);
                target.renderer.SetPropertyBlock(block,target.index);
                bindings.Add((target.renderer,target.index,before));
            }
        }
        private void Release()
        {
            foreach(var binding in bindings) if(binding.renderer!=null) binding.renderer.SetPropertyBlock(binding.before,binding.index);
            bindings.Clear();
            if(key!=null && cache.TryGetValue(key,out var entry) && --entry.users==0)
            { entry.texture.Release(); Destroy(entry.texture); cache.Remove(key); }
            key=null;
        }
        private void OnDestroy() => Release();
    }
}
