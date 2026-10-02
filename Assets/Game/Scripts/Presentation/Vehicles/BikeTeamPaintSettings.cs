using UnityEngine;
namespace RaceFatal.Presentation.Vehicles
{
    public sealed class BikeTeamPaintSettings : ScriptableObject
    {
        public Material sourceMaterial;
        public Texture2D paintMask;
        public Shader compositor;
        [Range(256,2048)] public int resolution = 1024;
        [Range(0,1)] public float accentStart = .44f;
        [Range(0,1)] public float accentWidth = .08f;
    }
}
