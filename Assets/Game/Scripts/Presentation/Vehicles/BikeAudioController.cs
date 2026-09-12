using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(BikeMotor))]
    [RequireComponent(typeof(RacerViewController))]
    public class BikeAudioController : MonoBehaviour
    {
        #region References

        [Header("Audio Sources")]
        [SerializeField] private AudioSource engineSource;
        [SerializeField] private AudioSource windSource;
        [SerializeField] private AudioSource boostLoopSource;
        [SerializeField] private AudioSource oneShotSource;

        #endregion

        #region Clips

        [Header("Engine")]
        [SerializeField] private AudioClip engineLoopClip;

        [Header("Wind")]
        [SerializeField] private AudioClip windLoopClip;

        [Header("Boost")]
        [SerializeField] private AudioClip boostStartClip;
        [SerializeField] private AudioClip boostLoopClip;
        [SerializeField] private AudioClip boostStopClip;

        #endregion

        #region Player / Opponent Mix

        [Header("Player / Opponent Mix")]
        [Tooltip("Final engine-volume multiplier for the player's own bike.")]
        [Range(0f, 2f)][SerializeField] private float playerEngineVolumeMultiplier = 1.15f;

        [Tooltip("Final engine-volume multiplier for opponent bikes. Keep substantially lower because many opponents can be audible simultaneously.")]
        [Range(0f, 2f)][SerializeField] private float opponentEngineVolumeMultiplier = 0.35f;

        [Tooltip("Spatial blend of the player's engine. Lower values make it feel anchored to the cockpit rather than entirely outside the listener.")]
        [Range(0f, 1f)][SerializeField] private float playerEngineSpatialBlend = 0.25f;

        [Tooltip("Opponent engines should normally remain fully positional.")]
        [Range(0f, 1f)][SerializeField] private float opponentEngineSpatialBlend = 1f;

        [Tooltip("Final boost-volume multiplier for the player's bike.")]
        [Range(0f, 2f)][SerializeField] private float playerBoostVolumeMultiplier = 1f;

        [Tooltip("Final boost-volume multiplier for opponent bikes.")]
        [Range(0f, 2f)][SerializeField] private float opponentBoostVolumeMultiplier = 0.45f;

        [Range(0f, 1f)][SerializeField] private float playerBoostSpatialBlend = 0.35f;
        [Range(0f, 1f)][SerializeField] private float opponentBoostSpatialBlend = 1f;

        [Tooltip("Volume multiplier for the player's boost start/stop sounds.")]
        [Range(0f, 2f)][SerializeField] private float playerOneShotVolumeMultiplier = 1f;

        [Tooltip("Volume multiplier for opponent boost start/stop sounds.")]
        [Range(0f, 2f)][SerializeField] private float opponentOneShotVolumeMultiplier = 0.45f;

        #endregion

        #region Engine Settings

        [Header("Engine Settings")]
        [Range(0f, 1f)][SerializeField] private float idleEngineVolume = 0.2f;
        [Range(0f, 1f)][SerializeField] private float maximumEngineVolume = 0.85f;

        [Tooltip("Engine volume multiplier while moving quickly with little/no throttle.")]
        [Range(0f, 1f)][SerializeField] private float coastingVolumeMultiplier = 0.55f;

        [Range(0.1f, 3f)][SerializeField] private float idleEnginePitch = 0.75f;
        [Range(0.1f, 3f)][SerializeField] private float maximumEnginePitch = 1.4f;

        [Tooltip("How much stationary throttle can raise the simulated engine RPM.")]
        [Range(0f, 1f)][SerializeField] private float stationaryThrottleRevInfluence = 0.7f;

        [Min(0.1f)][SerializeField] private float engineResponse = 8f;

        #endregion

        #region Wind Settings

        [Header("Wind Settings")]
        [SerializeField] private bool windPlayerOnly = true;

        [Range(0f, 1f)][SerializeField] private float windStartSpeed = 0.15f;
        [Range(0f, 1f)][SerializeField] private float maximumWindVolume = 0.75f;

        [Range(0.1f, 3f)][SerializeField] private float minimumWindPitch = 0.8f;
        [Range(0.1f, 3f)][SerializeField] private float maximumWindPitch = 1.2f;

        [Min(0.1f)][SerializeField] private float windResponse = 5f;

        #endregion

        #region Boost Settings

        [Header("Boost Settings")]
        [Range(0f, 1f)][SerializeField] private float boostStartVolume = 1f;
        [Range(0f, 1f)][SerializeField] private float boostLoopVolume = 0.8f;
        [Range(0f, 1f)][SerializeField] private float boostStopVolume = 0.8f;

        [Min(0.1f)][SerializeField] private float boostFadeResponse = 12f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private bool debugIsPlayer;
        [SerializeField] private float debugSpeedNormalized;
        [SerializeField] private float debugThrottle;
        [SerializeField] private float debugEngineVolume;
        [SerializeField] private float debugEnginePitch;
        [SerializeField] private float debugWindVolume;
        [SerializeField] private float debugWindPitch;
        [SerializeField] private bool debugBoosting;
        [SerializeField] private float debugBoostVolume;

        #endregion

        private BikeMotor motor;
        private RacerViewController racerView;
        private RaceEquipmentSystem equipment;

        private bool initialized;
        private bool isPlayer;
        private bool previousBoosting;

        private void Awake()
        {
            motor = GetComponent<BikeMotor>();
            racerView = GetComponent<RacerViewController>();

            PrepareSources();
        }

        private void Update()
        {
            if (!initialized && !TryInitialize())
                return;

            UpdateEngineAudio();
            UpdateWindAudio();
            UpdateBoostAudio();
        }

        private void OnDisable()
        {
            StopAllAudio();
        }

        private void OnEnable()
        {
            if (initialized)
                StartContinuousAudio();
        }

        #region Initialization

        private bool TryInitialize()
        {
            if (motor == null ||
                racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null ||
                racerView.Participant.Vehicle == null)
            {
                return false;
            }

            equipment =
                racerView.Participant.Vehicle
                    .EquipmentSystem;

            if (equipment == null)
                return false;

            isPlayer =
                racerView.Participant.Role ==
                RaceParticipantRole.Player;

            previousBoosting =
                equipment.IsBoosterActive;

            initialized = true;

            debugInitialized = true;
            debugIsPlayer = isPlayer;

            ConfigureRoleMix();
            StartContinuousAudio();

            if (previousBoosting)
                StartBoostLoop();

            return true;
        }

        private void PrepareSources()
        {
            PrepareLoopSource(
                engineSource,
                engineLoopClip);

            PrepareLoopSource(
                windSource,
                windLoopClip);

            PrepareLoopSource(
                boostLoopSource,
                boostLoopClip);

            if (oneShotSource != null)
            {
                oneShotSource.playOnAwake = false;
                oneShotSource.loop = false;
            }
        }

        private void PrepareLoopSource(
            AudioSource source,
            AudioClip clip)
        {
            if (source == null)
                return;

            source.playOnAwake = false;
            source.loop = true;

            if (clip != null)
                source.clip = clip;
        }

        private void ConfigureRoleMix()
        {
            if (engineSource != null)
            {
                engineSource.spatialBlend =
                    isPlayer
                        ? playerEngineSpatialBlend
                        : opponentEngineSpatialBlend;
            }

            if (boostLoopSource != null)
            {
                boostLoopSource.spatialBlend =
                    isPlayer
                        ? playerBoostSpatialBlend
                        : opponentBoostSpatialBlend;
            }

            if (oneShotSource != null)
            {
                oneShotSource.spatialBlend =
                    isPlayer
                        ? playerBoostSpatialBlend
                        : opponentBoostSpatialBlend;
            }
        }

        private void StartContinuousAudio()
        {
            if (engineSource != null &&
                engineLoopClip != null)
            {
                engineSource.clip = engineLoopClip;
                engineSource.loop = true;

                if (!engineSource.isPlaying)
                    engineSource.Play();
            }

            bool allowWind =
                !windPlayerOnly ||
                isPlayer;

            if (allowWind &&
                windSource != null &&
                windLoopClip != null)
            {
                windSource.clip = windLoopClip;
                windSource.loop = true;

                if (!windSource.isPlaying)
                    windSource.Play();
            }
            else if (windSource != null)
            {
                windSource.Stop();
                windSource.volume = 0f;
            }
        }

        #endregion

        #region Engine

        private void UpdateEngineAudio()
        {
            if (engineSource == null ||
                engineLoopClip == null)
            {
                return;
            }

            float speed =
                motor.NormalizedSpeed;

            float throttle =
                motor.ThrottleInput;

            debugSpeedNormalized = speed;
            debugThrottle = throttle;

            float throttleRev =
                throttle *
                stationaryThrottleRevInfluence;

            float rpm =
                Mathf.Clamp01(
                    Mathf.Max(
                        speed,
                        throttleRev));

            float targetPitch =
                Mathf.Lerp(
                    idleEnginePitch,
                    maximumEnginePitch,
                    rpm);

            float activity =
                Mathf.Clamp01(
                    Mathf.Max(
                        speed,
                        throttle * 0.75f));

            float targetVolume =
                Mathf.Lerp(
                    idleEngineVolume,
                    maximumEngineVolume,
                    activity);

            if (speed > 0.1f)
            {
                float loadMultiplier =
                    Mathf.Lerp(
                        coastingVolumeMultiplier,
                        1f,
                        throttle);

                targetVolume *=
                    loadMultiplier;
            }

            targetVolume *=
                isPlayer
                    ? playerEngineVolumeMultiplier
                    : opponentEngineVolumeMultiplier;

            targetVolume =
                Mathf.Clamp01(
                    targetVolume);

            float response =
                CalculateResponse(
                    engineResponse);

            engineSource.pitch =
                Mathf.Lerp(
                    engineSource.pitch,
                    targetPitch,
                    response);

            engineSource.volume =
                Mathf.Lerp(
                    engineSource.volume,
                    targetVolume,
                    response);

            debugEnginePitch =
                engineSource.pitch;

            debugEngineVolume =
                engineSource.volume;
        }

        #endregion

        #region Wind

        private void UpdateWindAudio()
        {
            bool allowWind =
                !windPlayerOnly ||
                isPlayer;

            if (!allowWind ||
                windSource == null ||
                windLoopClip == null)
            {
                debugWindVolume = 0f;
                return;
            }

            float windAmount =
                Mathf.InverseLerp(
                    windStartSpeed,
                    1f,
                    motor.NormalizedSpeed);

            windAmount =
                Mathf.Clamp01(
                    windAmount);

            float targetVolume =
                windAmount *
                maximumWindVolume;

            float targetPitch =
                Mathf.Lerp(
                    minimumWindPitch,
                    maximumWindPitch,
                    windAmount);

            float response =
                CalculateResponse(
                    windResponse);

            windSource.volume =
                Mathf.Lerp(
                    windSource.volume,
                    targetVolume,
                    response);

            windSource.pitch =
                Mathf.Lerp(
                    windSource.pitch,
                    targetPitch,
                    response);

            debugWindVolume =
                windSource.volume;

            debugWindPitch =
                windSource.pitch;
        }

        #endregion

        #region Boost

        private void UpdateBoostAudio()
        {
            if (equipment == null)
                return;

            bool boosting =
                equipment.IsBoosterActive;

            debugBoosting =
                boosting;

            if (boosting != previousBoosting)
            {
                if (boosting)
                    OnBoostStarted();
                else
                    OnBoostStopped();

                previousBoosting =
                    boosting;
            }

            UpdateBoostLoopVolume(
                boosting);
        }

        private void OnBoostStarted()
        {
            PlayOneShot(
                boostStartClip,
                boostStartVolume);

            StartBoostLoop();
        }

        private void OnBoostStopped()
        {
            PlayOneShot(
                boostStopClip,
                boostStopVolume);
        }

        private void StartBoostLoop()
        {
            if (boostLoopSource == null ||
                boostLoopClip == null)
            {
                return;
            }

            boostLoopSource.clip =
                boostLoopClip;

            boostLoopSource.loop = true;

            if (!boostLoopSource.isPlaying)
            {
                boostLoopSource.volume = 0f;
                boostLoopSource.Play();
            }
        }

        private void UpdateBoostLoopVolume(
            bool boosting)
        {
            if (boostLoopSource == null ||
                boostLoopClip == null)
            {
                debugBoostVolume = 0f;
                return;
            }

            float roleMultiplier =
                isPlayer
                    ? playerBoostVolumeMultiplier
                    : opponentBoostVolumeMultiplier;

            float targetVolume =
                boosting
                    ? boostLoopVolume *
                      roleMultiplier
                    : 0f;

            targetVolume =
                Mathf.Clamp01(
                    targetVolume);

            float response =
                CalculateResponse(
                    boostFadeResponse);

            boostLoopSource.volume =
                Mathf.Lerp(
                    boostLoopSource.volume,
                    targetVolume,
                    response);

            if (!boosting &&
                boostLoopSource.isPlaying &&
                boostLoopSource.volume <= 0.001f)
            {
                boostLoopSource.volume = 0f;
                boostLoopSource.Stop();
            }

            debugBoostVolume =
                boostLoopSource.volume;
        }

        #endregion

        #region Helpers

        private void PlayOneShot(
            AudioClip clip,
            float volume)
        {
            if (oneShotSource == null ||
                clip == null)
            {
                return;
            }

            float roleMultiplier =
                isPlayer
                    ? playerOneShotVolumeMultiplier
                    : opponentOneShotVolumeMultiplier;

            oneShotSource.PlayOneShot(
                clip,
                Mathf.Clamp01(
                    volume *
                    roleMultiplier));
        }

        private float CalculateResponse(
            float responseSpeed)
        {
            return 1f -
                   Mathf.Exp(
                       -Mathf.Max(
                           0.01f,
                           responseSpeed) *
                       Time.deltaTime);
        }

        private void StopAllAudio()
        {
            if (engineSource != null)
                engineSource.Stop();

            if (windSource != null)
                windSource.Stop();

            if (boostLoopSource != null)
                boostLoopSource.Stop();

            if (oneShotSource != null)
                oneShotSource.Stop();
        }

        #endregion
    }
}