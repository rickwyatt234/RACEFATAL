using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class BurningStatusView : MonoBehaviour
    {
        private ParticleSystem flames;
        private float activeUntil;

        public void Refresh(
            float duration)
        {
            EnsureEffect();

            activeUntil =
                Mathf.Max(
                    activeUntil,
                    Time.time +
                    Mathf.Max(
                        0.05f,
                        duration));

            if (flames != null &&
                !flames.isPlaying)
            {
                flames.Play(true);
            }
        }

        private void Update()
        {
            if (flames == null ||
                Time.time <
                    activeUntil)
            {
                return;
            }

            if (flames.isPlaying)
                flames.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmitting);
        }

        private void EnsureEffect()
        {
            if (flames != null)
                return;

            GameObject effect =
                new GameObject(
                    "On Fire Status");

            effect.transform.SetParent(
                transform,
                false);

            effect.transform.localPosition =
                Vector3.up *
                0.45f;

            flames =
                effect.AddComponent<
                    ParticleSystem>();

            ParticleSystem.MainModule main =
                flames.main;

            main.loop = true;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    0.18f,
                    0.42f);

            main.startSpeed =
                new ParticleSystem.MinMaxCurve(
                    0.6f,
                    1.8f);

            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    0.12f,
                    0.35f);

            main.startColor =
                new ParticleSystem.MinMaxGradient(
                    new Color(
                        1f,
                        0.35f,
                        0.04f,
                        0.9f),
                    new Color(
                        1f,
                        0.85f,
                        0.15f,
                        0.75f));

            ParticleSystem.EmissionModule emission =
                flames.emission;

            emission.rateOverTime = 28f;

            ParticleSystem.ShapeModule shape =
                flames.shape;

            shape.shapeType =
                ParticleSystemShapeType.Box;

            shape.scale =
                new Vector3(
                    0.65f,
                    0.25f,
                    1.15f);

            ParticleSystemRenderer renderer =
                effect.GetComponent<
                    ParticleSystemRenderer>();

            if (renderer != null)
            {
                Shader shader =
                    Shader.Find(
                        "Universal Render Pipeline/Particles/Unlit");

                if (shader == null)
                    shader = Shader.Find(
                        "Particles/Standard Unlit");

                if (shader != null)
                {
                    renderer.material =
                        new Material(
                            shader);
                }
            }

            flames.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear);
        }
    }
}
