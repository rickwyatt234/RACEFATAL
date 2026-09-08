using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(RacerViewController))]
    public class PlayerCockpitView : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform representing the rider's eye position and orientation.")]
        [SerializeField] private Transform cameraAnchor;

        [Tooltip("Camera used by this bike if it belongs to the player.")]
        [SerializeField] private Camera cockpitCamera;

        [Tooltip("Audio listener belonging to the cockpit camera.")]
        [SerializeField] private AudioListener audioListener;

        [Tooltip("Optional player-only reticle canvas.")]
        [SerializeField] private Canvas reticleCanvas;

        [Header("View")]
        [Min(1f)][SerializeField] private float normalFieldOfView = 75f;
        [Min(1f)][SerializeField] private float boostedFieldOfView = 82f;
        [Min(0f)][SerializeField] private float fieldOfViewResponse = 8f;

        [Header("Camera Motion")]
        [Tooltip("Zero makes the camera follow the anchor position exactly.")]
        [Min(0f)][SerializeField] private float positionFollowSpeed = 0f;

        [Tooltip("Zero makes the camera follow the anchor rotation exactly.")]
        [Min(0f)][SerializeField] private float rotationFollowSpeed = 0f;

        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugAwakeCalled;
        [SerializeField] private bool debugHasRacerView;
        [SerializeField] private bool debugRacerInitialized;
        [SerializeField] private bool debugHasParticipant;
        [SerializeField] private string debugRole = "Unknown";

        [SerializeField] private bool debugResolved;
        [SerializeField] private bool debugPlayerCameraActive;
        [SerializeField] private bool debugBoosting;
        [SerializeField] private float debugCurrentFov;

        private RacerViewController racerView;
        private bool resolved;
        private bool activePlayerView;

        public Camera CockpitCamera => cockpitCamera;
        public bool IsActivePlayerView => activePlayerView;

        private void Awake()
        {
            debugAwakeCalled = true;

            racerView = GetComponent<RacerViewController>();
            debugHasRacerView = racerView != null;

            if (cockpitCamera == null)
                cockpitCamera = GetComponentInChildren<Camera>(true);

            if (audioListener == null && cockpitCamera != null)
                audioListener = cockpitCamera.GetComponent<AudioListener>();

            SetViewActive(false);
        }

        private void Update()
        {
            UpdateDebugState();

            if (!resolved)
                TryResolvePlayer();

            if (!activePlayerView ||
                racerView.Participant?.Vehicle == null)
            {
                return;
            }

            UpdateFieldOfView();
        }

        private void LateUpdate()
        {
            if (!activePlayerView ||
                cockpitCamera == null ||
                cameraAnchor == null)
            {
                return;
            }

            Transform cameraTransform = cockpitCamera.transform;

            if (positionFollowSpeed <= 0f)
            {
                cameraTransform.position = cameraAnchor.position;
            }
            else
            {
                float factor = 1f - Mathf.Exp(-positionFollowSpeed * Time.deltaTime);

                cameraTransform.position = Vector3.Lerp(
                    cameraTransform.position,
                    cameraAnchor.position,
                    factor);
            }

            if (rotationFollowSpeed <= 0f)
            {
                cameraTransform.rotation = cameraAnchor.rotation;
            }
            else
            {
                float factor = 1f - Mathf.Exp(-rotationFollowSpeed * Time.deltaTime);

                cameraTransform.rotation = Quaternion.Slerp(
                    cameraTransform.rotation,
                    cameraAnchor.rotation,
                    factor);
            }
        }

        private void UpdateDebugState()
        {
            debugHasRacerView = racerView != null;
            debugRacerInitialized = racerView != null && racerView.IsInitialized;
            debugHasParticipant = racerView != null && racerView.Participant != null;

            debugRole =
                racerView?.Participant != null
                    ? racerView.Participant.Role.ToString()
                    : "None";
        }

        private void TryResolvePlayer()
        {
            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null)
            {
                return;
            }

            resolved = true;
            debugResolved = true;

            activePlayerView =
                racerView.Participant.Role ==
                RaceParticipantRole.Player;

            debugPlayerCameraActive = activePlayerView;

            SetViewActive(activePlayerView);

            if (!activePlayerView)
                return;

            SnapCamera();

            if (cockpitCamera != null)
            {
                cockpitCamera.fieldOfView = normalFieldOfView;
                debugCurrentFov = cockpitCamera.fieldOfView;
            }

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void UpdateFieldOfView()
        {
            if (cockpitCamera == null)
                return;

            bool boosting =
                racerView.Participant.Vehicle
                    .EquipmentSystem
                    .IsBoosterActive;

            debugBoosting = boosting;

            float targetFov =
                boosting
                    ? boostedFieldOfView
                    : normalFieldOfView;

            if (fieldOfViewResponse <= 0f)
            {
                cockpitCamera.fieldOfView = targetFov;
            }
            else
            {
                float factor =
                    1f -
                    Mathf.Exp(
                        -fieldOfViewResponse *
                        Time.deltaTime);

                cockpitCamera.fieldOfView =
                    Mathf.Lerp(
                        cockpitCamera.fieldOfView,
                        targetFov,
                        factor);
            }

            debugCurrentFov =
                cockpitCamera.fieldOfView;
        }

        private void SnapCamera()
        {
            if (cockpitCamera == null ||
                cameraAnchor == null)
            {
                return;
            }

            cockpitCamera.transform.SetPositionAndRotation(
                cameraAnchor.position,
                cameraAnchor.rotation);
        }

        private void SetViewActive(bool active)
        {
            if (cockpitCamera != null)
                cockpitCamera.enabled = active;

            if (audioListener != null)
                audioListener.enabled = active;

            if (reticleCanvas != null)
                reticleCanvas.enabled = active;
        }

        private void OnDestroy()
        {
            if (!activePlayerView ||
                !lockCursor)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}