using System.Collections.Generic;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;
using UnityEngine.Audio;

namespace RaceFatal.Presentation.Combat
{
    public class WeaponMountFeedbackView : MonoBehaviour
    {
        [Header("Fire Audio")]
        [SerializeField] private AudioSource fireAudioSource;
        [Range(1, 12)][SerializeField] private int fireAudioVoiceCount = 4;

        [Header("Charge Audio")]
        [Tooltip("Dedicated looping source used while a ChargeRelease weapon is charging.")]
        [SerializeField] private AudioSource chargeAudioSource;

        [Header("Audio Mixer Routing")]
        [SerializeField] private AudioMixerGroup playerWeaponGroup;
        [SerializeField] private AudioMixerGroup opponentWeaponGroup;

        [Header("Runtime Debug")]
        [SerializeField] private string debugLastWeapon = "None";
        [SerializeField] private string debugLastClip = "None";
        [SerializeField] private float debugLastVolume;
        [SerializeField] private float debugLastPitch;
        [SerializeField] private bool debugPlayedMuzzle;
        [SerializeField] private bool debugPlayedAudio;

        [SerializeField] private bool debugCharging;
        [SerializeField] private float debugChargeRatio;
        [SerializeField] private bool debugFullChargeTriggered;

        [SerializeField] private bool debugRoleResolved;
        [SerializeField] private bool debugIsPlayer;
        [SerializeField] private string debugMixerGroup = "Unresolved";

        private readonly List<AudioSource> fireVoices =
            new List<AudioSource>();

        private RacerViewController racerView;

        private WeaponPresentationProfile lastProfile;
        private WeaponPresentationProfile activeChargeProfile;

        private GameObject activeChargeEffect;
        private WeaponChargeEffectView activeChargeEffectView;
        private Transform activeChargeOrigin;

        private int lastFireClipIndex = -1;
        private int nextVoiceIndex;

        private bool roleResolved;
        private bool isPlayer;
        private bool chargeActive;
        private bool fullChargeTriggered;

        private void Awake()
        {
            racerView =
                GetComponentInParent<RacerViewController>();

            InitializeAudioVoices();
            InitializeChargeAudioSource();
        }

        private void OnDisable()
        {
            CancelCharge();
        }

        public void PlayFire(
            WeaponPresentationProfile profile,
            Transform fireOrigin)
        {
            if (profile == null)
                return;

            CancelCharge();
            ResolveRoleRouting();

            Transform origin =
                fireOrigin != null
                    ? fireOrigin
                    : transform;

            debugLastWeapon = profile.WeaponDefinitionId;
            debugPlayedMuzzle = false;
            debugPlayedAudio = false;

            PlayMuzzle(
                profile,
                origin);

            PlayFireAudio(
                profile);
        }

        #region Charge

        public void BeginCharge(
            WeaponPresentationProfile profile,
            Transform fireOrigin)
        {
            if (profile == null)
                return;

            if (chargeActive &&
                activeChargeProfile == profile &&
                activeChargeOrigin == fireOrigin)
            {
                return;
            }

            CancelCharge();
            ResolveRoleRouting();

            activeChargeProfile = profile;

            activeChargeOrigin =
                fireOrigin != null
                    ? fireOrigin
                    : transform;

            chargeActive = true;
            fullChargeTriggered = false;

            debugCharging = true;
            debugChargeRatio = 0f;
            debugFullChargeTriggered = false;
            debugLastWeapon = profile.WeaponDefinitionId;

            if (profile.ChargePrefab != null)
            {
                activeChargeEffect =
                    Instantiate(
                        profile.ChargePrefab,
                        activeChargeOrigin.position,
                        activeChargeOrigin.rotation,
                        activeChargeOrigin);

                activeChargeEffectView =
                    activeChargeEffect.GetComponentInChildren<
                        WeaponChargeEffectView>(true);

                activeChargeEffectView?.SetCharge(0f);
            }

            BeginChargeAudio(profile);
        }

