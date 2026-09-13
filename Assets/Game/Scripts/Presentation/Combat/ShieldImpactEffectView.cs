using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class ShieldImpactEffectView : MonoBehaviour
    {
        #region Settings

        [Header("Effect")]
        [Tooltip("The child object containing vfx_SciFiShield01 and its particle systems.")]
        [SerializeField] private GameObject effectRoot;

        [Header("Pulse")]
        [Tooltip("How long the shield effect is allowed to remain active after the most recent hit.")]
        [Min(0.01f)][SerializeField] private float visibleDuration = 0.35f;

        [Tooltip("If enabled, every incoming hit restarts the particle systems from the beginning.")]
        [SerializeField] private bool restartOnEveryHit = true;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugPlaying;
        [SerializeField] private float debugRemainingTime;
        [SerializeField] private int debugPulseCount;
        [SerializeField] private int debugParticleSystemCount;
        [SerializeField] private string debugLastPulse = "Never";

        #endregion

        private ParticleSystem[] particles;
        private float remainingTime;

        private void Awake()
        {
            if (effectRoot == null)
            {
                if (transform.childCount > 0)
                {
                    effectRoot =
                        transform.GetChild(0).gameObject;
                }
                else
                {
                    Debug.LogError(
                        $"ShieldImpactEffectView on '{name}' has no Effect Root.",
                        this);

                    return;
                }
            }

            /*
             * Keep the hierarchy active. Visibility is controlled
             * entirely by the ParticleSystems.
             */
            effectRoot.SetActive(true);

            CacheParticles();
            ClearParticles();
        }

        private void Update()
        {
            if (remainingTime <= 0f)
                return;

            remainingTime =
                Mathf.Max(
                    0f,
                    remainingTime - Time.deltaTime);

            debugRemainingTime =
                remainingTime;

            if (remainingTime > 0f)
                return;

            ClearParticles();
            debugPlaying = false;
        }

        public void Pulse()
        {
            if (effectRoot == null)
                return;

            if (particles == null ||
                particles.Length == 0)
            {
                Debug.LogWarning(
                    $"ShieldImpactEffectView '{name}' found no ParticleSystems.",
                    this);

                return;
            }

            if (!effectRoot.activeSelf)
                effectRoot.SetActive(true);

            remainingTime =
                visibleDuration;

            debugRemainingTime =
                remainingTime;

            debugPulseCount++;

            debugLastPulse =
                Time.time.ToString("F2");

            if (restartOnEveryHit)
            {
                RestartParticles();
            }
            else if (!AnyParticlePlaying())
            {
                PlayParticles();
            }

            debugPlaying = true;
        }

        [ContextMenu("Test Shield Pulse")]
        private void TestShieldPulse()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "Test Shield Pulse must be used during Play Mode.",
                    this);

                return;
            }

            Pulse();
        }

        [ContextMenu("Clear Shield Effect")]
        private void TestClear()
        {
            ClearParticles();

            remainingTime = 0f;
            debugRemainingTime = 0f;
            debugPlaying = false;
        }

        public void HideImmediate()
        {
            ClearParticles();

            remainingTime = 0f;
            debugRemainingTime = 0f;
            debugPlaying = false;
        }

        private void CacheParticles()
        {
            particles =
                effectRoot.GetComponentsInChildren<ParticleSystem>(
                    true);

            debugParticleSystemCount =
                particles != null
                    ? particles.Length
                    : 0;
        }

        private void RestartParticles()
        {
            /*
             * Clear every system independently.
             *
             * We deliberately use withChildren = false because
             * each cached system will be handled explicitly.
             */
            ClearParticles();

            PlayParticles();
        }

        private void PlayParticles()
        {
            if (particles == null)
                return;

            for (int i = 0;
                 i < particles.Length;
                 i++)
            {
                ParticleSystem particle =
                    particles[i];

                if (particle == null)
                    continue;

                /*
                 * Explicitly restart this exact particle system.
                 * This avoids depending on the root system to
                 * propagate playback correctly to purchased VFX.
                 */
                particle.Play(false);
            }
        }

        private void ClearParticles()
        {
            if (particles == null)
                return;

            for (int i = 0;
                 i < particles.Length;
                 i++)
            {
                ParticleSystem particle =
                    particles[i];

                if (particle == null)
                    continue;

                particle.Stop(
                    false,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear);

                particle.Clear(false);
            }
        }

        private bool AnyParticlePlaying()
        {
            if (particles == null)
                return false;

            for (int i = 0;
                 i < particles.Length;
                 i++)
            {
                ParticleSystem particle =
                    particles[i];

                if (particle != null &&
                    particle.isPlaying)
                {
                    return true;
                }
            }

            return false;
        }
    }
}