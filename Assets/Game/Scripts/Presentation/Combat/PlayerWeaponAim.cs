using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(RacerViewController))]
    [RequireComponent(typeof(PlayerCockpitView))]
    public class PlayerWeaponAim : MonoBehaviour
    {
        [Header("Aim")]
        [Tooltip("Maximum number of physics hits checked by the camera aim ray.")]
        [Min(4)][SerializeField] private int raycastBufferSize = 32;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugActive;
        [SerializeField] private bool debugCameraHit;
        [SerializeField] private string debugAimTarget = "None";
        [SerializeField] private float debugAimDistance;
        [SerializeField] private Vector3 debugAimPoint;
        [SerializeField] private Vector3 debugFireDirection;

        private RacerViewController racerView;
        private PlayerCockpitView cockpitView;
        private RaycastHit[] raycastBuffer;

        public bool IsActive =>
            racerView != null &&
            racerView.IsInitialized &&
            racerView.Participant != null &&
            racerView.Participant.Role == RaceParticipantRole.Player &&
            cockpitView != null &&
            cockpitView.IsActivePlayerView &&
            cockpitView.CockpitCamera != null &&
            cockpitView.CockpitCamera.enabled;

        private void Awake()
        {
            racerView = GetComponent<RacerViewController>();
            cockpitView = GetComponent<PlayerCockpitView>();

            raycastBuffer =
                new RaycastHit[
                    Mathf.Max(
                        4,
                        raycastBufferSize)];
        }

        private void Update()
        {
            debugActive = IsActive;
        }

        public bool TryGetAimDirection(
            Transform muzzle,
            float range,
            LayerMask hitMask,
            out Vector3 fireDirection)
        {
            fireDirection =
                muzzle != null
                    ? muzzle.forward
                    : transform.forward;

            if (!IsActive ||
                muzzle == null ||
                range <= 0f)
            {
                return false;
            }

            Camera camera =
                cockpitView.CockpitCamera;

            Ray cameraRay =
                camera.ViewportPointToRay(
                    new Vector3(
                        0.5f,
                        0.5f,
                        0f));

            Vector3 aimPoint =
                cameraRay.origin +
                cameraRay.direction *
                range;

            bool foundHit = false;
            float nearestDistance = float.PositiveInfinity;

            int hitCount =
                Physics.RaycastNonAlloc(
                    cameraRay,
                    raycastBuffer,
                    range,
                    hitMask,
                    QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit =
                    raycastBuffer[i];

                if (hit.collider == null)
                    continue;

                RacerViewController hitRacer =
                    hit.collider
                        .GetComponentInParent<
                            RacerViewController>();

                if (hitRacer == racerView)
                    continue;

                if (hit.distance >= nearestDistance)
                    continue;

                nearestDistance = hit.distance;
                aimPoint = hit.point;
                foundHit = true;
            }

            Vector3 muzzleToAim =
                aimPoint -
                muzzle.position;

            if (muzzleToAim.sqrMagnitude <
                0.001f)
            {
                return false;
            }

            fireDirection =
                muzzleToAim.normalized;

            debugCameraHit = foundHit;
            debugAimTarget =
                foundHit
                    ? "Hit"
                    : "Range Limit";

            debugAimPoint =
                aimPoint;

            debugAimDistance =
                Vector3.Distance(
                    muzzle.position,
                    aimPoint);

            debugFireDirection =
                fireDirection;

            return true;
        }
    }
}