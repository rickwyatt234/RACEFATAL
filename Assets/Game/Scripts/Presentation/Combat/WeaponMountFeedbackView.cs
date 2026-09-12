using System.Collections.Generic;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class WeaponMountFeedbackView : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Base AudioSource used for weapon firing. Additional voices are generated from this at runtime.")]
        [SerializeField] private AudioSource fireAudioSource;

        [Tooltip("Number of overlapping fire sounds this mount can play independently.")]
        [Range(1, 12)][SerializeField] private int fireAudioVoiceCount = 4;

        [Header("Runtime Debug")]
        [SerializeField] private string debugLastWeapon = "None";
        [SerializeField] private string debugLastClip = "None";
        [SerializeField] private float debugLastVolume;
        [SerializeField] private float debugLastPitch;
        [SerializeField] private bool debugPlayedMuzzle;
        [SerializeField] private bool debugPlayedAudio;

        private readonly List<AudioSource> fireVoices =
            new List<AudioSource>();

        private WeaponPresentationProfile lastProfile;
        private int lastFireClipIndex = -1;
        private int nextVoiceIndex;

        private void Awake()
        {
            InitializeAudioVoices();
        }

        public void PlayFire(
            WeaponPresentationProfile profile,
            Transform fireOrigin)
        {
            if (profile == null)
                return;

            Transform origin =
                fireOrigin != null
                    ? fireOrigin
                    : transform;

            debugLastWeapon = profile.WeaponDefinitionId;
            debugPlayedMuzzle = false;
            debugPlayedAudio = false;

            PlayMuzzle(profile, origin);
            PlayAudio(profile);
        }

        #region Muzzle

        private void PlayMuzzle(
            WeaponPresentationProfile profile,
            Transform origin)
        {
            if (profile.MuzzlePrefab == null ||
                origin == null)
            {
                return;
            }

            GameObject muzzle =
                Instantiate(
                    profile.MuzzlePrefab,
                    origin.position,
                    origin.rotation);

            float lifetime =
                CalculateEffectLifetime(
                    muzzle,
                    profile.MuzzleFallbackLifetime);

            Destroy(
                muzzle,
                lifetime);

            debugPlayedMuzzle = true;
        }

        private float CalculateEffectLifetime(
            GameObject effect,
            float fallback)
        {
            if (effect == null)
                return fallback;

            ParticleSystem[] particles =
                effect.GetComponentsInChildren<ParticleSystem>(true);

            if (particles == null ||
                particles.Length == 0)
            {
                return fallback;
            }

            float longestLifetime = 0f;

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];

                if (particle == null)
                    continue;

                ParticleSystem.MainModule main =
                    particle.main;

                float lifetime =
                    main.duration +
                    main.startDelay.constantMax +
                    main.startLifetime.constantMax;

                longestLifetime =
                    Mathf.Max(
                        longestLifetime,
                        lifetime);
            }

            return Mathf.Max(
                0.01f,
                longestLifetime);
        }

        #endregion

        #region Audio

        private void InitializeAudioVoices()
        {
            fireVoices.Clear();

            if (fireAudioSource == null)
                fireAudioSource = GetComponent<AudioSource>();

            if (fireAudioSource == null)
                fireAudioSource = gameObject.AddComponent<AudioSource>();

            ConfigureBaseVoice(
                fireAudioSource);

            fireVoices.Add(
                fireAudioSource);

            int targetCount =
                Mathf.Max(
                    1,
                    fireAudioVoiceCount);

            for (int i = 1; i < targetCount; i++)
            {
                AudioSource voice =
                    gameObject.AddComponent<AudioSource>();

                CopyAudioSourceSettings(
                    fireAudioSource,
                    voice);

                ConfigureBaseVoice(
                    voice);

                fireVoices.Add(
                    voice);
            }
        }

        private void ConfigureBaseVoice(
            AudioSource source)
        {
            if (source == null)
                return;

            source.playOnAwake = false;
            source.loop = false;
        }

        private void CopyAudioSourceSettings(
            AudioSource source,
            AudioSource destination)
        {
            if (source == null ||
                destination == null)
            {
                return;
            }

            destination.outputAudioMixerGroup =
                source.outputAudioMixerGroup;

            destination.mute =
                source.mute;

            destination.bypassEffects =
                source.bypassEffects;

            destination.bypassListenerEffects =
                source.bypassListenerEffects;

            destination.bypassReverbZones =
                source.bypassReverbZones;

            destination.priority =
                source.priority;

            destination.reverbZoneMix =
                source.reverbZoneMix;

            destination.spread =
                source.spread;

            destination.spatialize =
                source.spatialize;
        }

        private void PlayAudio(
            WeaponPresentationProfile profile)
        {
            if (fireVoices.Count == 0)
                return;

            if (!TrySelectFireClip(
                    profile,
                    out AudioClip clip,
                    out int clipIndex))
            {
                return;
            }

            AudioSource voice =
                GetNextVoice();

            if (voice == null)
                return;

            float minimumPitch =
                Mathf.Min(
                    profile.MinimumFirePitch,
                    profile.MaximumFirePitch);

            float maximumPitch =
                Mathf.Max(
                    profile.MinimumFirePitch,
                    profile.MaximumFirePitch);

            float pitch =
                Random.Range(
                    minimumPitch,
                    maximumPitch);

            float volume =
                profile.FireVolume +
                Random.Range(
                    -profile.FireVolumeVariation,
                    profile.FireVolumeVariation);

            volume =
                Mathf.Clamp01(
                    volume);

            voice.Stop();

            voice.clip = clip;
            voice.volume = volume;
            voice.pitch = pitch;

            voice.spatialBlend =
                profile.FireSpatialBlend;

            voice.rolloffMode =
                profile.FireRolloffMode;

            voice.minDistance =
                profile.FireMinDistance;

            voice.maxDistance =
                Mathf.Max(
                    profile.FireMinDistance,
                    profile.FireMaxDistance);

            voice.dopplerLevel =
                profile.FireDopplerLevel;

            voice.Play();

            lastProfile = profile;
            lastFireClipIndex = clipIndex;

            debugLastClip = clip.name;
            debugLastVolume = volume;
            debugLastPitch = pitch;
            debugPlayedAudio = true;
        }

        private AudioSource GetNextVoice()
        {
            if (fireVoices.Count == 0)
                return null;

            /*
             * Prefer an unused voice so existing gunshots can
             * finish naturally.
             */
            for (int offset = 0;
                 offset < fireVoices.Count;
                 offset++)
            {
                int index =
                    (nextVoiceIndex + offset) %
                    fireVoices.Count;

                AudioSource voice =
                    fireVoices[index];

                if (voice == null ||
                    voice.isPlaying)
                {
                    continue;
                }

                nextVoiceIndex =
                    (index + 1) %
                    fireVoices.Count;

                return voice;
            }

            /*
             * All voices are occupied. Reuse the oldest position
             * in the round-robin pool.
             */
            AudioSource fallback =
                fireVoices[nextVoiceIndex];

            nextVoiceIndex =
                (nextVoiceIndex + 1) %
                fireVoices.Count;

            return fallback;
        }

        private bool TrySelectFireClip(
            WeaponPresentationProfile profile,
            out AudioClip clip,
            out int clipIndex)
        {
            clip = null;
            clipIndex = -1;

            AudioClip[] clips =
                profile.FireClips;

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

            int selectedOrdinal =
                Random.Range(
                    0,
                    validCount);

            int selectedIndex =
                GetValidClipIndex(
                    clips,
                    selectedOrdinal);

            /*
             * If possible, never use the exact same recording twice
             * in succession for this weapon.
             */
            if (validCount > 1 &&
                profile == lastProfile &&
                selectedIndex == lastFireClipIndex)
            {
                selectedOrdinal =
                    (selectedOrdinal + 1) %
                    validCount;

                selectedIndex =
                    GetValidClipIndex(
                        clips,
                        selectedOrdinal);
            }

            if (selectedIndex < 0)
                return false;

            clipIndex = selectedIndex;
            clip = clips[selectedIndex];

            return clip != null;
        }

        private int GetValidClipIndex(
            AudioClip[] clips,
            int validOrdinal)
        {
            int ordinal = 0;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;

                if (ordinal == validOrdinal)
                    return i;

                ordinal++;
            }

            return -1;
        }

        #endregion
    }
}