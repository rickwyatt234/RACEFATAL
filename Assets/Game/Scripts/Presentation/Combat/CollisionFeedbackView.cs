using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using UnityEngine;
using UnityEngine.Audio;

namespace RaceFatal.Presentation.Combat
{
    public enum CollisionImpactType
    {
        TrackWall,
        Racer
    }

    [RequireComponent(typeof(RacerViewController))]
    public class CollisionFeedbackView : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource impactAudioSource;
        [SerializeField] private AudioMixerGroup impactGroup;

        [Header("Wall Audio")]
        [SerializeField] private AudioClip[] wallLightClips;
        [SerializeField] private AudioClip[] wallMediumClips;
        [SerializeField] private AudioClip[] wallHeavyClips;

        [Header("Racer Audio")]
        [SerializeField] private AudioClip[] racerLightClips;
        [SerializeField] private AudioClip[] racerMediumClips;
        [SerializeField] private AudioClip[] racerHeavyClips;

        [Header("Audio Severity")]
        [Range(0f, 1f)][SerializeField] private float mediumThreshold = 0.35f;
        [Range(0f, 1f)][SerializeField] private float heavyThreshold = 0.7f;
        [Range(0f, 1f)][SerializeField] private float minimumImpactVolume = 0.3f;
        [Range(0f, 1f)][SerializeField] private float maximumImpactVolume = 1f;
        [Range(0f, 0.25f)][SerializeField] private float pitchVariation = 0.06f;

        [Header("Player / Opponent Mix")]
        [Range(0f, 2f)][SerializeField] private float playerVolumeMultiplier = 1f;
        [Range(0f, 2f)][SerializeField] private float opponentVolumeMultiplier = 0.55f;
        [Range(0f, 1f)][SerializeField] private float playerSpatialBlend = 0.25f;
        [Range(0f, 1f)][SerializeField] private float opponentSpatialBlend = 1f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem wallImpactPrefab;
        [SerializeField] private ParticleSystem racerImpactPrefab;
        [SerializeField] private ParticleSystem heavyImpactPrefab;
        [SerializeField] private bool alignParticlesToNormal = true;
        [SerializeField] private bool scaleParticlesBySeverity = true;
        [Min(0.01f)][SerializeField] private float minimumParticleScale = 0.65f;
        [Min(0.01f)][SerializeField] private float maximumParticleScale = 1.25f;
        [Min(0.1f)][SerializeField] private float particleLifetimeFallback = 3f;

        [Header("Player Camera")]
        [SerializeField] private bool enablePlayerCameraKick = true;
        [Range(0f, 2f)][SerializeField] private float cameraSeverityMultiplier = 1f;

        [Header("Optional Player Screen Flash")]
        [Tooltip("Optional full-screen CanvasGroup. Leave empty if you do not want collision flash.")]
        [SerializeField] private CanvasGroup impactFlashGroup;
        [Range(0f, 1f)][SerializeField] private float minimumFlashAlpha = 0.05f;
        [Range(0f, 1f)][SerializeField] private float maximumFlashAlpha = 0.3f;
        [Min(0.01f)][SerializeField] private float flashFadeSpeed = 5f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugIsPlayer;
        [SerializeField] private string debugImpactType = "None";
        [SerializeField] private string debugImpactTier = "None";
        [SerializeField] private float debugSeverity;
        [SerializeField] private float debugAudioVolume;
        [SerializeField] private string debugMixerGroup = "Unassigned";

        private RacerViewController racerView;
        private PlayerCockpitView cockpitView;

        private bool roleResolved;
        private bool isPlayer;

        private void Awake()
        {
            racerView = GetComponent<RacerViewController>();
            cockpitView = GetComponent<PlayerCockpitView>();

            if (impactAudioSource != null)
            {
                impactAudioSource.playOnAwake = false;
                impactAudioSource.loop = false;

                if (impactGroup != null)
                {
                    impactAudioSource.outputAudioMixerGroup = impactGroup;
                    debugMixerGroup = impactGroup.name;
                }
            }

            if (impactFlashGroup != null)
                impactFlashGroup.alpha = 0f;
        }

        private void Update()
        {
            UpdateFlash();
        }

        private void OnDestroy()
        {
            if (impactFlashGroup != null)
                impactFlashGroup.alpha = 0f;
        }

        public void PlayImpact(
            CollisionImpactType type,
            Vector3 contactPoint,
            Vector3 contactNormal,
            float severity,
            bool catastrophic = false)
        {
            ResolveRole();

            severity = Mathf.Clamp01(severity);

            if (catastrophic)
                severity = 1f;

            debugImpactType = type.ToString();
            debugSeverity = severity;

            PlayImpactAudio(type, severity);
            SpawnImpactParticles(type, contactPoint, contactNormal, severity);

            if (isPlayer)
            {
                ApplyCameraKick(contactNormal, severity);
                ApplyScreenFlash(severity);
            }
        }

        #region Role

        private void ResolveRole()
        {
            if (roleResolved)
                return;

            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null)
            {
                return;
            }

            isPlayer =
                racerView.Participant.Role ==
                RaceParticipantRole.Player;

            roleResolved = true;
            debugIsPlayer = isPlayer;

