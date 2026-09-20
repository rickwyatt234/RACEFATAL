using RaceFatal.Infrastructure.Input;
using RaceFatal.Presentation.Bootstrap;
using UnityEngine;
using UnityEngine.Audio;

namespace RaceFatal.Presentation.Vehicles
{
    public class PlayerFocusModeController :
        MonoBehaviour
    {
        [Header("Focus Audio")]

        [SerializeField]
        private AudioMixer audioMixer;

        [Range(0.1f, 1f)]
        [SerializeField]
        private float focusAudioPitch =
            0.45f;

        [SerializeField]
        private string[] slowedPitchParameters =
        {
            "PlayerVehiclePitch",
            "OpponentVehiclePitch",
            "PlayerWeaponPitch",
            "OpponentWeaponPitch",
            "ImpactPitch",
            "ExplosionPitch",
            "AmbiencePitch"
        };

        [Header("Focus Mode")]

        [Range(0.01f, 1f)]
        [SerializeField]
        private float focusTimeScale =
            0.15f;

        [Tooltip(
            "How quickly the game enters slow motion.")]
        [Min(0f)]
        [SerializeField]
        private float enterSpeed =
            8f;

        [Tooltip(
            "How quickly the game returns to normal speed.")]
        [Min(0f)]
        [SerializeField]
        private float exitSpeed =
            10f;

        [Header("Runtime Debug")]

        [SerializeField]
        private bool focusActive;

        [SerializeField]
        private float currentTimeScale =
            1f;

        private IRaceInputService input;

        private float originalFixedDeltaTime;

        public bool IsFocusActive =>
            focusActive;

        private void Awake()
        {
            originalFixedDeltaTime =
                Time.fixedDeltaTime;
        }

        private void Start()
        {
            ResolveInput();
        }

        private void Update()
        {
            if (input == null)
            {
                ResolveInput();
            }

            focusActive =
                input != null &&
                input.FocusHeld;

            float targetScale =
                focusActive
                    ? focusTimeScale
                    : 1f;

            /*
             * Use unscaled time here because Time.deltaTime
             * itself becomes tiny during slow motion.
             */
            float speed =
                focusActive
                    ? enterSpeed
                    : exitSpeed;

            currentTimeScale =
                Mathf.MoveTowards(
                    Time.timeScale,
                    targetScale,
                    speed *
                    Time.unscaledDeltaTime);

            ApplyTimeScale(
                currentTimeScale);
                
            float focusAmount = Mathf.InverseLerp(
                    1f,
                    focusTimeScale,
                    currentTimeScale);

            float targetPitch = Mathf.Lerp(
                    1f,
                    focusAudioPitch,
                    focusAmount);

            ApplyAudioPitch(targetPitch);
        }
        private void ApplyAudioPitch(
            float pitch)
        {
            if (audioMixer == null)
                return;

            pitch =
                Mathf.Clamp(
                    pitch,
                    0.1f,
                    1f);

            for (int i = 0;
                i < slowedPitchParameters.Length;
                i++)
            {
                string parameter =
                    slowedPitchParameters[i];

                if (string.IsNullOrWhiteSpace(
                        parameter))
                {
                    continue;
                }

                audioMixer.SetFloat(
                    parameter,
                    pitch);
            }
        }
        private void ResolveInput()
        {
            if (BootstrapController.Context == null)
                return;

            input =
                BootstrapController
                    .Context
                    .Input;
        }

        private void ApplyTimeScale(
            float scale)
        {
            scale =
                Mathf.Clamp(
                    scale,
                    0.01f,
                    1f);

            Time.timeScale =
                scale;

            /*
             * Scale the physics timestep as well.
             *
             * Otherwise physics becomes extremely
             * low-frequency during heavy slow motion.
             */
            Time.fixedDeltaTime =
                originalFixedDeltaTime *
                scale;
        }

        private void RestoreNormalTime()
        {
            Time.timeScale =
                1f;

            Time.fixedDeltaTime =
                originalFixedDeltaTime;

            currentTimeScale =
                1f;

            focusActive =
                false;

            ApplyAudioPitch(
                1f);
        }

        private void OnDisable()
        {
            RestoreNormalTime();
        }

        private void OnDestroy()
        {
            RestoreNormalTime();
        }
    }
}