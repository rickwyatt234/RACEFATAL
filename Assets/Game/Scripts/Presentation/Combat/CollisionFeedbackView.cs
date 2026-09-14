using RaceFatal.Combat;
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
        #region Collision Audio

        [Header("Collision Audio")]
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

        [Header("Collision Audio Severity")]
        [Range(0f, 1f)][SerializeField] private float mediumThreshold = 0.35f;
        [Range(0f, 1f)][SerializeField] private float heavyThreshold = 0.7f;
        [Range(0f, 1f)][SerializeField] private float minimumImpactVolume = 0.3f;
        [Range(0f, 1f)][SerializeField] private float maximumImpactVolume = 1f;
        [Range(0f, 0.25f)][SerializeField] private float collisionPitchVariation = 0.06f;

        [Header("Collision Player / Opponent Mix")]
        [Range(0f, 2f)][SerializeField] private float playerCollisionVolumeMultiplier = 1f;
        [Range(0f, 2f)][SerializeField] private float opponentCollisionVolumeMultiplier = 0.55f;
        [Range(0f, 1f)][SerializeField] private float playerCollisionSpatialBlend = 0.25f;
        [Range(0f, 1f)][SerializeField] private float opponentCollisionSpatialBlend = 1f;

        #endregion

        #region Collision Particles

        [Header("Collision Particles")]
        [SerializeField] private ParticleSystem wallImpactPrefab;
        [SerializeField] private ParticleSystem racerImpactPrefab;
        [SerializeField] private ParticleSystem heavyImpactPrefab;

        [SerializeField] private bool alignParticlesToNormal = true;
        [SerializeField] private bool scaleParticlesBySeverity = true;

        [Min(0.01f)][SerializeField] private float minimumParticleScale = 0.65f;
        [Min(0.01f)][SerializeField] private float maximumParticleScale = 1.25f;
        [Min(0.1f)][SerializeField] private float particleLifetimeFallback = 3f;

        #endregion

        #region Shield Feedback

        [Header("Directional Shield Effects")]
        [SerializeField] private ShieldImpactEffectView frontShieldEffect;
        [SerializeField] private ShieldImpactEffectView rearShieldEffect;
        [SerializeField] private ShieldImpactEffectView leftShieldEffect;
        [SerializeField] private ShieldImpactEffectView rightShieldEffect;
        [SerializeField] private ShieldImpactEffectView fallbackShieldEffect;

        [Header("Shield Break")]
        [SerializeField] private Transform shieldBreakOrigin;
        [SerializeField] private ParticleSystem shieldDepletedPrefab;
        [SerializeField] private AudioClip[] shieldDepletedClips;

        [Range(0f, 1f)]
        [SerializeField] private float shieldDepletedVolume = 1f;

        [SerializeField] private bool playDirectionalPulseOnBreak = true;

        [Header("Shield Hit Audio")]
        [SerializeField] private AudioClip[] shieldHitClips;

        [Range(0f, 1f)]
        [SerializeField] private float shieldHitVolume = 0.75f;

        #endregion

        #region Hull Feedback

        [Header("Hull Hit")]
        [SerializeField] private Transform hullEffectOrigin;
        [SerializeField] private ParticleSystem hullHitPrefab;
        [SerializeField] private AudioClip[] hullHitClips;

        [Range(0f, 1f)]
        [SerializeField] private float hullHitVolume = 0.8f;

        #endregion

        #region Damage Audio

        [Header("Damage Audio")]
        [Tooltip("Audio source used for shield and hull impacts. Falls back to Collision Audio Source when unassigned.")]
        [SerializeField] private AudioSource damageAudioSource;

        [Tooltip("Mixer group used by shield and hull impact sounds. Falls back to Collision Impact Group when unassigned.")]
        [SerializeField] private AudioMixerGroup damageImpactGroup;

        [Header("Damage Player / Opponent Mix")]
        [Range(0f, 2f)][SerializeField] private float playerDamageVolumeMultiplier = 1f;
        [Range(0f, 2f)][SerializeField] private float opponentDamageVolumeMultiplier = 0.5f;
        [Range(0f, 1f)][SerializeField] private float playerDamageSpatialBlend = 0.35f;
        [Range(0f, 1f)][SerializeField] private float opponentDamageSpatialBlend = 1f;

        [Header("Damage Audio Variation")]
        [Range(0f, 0.25f)][SerializeField] private float damagePitchVariation = 0.04f;

        [Header("Damage Intensity")]
        [Min(0.01f)][SerializeField] private float damageForFullIntensity = 20f;
        [Range(0f, 1f)][SerializeField] private float minimumDamageIntensity = 0.3f;

        #endregion

        #region Player Camera

        [Header("Player Camera")]
        [SerializeField] private bool enablePlayerCameraKick = true;

        [Range(0f, 2f)]
        [SerializeField] private float cameraSeverityMultiplier = 1f;

        #endregion

        #region Player Screen Flash

        [Header("Optional Player Screen Flash")]
        [Tooltip("Optional full-screen CanvasGroup. Leave empty if you do not want collision flash.")]
        [SerializeField] private CanvasGroup impactFlashGroup;

        [Range(0f, 1f)][SerializeField] private float minimumFlashAlpha = 0.05f;
        [Range(0f, 1f)][SerializeField] private float maximumFlashAlpha = 0.3f;
        [Min(0.01f)][SerializeField] private float flashFadeSpeed = 5f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugBound;
        [SerializeField] private bool debugIsPlayer;

        [SerializeField] private string debugCollisionMixerGroup = "Unassigned";
        [SerializeField] private string debugDamageMixerGroup = "Unassigned";

        [SerializeField] private string debugImpactType = "None";
        [SerializeField] private string debugImpactTier = "None";
        [SerializeField] private float debugSeverity;
        [SerializeField] private float debugCollisionAudioVolume;

        [SerializeField] private string debugLastDamageCause = "None";
        [SerializeField] private string debugLastShieldDirection = "Unknown";
        [SerializeField] private float debugLastIncomingDamage;
        [SerializeField] private float debugLastShieldAbsorbed;
        [SerializeField] private float debugLastHullDamage;
        [SerializeField] private bool debugLastShieldDepleted;
        [SerializeField] private bool debugLastDestroyed;

        #endregion

        #region Runtime

        private RacerViewController racerView;
        private PlayerCockpitView cockpitView;
        private RaceRuntimeController runtime;

        private bool bound;
        private bool roleResolved;
        private bool isPlayer;

        private int lastShieldHitClip = -1;
        private int lastShieldDepletedClip = -1;
        private int lastHullHitClip = -1;

        #endregion

        #region Unity

        private void Awake()
        {
            racerView =
                GetComponent<RacerViewController>();

            cockpitView =
                GetComponentInChildren<
                    PlayerCockpitView>(true);

            if (shieldBreakOrigin == null)
                shieldBreakOrigin = transform;

            if (hullEffectOrigin == null)
                hullEffectOrigin = transform;

            PrepareAudioSource(
                impactAudioSource,
                impactGroup);

            AudioSource damageSource =
                GetDamageAudioSource();

            AudioMixerGroup damageGroup =
                GetDamageMixerGroup();

            if (damageSource != impactAudioSource)
            {
                PrepareAudioSource(
                    damageSource,
                    damageGroup);
            }

            if (impactFlashGroup != null)
                impactFlashGroup.alpha = 0f;
        }

        private void Update()
        {
            if (!bound)
                TryBind();

            UpdateFlash();
        }

        private void OnDisable()
        {
            Unbind();
            HideDirectionalShields();

            if (impactFlashGroup != null)
                impactFlashGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            Unbind();

            if (impactFlashGroup != null)
                impactFlashGroup.alpha = 0f;
        }

        #endregion

        #region Runtime Binding

        private void TryBind()
        {
            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null)
            {
                return;
            }

            ResolveRole();

            if (runtime == null)
            {
                runtime =
                    FindFirstObjectByType<
                        RaceRuntimeController>();
            }

            if (runtime == null ||
                runtime.Director == null)
            {
                return;
            }

            runtime.Director.DamageApplied +=
                OnDamageApplied;

            bound = true;
            debugBound = true;
        }

        private void Unbind()
        {
            if (!bound)
                return;

            if (runtime != null &&
                runtime.Director != null)
            {
                runtime.Director.DamageApplied -=
                    OnDamageApplied;
            }

            bound = false;
            debugBound = false;
        }

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

            ConfigureCollisionAudio();
            ConfigureDamageAudio();
        }

        #endregion

        #region Damage Events

        private void OnDamageApplied(
            DamageEvent damageEvent)
        {
            if (racerView == null ||
                racerView.Participant == null ||
                damageEvent.VictimRacerId !=
                    racerView.RacerId)
            {
                return;
            }

            debugLastDamageCause =
                damageEvent.Cause.ToString();

            debugLastShieldDirection =
                damageEvent.ImpactSide.ToString();

            debugLastIncomingDamage =
                damageEvent.IncomingDamage;

            debugLastShieldAbsorbed =
                damageEvent.ShieldAbsorbed;

            debugLastHullDamage =
                damageEvent.BikeDamage;

            debugLastShieldDepleted =
                damageEvent.ShieldDepleted;

            debugLastDestroyed =
                damageEvent.CausedDestruction;

            if (damageEvent.ShieldAbsorbed > 0f)
            {
                PlayShieldHit(
                    damageEvent);
            }

            if (damageEvent.BikeDamage > 0f)
            {
                PlayHullHit(
                    damageEvent.BikeDamage);
            }
        }

        #endregion

        #region Shield Feedback

        private void PlayShieldHit(
            DamageEvent damageEvent)
        {
            float intensity =
                CalculateDamageIntensity(
                    damageEvent.ShieldAbsorbed);

            if (!damageEvent.ShieldDepleted ||
                playDirectionalPulseOnBreak)
            {
                ShieldImpactEffectView effect =
                    GetDirectionalShield(
                        damageEvent.ImpactSide);

                effect?.Pulse();

                PlayRandomDamageClip(
                    shieldHitClips,
                    ref lastShieldHitClip,
                    shieldHitVolume *
                    intensity);
            }

            if (damageEvent.ShieldDepleted)
            {
                PlayShieldDepleted();
            }
        }

        private void PlayShieldDepleted()
        {
            PlayRandomDamageClip(
                shieldDepletedClips,
                ref lastShieldDepletedClip,
                shieldDepletedVolume);

            SpawnDamageParticle(
                shieldDepletedPrefab,
                shieldBreakOrigin);

            HideDirectionalShields();
        }

        private ShieldImpactEffectView GetDirectionalShield(
            DamageImpactSide side)
        {
            switch (side)
            {
                case DamageImpactSide.Front:
                    return frontShieldEffect != null
                        ? frontShieldEffect
                        : fallbackShieldEffect;

                case DamageImpactSide.Rear:
                    return rearShieldEffect != null
                        ? rearShieldEffect
                        : fallbackShieldEffect;

                case DamageImpactSide.Left:
                    return leftShieldEffect != null
                        ? leftShieldEffect
                        : fallbackShieldEffect;

                case DamageImpactSide.Right:
                    return rightShieldEffect != null
                        ? rightShieldEffect
                        : fallbackShieldEffect;

                default:
                    return fallbackShieldEffect;
            }
        }

        private void HideDirectionalShields()
        {
            frontShieldEffect?.HideImmediate();
            rearShieldEffect?.HideImmediate();
            leftShieldEffect?.HideImmediate();
            rightShieldEffect?.HideImmediate();
            fallbackShieldEffect?.HideImmediate();
        }

        #endregion

        #region Hull Feedback

        private void PlayHullHit(
            float hullDamage)
        {
            float intensity =
                CalculateDamageIntensity(
                    hullDamage);

            PlayRandomDamageClip(
                hullHitClips,
                ref lastHullHitClip,
                hullHitVolume *
                intensity);

            SpawnDamageParticle(
                hullHitPrefab,
                hullEffectOrigin);
        }

        #endregion

        #region Collision Feedback

        public void PlayImpact(
            CollisionImpactType type,
            Vector3 contactPoint,
            Vector3 contactNormal,
            float severity,
            bool catastrophic = false)
        {
            ResolveRole();

            severity =
                Mathf.Clamp01(
                    severity);

            if (catastrophic)
                severity = 1f;

            debugImpactType =
                type.ToString();

            debugSeverity =
                severity;

            PlayCollisionAudio(
                type,
                severity);

            SpawnCollisionParticles(
                type,
                contactPoint,
                contactNormal,
                severity);

            if (!isPlayer)
                return;

            ApplyCameraKick(
                contactNormal,
                severity);

            ApplyScreenFlash(
                severity);
        }

        #endregion

        #region Collision Audio

        private void PlayCollisionAudio(
            CollisionImpactType type,
            float severity)
        {
            if (impactAudioSource == null)
                return;

            ConfigureCollisionAudio();

            AudioClip[] clips =
                GetCollisionClipSet(
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
                    ? playerCollisionVolumeMultiplier
                    : opponentCollisionVolumeMultiplier;

            float volume =
                Mathf.Lerp(
                    minimumImpactVolume,
                    maximumImpactVolume,
                    severity) *
                roleMultiplier;

            volume =
                Mathf.Clamp01(
                    volume);

            impactAudioSource.pitch =
                Random.Range(
                    1f - collisionPitchVariation,
                    1f + collisionPitchVariation);

            impactAudioSource.PlayOneShot(
                clip,
                volume);

            debugCollisionAudioVolume =
                volume;
        }

        private AudioClip[] GetCollisionClipSet(
            CollisionImpactType type,
            float severity)
        {
            if (severity >= heavyThreshold)
            {
                debugImpactTier =
                    "Heavy";

                return type ==
                    CollisionImpactType.TrackWall
                        ? wallHeavyClips
                        : racerHeavyClips;
            }

            if (severity >= mediumThreshold)
            {
                debugImpactTier =
                    "Medium";

                return type ==
                    CollisionImpactType.TrackWall
                        ? wallMediumClips
                        : racerMediumClips;
            }

            debugImpactTier =
                "Light";

            return type ==
                CollisionImpactType.TrackWall
                    ? wallLightClips
                    : racerLightClips;
        }

        #endregion

        #region Damage Audio

        private void PlayRandomDamageClip(
            AudioClip[] clips,
            ref int previousIndex,
            float baseVolume)
        {
            AudioSource source =
                GetDamageAudioSource();

            if (source == null ||
                clips == null ||
                clips.Length == 0)
            {
                return;
            }

            ConfigureDamageAudio();

            int index =
                SelectClipIndex(
                    clips,
                    previousIndex);

            if (index < 0 ||
                clips[index] == null)
            {
                return;
            }

            previousIndex =
                index;

            float roleMultiplier =
                isPlayer
                    ? playerDamageVolumeMultiplier
                    : opponentDamageVolumeMultiplier;

            source.pitch =
                Random.Range(
                    1f - damagePitchVariation,
                    1f + damagePitchVariation);

            source.PlayOneShot(
                clips[index],
                Mathf.Clamp01(
                    baseVolume *
                    roleMultiplier));
        }

        #endregion

        #region Audio Setup

        private AudioSource GetDamageAudioSource()
        {
            return damageAudioSource != null
                ? damageAudioSource
                : impactAudioSource;
        }

        private AudioMixerGroup GetDamageMixerGroup()
        {
            return damageImpactGroup != null
                ? damageImpactGroup
                : impactGroup;
        }

        private void PrepareAudioSource(
            AudioSource source,
            AudioMixerGroup group)
        {
            if (source == null)
                return;

            source.playOnAwake = false;
            source.loop = false;

            if (group != null)
            {
                source.outputAudioMixerGroup =
                    group;
            }
        }

        private void ConfigureCollisionAudio()
        {
            if (impactAudioSource == null)
                return;

            impactAudioSource.spatialBlend =
                isPlayer
                    ? playerCollisionSpatialBlend
                    : opponentCollisionSpatialBlend;

            if (impactGroup != null)
            {
                impactAudioSource.outputAudioMixerGroup =
                    impactGroup;

                debugCollisionMixerGroup =
                    impactGroup.name;
            }
            else
            {
                debugCollisionMixerGroup =
                    "Unassigned";
            }
        }

        private void ConfigureDamageAudio()
        {
            AudioSource source =
                GetDamageAudioSource();

            if (source == null)
                return;

            source.spatialBlend =
                isPlayer
                    ? playerDamageSpatialBlend
                    : opponentDamageSpatialBlend;

            AudioMixerGroup group =
                GetDamageMixerGroup();

            if (group != null)
            {
                source.outputAudioMixerGroup =
                    group;

                debugDamageMixerGroup =
                    group.name;
            }
            else
            {
                debugDamageMixerGroup =
                    "Unassigned";
            }
        }

        #endregion

        #region Collision Particles

        private void SpawnCollisionParticles(
            CollisionImpactType type,
            Vector3 position,
            Vector3 normal,
            float severity)
        {
            ParticleSystem basePrefab =
                type ==
                    CollisionImpactType.TrackWall
                    ? wallImpactPrefab
                    : racerImpactPrefab;

            SpawnCollisionParticle(
                basePrefab,
                position,
                normal,
                severity);

            if (severity >= heavyThreshold &&
                heavyImpactPrefab != null)
            {
                SpawnCollisionParticle(
                    heavyImpactPrefab,
                    position,
                    normal,
                    severity);
            }
        }

        private void SpawnCollisionParticle(
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
                    ? Quaternion.LookRotation(
                        normal.normalized)
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

                effect.transform.localScale *=
                    scale;
            }

            effect.Play(true);

            Destroy(
                effect.gameObject,
                GetParticleLifetime(
                    effect));
        }

        #endregion

        #region Damage Particles

        private void SpawnDamageParticle(
            ParticleSystem prefab,
            Transform origin)
        {
            if (prefab == null)
                return;

            Transform spawnOrigin =
                origin != null
                    ? origin
                    : transform;

            ParticleSystem effect =
                Instantiate(
                    prefab,
                    spawnOrigin.position,
                    spawnOrigin.rotation);

            effect.Play(true);

            Destroy(
                effect.gameObject,
                GetParticleLifetime(
                    effect));
        }

        #endregion

        #region Player Camera

        private void ApplyCameraKick(
            Vector3 contactNormal,
            float severity)
        {
            if (!enablePlayerCameraKick)
                return;

            if (cockpitView == null)
            {
                cockpitView =
                    GetComponentInChildren<
                        PlayerCockpitView>(true);
            }

            if (cockpitView == null)
                return;

            cockpitView.ApplyCollisionImpulse(
                contactNormal,
                Mathf.Clamp01(
                    severity *
                    cameraSeverityMultiplier));
        }

        #endregion

        #region Player Flash

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

        #region Helpers

        private float CalculateDamageIntensity(
            float damage)
        {
            float normalized =
                Mathf.Clamp01(
                    damage /
                    Mathf.Max(
                        0.01f,
                        damageForFullIntensity));

            return Mathf.Lerp(
                minimumDamageIntensity,
                1f,
                normalized);
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

            for (int i = 0;
                 i < systems.Length;
                 i++)
            {
                ParticleSystem system =
                    systems[i];

                if (system == null)
                    continue;

                ParticleSystem.MainModule main =
                    system.main;

                float lifetime =
                    main.duration +
                    main.startDelay.constantMax +
                    main.startLifetime.constantMax;

                longest =
                    Mathf.Max(
                        longest,
                        lifetime);
            }

            return longest > 0f
                ? longest + 0.25f
                : particleLifetimeFallback;
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

            for (int i = 0;
                 i < clips.Length;
                 i++)
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

            for (int i = 0;
                 i < clips.Length;
                 i++)
            {
                if (clips[i] == null)
                    continue;

                if (current == target)
                {
                    clip =
                        clips[i];

                    return true;
                }

                current++;
            }

            return false;
        }

        private int SelectClipIndex(
            AudioClip[] clips,
            int previousIndex)
        {
            int validCount = 0;

            for (int i = 0;
                 i < clips.Length;
                 i++)
            {
                if (clips[i] != null)
                    validCount++;
            }

            if (validCount == 0)
                return -1;

            int ordinal =
                Random.Range(
                    0,
                    validCount);

            int selected =
                GetValidClipIndex(
                    clips,
                    ordinal);

            if (validCount > 1 &&
                selected == previousIndex)
            {
                ordinal =
                    (ordinal + 1) %
                    validCount;

                selected =
                    GetValidClipIndex(
                        clips,
                        ordinal);
            }

            return selected;
        }

        private int GetValidClipIndex(
            AudioClip[] clips,
            int ordinal)
        {
            int current = 0;

            for (int i = 0;
                 i < clips.Length;
                 i++)
            {
                if (clips[i] == null)
                    continue;

                if (current == ordinal)
                    return i;

                current++;
            }

            return -1;
        }

        #endregion
    }
}