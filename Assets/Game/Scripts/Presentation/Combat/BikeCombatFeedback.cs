using RaceFatal.Combat;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;
using UnityEngine.Audio;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(RacerViewController))]
    public class BikeCombatFeedback : MonoBehaviour
    {
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
        [Range(0f, 1f)][SerializeField] private float shieldDepletedVolume = 1f;
        [SerializeField] private bool playDirectionalPulseOnBreak = true;

        [Header("Shield Hit Audio")]
        [SerializeField] private AudioClip[] shieldHitClips;
        [Range(0f, 1f)][SerializeField] private float shieldHitVolume = 0.75f;

        [Header("Hull Hit")]
        [SerializeField] private Transform hullEffectOrigin;
        [SerializeField] private ParticleSystem hullHitPrefab;
        [SerializeField] private AudioClip[] hullHitClips;
        [Range(0f, 1f)][SerializeField] private float hullHitVolume = 0.8f;

        [Header("Audio")]
        [SerializeField] private AudioSource impactAudioSource;

        [Header("Audio Mixer Routing")]
        [SerializeField] private AudioMixerGroup impactGroup;

        [Header("Player / Opponent Mix")]
        [Range(0f, 2f)][SerializeField] private float playerVolumeMultiplier = 1f;
        [Range(0f, 2f)][SerializeField] private float opponentVolumeMultiplier = 0.5f;
        [Range(0f, 1f)][SerializeField] private float playerSpatialBlend = 0.35f;
        [Range(0f, 1f)][SerializeField] private float opponentSpatialBlend = 1f;

        [Header("Audio Variation")]
        [Range(0f, 0.25f)][SerializeField] private float pitchVariation = 0.04f;

        [Header("Damage Intensity")]
        [Min(0.01f)][SerializeField] private float damageForFullIntensity = 20f;
        [Range(0f, 1f)][SerializeField] private float minimumImpactIntensity = 0.3f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugBound;
        [SerializeField] private bool debugIsPlayer;
        [SerializeField] private string debugMixerGroup = "Unassigned";
        [SerializeField] private string debugLastCause = "None";
        [SerializeField] private string debugLastShieldDirection = "Unknown";
        [SerializeField] private float debugLastIncomingDamage;
        [SerializeField] private float debugLastShieldAbsorbed;
        [SerializeField] private float debugLastHullDamage;
        [SerializeField] private bool debugLastShieldDepleted;
        [SerializeField] private bool debugLastDestroyed;

        private RacerViewController racerView;
        private RaceRuntimeController runtime;

        private bool bound;
        private bool isPlayer;

        private int lastShieldHitClip = -1;
        private int lastShieldDepletedClip = -1;
        private int lastHullHitClip = -1;

        private void Awake()
        {
            racerView =
                GetComponent<RacerViewController>();

            if (shieldBreakOrigin == null)
                shieldBreakOrigin = transform;

            if (hullEffectOrigin == null)
                hullEffectOrigin = transform;

            if (impactAudioSource != null)
            {
                impactAudioSource.playOnAwake = false;
                impactAudioSource.loop = false;

                if (impactGroup != null)
                {
                    impactAudioSource.outputAudioMixerGroup =
                        impactGroup;

                    debugMixerGroup =
                        impactGroup.name;
                }
            }
        }

        private void Update()
        {
            if (!bound)
                TryBind();
        }

        private void OnDisable()
        {
            Unbind();
            HideDirectionalShields();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void TryBind()
        {
            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null)
            {
                return;
            }

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

            isPlayer =
                racerView.Participant.Role ==
                RaceParticipantRole.Player;

            ConfigureAudioMix();

            runtime.Director.DamageApplied +=
                OnDamageApplied;

            bound = true;
            debugBound = true;
            debugIsPlayer = isPlayer;
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

            debugLastCause =
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

        private void PlayShieldHit(
            DamageEvent damageEvent)
        {
            float intensity =
                CalculateIntensity(
                    damageEvent.ShieldAbsorbed);

            if (!damageEvent.ShieldDepleted ||
                playDirectionalPulseOnBreak)
            {
                ShieldImpactEffectView effect =
                    GetDirectionalShield(
                        damageEvent.ImpactSide);

                effect?.Pulse();

                PlayRandomClip(
                    shieldHitClips,
                    ref lastShieldHitClip,
                    shieldHitVolume *
                    intensity);
            }

            if (damageEvent.ShieldDepleted)
                PlayShieldDepleted();
        }

        private void PlayShieldDepleted()
        {
            PlayRandomClip(
                shieldDepletedClips,
                ref lastShieldDepletedClip,
                shieldDepletedVolume);

            SpawnParticle(
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

        private void PlayHullHit(
            float hullDamage)
        {
            float intensity =
                CalculateIntensity(
                    hullDamage);

            PlayRandomClip(
                hullHitClips,
                ref lastHullHitClip,
                hullHitVolume *
                intensity);

            SpawnParticle(
                hullHitPrefab,
                hullEffectOrigin);
        }

        private void ConfigureAudioMix()
        {
            if (impactAudioSource == null)
                return;

            impactAudioSource.spatialBlend =
                isPlayer
                    ? playerSpatialBlend
                    : opponentSpatialBlend;

            if (impactGroup != null)
            {
                impactAudioSource.outputAudioMixerGroup =
                    impactGroup;

                debugMixerGroup =
                    impactGroup.name;
            }
        }

        private void PlayRandomClip(
            AudioClip[] clips,
            ref int previousIndex,
            float baseVolume)
        {
            if (impactAudioSource == null ||
                clips == null ||
                clips.Length == 0)
            {
                return;
            }

            int index =
                SelectClipIndex(
                    clips,
                    previousIndex);

            if (index < 0 ||
                clips[index] == null)
            {
                return;
            }

            previousIndex = index;

            float roleMultiplier =
                isPlayer
                    ? playerVolumeMultiplier
                    : opponentVolumeMultiplier;

            impactAudioSource.pitch =
                Random.Range(
                    1f - pitchVariation,
                    1f + pitchVariation);

            impactAudioSource.PlayOneShot(
                clips[index],
                Mathf.Clamp01(
                    baseVolume *
                    roleMultiplier));
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

        private void SpawnParticle(
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

        private float GetParticleLifetime(
            ParticleSystem particles)
        {
            if (particles == null)
                return 5f;

            ParticleSystem.MainModule main =
                particles.main;

            return
                main.duration +
                main.startDelay.constantMax +
                main.startLifetime.constantMax +
                0.5f;
        }

        private float CalculateIntensity(
            float damage)
        {
            float normalized =
                Mathf.Clamp01(
                    damage /
                    Mathf.Max(
                        0.01f,
                        damageForFullIntensity));

            return Mathf.Lerp(
                minimumImpactIntensity,
                1f,
                normalized);
        }
    }
}