        public void UpdateCharge(
            WeaponPresentationProfile profile,
            Transform fireOrigin,
            float ratio)
        {
            if (profile == null)
                return;

            if (!chargeActive ||
                activeChargeProfile != profile)
            {
                BeginCharge(
                    profile,
                    fireOrigin);
            }

            ratio =
                Mathf.Clamp01(
                    ratio);

            debugChargeRatio = ratio;

            activeChargeEffectView?.SetCharge(
                ratio);

            UpdateChargeAudio(
                profile,
                ratio);

            if (ratio >= 0.999f &&
                !fullChargeTriggered)
            {
                TriggerFullCharge(
                    profile);
            }
        }

        public void CancelCharge()
        {
            if (chargeAudioSource != null)
            {
                chargeAudioSource.Stop();
                chargeAudioSource.clip = null;
            }

            if (activeChargeEffect != null)
                Destroy(activeChargeEffect);

            activeChargeEffect = null;
            activeChargeEffectView = null;
            activeChargeOrigin = null;
            activeChargeProfile = null;

            chargeActive = false;
            fullChargeTriggered = false;

            debugCharging = false;
            debugChargeRatio = 0f;
            debugFullChargeTriggered = false;
        }

        private void TriggerFullCharge(
            WeaponPresentationProfile profile)
        {
            fullChargeTriggered = true;
            debugFullChargeTriggered = true;

            Transform origin =
                activeChargeOrigin != null
                    ? activeChargeOrigin
                    : transform;

            if (profile.FullChargePrefab != null)
            {
                GameObject effect =
                    Instantiate(
                        profile.FullChargePrefab,
                        origin.position,
                        origin.rotation,
                        origin);

                Destroy(
                    effect,
                    CalculateEffectLifetime(
                        effect,
                        profile.FullChargeFallbackLifetime));
            }

            if (profile.FullChargeClip != null)
            {
                PlayClipOnVoice(
                    profile,
                    profile.FullChargeClip,
                    profile.FullChargeVolume,
                    1f);
            }
        }

        private void BeginChargeAudio(
            WeaponPresentationProfile profile)
        {
            if (chargeAudioSource == null ||
                profile.ChargeLoopClip == null)
            {
                return;
            }

            ConfigureSourceForProfile(
                chargeAudioSource,
                profile);

            chargeAudioSource.Stop();
            chargeAudioSource.clip = profile.ChargeLoopClip;
            chargeAudioSource.loop = true;
            chargeAudioSource.volume = profile.MinimumChargeVolume;
            chargeAudioSource.pitch = profile.MinimumChargePitch;
            chargeAudioSource.Play();
        }

        private void UpdateChargeAudio(
            WeaponPresentationProfile profile,
            float ratio)
        {
            if (chargeAudioSource == null ||
                profile.ChargeLoopClip == null)
            {
                return;
            }

            if (!chargeAudioSource.isPlaying)
                BeginChargeAudio(profile);

            chargeAudioSource.volume =
                Mathf.Lerp(
                    profile.MinimumChargeVolume,
                    profile.MaximumChargeVolume,
                    ratio);

            chargeAudioSource.pitch =
                Mathf.Lerp(
                    profile.MinimumChargePitch,
                    profile.MaximumChargePitch,
                    ratio);
        }

        #endregion

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

            Destroy(
                muzzle,
                CalculateEffectLifetime(
                    muzzle,
                    profile.MuzzleFallbackLifetime));

            debugPlayedMuzzle = true;
        }

