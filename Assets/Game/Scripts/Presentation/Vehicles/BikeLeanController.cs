using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(BikeMotor))]
    public class BikeLeanController : MonoBehaviour
    {
        [Header("References")] [Tooltip("Visual-only root containing the motorcycle model and mounted equipment.")] [SerializeField] private
            Transform visualLeanRoot;
        [Tooltip("Pivot used to lean the player's cockpit view. CockpitCameraAnchor should be a child of this pivot.")] [SerializeField] private
            Transform cameraLeanPivot;
        [Header("Bike Lean")] [Range(0f, 70f)] [SerializeField] private float maximumLeanAngle = 48f;
        [Tooltip("Speed where steering can produce the full configured lean.")] [Min(0.1f)] [SerializeField] private float fullLeanSpeed = 25f;
        [Tooltip("Below this speed the motorcycle progressively remains upright.")] [Min(0f)] [SerializeField] private float minimumLeanSpeed = 3f;
        [Tooltip("How quickly the motorcycle enters a lean.")] [Min(0f)] [SerializeField] private float leanResponse = 8f;
        [Tooltip("How quickly the motorcycle returns upright.")] [Min(0f)] [SerializeField] private float uprightResponse = 10f;
        [Header("AI Lean")]
        [Tooltip("Visual banking limit for non-player racers, based on actual cornering rather than their small steering inputs.")]
        [Range(0f, 45f)] [SerializeField] private float maximumAILeanAngle = 16f;
        [Min(0f)] [SerializeField] private float aiTurnResponse = 6f;
        [Header("Cockpit Lean")] [Tooltip("Fraction of the motorcycle lean applied to the cockpit pivot.")] [Range(0f,
            1f)] [SerializeField] private float cameraLeanMultiplier = 0.55f;
        [Header("Runtime Debug")] [SerializeField] private float debugSteering;
        [SerializeField] private float debugSpeedKph;
        [SerializeField] private float debugTargetLean;
        [SerializeField] private float debugCurrentLean;
        private BikeMotor motor;
        private RacerViewController racerView;
        private Vector3 previousForward;
        private float aiYawRate;
        private Quaternion visualBaseRotation;
        private Quaternion cameraBaseRotation;
        private float currentLean;
        private void Awake()
        {
            motor = GetComponent<BikeMotor>();
            racerView = GetComponent<RacerViewController>();
            previousForward = transform.forward;
            if (visualLeanRoot != null)
                visualBaseRotation = visualLeanRoot.localRotation;
            if (cameraLeanPivot != null)
                cameraBaseRotation = cameraLeanPivot.localRotation;
        }

        private void LateUpdate()
        {
            if (RacePauseController.IsGameplayBlocked)
                return;
            if (motor == null)
                return;
            float steering = motor.SteeringInput;
            float speed = motor.SpeedMetersPerSecond;
            float speedFactor = Mathf.InverseLerp(minimumLeanSpeed, fullLeanSpeed, speed);
            bool isAI = racerView?.Participant != null && racerView.Participant.Role != RaceFatal.Racing.RaceParticipantRole.Player;
            Vector3 oldForward = Vector3.ProjectOnPlane(previousForward, transform.up);
            float yawRate = Time.deltaTime > 0f && oldForward.sqrMagnitude > 0.001f
                ? Vector3.SignedAngle(oldForward, transform.forward, transform.up) * Mathf.Deg2Rad / Time.deltaTime : 0f;
            previousForward = transform.forward;
            float yawFactor = aiTurnResponse <= 0f ? 1f : 1f - Mathf.Exp(-aiTurnResponse * Time.deltaTime);
            aiYawRate = Mathf.Lerp(aiYawRate, yawRate, yawFactor);
            float targetLean = isAI
                ? -Mathf.Clamp(Mathf.Atan(speed * aiYawRate / 9.81f) * Mathf.Rad2Deg, -maximumAILeanAngle, maximumAILeanAngle) * speedFactor
                : -steering * maximumLeanAngle * speedFactor;
            float response = Mathf.Abs(targetLean) > Mathf.Abs(currentLean) ? leanResponse : uprightResponse;
            response *= motor.LeanResponseMultiplier;
            float factor = response <= 0f ? 1f : 1f - Mathf.Exp(-response * Time.deltaTime);
            currentLean = Mathf.Lerp(currentLean, targetLean, factor);
            ApplyLean();
            debugSteering = steering;
            debugSpeedKph = speed * 3.6f;
            debugTargetLean = targetLean;
            debugCurrentLean = currentLean;
        }

        private void ApplyLean()
        {
            if (visualLeanRoot != null)
            {
                visualLeanRoot.localRotation = visualBaseRotation * Quaternion.AngleAxis(currentLean, Vector3.forward);
            }

            if (cameraLeanPivot != null)
            {
                cameraLeanPivot.localRotation = cameraBaseRotation * Quaternion.AngleAxis(currentLean * cameraLeanMultiplier, Vector3.forward);
            }
        }

        private void OnDisable()
        {
            currentLean = 0f;
            aiYawRate = 0f;
            previousForward = transform.forward;
            if (visualLeanRoot != null)
                visualLeanRoot.localRotation = visualBaseRotation;
            if (cameraLeanPivot != null)
                cameraLeanPivot.localRotation = cameraBaseRotation;
        }
    }
}
