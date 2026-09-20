using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [DefaultExecutionOrder(1000)]
    public class PlayerCockpitView : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Rider eye transform. This should be parented beneath the transform that receives the bike's visual lean.")]
        [SerializeField] private Transform cameraAnchor;

        [Tooltip("Root of the player camera hierarchy that should survive bike destruction.")]
        [SerializeField] private Transform cameraRigRoot;

        [Tooltip("Camera used by this bike if it belongs to the player.")]
        [SerializeField] private Camera cockpitCamera;

        [Tooltip("Audio listener belonging to the cockpit camera.")]
        [SerializeField] private AudioListener audioListener;

        [Tooltip("Optional player-only reticle canvas.")]
        [SerializeField] private Canvas reticleCanvas;

        [Tooltip("Player-only diegetic windshield HUD.")]
        [SerializeField]
        private Canvas windshieldHudCanvas;

        [Header("View")]
        [Min(1f)][SerializeField] private float normalFieldOfView = 75f;
        [Min(1f)][SerializeField] private float boostedFieldOfView = 82f;
        [Min(0f)][SerializeField] private float fieldOfViewResponse = 8f;

        [Header("Death View")]
        [Min(1f)][SerializeField] private float deathFieldOfView = 80f;
        [Min(0f)][SerializeField] private float deathHoldDuration = 0.2f;
        [Min(0.1f)][SerializeField] private float deathDriftDuration = 2.5f;
        [Min(0f)][SerializeField] private float deathBackwardDistance = 6f;
        [Min(0f)][SerializeField] private float deathUpwardDistance = 2f;
        [Min(0f)][SerializeField] private float deathLookResponse = 4f;

        [Header("Collision Camera Kick")]
        [Tooltip("Maximum positional camera displacement from a full-strength impact.")]
        [Min(0f)][SerializeField] private float maximumCollisionPositionKick = 0.16f;

        [Tooltip("Maximum pitch rotation from a full-strength impact.")]
        [Min(0f)][SerializeField] private float maximumCollisionPitchKick = 4.5f;

        [Tooltip("Maximum roll rotation from a full-strength side impact.")]
        [Min(0f)][SerializeField] private float maximumCollisionRollKick = 5.5f;

        [Tooltip("How quickly collision displacement returns to neutral.")]
        [Min(0.01f)][SerializeField] private float collisionKickReturnTime = 0.16f;

        [Tooltip("Prevents repeated impacts from displacing the camera excessively.")]
        [Min(0f)][SerializeField] private float maximumAccumulatedPositionKick = 0.25f;

        [Tooltip("Maximum accumulated rotational kick.")]
        [Min(0f)][SerializeField] private float maximumAccumulatedRotationKick = 8f;

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

        [SerializeField] private bool debugDeathView;
        [SerializeField] private bool debugCameraDetached;
        [SerializeField] private bool debugDeathControllerCreated;

        [SerializeField] private float debugCurrentFov;
        [SerializeField] private Vector3 debugCollisionPositionOffset;
        [SerializeField] private Vector3 debugCollisionRotationOffset;

        private RacerViewController racerView;

        private bool resolved;
        private bool activePlayerView;
        private bool deathViewActive;

        private Vector3 collisionPositionOffset;
        private Vector3 collisionPositionVelocity;

        private Vector3 collisionRotationOffset;
        private Vector3 collisionRotationVelocity;

        public Camera CockpitCamera => cockpitCamera;
        public bool IsActivePlayerView => activePlayerView;
        public bool IsDeathViewActive => deathViewActive;

        private void Awake()
        {
            debugAwakeCalled = true;

            racerView =
                GetComponentInParent<
                    RacerViewController>();
            debugHasRacerView = racerView != null;

            if (cockpitCamera == null)
                cockpitCamera = GetComponentInChildren<Camera>(true);

            if (audioListener == null &&
                cockpitCamera != null)
            {
                audioListener =
                    cockpitCamera.GetComponent<AudioListener>();
            }

            if (cameraRigRoot == null &&
                cockpitCamera != null)
            {
                cameraRigRoot =
                    cockpitCamera.transform;
            }

            SetViewActive(false);
        }

        private void Update()
        {
            UpdateDebugState();

            if (!resolved)
                TryResolvePlayer();

            if (deathViewActive)
                return;

            if (!activePlayerView ||
                racerView.Participant?.Vehicle == null)
            {
                return;
            }

            UpdateFieldOfView();
        }

        private void LateUpdate()
        {
            if (deathViewActive ||
                !activePlayerView ||
                cockpitCamera == null ||
                cameraAnchor == null)
            {
                return;
            }

            UpdateCollisionKick(Time.deltaTime);
            ApplyCameraPose();
        }

        private void OnDestroy()
        {
            if (!activePlayerView &&
                !deathViewActive)
            {
                return;
            }

            UnlockCursor();
        }

        #region Initialization

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

            debugPlayerCameraActive =
                activePlayerView;

            SetViewActive(
                activePlayerView);

            if (!activePlayerView)
                return;

            SnapCamera();

            if (cockpitCamera != null)
            {
                cockpitCamera.fieldOfView =
                    normalFieldOfView;

                debugCurrentFov =
                    cockpitCamera.fieldOfView;
            }

            if (lockCursor)
            {
                Cursor.lockState =
                    CursorLockMode.Locked;

                Cursor.visible =
                    false;
            }
        }

        #endregion

        #region Camera Pose

        private void ApplyCameraPose()
        {
            /*
             * The anchor supplies the complete baseline cockpit pose.
             *
             * Because the anchor is beneath VisualLeanRoot, any visual
             * banking/rolling of the bike is inherited automatically.
             */
            Vector3 basePosition =
                cameraAnchor.position;

            Quaternion baseRotation =
                cameraAnchor.rotation;

            /*
             * Collision movement remains local to the cockpit.
             */
            Vector3 finalPosition =
                basePosition +
                baseRotation *
                collisionPositionOffset;

            Quaternion finalRotation =
                baseRotation *
                Quaternion.Euler(
                    collisionRotationOffset);

            cockpitCamera.transform
                .SetPositionAndRotation(
                    finalPosition,
                    finalRotation);
        }

        private void SnapCamera()
        {
            if (cockpitCamera == null ||
                cameraAnchor == null)
            {
                return;
            }

            ClearCollisionImpulse();

            cockpitCamera.transform
                .SetPositionAndRotation(
                    cameraAnchor.position,
                    cameraAnchor.rotation);
        }

        #endregion

        #region Collision Kick

        public void ApplyCollisionImpulse(
            Vector3 worldNormal,
            float severity)
        {
            if (!activePlayerView ||
                deathViewActive ||
                cameraAnchor == null)
            {
                return;
            }

            severity =
                Mathf.Clamp01(
                    severity);

            if (severity <= 0f ||
                worldNormal.sqrMagnitude < 0.001f)
            {
                return;
            }

            Vector3 localNormal =
                cameraAnchor.InverseTransformDirection(
                    worldNormal.normalized);

            if (localNormal.sqrMagnitude < 0.001f)
                return;

            localNormal.Normalize();

            Vector3 positionalKick =
                localNormal *
                maximumCollisionPositionKick *
                severity;

            collisionPositionOffset +=
                positionalKick;

            collisionPositionOffset =
                Vector3.ClampMagnitude(
                    collisionPositionOffset,
                    maximumAccumulatedPositionKick);

            Vector3 rotationalKick =
                new Vector3(
                    -localNormal.z *
                    maximumCollisionPitchKick *
                    severity,
                    0f,
                    localNormal.x *
                    maximumCollisionRollKick *
                    severity);

            collisionRotationOffset +=
                rotationalKick;

            collisionRotationOffset =
                Vector3.ClampMagnitude(
                    collisionRotationOffset,
                    maximumAccumulatedRotationKick);

            debugCollisionPositionOffset =
                collisionPositionOffset;

            debugCollisionRotationOffset =
                collisionRotationOffset;
        }

        private void UpdateCollisionKick(
            float deltaTime)
        {
            float smoothTime =
                Mathf.Max(
                    0.01f,
                    collisionKickReturnTime);

            collisionPositionOffset =
                Vector3.SmoothDamp(
                    collisionPositionOffset,
                    Vector3.zero,
                    ref collisionPositionVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    deltaTime);

            collisionRotationOffset =
                Vector3.SmoothDamp(
                    collisionRotationOffset,
                    Vector3.zero,
                    ref collisionRotationVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    deltaTime);

            debugCollisionPositionOffset =
                collisionPositionOffset;

            debugCollisionRotationOffset =
                collisionRotationOffset;
        }

        private void ClearCollisionImpulse()
        {
            collisionPositionOffset =
                Vector3.zero;

            collisionPositionVelocity =
                Vector3.zero;

            collisionRotationOffset =
                Vector3.zero;

            collisionRotationVelocity =
                Vector3.zero;

            debugCollisionPositionOffset =
                Vector3.zero;

            debugCollisionRotationOffset =
                Vector3.zero;
        }

        #endregion

        #region Death View

        public void PrepareForDestruction(
            Vector3 destructionPosition)
        {
            if (!activePlayerView ||
                deathViewActive)
            {
                return;
            }

            deathViewActive = true;
            debugDeathView = true;

            ClearCollisionImpulse();

            if (cockpitCamera != null &&
                cameraAnchor != null)
            {
                cockpitCamera.transform
                    .SetPositionAndRotation(
                        cameraAnchor.position,
                        cameraAnchor.rotation);
            }

            if (cameraRigRoot == null)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerCockpitView)} has no camera rig root to preserve.",
                    this);

                return;
            }

            if (racerView != null &&
                    cameraRigRoot.IsChildOf(
                        racerView.transform))
            {
                cameraRigRoot.SetParent(
                    null,
                    true);

                debugCameraDetached = true;
            }

            PlayerDeathCameraController deathController =
                cameraRigRoot.GetComponent<
                    PlayerDeathCameraController>();

            if (deathController == null)
            {
                deathController =
                    cameraRigRoot.gameObject.AddComponent<
                        PlayerDeathCameraController>();
            }

            deathController.Initialize(
                destructionPosition,
                deathHoldDuration,
                deathDriftDuration,
                deathBackwardDistance,
                deathUpwardDistance,
                deathLookResponse);

            debugDeathControllerCreated = true;

            if (cockpitCamera != null)
            {
                cockpitCamera.enabled = true;
                cockpitCamera.fieldOfView = deathFieldOfView;

                debugCurrentFov =
                    cockpitCamera.fieldOfView;
            }

            if (audioListener != null)
                audioListener.enabled = true;

            SetReticleVisible(
                false);

            debugBoosting = false;

            UnlockCursor();
        }

        #endregion

        #region Field Of View

        private void UpdateFieldOfView()
        {
            if (cockpitCamera == null)
                return;

            bool boosting =
                racerView.Participant.Vehicle
                    .EquipmentSystem
                    .IsBoosterActive;

            debugBoosting =
                boosting;

            float targetFov =
                boosting
                    ? boostedFieldOfView
                    : normalFieldOfView;

            if (fieldOfViewResponse <= 0f)
            {
                cockpitCamera.fieldOfView =
                    targetFov;
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

        #endregion

        #region View

        private void SetViewActive(
            bool active)
        {
            if (cockpitCamera != null)
                cockpitCamera.enabled = active;

            if (audioListener != null)
                audioListener.enabled = active;

            if (reticleCanvas != null)
                reticleCanvas.enabled = active;

            if (windshieldHudCanvas != null)
                windshieldHudCanvas.enabled = active;
        }

        public void SetReticleVisible(
            bool visible)
        {
            if (reticleCanvas == null)
                return;

            reticleCanvas.enabled =
                visible &&
                activePlayerView &&
                !deathViewActive;
        }

        #endregion

        #region Debug

        private void UpdateDebugState()
        {
            debugHasRacerView =
                racerView != null;

            debugRacerInitialized =
                racerView != null &&
                racerView.IsInitialized;

            debugHasParticipant =
                racerView != null &&
                racerView.Participant != null;

            debugRole =
                racerView?.Participant != null
                    ? racerView.Participant.Role.ToString()
                    : "None";

            debugPlayerCameraActive =
                activePlayerView;
        }

        #endregion

        #region Cursor

        private void UnlockCursor()
        {
            if (!lockCursor)
                return;

            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;
        }

        #endregion
    }
}