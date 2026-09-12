using System.Collections.Generic;
using RaceFatal.Combat;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class RaceWeaponPresenter : MonoBehaviour
    {
        #region Configuration

        [Header("Hitscan / Area")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Tooltip("Maximum number of hits inspected by a single hitscan shot.")]
        [Min(4)][SerializeField] private int hitscanBufferSize = 32;

        [Header("Weapon Presentation")]
        [Tooltip("Presentation profile for each weapon definition that can appear in the race.")]
        [SerializeField] private List<WeaponPresentationProfile> weaponProfiles =
            new List<WeaponPresentationProfile>();

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private string debugLastShooter;
        [SerializeField] private string debugLastWeapon;
        [SerializeField] private bool debugPresentationFound;
        [SerializeField] private bool debugUsedPlayerAim;
        [SerializeField] private bool debugLastShotHit;
        [SerializeField] private string debugLastVictim = "None";
        [SerializeField] private Vector3 debugLastFireDirection;
        [SerializeField] private Vector3 debugLastHitPoint;

        #endregion

        #region Runtime

        private readonly Dictionary<string, WeaponPresentationProfile>
            presentationLookup =
                new Dictionary<string, WeaponPresentationProfile>();

        private RaceRuntimeController runtime;
        private RaycastHit[] hitscanBuffer;

        #endregion

        #region Unity

        private void Awake()
        {
            hitscanBuffer =
                new RaycastHit[
                    Mathf.Max(
                        4,
                        hitscanBufferSize)];

            BuildPresentationLookup();
        }

        private void OnDestroy()
        {
            UnsubscribeFromRuntime();
        }

        #endregion

        #region Initialization

        public void Initialize(
            RaceRuntimeController raceRuntime)
        {
            UnsubscribeFromRuntime();

            runtime = raceRuntime;

            if (runtime == null)
            {
                Debug.LogError(
                    "RaceWeaponPresenter requires a RaceRuntimeController.",
                    this);

                return;
            }

            if (runtime.Director == null)
            {
                Debug.LogError(
                    "RaceRuntimeController does not have an initialized RaceDirector.",
                    this);

                return;
            }

            runtime.Director.WeaponFired +=
                OnWeaponFired;
        }

        private void UnsubscribeFromRuntime()
        {
            if (runtime == null ||
                runtime.Director == null)
            {
                return;
            }

            runtime.Director.WeaponFired -=
                OnWeaponFired;
        }

        private void BuildPresentationLookup()
        {
            presentationLookup.Clear();

            for (int i = 0;
                 i < weaponProfiles.Count;
                 i++)
            {
                WeaponPresentationProfile profile =
                    weaponProfiles[i];

                if (profile == null ||
                    string.IsNullOrWhiteSpace(
                        profile.WeaponDefinitionId))
                {
                    continue;
                }

                if (presentationLookup.ContainsKey(
                        profile.WeaponDefinitionId))
                {
                    Debug.LogWarning(
                        $"Duplicate WeaponPresentationProfile for " +
                        $"'{profile.WeaponDefinitionId}'. " +
                        "The later profile will replace the earlier one.",
                        this);
                }

                presentationLookup[
                    profile.WeaponDefinitionId] =
                        profile;
            }
        }

        #endregion

        #region Weapon Fired

        private void OnWeaponFired(
            WeaponFireEvent fireEvent)
        {
            if (runtime == null ||
                runtime.Director == null)
            {
                return;
            }

            if (!runtime.TryGetRacerView(
                    fireEvent.RacerId,
                    out RacerViewController racer))
            {
                Debug.LogWarning(
                    $"No RacerViewController was found for racer " +
                    $"'{fireEvent.RacerId}'.",
                    this);

                return;
            }

            if (!racer.TryGetEquipmentMount(
                    fireEvent.EquipmentId,
                    out BikeEquipmentMountView mount))
            {
                Debug.LogWarning(
                    $"Racer '{fireEvent.RacerId}' has no physical mount bound " +
                    $"to equipment '{fireEvent.EquipmentId}'.",
                    racer);

                return;
            }

            Transform origin =
                mount.EquipmentOrigin;

            WeaponPresentationProfile profile =
                GetPresentationProfile(
                    fireEvent.DefinitionId);

            debugLastShooter =
                fireEvent.RacerId;

            debugLastWeapon =
                fireEvent.DefinitionId;

            debugPresentationFound =
                profile != null;

            Vector3 fireDirection =
                ResolveFireDirection(
                    racer,
                    origin,
                    fireEvent.Range);

            debugLastFireDirection =
                fireDirection;

            debugLastShotHit = false;
            debugLastVictim = "None";

            PlayFireFeedback(
                mount,
                origin,
                profile);

            switch (fireEvent.DeliveryMode)
            {
                case WeaponDeliveryMode.Hitscan:
                    FireHitscan(
                        fireEvent,
                        origin,
                        fireDirection);
                    break;

                case WeaponDeliveryMode.Projectile:
                    SpawnProjectile(
                        fireEvent,
                        profile,
                        origin,
                        fireDirection);
                    break;

                case WeaponDeliveryMode.GuidedProjectile:
                    SpawnProjectile(
                        fireEvent,
                        profile,
                        origin,
                        fireDirection);
                    break;

                case WeaponDeliveryMode.Dropped:
                    SpawnProjectile(
                        fireEvent,
                        profile,
                        origin,
                        origin != null
                            ? origin.forward
                            : transform.forward);
                    break;

                case WeaponDeliveryMode.Area:
                    FireArea(
                        fireEvent,
                        origin);
                    break;

                default:
                    Debug.LogWarning(
                        $"Unsupported weapon delivery mode " +
                        $"'{fireEvent.DeliveryMode}'.",
                        this);
                    break;
            }
        }

        private WeaponPresentationProfile GetPresentationProfile(
            string definitionId)
        {
            if (string.IsNullOrWhiteSpace(
                    definitionId))
            {
                return null;
            }

            if (presentationLookup.TryGetValue(
                    definitionId,
                    out WeaponPresentationProfile profile))
            {
                return profile;
            }

            Debug.LogWarning(
                $"No WeaponPresentationProfile is registered for " +
                $"weapon definition '{definitionId}'.",
                this);

            return null;
        }

        #endregion

        #region Fire Feedback

        private void PlayFireFeedback(
            BikeEquipmentMountView mount,
            Transform origin,
            WeaponPresentationProfile profile)
        {
            if (mount == null ||
                profile == null)
            {
                return;
            }

            WeaponMountFeedbackView feedback =
                mount.GetComponentInChildren<
                    WeaponMountFeedbackView>(true);

            if (feedback == null)
                return;

            feedback.PlayFire(
                profile,
                origin);
        }

        #endregion

        #region Aim

        private Vector3 ResolveFireDirection(
            RacerViewController racer,
            Transform origin,
            float range)
        {
            debugUsedPlayerAim = false;

            if (origin == null)
                return transform.forward;

            PlayerWeaponAim playerAim =
                racer.GetComponent<PlayerWeaponAim>();

            if (playerAim != null &&
                playerAim.TryGetAimDirection(
                    origin,
                    range,
                    hitMask,
                    out Vector3 aimDirection))
            {
                debugUsedPlayerAim = true;
                return aimDirection;
            }

            return origin.forward;
        }

        #endregion

        #region Hitscan

        private void FireHitscan(
            WeaponFireEvent fireEvent,
            Transform origin,
            Vector3 direction)
        {
            if (origin == null ||
                direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            direction.Normalize();

            if (!TryGetFirstHitIgnoringOwner(
                    origin.position,
                    direction,
                    fireEvent.Range,
                    fireEvent.RacerId,
                    out RaycastHit hit))
            {
                return;
            }

            debugLastShotHit = true;
            debugLastHitPoint = hit.point;

            RacerViewController victim =
                hit.collider.GetComponentInParent<RacerViewController>();

            if (victim == null ||
                !victim.IsInitialized)
            {
                return;
            }

            if (victim.RacerId ==
                fireEvent.RacerId)
            {
                return;
            }

            debugLastVictim =
                victim.RacerId;

            runtime.Director.ApplyDamage(
                fireEvent.RacerId,
                victim.RacerId,
                fireEvent.Damage,
                DamageCause.Weapon);
        }

        private bool TryGetFirstHitIgnoringOwner(
            Vector3 origin,
            Vector3 direction,
            float range,
            string ownerRacerId,
            out RaycastHit nearestHit)
        {
            nearestHit = default;

            int hitCount =
                Physics.RaycastNonAlloc(
                    origin,
                    direction,
                    hitscanBuffer,
                    range,
                    hitMask,
                    QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
                return false;

            bool found = false;

            float nearestDistance =
                float.PositiveInfinity;

            for (int i = 0;
                 i < hitCount;
                 i++)
            {
                RaycastHit hit =
                    hitscanBuffer[i];

                if (hit.collider == null)
                    continue;

                RacerViewController hitRacer =
                    hit.collider
                        .GetComponentInParent<RacerViewController>();

                if (hitRacer != null &&
                    hitRacer.IsInitialized &&
                    hitRacer.RacerId ==
                        ownerRacerId)
                {
                    continue;
                }

                if (hit.distance >=
                    nearestDistance)
                {
                    continue;
                }

                nearestDistance =
                    hit.distance;

                nearestHit =
                    hit;

                found = true;
            }

            return found;
        }

        #endregion

        #region Projectiles

        private void SpawnProjectile(
            WeaponFireEvent fireEvent,
            WeaponPresentationProfile profile,
            Transform origin,
            Vector3 direction)
        {
            if (origin == null)
                return;

            if (profile == null)
            {
                Debug.LogWarning(
                    $"Weapon '{fireEvent.DefinitionId}' cannot spawn a projectile " +
                    "because it has no WeaponPresentationProfile.",
                    this);

                return;
            }

            ProjectileView prefab =
                profile.ProjectilePrefab;

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Weapon presentation profile '{profile.name}' does not " +
                    $"have a Projectile Prefab assigned.",
                    profile);

                return;
            }

            Quaternion rotation =
                CalculateProjectileRotation(
                    origin,
                    direction);

            ProjectileView projectile =
                Instantiate(
                    prefab,
                    origin.position,
                    rotation);

            projectile.Initialize(
                runtime,
                fireEvent.RacerId,
                fireEvent.Damage,
                fireEvent.ProjectileSpeed,
                fireEvent.Range,
                direction);
        }

        private Quaternion CalculateProjectileRotation(
            Transform origin,
            Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return origin.rotation;

            direction.Normalize();

            Vector3 up =
                origin.up;

            if (Mathf.Abs(
                    Vector3.Dot(
                        direction,
                        up)) >
                0.98f)
            {
                up = origin.right;
            }

            return Quaternion.LookRotation(
                direction,
                up);
        }

        #endregion

        #region Area

        private void FireArea(
            WeaponFireEvent fireEvent,
            Transform origin)
        {
            if (origin == null)
                return;

            Collider[] hits =
                Physics.OverlapSphere(
                    origin.position,
                    fireEvent.Range,
                    hitMask,
                    QueryTriggerInteraction.Ignore);

            HashSet<string> damagedRacerIds =
                new HashSet<string>();

            foreach (Collider hit in hits)
            {
                if (hit == null)
                    continue;

                RacerViewController victim =
                    hit.GetComponentInParent<RacerViewController>();

                if (victim == null ||
                    !victim.IsInitialized)
                {
                    continue;
                }

                if (victim.RacerId ==
                    fireEvent.RacerId)
                {
                    continue;
                }

                if (!damagedRacerIds.Add(
                        victim.RacerId))
                {
                    continue;
                }

                runtime.Director.ApplyDamage(
                    fireEvent.RacerId,
                    victim.RacerId,
                    fireEvent.Damage,
                    DamageCause.Weapon);
            }
        }

        #endregion
    }
}