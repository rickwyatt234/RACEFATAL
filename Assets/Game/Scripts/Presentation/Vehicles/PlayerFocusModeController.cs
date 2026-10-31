using RaceFatal.Infrastructure.Input;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.Racing;
using UnityEngine;
using UnityEngine.Audio;

namespace RaceFatal.Presentation.Vehicles
{
    public class PlayerFocusModeController : MonoBehaviour
    {
        [Header("Focus Audio")] [SerializeField] private AudioMixer audioMixer;
        [Range(0.1f, 1f)] [SerializeField] private float focusAudioPitch = 0.45f;
        [SerializeField] private string[]
            slowedPitchParameters = {  "PlayerVehiclePitch",  "OpponentVehiclePitch",  "PlayerWeaponPitch",  "OpponentWeaponPitch",  "ImpactPitch",  "ExplosionPitch",  "AmbiencePitch" };
        [Header("Focus Mode")] [Range(0.01f, 1f)] [SerializeField] private float focusTimeScale = 0.15f;
        [Tooltip("How quickly the game enters slow motion.")] [Min(0f)] [SerializeField] private float enterSpeed = 8f;
        [Tooltip("How quickly the game returns to normal speed.")] [Min(0f)] [SerializeField] private float exitSpeed = 10f;
        [Header("Tactical Return")]
        [Tooltip("Unscaled seconds to settle into focus speed after returning to the cockpit.")]
        [Min(0.01f)] [SerializeField] private float tacticalFocusDuration = 0.25f;
        [Tooltip("Unscaled seconds to ease from focus speed back to racing.")]
        [Min(0.01f)] [SerializeField] private float tacticalReturnDuration = 1.1f;
        private bool wasTactical;
        private bool returningFromTactical;
        private float returnElapsed;
        private float returnStartScale;
        [Header("Runtime Debug")] [SerializeField] private bool focusActive;
        [SerializeField] private float currentTimeScale = 1f;
        private IRaceInputService input;
        private RaceRuntimeController runtime;
        private float originalFixedDeltaTime;
        public bool IsFocusActive => focusActive;

        private void Awake()
        {
            originalFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void Start()
        {
            runtime = FindFirstObjectByType<RaceRuntimeController>();
            ResolveInput();
        }

        private void Update()
        {
            if (RacePauseController.IsGameplayBlocked)
                return;
            if (input == null)
            {
                ResolveInput();
            }

            focusActive = runtime != null && runtime.IsRaceActive && input != null && input.FocusHeld;
            bool tactical = RaceFatal.Presentation.Tactics.TacticalRaceController.IsActive;
            bool raceActive = runtime != null && runtime.IsRaceActive;
            if (wasTactical && !tactical && raceActive)
            {
                returningFromTactical = true;
                returnElapsed = 0f;
                returnStartScale = Time.timeScale;
            }
            wasTactical = tactical;
            if (tactical || !raceActive) returningFromTactical = false;
            float targetScale = tactical ? RaceFatal.Presentation.Tactics.TacticalRaceController.Active.tacticalTimeScale : focusActive ? focusTimeScale : 1f;
            float speed = focusActive ? enterSpeed : exitSpeed;
            currentTimeScale = Mathf.MoveTowards(Time.timeScale, targetScale, speed * Time.unscaledDeltaTime);
            if (returningFromTactical)
            {
                returnElapsed += Time.unscaledDeltaTime;
                float settle = Mathf.Max(0.01f, tacticalFocusDuration);
                float resume = Mathf.Max(0.01f, tacticalReturnDuration);
                if (focusActive) returnElapsed = Mathf.Min(returnElapsed, settle);
                currentTimeScale = returnElapsed < settle
                    ? Mathf.Lerp(returnStartScale, focusTimeScale, Mathf.SmoothStep(0f, 1f, returnElapsed / settle))
                    : Mathf.Lerp(focusTimeScale, 1f, Mathf.SmoothStep(0f, 1f, (returnElapsed - settle) / resume));
                // Holding normal focus keeps the cockpit slowed after tactical closes.
                if (focusActive) currentTimeScale = Mathf.Min(currentTimeScale, focusTimeScale);
                if (returnElapsed >= settle + resume) returningFromTactical = false;
            }
            ApplyTimeScale(currentTimeScale);
            float focusAmount = Mathf.InverseLerp(1f, focusTimeScale, currentTimeScale);
            float targetPitch = Mathf.Lerp(1f, focusAudioPitch, focusAmount);
            ApplyAudioPitch(targetPitch);
        }

        private void ApplyAudioPitch(float pitch)
        {
            if (audioMixer == null)
                return;
            pitch = Mathf.Clamp(pitch, 0.1f, 1f);
            for (int i = 0; i < slowedPitchParameters.Length; i++)
            {
                string parameter = slowedPitchParameters[i];
                if (string.IsNullOrWhiteSpace(parameter))
                {
                    continue;
                }

                audioMixer.SetFloat(parameter, pitch);
            }
        }

        private void ResolveInput()
        {
            if (BootstrapController.Context == null)
                return;
            input = BootstrapController.Context.Input;
        }

        private void ApplyTimeScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.001f, 1f);
            Time.timeScale = scale;
            Time.fixedDeltaTime = originalFixedDeltaTime * scale;
        }

        private void RestoreNormalTime()
        {
            RacePauseController.RestoreGameplayTime(1f, originalFixedDeltaTime);
            currentTimeScale = 1f;
            wasTactical = false;
            returningFromTactical = false;
            focusActive = false;
            ApplyAudioPitch(1f);
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
