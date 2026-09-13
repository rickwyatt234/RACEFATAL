using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace RaceFatal.Presentation.Audio
{
    public enum AudioBus
    {
        Master,
        Music,
        UI,
        PlayerVehicle,
        OpponentVehicle,
        PlayerWeapon,
        OpponentWeapon,
        Impact,
        Explosion,
        Ambience
    }

    [DefaultExecutionOrder(-1000)]
    public class AudioSettingsController : MonoBehaviour
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Exposed Parameters")]
        [SerializeField] private string masterParameter = "MasterVolume";
        [SerializeField] private string musicParameter = "MusicVolume";
        [SerializeField] private string uiParameter = "UIVolume";
        [SerializeField] private string playerVehicleParameter = "PlayerVehicleVolume";
        [SerializeField] private string opponentVehicleParameter = "OpponentVehicleVolume";
        [SerializeField] private string playerWeaponParameter = "PlayerWeaponVolume";
        [SerializeField] private string opponentWeaponParameter = "OpponentWeaponVolume";
        [SerializeField] private string impactParameter = "ImpactVolume";
        [SerializeField] private string explosionParameter = "ExplosionVolume";
        [SerializeField] private string ambienceParameter = "AmbienceVolume";

        [Header("Settings")]
        [Tooltip("Lowest mixer volume used for a zero-volume slider or mute.")]
        [Range(-80f, -40f)][SerializeField] private float minimumDecibels = -80f;

        [Tooltip("Load saved audio preferences during Awake.")]
        [SerializeField] private bool loadOnAwake = true;

        [Header("Persistence")]
        [SerializeField] private string playerPrefsPrefix = "RaceFatal.Audio.";

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private string debugLastChangedBus = "None";
        [SerializeField] private float debugLastNormalizedVolume;
        [SerializeField] private float debugLastDecibels;
        [SerializeField] private bool debugLastMuted;

        private static readonly AudioBus[] AllBuses =
        {
            AudioBus.Master,
            AudioBus.Music,
            AudioBus.UI,
            AudioBus.PlayerVehicle,
            AudioBus.OpponentVehicle,
            AudioBus.PlayerWeapon,
            AudioBus.OpponentWeapon,
            AudioBus.Impact,
            AudioBus.Explosion,
            AudioBus.Ambience
        };

        private readonly Dictionary<AudioBus, BusState> states =
            new Dictionary<AudioBus, BusState>();

        private class BusState
        {
            public float Volume;
            public float DefaultVolume;
            public bool Muted;

            public BusState(float volume)
            {
                Volume = volume;
                DefaultVolume = volume;
                Muted = false;
            }
        }

        private void Awake()
        {
            if (audioMixer == null)
            {
                Debug.LogError(
                    $"{nameof(AudioSettingsController)} requires an AudioMixer.",
                    this);

                enabled = false;
                return;
            }

            InitializeStates();

            if (loadOnAwake)
                LoadSettings();

            ApplyAll();

            debugInitialized = true;
        }

        private void OnApplicationQuit()
        {
            SaveSettings();
        }

        #region Public Volume API

        public void SetVolume(AudioBus bus, float normalizedVolume)
        {
            if (!states.TryGetValue(bus, out BusState state))
                return;

            state.Volume = Mathf.Clamp01(normalizedVolume);

            PlayerPrefs.SetFloat(
                GetVolumeKey(bus),
                state.Volume);

            if (!state.Muted)
                Apply(bus);

            debugLastChangedBus = bus.ToString();
            debugLastNormalizedVolume = state.Volume;
            debugLastMuted = state.Muted;
        }

        public float GetVolume(AudioBus bus)
        {
            return states.TryGetValue(bus, out BusState state)
                ? state.Volume
                : 1f;
        }

        public void SetMuted(AudioBus bus, bool muted)
        {
            if (!states.TryGetValue(bus, out BusState state))
                return;

            state.Muted = muted;

            PlayerPrefs.SetInt(
                GetMuteKey(bus),
                muted ? 1 : 0);

            Apply(bus);

            debugLastChangedBus = bus.ToString();
            debugLastNormalizedVolume = state.Volume;
            debugLastMuted = muted;
        }

        public bool IsMuted(AudioBus bus)
        {
            return states.TryGetValue(bus, out BusState state) &&
                   state.Muted;
        }

        #endregion

        #region Slider Methods

        public void SetMasterVolume(float value) =>
            SetVolume(AudioBus.Master, value);

        public void SetMusicVolume(float value) =>
            SetVolume(AudioBus.Music, value);

        public void SetUIVolume(float value) =>
            SetVolume(AudioBus.UI, value);

        public void SetPlayerVehicleVolume(float value) =>
            SetVolume(AudioBus.PlayerVehicle, value);

        public void SetOpponentVehicleVolume(float value) =>
            SetVolume(AudioBus.OpponentVehicle, value);

        public void SetPlayerWeaponVolume(float value) =>
            SetVolume(AudioBus.PlayerWeapon, value);

        public void SetOpponentWeaponVolume(float value) =>
            SetVolume(AudioBus.OpponentWeapon, value);

        public void SetImpactVolume(float value) =>
            SetVolume(AudioBus.Impact, value);

        public void SetExplosionVolume(float value) =>
            SetVolume(AudioBus.Explosion, value);

        public void SetAmbienceVolume(float value) =>
            SetVolume(AudioBus.Ambience, value);

        #endregion

        #region Toggle Methods

        public void SetMasterMuted(bool muted) =>
            SetMuted(AudioBus.Master, muted);

        public void SetMusicMuted(bool muted) =>
            SetMuted(AudioBus.Music, muted);

        public void SetUIMuted(bool muted) =>
            SetMuted(AudioBus.UI, muted);

        public void SetPlayerVehicleMuted(bool muted) =>
            SetMuted(AudioBus.PlayerVehicle, muted);

        public void SetOpponentVehicleMuted(bool muted) =>
            SetMuted(AudioBus.OpponentVehicle, muted);

        public void SetPlayerWeaponMuted(bool muted) =>
            SetMuted(AudioBus.PlayerWeapon, muted);

        public void SetOpponentWeaponMuted(bool muted) =>
            SetMuted(AudioBus.OpponentWeapon, muted);

        public void SetImpactMuted(bool muted) =>
            SetMuted(AudioBus.Impact, muted);

        public void SetExplosionMuted(bool muted) =>
            SetMuted(AudioBus.Explosion, muted);

        public void SetAmbienceMuted(bool muted) =>
            SetMuted(AudioBus.Ambience, muted);

        #endregion

        #region Persistence

        public void LoadSettings()
        {
            for (int i = 0; i < AllBuses.Length; i++)
            {
                AudioBus bus = AllBuses[i];

                if (!states.TryGetValue(bus, out BusState state))
                    continue;

                string volumeKey = GetVolumeKey(bus);
                string muteKey = GetMuteKey(bus);

                if (PlayerPrefs.HasKey(volumeKey))
                {
                    state.Volume =
                        Mathf.Clamp01(
                            PlayerPrefs.GetFloat(
                                volumeKey,
                                state.DefaultVolume));
                }

                if (PlayerPrefs.HasKey(muteKey))
                {
                    state.Muted =
                        PlayerPrefs.GetInt(
                            muteKey,
                            0) != 0;
                }
            }
        }

        public void SaveSettings()
        {
            for (int i = 0; i < AllBuses.Length; i++)
            {
                AudioBus bus = AllBuses[i];

                if (!states.TryGetValue(bus, out BusState state))
                    continue;

                PlayerPrefs.SetFloat(
                    GetVolumeKey(bus),
                    state.Volume);

                PlayerPrefs.SetInt(
                    GetMuteKey(bus),
                    state.Muted ? 1 : 0);
            }

            PlayerPrefs.Save();
        }

        [ContextMenu("Reset Saved Audio Settings")]
        public void ResetToMixerDefaults()
        {
            for (int i = 0; i < AllBuses.Length; i++)
            {
                AudioBus bus = AllBuses[i];

                if (!states.TryGetValue(bus, out BusState state))
                    continue;

                state.Volume = state.DefaultVolume;
                state.Muted = false;

                PlayerPrefs.DeleteKey(
                    GetVolumeKey(bus));

                PlayerPrefs.DeleteKey(
                    GetMuteKey(bus));

                Apply(bus);
            }

            PlayerPrefs.Save();
        }

        #endregion

        #region Initialization

        private void InitializeStates()
        {
            states.Clear();

            for (int i = 0; i < AllBuses.Length; i++)
            {
                AudioBus bus = AllBuses[i];
                string parameter = GetParameterName(bus);

                float normalized = 1f;

                if (!string.IsNullOrWhiteSpace(parameter) &&
                    audioMixer.GetFloat(parameter, out float decibels))
                {
                    normalized =
                        DecibelsToNormalized(decibels);
                }
                else
                {
                    Debug.LogWarning(
                        $"Audio mixer parameter '{parameter}' for {bus} could not be read. " +
                        $"Make sure it is exposed and the name matches exactly.",
                        this);
                }

                states.Add(
                    bus,
                    new BusState(normalized));
            }
        }

        #endregion

        #region Mixer

        private void ApplyAll()
        {
            for (int i = 0; i < AllBuses.Length; i++)
                Apply(AllBuses[i]);
        }

        private void Apply(AudioBus bus)
        {
            if (!states.TryGetValue(bus, out BusState state))
                return;

            string parameter =
                GetParameterName(bus);

            if (string.IsNullOrWhiteSpace(parameter))
                return;

            float decibels =
                state.Muted
                    ? minimumDecibels
                    : NormalizedToDecibels(
                        state.Volume);

            if (!audioMixer.SetFloat(
                    parameter,
                    decibels))
            {
                Debug.LogWarning(
                    $"Could not set AudioMixer parameter '{parameter}'.",
                    this);

                return;
            }

            debugLastChangedBus = bus.ToString();
            debugLastNormalizedVolume = state.Volume;
            debugLastDecibels = decibels;
            debugLastMuted = state.Muted;
        }

        private string GetParameterName(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Master:
                    return masterParameter;

                case AudioBus.Music:
                    return musicParameter;

                case AudioBus.UI:
                    return uiParameter;

                case AudioBus.PlayerVehicle:
                    return playerVehicleParameter;

                case AudioBus.OpponentVehicle:
                    return opponentVehicleParameter;

                case AudioBus.PlayerWeapon:
                    return playerWeaponParameter;

                case AudioBus.OpponentWeapon:
                    return opponentWeaponParameter;

                case AudioBus.Impact:
                    return impactParameter;

                case AudioBus.Explosion:
                    return explosionParameter;

                case AudioBus.Ambience:
                    return ambienceParameter;

                default:
                    return string.Empty;
            }
        }

        private float NormalizedToDecibels(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);

            if (normalized <= 0.0001f)
                return minimumDecibels;

            return Mathf.Max(
                minimumDecibels,
                20f * Mathf.Log10(normalized));
        }

        private float DecibelsToNormalized(float decibels)
        {
            if (decibels <= minimumDecibels)
                return 0f;

            return Mathf.Clamp01(
                Mathf.Pow(
                    10f,
                    decibels / 20f));
        }

        #endregion

        #region Keys

        private string GetVolumeKey(AudioBus bus)
        {
            return
                playerPrefsPrefix +
                bus +
                ".Volume";
        }

        private string GetMuteKey(AudioBus bus)
        {
            return
                playerPrefsPrefix +
                bus +
                ".Muted";
        }

        #endregion
    }
}