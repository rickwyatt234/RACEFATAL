using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Vehicles
{
    public sealed class AudienceEqualizerGraphic : MaskableGraphic
    {
        private float favor = 100f, animationTime;
        public void SetAudience(float value, float time)
        {
            favor = Mathf.Clamp(value, 0f, 200f);
            animationTime = time;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            const int bars = 20, segments = 10;
            float width = rect.width / bars, height = rect.height / segments;
            Color tint = favor < 100f ? Color.Lerp(new Color(1f, 0.18f, 0.32f), new Color(0.2f, 0.85f, 1f), favor / 100f) : Color.Lerp(new Color(0.2f, 0.85f, 1f), new Color(1f, 0.25f, 0.9f), (favor - 100f) / 100f);
            for (int bar = 0; bar < bars; bar++)
            {
                float pulse = 0.65f + 0.35f * Mathf.PerlinNoise(bar * 0.43f, animationTime * 3f);
                int lit = Mathf.RoundToInt(segments * favor / 200f * pulse);
                for (int segment = 0; segment < segments; segment++)
                {
                    float x = rect.xMin + bar * width, y = rect.yMin + segment * height;
                    Color cell = tint * color;
                    cell.a *= segment < lit ? 1f : 0.12f;
                    int start = mesh.currentVertCount;
                    mesh.AddVert(new Vector3(x, y), cell, Vector2.zero);
                    mesh.AddVert(new Vector3(x, y + height * 0.72f), cell, Vector2.zero);
                    mesh.AddVert(new Vector3(x + width * 0.65f, y + height * 0.72f), cell, Vector2.zero);
                    mesh.AddVert(new Vector3(x + width * 0.65f, y), cell, Vector2.zero);
                    mesh.AddTriangle(start, start + 1, start + 2);
                    mesh.AddTriangle(start, start + 2, start + 3);
                }
            }
        }
    }
}
