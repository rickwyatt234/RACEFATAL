using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    /// <summary>One whole-bike bubble, selected by remaining absolute shield capacity.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RacerViewController))]
    public sealed class ShieldBubbleEffectView : MonoBehaviour
    {
        [Header("Health Prefabs (High to Low)")]
        [SerializeField] private GameObject bluePrefab;
        [SerializeField] private GameObject tealPrefab;
        [SerializeField] private GameObject purplePrefab;
        [SerializeField] private GameObject redPrefab;
        [Header("Remaining Shield Thresholds")]
        [Range(0f, 1f)] [SerializeField] private float blueThreshold = 0.75f;
        [Range(0f, 1f)] [SerializeField] private float tealThreshold = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float purpleThreshold = 0.25f;
        [Header("Placement")]
        [Tooltip("Defaults to this bike. Prefab child offsets and scale are preserved.")]
        [SerializeField] private Transform bubbleOrigin;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [Header("Pulse Timing")]
        [Min(0.01f)] [SerializeField] private float visibleDuration = 0.65f;
        [Min(0.01f)] [SerializeField] private float breakDuration = 0.45f;
        [Header("Visibility")]
        [Tooltip("Multiplies the shield material's HDR tint/emission. Applied to runtime instances only; alpha stays unchanged.")]
        [Min(1f)] [SerializeField] private float brightnessMultiplier = 3f;

        private readonly GameObject[] instances = new GameObject[4];
        private readonly ParticleSystem[][] systems = new ParticleSystem[4][];
        private RacerViewController racerView;
        private Transform instanceRoot;
        private int activeBand = -1;
        private float remainingTime;
        private bool breaking;

        public bool IsConfigured => bluePrefab != null && tealPrefab != null &&
                                    purplePrefab != null && redPrefab != null;

        private void Awake()
        {
            racerView = GetComponent<RacerViewController>();
        }

        private void OnValidate()
        {
            purpleThreshold = Mathf.Clamp01(purpleThreshold);
            tealThreshold = Mathf.Clamp(tealThreshold, purpleThreshold, 1f);
            blueThreshold = Mathf.Clamp(blueThreshold, tealThreshold, 1f);
            visibleDuration = Mathf.Max(0.01f, visibleDuration);
            breakDuration = Mathf.Max(0.01f, breakDuration);
            brightnessMultiplier = Mathf.Max(1f, brightnessMultiplier);
        }

        public void Pulse(bool depleted)
        {
            if (!isActiveAndEnabled || !IsConfigured)
                return;
            if (racerView == null)
                racerView = GetComponent<RacerViewController>();
            var shield = racerView.Participant?.Vehicle?.EquipmentSystem?.Shield;
            if (shield == null || shield.BaseMaximum <= 0f)
                return;
            // A break is allowed to show red at zero; hull-only hits never call Pulse.
            breaking = depleted;
            remainingTime = depleted ? breakDuration : visibleDuration;
            ShowBand(depleted ? 3 : SelectBand(shield.Current / shield.BaseMaximum));
        }

        private int SelectBand(float fraction)
        {
            if (fraction > blueThreshold) return 0;
            if (fraction > tealThreshold) return 1;
            if (fraction > purpleThreshold) return 2;
            return 3;
        }

        private void Update()
        {
            if (activeBand < 0)
                return;
            remainingTime -= Time.deltaTime;
            var vehicle = racerView.Participant?.Vehicle;
            if (remainingTime <= 0f || vehicle == null || vehicle.IsDestroyed)
            {
                HideImmediate();
                return;
            }
            if (breaking)
                return;
            var shield = vehicle.EquipmentSystem?.Shield;
            if (shield == null || shield.Current <= 0f || shield.BaseMaximum <= 0f)
            {
                HideImmediate();
                return;
            }
            // Recharge/energy changes can also change the color during an active pulse.
            ShowBand(SelectBand(shield.Current / shield.BaseMaximum));
        }

        private void ShowBand(int band)
        {
            if (band == activeBand)
                return; // Repeated hits extend visibility without restarting the particles.
            StopActiveBand();
            EnsureInstance(band);
            activeBand = band;
            instances[band].SetActive(true);
            foreach (var particle in systems[band])
                particle.Play(false);
        }

        private void EnsureInstance(int band)
        {
            if (instances[band] != null)
                return;
            if (instanceRoot == null)
            {
                var root = new GameObject("Shield Bubble Effects");
                instanceRoot = root.transform;
                instanceRoot.SetParent(bubbleOrigin != null ? bubbleOrigin : transform, false);
                instanceRoot.localPosition = localOffset;
                instanceRoot.localScale = localScale;
            }
            // Instantiate under an inactive root so authored Play On Awake never flashes.
            instanceRoot.gameObject.SetActive(false);
            GameObject prefab = band == 0 ? bluePrefab : band == 1 ? tealPrefab :
                                band == 2 ? purplePrefab : redPrefab;
            var instance = Instantiate(prefab, instanceRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instances[band] = instance;
            systems[band] = instance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var particle in systems[band])
            {
                var main = particle.main;
                main.playOnAwake = false;
                main.stopAction = ParticleSystemStopAction.None;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.useUnscaledTime = false;
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            ApplyBrightness(instance);
            instance.SetActive(false);
            instanceRoot.gameObject.SetActive(true);
        }

        private void ApplyBrightness(GameObject instance)
        {
            // Tint is the HDR emission input on the authored Magic Shield shader.
            // Property blocks keep vendor materials and other effects sharing them intact.
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null) continue;
                    string property = material.HasProperty("_TintColor") ? "_TintColor" :
                        material.HasProperty("_EmissiveColor") ? "_EmissiveColor" :
                        material.HasProperty("_EmissionColor") ? "_EmissionColor" : null;
                    if (property == null) continue;
                    var color = material.GetColor(property);
                    color.r *= brightnessMultiplier;
                    color.g *= brightnessMultiplier;
                    color.b *= brightnessMultiplier;
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties, i);
                    properties.SetColor(property, color);
                    renderer.SetPropertyBlock(properties, i);
                }
            }
            foreach (var particle in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                var colors = main.startColor;
                // These prefabs use constant colors. Bring dark health-band colors
                // to full value without changing hue, alpha or authored gradients.
                if (colors.mode != ParticleSystemGradientMode.Color) continue;
                var color = colors.color;
                float value = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                if (value <= 0f || value >= 1f) continue;
                color.r /= value;
                color.g /= value;
                color.b /= value;
                main.startColor = color;
            }
        }

        private void StopActiveBand()
        {
            if (activeBand < 0)
                return;
            foreach (var particle in systems[activeBand])
                if (particle != null)
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (instances[activeBand] != null)
                instances[activeBand].SetActive(false); // Also hides authored lights.
            activeBand = -1;
        }

        public void HideImmediate()
        {
            StopActiveBand();
            remainingTime = 0f;
            breaking = false;
        }

        private void OnDisable() => HideImmediate();

        private void OnDestroy()
        {
            if (instanceRoot != null)
                Destroy(instanceRoot.gameObject);
        }
    }
}
