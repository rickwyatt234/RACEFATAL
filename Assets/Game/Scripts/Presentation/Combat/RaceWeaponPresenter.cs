using System.Collections.Generic;
using RaceFatal.Combat;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class RaceWeaponPresenter : MonoBehaviour
    {
        [Header("Hitscan / Area")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Tooltip("Maximum number of hits inspected by a single hitscan shot.")]
        [Min(4)][SerializeField] private int hitscanBufferSize = 32;

        [Header("Weapon Presentation")]
        [Tooltip("Presentation profile for each weapon definition that can appear in the race.")]
        [SerializeField] private List<WeaponPresentationProfile> weaponProfiles =
            new List<WeaponPresentationProfile>();

        [Header("Runtime Debug")]
        [SerializeField] private string debugLastShooter;
        [SerializeField] private string debugLastWeapon;
        [SerializeField] private bool debugPresentationFound;
        [SerializeField] private bool debugUsedPlayerAim;
        [SerializeField] private bool debugLastShotHit;
        [SerializeField] private string debugLastVictim = "None";
        [SerializeField] private Vector3 debugLastFireDirection;
        [SerializeField] private Vector3 debugLastHitPoint;

        [SerializeField] private bool debugGuidedShot;
        [SerializeField] private bool debugGuidedTargetFound;
        [SerializeField] private string debugGuidedTarget = "None";
        [SerializeField] private float debugGuidedTargetDistance;
        [SerializeField] private float debugGuidedTargetAngle;

        private readonly Dictionary<string, WeaponPresentationProfile>
            presentationLookup =
                new Dictionary<string, WeaponPresentationProfile>();

        private RaceRuntimeController runtime;
        private RaycastHit[] hitscanBuffer;

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

            debugLastShooter = fireEvent.RacerId;
            debugLastWeapon = fireEvent.DefinitionId;
            debugPresentationFound = profile != null;

            debugGuidedShot = false;
            debugGuidedTargetFound = false;
            debugGuidedTarget = "None";
            debugGuidedTargetDistance = 0f;
            debugGuidedTargetAngle = 0f;

            Vector3 fireDirection =
                ResolveFireDirection(
                    racer,
                    origin,
                    fireEvent.Range);

            debugLastFireDirection = fireDirection;
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
                    SpawnGuidedProjectile(
                        fireEvent,
                        profile,
                        racer,
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
            if (string.IsNullOrWhiteSpace(definitionId))
                return null;

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

        #region Feedback

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
                hit.collider.GetComponentInParent<
                    RacerViewController>();

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
                    hit.collider.GetComponentInParent<
                        RacerViewController>();

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

                nearestHit = hit;
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
            if (!TryGetProjectilePrefab(
                    fireEvent,
                    profile,
                    origin,
                    out ProjectileView prefab))
            {
                return;
            }

            ProjectileView projectile =
                SpawnProjectileInstance(
                    prefab,
                    origin,
                    direction);

            projectile.Initialize(
                runtime,
                fireEvent.RacerId,
                fireEvent.Damage,
                fireEvent.ProjectileSpeed,
                fireEvent.Range,
                direction);
        }

        private void SpawnGuidedProjectile(
            WeaponFireEvent fireEvent,
            WeaponPresentationProfile profile,
            RacerViewController shooter,
            Transform origin,
            Vector3 direction)
        {
            debugGuidedShot = true;

            if (!TryGetProjectilePrefab(
                    fireEvent,
                    profile,
                    origin,
                    out ProjectileView prefab))
            {
                return;
            }

            GuidedProjectileView guidedPrefab =
                prefab as GuidedProjectileView;

            if (guidedPrefab == null)
            {
                Debug.LogWarning(
                    $"Weapon '{fireEvent.DefinitionId}' uses GuidedProjectile delivery, " +
                    $"but its projectile prefab does not use {nameof(GuidedProjectileView)}. " +
                    "It will fly straight.",
                    prefab);

                ProjectileView fallback =
                    SpawnProjectileInstance(
                        prefab,
                        origin,
                        direction);

                fallback.Initialize(
                    runtime,
                    fireEvent.RacerId,
                    fireEvent.Damage,
                    fireEvent.ProjectileSpeed,
                    fireEvent.Range,
                    direction);

                return;
            }

            RacerViewController target =
                FindGuidedTarget(
                    shooter,
                    origin,
                    direction,
                    fireEvent.Range,
                    guidedPrefab);

            GuidedProjectileView projectile =
                Instantiate(
                    guidedPrefab,
                    origin.position,
                    CalculateProjectileRotation(
                        origin,
                        direction));

            projectile.Initialize(
                runtime,
                fireEvent.RacerId,
                fireEvent.Damage,
                fireEvent.ProjectileSpeed,
                fireEvent.Range,
                direction);

            projectile.InitializeGuidance(
                target);
        }

        private bool TryGetProjectilePrefab(
            WeaponFireEvent fireEvent,
            WeaponPresentationProfile profile,
            Transform origin,
            out ProjectileView prefab)
        {
            prefab = null;

            if (origin == null)
                return false;

            if (profile == null)
            {
                Debug.LogWarning(
                    $"Weapon '{fireEvent.DefinitionId}' cannot spawn a projectile " +
                    "because it has no WeaponPresentationProfile.",
                    this);

                return false;
            }

            prefab = profile.ProjectilePrefab;

            if (prefab != null)
                return true;

            Debug.LogWarning(
                $"Weapon presentation profile '{profile.name}' does not " +
                "have a Projectile Prefab assigned.",
                profile);

            return false;
        }

        private ProjectileView SpawnProjectileInstance(
            ProjectileView prefab,
            Transform origin,
            Vector3 direction)
        {
            Quaternion rotation =
                CalculateProjectileRotation(
                    origin,
                    direction);

            return Instantiate(
                prefab,
                origin.position,
                rotation);
        }

        private Quaternion CalculateProjectileRotation(
            Transform origin,
            Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return origin.rotation;

            direction.Normalize();

            Vector3 up = origin.up;

            if (Mathf.Abs(Vector3.Dot(direction, up)) > 0.98f)
                up = origin.right;

            return Quaternion.LookRotation(
                direction,
                up);
        }

        #endregion

        #region Guided Targeting

        private RacerViewController FindGuidedTarget(
            RacerViewController shooter,
            Transform origin,
            Vector3 fireDirection,
            float weaponRange,
            GuidedProjectileView guidedPrefab)
        {
            if (shooter == null ||
                shooter.Participant == null ||
                origin == null ||
                guidedPrefab == null)
            {
                return null;
            }

            if (fireDirection.sqrMagnitude < 0.001f)
                fireDirection = origin.forward;

            fireDirection.Normalize();

            RaceParticipant shooterParticipant =
                shooter.Participant;

            RacerViewController bestTarget = null;

            float bestScore = float.PositiveInfinity;
            float bestDistance = 0f;
            float bestAngle = 0f;

            foreach (RaceParticipant candidate
                     in runtime.Director.State.Participants)
            {
                if (candidate == null ||
                    candidate.RacerId ==
                    shooterParticipant.RacerId)
                {
                    continue;
                }

                if (candidate.Status !=
                    RaceParticipantStatus.Racing)
                {
                    continue;
                }

                if (candidate.Vehicle == null ||
                    candidate.Vehicle.IsDestroyed)
                {
                    continue;
                }

                if (string.Equals(
                        candidate.TeamId,
                        shooterParticipant.TeamId,
                        System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (!runtime.TryGetRacerView(
                        candidate.RacerId,
                        out RacerViewController candidateView))
                {
                    continue;
                }

                if (candidateView == null ||
                    !candidateView.IsInitialized)
                {
                    continue;
                }

                Vector3 toTarget =
                    candidateView.transform.position -
                    origin.position;

                float distance =
                    toTarget.magnitude;

                if (distance <
                        guidedPrefab.MinimumAcquisitionDistance ||
                    distance >
                        weaponRange)
                {
                    continue;
                }

                if (toTarget.sqrMagnitude < 0.001f)
                    continue;

                float angle =
                    Vector3.Angle(
                        fireDirection,
                        toTarget.normalized);

                if (angle >
                    guidedPrefab.AcquisitionHalfAngle)
                {
                    continue;
                }

                /*
                 * Favor whatever racer is closest to the player's
                 * crosshair/muzzle direction. Distance is a smaller
                 * secondary preference.
                 */
                float angleScore =
                    angle /
                    Mathf.Max(
                        0.01f,
                        guidedPrefab.AcquisitionHalfAngle);

                float distanceScore =
                    distance /
                    Mathf.Max(
                        0.01f,
                        weaponRange);

                float score =
                    angleScore * 0.8f +
                    distanceScore * 0.2f;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTarget = candidateView;
                bestDistance = distance;
                bestAngle = angle;
            }

            if (bestTarget != null)
            {
                debugGuidedTargetFound = true;
                debugGuidedTarget = bestTarget.RacerId;
                debugGuidedTargetDistance = bestDistance;
                debugGuidedTargetAngle = bestAngle;
            }

            return bestTarget;
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
                    hit.GetComponentInParent<
                        RacerViewController>();

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