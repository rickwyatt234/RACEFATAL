using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class WeaponChargeEffectView : MonoBehaviour
    {
        [Header("Scale")]
        [SerializeField] private Transform scaleRoot;
        [Min(0f)][SerializeField] private float minimumScaleMultiplier = 0.2f;
        [Min(0f)][SerializeField] private float maximumScaleMultiplier = 1f;

        [Header("Lights")]
        [SerializeField] private Light[] lights;
        [Min(0f)][SerializeField] private float minimumLightMultiplier = 0.1f;
        [Min(0f)][SerializeField] private float maximumLightMultiplier = 1.25f;

        private Vector3 baseScale;
        private float[] baseLightIntensities;

        private void Awake()
        {
            if (scaleRoot == null)
                scaleRoot = transform;

            baseScale = scaleRoot.localScale;

            if (lights == null ||
                lights.Length == 0)
            {
                lights =
                    GetComponentsInChildren<Light>(true);
            }

            baseLightIntensities =
                new float[lights.Length];

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    baseLightIntensities[i] = lights[i].intensity;
            }

            SetCharge(0f);
        }

        public void SetCharge(float ratio)
        {
            ratio =
                Mathf.Clamp01(
                    ratio);

            float scaleMultiplier =
                Mathf.Lerp(
                    minimumScaleMultiplier,
                    maximumScaleMultiplier,
                    ratio);

            scaleRoot.localScale =
                baseScale *
                scaleMultiplier;

            float lightMultiplier =
                Mathf.Lerp(
                    minimumLightMultiplier,
                    maximumLightMultiplier,
                    ratio);

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null)
                    continue;

                lights[i].intensity =
                    baseLightIntensities[i] *
                    lightMultiplier;
            }
        }
    }
}