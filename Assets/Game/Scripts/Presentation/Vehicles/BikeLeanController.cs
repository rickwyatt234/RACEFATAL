using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(BikeMotor))]
    public class BikeLeanController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Visual-only root containing the motorcycle model and mounted equipment.")]
        [SerializeField] private Transform visualLeanRoot;

        [Tooltip("Pivot used to lean the player's cockpit view. CockpitCameraAnchor should be a child of this pivot.")]
        [SerializeField] private Transform cameraLeanPivot;

        [Header("Bike Lean")]
        [Range(0f, 70f)][SerializeField] private float maximumLeanAngle = 48f;

        [Tooltip("Speed where steering can produce the full configured lean.")]
        [Min(0.1f)][SerializeField] private float fullLeanSpeed = 25f;

        [Tooltip("Below this speed the motorcycle progressively remains upright.")]
        [Min(0f)][SerializeField] private float minimumLeanSpeed = 3f;

        [Tooltip("How quickly the motorcycle enters a lean.")]
        [Min(0f)][SerializeField] private float leanResponse = 8f;

        [Tooltip("How quickly the motorcycle returns upright.")]
        [Min(0f)][SerializeField] private float uprightResponse = 10f;

        [Header("Cockpit Lean")]
        [Tooltip("Fraction of the motorcycle lean applied to the cockpit pivot.")]
        [Range(0f, 1f)][SerializeField] private float cameraLeanMultiplier = 0.55f;

        [Header("Runtime Debug")]
        [SerializeField] private float debugSteering;
        [SerializeField] private float debugSpeedKph;
        [SerializeField] private float debugTargetLean;
        [SerializeField] private float debugCurrentLean;

        private BikeMotor motor;

        private Quaternion visualBaseRotation;
        private Quaternion cameraBaseRotation;

        private float currentLean;

        private void Awake()
        {
            motor = GetComponent<BikeMotor>();

            if (visualLeanRoot != null)
                visualBaseRotation = visualLeanRoot.localRotation;

            if (cameraLeanPivot != null)
                cameraBaseRotation = cameraLeanPivot.localRotation;
        }

        private void LateUpdate()
        {
            if (motor == null)
                return;

            float steering = motor.SteeringInput;
            float speed = motor.SpeedMetersPerSecond;

            float speedFactor = Mathf.InverseLerp(
                minimumLeanSpeed,
                fullLeanSpeed,
                speed);

            float targetLean =
                -steering *
                maximumLeanAngle *
                speedFactor;

            float response =
                Mathf.Abs(targetLean) > Mathf.Abs(currentLean)
                    ? leanResponse
                    : uprightResponse;

            float factor =
                response <= 0f
                    ? 1f
                    : 1f - Mathf.Exp(-response * Time.deltaTime);

            currentLean = Mathf.Lerp(
                currentLean,
                targetLean,
                factor);

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
                visualLeanRoot.localRotation =
                    visualBaseRotation *
                    Quaternion.AngleAxis(
                        currentLean,
                        Vector3.forward);
            }

            if (cameraLeanPivot != null)
            {
                cameraLeanPivot.localRotation =
                    cameraBaseRotation *
                    Quaternion.AngleAxis(
                        currentLean * cameraLeanMultiplier,
                        Vector3.forward);
            }
        }

        private void OnDisable()
        {
            currentLean = 0f;

            if (visualLeanRoot != null)
                visualLeanRoot.localRotation = visualBaseRotation;

            if (cameraLeanPivot != null)
                cameraLeanPivot.localRotation = cameraBaseRotation;
        }
    }
}