using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(RacerViewController))]
    public class PlayerCockpitView : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform representing the rider's eye position and forward direction.")]
        [SerializeField] private Transform cameraAnchor;

        [Tooltip("Camera used only when this bike belongs to the player.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Audio listener associated with the player camera.")]
        [SerializeField] private AudioListener audioListener;

        [Header("Follow")]
        [Tooltip("How quickly the camera follows cockpit position. Zero snaps immediately.")]
        [Min(0f)][SerializeField] private float positionFollowSpeed = 30f;

        [Tooltip("How quickly the camera follows cockpit rotation. Zero snaps immediately.")]
        [Min(0f)][SerializeField] private float rotationFollowSpeed = 30f;

        [Header("Field Of View")]
        [Min(1f)][SerializeField] private float normalFieldOfView = 75f;
        [Min(1f)][SerializeField] private float boostedFieldOfView = 82f;

        [Tooltip("How quickly FOV reacts to boost state.")]
        [Min(0f)][SerializeField] private float fieldOfViewResponse = 8f;

        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugResolved;
        [SerializeField] private bool debugPlayerViewActive;
        [SerializeField] private bool debugBoostActive;
        [SerializeField] private float debugCurrentFov;

        private RacerViewController racerView;

        private bool roleResolved;
        private bool activeForPlayer;

        private void Awake()
        {
            racerView =
                GetComponent<RacerViewController>();

            if (playerCamera == null)
            {
                playerCamera =
                    GetComponentInChildren<Camera>(true);
            }

            if (audioListener == null &&
                playerCamera != null)
            {
                audioListener =
                    playerCamera.GetComponent<AudioListener>();
            }

            SetCameraEnabled(false);
        }

        private void Start()
        {
            TryResolveRole();
        }

        private void Update()
        {
            if (!roleResolved)
                TryResolveRole();

            if (!activeForPlayer ||
                racerView.Participant?.Vehicle == null)
            {
                return;
            }

            UpdateFieldOfView();
        }

        private void LateUpdate()
        {
            if (!activeForPlayer ||
                playerCamera == null ||
                cameraAnchor == null)
            {
                return;
            }

            float positionFactor =
                positionFollowSpeed <= 0f
                    ? 1f
                    : 1f - Mathf.Exp(
                        -positionFollowSpeed *
                        Time.deltaTime);

            float rotationFactor =
                rotationFollowSpeed <= 0f
                    ? 1f
                    : 1f - Mathf.Exp(
                        -rotationFollowSpeed *
                        Time.deltaTime);

            playerCamera.transform.position =
                Vector3.Lerp(
                    playerCamera.transform.position,
                    cameraAnchor.position,
                    positionFactor);

            playerCamera.transform.rotation =
                Quaternion.Slerp(
                    playerCamera.transform.rotation,
                    cameraAnchor.rotation,
                    rotationFactor);
        }

        private void TryResolveRole()
        {
            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null)
            {
                return;
            }

            roleResolved = true;
            debugResolved = true;

            activeForPlayer =
                racerView.Participant.Role ==
                RaceParticipantRole.Player;

            debugPlayerViewActive =
                activeForPlayer;

            SetCameraEnabled(
                activeForPlayer);

            if (!activeForPlayer)
                return;

            SnapToAnchor();

            if (playerCamera != null)
            {
                playerCamera.fieldOfView =
                    normalFieldOfView;
            }

            if (lockCursor)
            {
                Cursor.lockState =
                    CursorLockMode.Locked;

                Cursor.visible =
                    false;
            }
        }

        private void UpdateFieldOfView()
        {
            bool boosting =
                racerView.Participant
                    .Vehicle
                    .EquipmentSystem
                    .IsBoosterActive;

            debugBoostActive =
                boosting;

            float targetFov =
                boosting
                    ? boostedFieldOfView
                    : normalFieldOfView;

            float factor =
                fieldOfViewResponse <= 0f
                    ? 1f
                    : 1f - Mathf.Exp(
                        -fieldOfViewResponse *
                        Time.deltaTime);

            playerCamera.fieldOfView =
                Mathf.Lerp(
                    playerCamera.fieldOfView,
                    targetFov,
                    factor);

            debugCurrentFov =
                playerCamera.fieldOfView;
        }

        private void SnapToAnchor()
        {
            if (playerCamera == null ||
                cameraAnchor == null)
            {
                return;
            }

            playerCamera.transform.SetPositionAndRotation(
                cameraAnchor.position,
                cameraAnchor.rotation);
        }

        private void SetCameraEnabled(
            bool enabled)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled =
                    enabled;
            }

            if (audioListener != null)
            {
                audioListener.enabled =
                    enabled;
            }
        }

        private void OnDestroy()
        {
            if (!activeForPlayer ||
                !lockCursor)
            {
                return;
            }

            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;
        }
    }
}