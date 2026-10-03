using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Combat
{
    // Position the reticle after the cockpit has applied its camera framing/kick.
    [DefaultExecutionOrder(1100)]
    [RequireComponent(typeof(PlayerCockpitView))]
    public class PlayerWeaponAim : MonoBehaviour
    {
        [Header("Aim")] [Tooltip("Maximum number of physics hits checked by the reticle aim ray.")] [Min(4)] [SerializeField] private int raycastBufferSize = 32;
        [Header("Runtime Debug")] [SerializeField] private bool debugActive;
        [SerializeField] private bool debugCameraHit;
        [SerializeField] private string debugAimTarget = "None";
        [SerializeField] private float debugAimDistance;
        [SerializeField] private Vector3 debugAimPoint;
        [SerializeField] private Vector3 debugFireDirection;
        private RacerViewController racerView;
        private PlayerCockpitView cockpitView;
        private RaceWeaponPresenter presenter;
        private RaycastHit[] raycastBuffer;
        private RectTransform reticleRoot, fallbackRoot;
        private Image reticleImage;
        private Image[] crosshairImages;
        public bool IsActive => racerView != null && racerView.IsInitialized && racerView.Participant != null && racerView.Participant.Role == RaceParticipantRole.Player && cockpitView != null && cockpitView.IsActivePlayerView && !cockpitView.IsDeathViewActive && cockpitView.CockpitCamera != null && cockpitView.CockpitCamera.enabled;

        private void Awake() => CacheReferences();

        private void CacheReferences()
        {
            if (racerView == null) racerView = GetComponentInParent<RacerViewController>();
            if (cockpitView == null) cockpitView = GetComponent<PlayerCockpitView>();
            if (raycastBuffer == null) raycastBuffer = new RaycastHit[Mathf.Max(4, raycastBufferSize)];
        }

        public void Initialize(RaceWeaponPresenter weaponPresenter)
        {
            CacheReferences();
            presenter = weaponPresenter;
        }

        public static bool UsesGunReticle(WeaponAimMode aimMode, WeaponDeliveryMode deliveryMode) =>
            aimMode == WeaponAimMode.Forward && (deliveryMode == WeaponDeliveryMode.Hitscan ||
                deliveryMode == WeaponDeliveryMode.Projectile || deliveryMode == WeaponDeliveryMode.ConeProjectile);

        private void LateUpdate()
        {
            debugActive = IsActive;
            var canvas = cockpitView?.ReticleCanvas;
            var equipment = racerView?.Participant?.Vehicle?.EquipmentSystem;
            var weapon = equipment?.SelectedWeaponDefinition;
            if (!IsActive || canvas == null || !canvas.enabled || weapon == null ||
                !UsesGunReticle(weapon.AimMode, weapon.DeliveryMode) || racerView.Participant.Vehicle.IsDestroyed)
            {
                HideReticle();
                return;
            }
            WeaponPresentationProfile profile = null;
            if (presenter != null) presenter.TryGetPresentationProfile(weapon.Id, out profile);
            Transform muzzle = racerView.transform;
            if (racerView.TryGetEquipmentMount(equipment.SelectedEquipmentId, out var mount) && mount.EquipmentOrigin != null)
                muzzle = mount.EquipmentOrigin;
            Vector2 viewport = GetReticleViewport(muzzle, weapon.Range, profile);
            if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            {
                HideReticle();
                return;
            }
            EnsureReticle(canvas);
            var parent = canvas.transform as RectTransform;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera ?? cockpitView.CockpitCamera;
            Vector3 screen = cockpitView.CockpitCamera.ViewportToScreenPoint(new Vector3(viewport.x, viewport.y, 0f));
            if (parent == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out Vector2 localPoint))
            {
                HideReticle();
                return;
            }
            reticleRoot.anchoredPosition = localPoint - parent.rect.center;
            reticleRoot.sizeDelta = profile != null ? profile.ReticleSize : new Vector2(36f, 36f);
            reticleImage.sprite = profile?.ReticleSprite;
            reticleImage.color = profile != null ? profile.ReticleColor : new Color(0.4f, 1f, 1f, 0.9f);
            reticleImage.enabled = reticleImage.sprite != null;
            fallbackRoot.gameObject.SetActive(reticleImage.sprite == null);
            foreach (var image in crosshairImages) image.color = reticleImage.color;
            reticleRoot.gameObject.SetActive(true);
        }

        public Vector2 GetReticleViewport(Transform muzzle, float range, WeaponPresentationProfile profile)
        {
            var camera = cockpitView.CockpitCamera;
            Vector2 position;
            if (profile != null && profile.ReticlePlacement == WeaponReticlePlacement.FixedViewport)
                position = profile.ReticleViewportPosition;
            else
            {
                // Project chassis-forward aim into the framed camera. Pitching the
                // camera down moves the crosshair up rather than steering the guns down.
                Vector3 forward = racerView.transform.forward;
                Vector3 origin = muzzle != null ? muzzle.position : racerView.transform.position;
                Vector3 projected = camera.WorldToViewportPoint(origin + forward * Mathf.Max(1f, range));
                position = new Vector2(projected.x, projected.y);
            }
            return position + (profile != null ? profile.ReticleViewportOffset : Vector2.zero);
        }

        public bool TryGetReticleAimDirection(Transform muzzle, float range, LayerMask hitMask,
            WeaponPresentationProfile profile, out Vector3 fireDirection)
        {
            fireDirection = muzzle != null ? muzzle.forward : transform.forward;
            if (!IsActive || muzzle == null || range <= 0f) return false;
            Vector2 viewport = GetReticleViewport(muzzle, range, profile);
            Ray ray = cockpitView.CockpitCamera.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
            float aimDistance = Vector3.Distance(ray.origin, muzzle.position + racerView.transform.forward * range);
            return AimFromRay(muzzle, range, hitMask, ray, out fireDirection, aimDistance);
        }

        // Locking/special weapon callers retain their previous camera-centered path.
        public bool TryGetAimDirection(Transform muzzle, float range, LayerMask hitMask, out Vector3 fireDirection)
        {
            fireDirection = muzzle != null ? muzzle.forward : transform.forward;
            if (!IsActive || muzzle == null || range <= 0f) return false;
            Ray ray = cockpitView.CockpitCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return AimFromRay(muzzle, range, hitMask, ray, out fireDirection);
        }

        private bool AimFromRay(Transform muzzle, float range, LayerMask hitMask, Ray cameraRay, out Vector3 fireDirection, float aimDistance = 0f)
        {
            CacheReferences();
            Vector3 aimPoint = cameraRay.origin + cameraRay.direction * (aimDistance > 0f ? aimDistance : range);
            bool foundHit = false;
            float nearestDistance = float.PositiveInfinity;
            int hitCount = Physics.RaycastNonAlloc(cameraRay, raycastBuffer, range, hitMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = raycastBuffer[i];
                if (hit.collider == null || hit.collider.GetComponentInParent<RacerViewController>() == racerView || hit.distance >= nearestDistance)
                    continue;
                // A camera behind a muzzle can see geometry already behind the gun.
                if (Vector3.Dot(hit.point - muzzle.position, cameraRay.direction) <= 0f) continue;
                nearestDistance = hit.distance;
                aimPoint = hit.point;
                foundHit = true;
            }
            Vector3 muzzleToAim = aimPoint - muzzle.position;
            fireDirection = muzzleToAim.sqrMagnitude < 0.001f ? muzzle.forward : muzzleToAim.normalized;
            debugCameraHit = foundHit;
            debugAimTarget = foundHit ? "Reticle Hit" : "Range Limit";
            debugAimPoint = aimPoint;
            debugAimDistance = Vector3.Distance(muzzle.position, aimPoint);
            debugFireDirection = fireDirection;
            return muzzleToAim.sqrMagnitude >= 0.001f;
        }

        private void EnsureReticle(Canvas canvas)
        {
            if (reticleRoot != null) return;
            var root = new GameObject("Selected Weapon Reticle", typeof(RectTransform), typeof(Image));
            reticleRoot = root.GetComponent<RectTransform>();
            reticleRoot.SetParent(canvas.transform, false);
            reticleRoot.anchorMin = reticleRoot.anchorMax = reticleRoot.pivot = new Vector2(0.5f, 0.5f);
            reticleImage = root.GetComponent<Image>();
            reticleImage.raycastTarget = false;
            reticleImage.preserveAspect = true;
            var fallback = new GameObject("Default Crosshair", typeof(RectTransform));
            fallbackRoot = fallback.GetComponent<RectTransform>();
            fallbackRoot.SetParent(reticleRoot, false);
            fallbackRoot.anchorMin = Vector2.zero;
            fallbackRoot.anchorMax = Vector2.one;
            fallbackRoot.offsetMin = fallbackRoot.offsetMax = Vector2.zero;
            crosshairImages = new[]
            {
                CreateLine(new Vector2(0.15f, 0.5f), new Vector2(0.3f, 0.5f), new Vector2(0f, 2f)),
                CreateLine(new Vector2(0.7f, 0.5f), new Vector2(0.85f, 0.5f), new Vector2(0f, 2f)),
                CreateLine(new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.3f), new Vector2(2f, 0f)),
                CreateLine(new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.85f), new Vector2(2f, 0f))
            };
            root.SetActive(false);
        }

        private Image CreateLine(Vector2 minimum, Vector2 maximum, Vector2 thickness)
        {
            var line = new GameObject("Crosshair Line", typeof(RectTransform), typeof(Image));
            var rect = line.GetComponent<RectTransform>();
            rect.SetParent(fallbackRoot, false);
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = thickness;
            var image = line.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private void HideReticle() { if (reticleRoot != null) reticleRoot.gameObject.SetActive(false); }
        private void OnDisable() => HideReticle();
        private void OnDestroy() { if (reticleRoot != null) Destroy(reticleRoot.gameObject); }
    }
}