            if (impactAudioSource != null)
            {
                impactAudioSource.spatialBlend =
                    isPlayer
                        ? playerSpatialBlend
                        : opponentSpatialBlend;
            }
        }

        #endregion

        #region Audio

        private void PlayImpactAudio(
            CollisionImpactType type,
            float severity)
        {
            if (impactAudioSource == null)
                return;

            AudioClip[] clips =
                GetClipSet(
                    type,
                    severity);

            if (!TryGetRandomClip(
                    clips,
                    out AudioClip clip))
            {
                return;
            }

            float roleMultiplier =
                isPlayer
                    ? playerVolumeMultiplier
                    : opponentVolumeMultiplier;

            float volume =
                Mathf.Lerp(
                    minimumImpactVolume,
                    maximumImpactVolume,
                    severity) *
                roleMultiplier;

            volume = Mathf.Clamp01(volume);

            impactAudioSource.pitch =
                Random.Range(
                    1f - pitchVariation,
                    1f + pitchVariation);

            impactAudioSource.PlayOneShot(
                clip,
                volume);

            debugAudioVolume = volume;
        }

        private AudioClip[] GetClipSet(
            CollisionImpactType type,
            float severity)
        {
            if (severity >= heavyThreshold)
            {
                debugImpactTier = "Heavy";

                return type == CollisionImpactType.TrackWall
                    ? wallHeavyClips
                    : racerHeavyClips;
            }

            if (severity >= mediumThreshold)
            {
                debugImpactTier = "Medium";

                return type == CollisionImpactType.TrackWall
                    ? wallMediumClips
                    : racerMediumClips;
            }

            debugImpactTier = "Light";

            return type == CollisionImpactType.TrackWall
                ? wallLightClips
                : racerLightClips;
        }

        private bool TryGetRandomClip(
            AudioClip[] clips,
            out AudioClip clip)
        {
            clip = null;

            if (clips == null ||
                clips.Length == 0)
            {
                return false;
            }

            int validCount = 0;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    validCount++;
            }

            if (validCount == 0)
                return false;

            int target =
                Random.Range(
                    0,
                    validCount);

            int current = 0;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;

                if (current == target)
                {
                    clip = clips[i];
                    return true;
                }

                current++;
            }

            return false;
        }

        #endregion

        #region Particles

        private void SpawnImpactParticles(
            CollisionImpactType type,
            Vector3 position,
            Vector3 normal,
            float severity)
        {
            ParticleSystem basePrefab =
                type == CollisionImpactType.TrackWall
                    ? wallImpactPrefab
                    : racerImpactPrefab;

            SpawnParticle(
                basePrefab,
                position,
                normal,
                severity);

            if (severity >= heavyThreshold &&
                heavyImpactPrefab != null)
            {
                SpawnParticle(
                    heavyImpactPrefab,
                    position,
                    normal,
                    severity);
            }
        }

        private void SpawnParticle(
            ParticleSystem prefab,
            Vector3 position,
            Vector3 normal,
            float severity)
        {
            if (prefab == null)
                return;

            if (normal.sqrMagnitude < 0.001f)
                normal = transform.up;

            Quaternion rotation =
                alignParticlesToNormal
                    ? Quaternion.LookRotation(normal.normalized)
                    : Quaternion.identity;

            ParticleSystem effect =
                Instantiate(
                    prefab,
                    position,
                    rotation);

            if (scaleParticlesBySeverity)
            {
                float scale =
                    Mathf.Lerp(
                        minimumParticleScale,
                        maximumParticleScale,
                        severity);

                effect.transform.localScale *= scale;
            }

            effect.Play(true);

            Destroy(
                effect.gameObject,
                GetParticleLifetime(effect));
        }

        private float GetParticleLifetime(
            ParticleSystem root)
        {
            if (root == null)
                return particleLifetimeFallback;

            ParticleSystem[] systems =
                root.GetComponentsInChildren<
                    ParticleSystem>(true);

            float longest = 0f;

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];

                if (system == null)
                    continue;

                ParticleSystem.MainModule main = system.main;

                float lifetime =
                    main.duration +
                    main.startDelay.constantMax +
                    main.startLifetime.constantMax;

                longest = Mathf.Max(
                    longest,
                    lifetime);
            }

            return longest > 0f
                ? longest + 0.25f
                : particleLifetimeFallback;
        }

        #endregion

        #region Camera

        private void ApplyCameraKick(
            Vector3 contactNormal,
            float severity)
        {
            if (!enablePlayerCameraKick ||
                cockpitView == null)
            {
                return;
            }

            cockpitView.ApplyCollisionImpulse(
                contactNormal,
                Mathf.Clamp01(
                    severity *
                    cameraSeverityMultiplier));
        }

        #endregion

        #region Flash

        private void ApplyScreenFlash(
            float severity)
        {
            if (impactFlashGroup == null)
                return;

            float alpha =
                Mathf.Lerp(
                    minimumFlashAlpha,
                    maximumFlashAlpha,
                    severity);

            impactFlashGroup.alpha =
                Mathf.Max(
                    impactFlashGroup.alpha,
                    alpha);
        }

        private void UpdateFlash()
        {
            if (impactFlashGroup == null ||
                impactFlashGroup.alpha <= 0f)
            {
                return;
            }

            impactFlashGroup.alpha =
                Mathf.MoveTowards(
                    impactFlashGroup.alpha,
                    0f,
                    flashFadeSpeed *
                    Time.deltaTime);
        }

        #endregion
    }
}