        private float CalculateEffectLifetime(
            GameObject effect,
            float fallback)
        {
            if (effect == null)
                return fallback;

            ParticleSystem[] particles =
                effect.GetComponentsInChildren<
                    ParticleSystem>(true);

            if (particles == null ||
                particles.Length == 0)
            {
                return fallback;
            }

            float longestLifetime = 0f;

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle =
                    particles[i];

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

        #region Routing

        private void ResolveRoleRouting()
        {
            if (roleResolved)
                return;

            if (racerView == null)
            {
                racerView =
                    GetComponentInParent<
                        RacerViewController>();
            }

            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null)
            {
                return;
            }

            isPlayer =
                racerView.Participant.Role ==
                RaceParticipantRole.Player;

            AudioMixerGroup targetGroup =
                isPlayer
                    ? playerWeaponGroup
                    : opponentWeaponGroup;

            if (targetGroup != null)
            {
                for (int i = 0; i < fireVoices.Count; i++)
                {
                    if (fireVoices[i] != null)
                    {
                        fireVoices[i].outputAudioMixerGroup =
                            targetGroup;
                    }
                }

                if (chargeAudioSource != null)
                {
                    chargeAudioSource.outputAudioMixerGroup =
                        targetGroup;
                }

                debugMixerGroup =
                    targetGroup.name;
            }
            else
            {
                debugMixerGroup =
                    "Unassigned";
            }

            roleResolved = true;

            debugRoleResolved = true;
            debugIsPlayer = isPlayer;
        }

        #endregion

        #region Audio Setup

        private void InitializeAudioVoices()
        {
            fireVoices.Clear();

            if (fireAudioSource == null)
                fireAudioSource = GetComponent<AudioSource>();

            if (fireAudioSource == null)
            {
                fireAudioSource =
                    gameObject.AddComponent<AudioSource>();
            }

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

        private void InitializeChargeAudioSource()
        {
            if (chargeAudioSource == null ||
                chargeAudioSource == fireAudioSource)
            {
                chargeAudioSource =
                    gameObject.AddComponent<AudioSource>();

                CopyAudioSourceSettings(
                    fireAudioSource,
                    chargeAudioSource);
            }

            chargeAudioSource.playOnAwake = false;
            chargeAudioSource.loop = true;
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

        #endregion

        #region Fire Audio

        private void PlayFireAudio(
            WeaponPresentationProfile profile)
        {
            if (!TrySelectFireClip(
                    profile,
                    out AudioClip clip,
                    out int clipIndex))
            {
                return;
            }

            float pitch =
                Random.Range(
                    Mathf.Min(
                        profile.MinimumFirePitch,
                        profile.MaximumFirePitch),
                    Mathf.Max(
                        profile.MinimumFirePitch,
                        profile.MaximumFirePitch));

            float volume =
                profile.FireVolume +
                Random.Range(
                    -profile.FireVolumeVariation,
                    profile.FireVolumeVariation);

            volume =
                Mathf.Clamp01(
                    volume);

            PlayClipOnVoice(
                profile,
                clip,
                volume,
                pitch);

            lastProfile = profile;
            lastFireClipIndex = clipIndex;

            debugLastClip = clip.name;
            debugLastVolume = volume;
            debugLastPitch = pitch;
            debugPlayedAudio = true;
        }

        private void PlayClipOnVoice(
            WeaponPresentationProfile profile,
            AudioClip clip,
            float volume,
            float pitch)
        {
            if (clip == null)
                return;

            ResolveRoleRouting();

            AudioSource voice =
                GetNextVoice();

            if (voice == null)
                return;

            ConfigureSourceForProfile(
                voice,
                profile);

            voice.Stop();

            voice.clip = clip;
            voice.loop = false;
            voice.volume = Mathf.Clamp01(volume);
            voice.pitch = pitch;
            voice.Play();
        }

        private void ConfigureSourceForProfile(
            AudioSource source,
            WeaponPresentationProfile profile)
        {
            if (source == null ||
                profile == null)
            {
                return;
            }

            source.spatialBlend =
                profile.FireSpatialBlend;

            source.rolloffMode =
                profile.FireRolloffMode;

            source.minDistance =
                profile.FireMinDistance;

            source.maxDistance =
                Mathf.Max(
                    profile.FireMinDistance,
                    profile.FireMaxDistance);

            source.dopplerLevel =
                profile.FireDopplerLevel;
        }

        private AudioSource GetNextVoice()
        {
            if (fireVoices.Count == 0)
                return null;

            for (int offset = 0; offset < fireVoices.Count; offset++)
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

            if (validCount > 1 &&
                profile == lastProfile &&
                selectedIndex ==
                lastFireClipIndex)
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