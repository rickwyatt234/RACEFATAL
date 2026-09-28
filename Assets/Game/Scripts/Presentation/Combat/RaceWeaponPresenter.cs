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

        [Header("Special Weapon Runtime")]
        [Tooltip("How often passive rear turrets scan for a valid target.")]
        [Min(0.02f)][SerializeField] private float automaticTurretScanInterval = 0.08f;

        [Tooltip("How long flame exposure may be interrupted before the accumulated exposure resets.")]
        [Min(0.05f)][SerializeField] private float flameExposureGrace = 0.45f;

        [Tooltip("Burn damage is batched at this interval instead of producing a damage event every rendered frame.")]
        [Min(0.02f)][SerializeField] private float burnDamageTickInterval = 0.2f;

        private readonly Dictionary<string, WeaponPresentationProfile>
            presentationLookup =
                new Dictionary<string, WeaponPresentationProfile>();

        private RaceRuntimeController runtime;
        private RaycastHit[] hitscanBuffer;

        private readonly Dictionary<string, FlameExposureState>
            flameExposure =
                new Dictionary<string, FlameExposureState>();

        private readonly Dictionary<string, BurnState>
            activeBurns =
                new Dictionary<string, BurnState>();

        private readonly List<string> stateKeyScratch =
            new List<string>();

        private float nextAutomaticTurretScanTime;

        private void Awake()
        {
            hitscanBuffer =
                new RaycastHit[
                    Mathf.Max(
                        4,
                        hitscanBufferSize)];

            BuildPresentationLookup();
        }

        private void Update()
        {
            if (runtime == null ||
                runtime.Director == null ||
                !runtime.IsRaceActive)
            {
                return;
            }

            UpdateAutomaticTurrets();
            UpdateBurns(
                Time.deltaTime);

            PruneFlameExposure();
        }

        private void OnDestroy()
        {
            UnsubscribeFromRuntime();

            flameExposure.Clear();
            activeBurns.Clear();
            stateKeyScratch.Clear();
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
                    out BikeEquipmentMountBinding mount))
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
                    fireEvent);

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
                    SpawnMine(
                        fireEvent,
                        profile,
                        racer,
                        origin);
                    break;

                case WeaponDeliveryMode.Area:
                    FireArea(
                        fireEvent,
                        origin);
                    break;

                case WeaponDeliveryMode.ConeProjectile:
                    FireConeProjectiles(
                        fireEvent,
                        profile,
                        origin,
                        fireDirection);
                    break;

                case WeaponDeliveryMode.Ram:
                    ActivateRam(
                        fireEvent,
                        profile,
                        racer);
                    break;

                case WeaponDeliveryMode.FlameCone:
                    FireFlameCone(
                        fireEvent,
                        profile,
                        racer,
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
            if (TryGetPresentationProfile(
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

        public bool TryGetPresentationProfile(
            string definitionId,
            out WeaponPresentationProfile profile)
        {
            profile = null;

            if (string.IsNullOrWhiteSpace(
                    definitionId))
            {
                return false;
            }

            return presentationLookup.TryGetValue(
                definitionId,
                out profile);
        }

        #endregion

        #region Feedback

        private void PlayFireFeedback(
            BikeEquipmentMountBinding mount,
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
            WeaponFireEvent fireEvent)
        {
            debugUsedPlayerAim = false;

            if (origin == null)
                return transform.forward;

            if (fireEvent.AimMode ==
                WeaponAimMode.RearTargeted)
            {
                RacerViewController target =
                    FindAutomaticRearTarget(
                        racer,
                        origin,
                        fireEvent.Range,
                        fireEvent.TargetingHalfAngle);

                if (target != null)
                {
                    Vector3 toTarget =
                        target.transform.position -
                        origin.position;

                    if (toTarget.sqrMagnitude >
                        0.001f)
                    {
                        return toTarget.normalized;
                    }
                }

                return
                    racer != null
                        ? -racer.transform.forward
                        : -origin.forward;
            }

            if (fireEvent.AimMode ==
                WeaponAimMode.RearDrop)
            {
                return
                    racer != null
                        ? -racer.transform.forward
                        : -origin.forward;
            }

            PlayerWeaponAim playerAim =
                racer.GetComponentInChildren<
                    PlayerWeaponAim>(true);

            if (playerAim != null &&
                playerAim.TryGetAimDirection(
                    origin,
                    fireEvent.Range,
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
                null;

            GuidedTargetLockState lockState =
                shooter.GetComponent<
                    GuidedTargetLockState>();

            if (lockState != null)
            {
                lockState.TryGetLockedTarget(
                    out target);
            }

            if (target == null)
            {
                target =
                    FindGuidedTarget(
                        shooter,
                        origin,
                        direction,
                        fireEvent.Range,
                        guidedPrefab);
            }

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

            prefab =
                profile.ProjectilePrefab;

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
            if (direction.sqrMagnitude <
                0.001f)
            {
                return origin.rotation;
            }

            direction.Normalize();

            Vector3 up =
                origin.up;

            if (Mathf.Abs(
                    Vector3.Dot(
                        direction,
                        up)) >
                0.98f)
            {
                up =
                    origin.right;
            }

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

            if (fireDirection.sqrMagnitude <
                0.001f)
            {
                fireDirection =
                    origin.forward;
            }

            fireDirection.Normalize();

            RaceParticipant shooterParticipant =
                shooter.Participant;

            RacerViewController bestTarget =
                null;

            float bestScore =
                float.PositiveInfinity;

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

                if (runtime?.Director?.State.Deathmatch?.Mode != DeathmatchVictoryMode.Individual && string.Equals(
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

                if (toTarget.sqrMagnitude <
                    0.001f)
                {
                    continue;
                }

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

                if (score >=
                    bestScore)
                {
                    continue;
                }

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

        #region Special Weapons

        private void UpdateAutomaticTurrets()
        {
            if (Time.time <
                nextAutomaticTurretScanTime)
            {
                return;
            }

            nextAutomaticTurretScanTime =
                Time.time +
                Mathf.Max(
                    0.02f,
                    automaticTurretScanInterval);

            IReadOnlyList<RaceParticipant> participants =
                runtime.Director.State.Participants;

            for (int participantIndex = 0;
                 participantIndex < participants.Count;
                 participantIndex++)
            {
                RaceParticipant participant =
                    participants[participantIndex];

                if (participant == null ||
                    participant.Status !=
                        RaceParticipantStatus.Racing ||
                    participant.Vehicle == null ||
                    participant.Vehicle.IsDestroyed ||
                    participant.Vehicle.EquipmentSystem == null)
                {
                    continue;
                }

                if (!runtime.TryGetRacerView(
                        participant.RacerId,
                        out RacerViewController shooter) ||
                    shooter == null ||
                    !shooter.IsInitialized)
                {
                    continue;
                }

                RaceEquipmentSystem equipment =
                    participant.Vehicle.EquipmentSystem;

                for (int weaponIndex = 0;
                     weaponIndex < equipment.WeaponCount;
                     weaponIndex++)
                {
                    if (!equipment.TryGetWeaponSnapshot(
                            weaponIndex,
                            out RaceWeaponSnapshot weapon) ||
                        weapon.Definition == null ||
                        !weapon.HasAmmo ||
                        weapon.Definition.ActivationMode !=
                            EquipmentActivationMode.Passive ||
                        weapon.Definition.AimMode !=
                            WeaponAimMode.RearTargeted)
                    {
                        continue;
                    }

                    if (!shooter.TryGetEquipmentMount(
                            weapon.EquipmentId,
                            out BikeEquipmentMountBinding mount))
                    {
                        continue;
                    }

                    Transform origin =
                        mount.EquipmentOrigin != null
                            ? mount.EquipmentOrigin
                            : mount.MountRoot;

                    if (origin == null)
                        continue;

                    RacerViewController target =
                        FindAutomaticRearTarget(
                            shooter,
                            origin,
                            weapon.Definition.Range,
                            weapon.Definition.TargetingHalfAngle);

                    if (target == null)
                        continue;

                    equipment.TryFirePassiveWeapon(
                        weapon.EquipmentId);
                }
            }
        }

        private RacerViewController FindAutomaticRearTarget(
            RacerViewController shooter,
            Transform origin,
            float range,
            float halfAngle)
        {
            if (shooter == null ||
                shooter.Participant == null ||
                origin == null)
            {
                return null;
            }

            Vector3 rear =
                -shooter.transform.forward;

            float allowedAngle =
                halfAngle > 0f
                    ? halfAngle
                    : 55f;

            float maximumRange =
                Mathf.Max(
                    0.01f,
                    range);

            RacerViewController best =
                null;

            float bestScore =
                float.PositiveInfinity;

            IReadOnlyList<RaceParticipant> participants =
                runtime.Director.State.Participants;

            for (int i = 0;
                 i < participants.Count;
                 i++)
            {
                RaceParticipant participant =
                    participants[i];

                if (participant == null ||
                    participant.RacerId ==
                        shooter.RacerId)
                {
                    continue;
                }

                if (!runtime.TryGetRacerView(
                        participant.RacerId,
                        out RacerViewController candidate) ||
                    !IsEnemy(
                        shooter,
                        candidate))
                {
                    continue;
                }

                Vector3 toTarget =
                    candidate.transform.position -
                    origin.position;

                float distance =
                    toTarget.magnitude;

                if (distance <= 0.01f ||
                    distance > maximumRange)
                {
                    continue;
                }

                float angle =
                    Vector3.Angle(
                        rear,
                        toTarget /
                        distance);

                if (angle > allowedAngle)
                    continue;

                float score =
                    angle /
                    Mathf.Max(
                        0.01f,
                        allowedAngle) *
                    0.75f +
                    distance /
                    maximumRange *
                    0.25f;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                best = candidate;
            }

            return best;
        }

        private void FireConeProjectiles(
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

            int count =
                Mathf.Max(
                    1,
                    fireEvent.ProjectileCount);

            float halfAngle =
                Mathf.Max(
                    0f,
                    fireEvent.SpreadAngle *
                    0.5f);

            for (int i = 0;
                 i < count;
                 i++)
            {
                Vector3 pelletDirection =
                    RandomDirectionInCone(
                        direction,
                        origin.up,
                        halfAngle);

                ProjectileView projectile =
                    SpawnProjectileInstance(
                        prefab,
                        origin,
                        pelletDirection);

                projectile.Initialize(
                    runtime,
                    fireEvent.RacerId,
                    fireEvent.Damage,
                    fireEvent.ProjectileSpeed,
                    fireEvent.Range,
                    pelletDirection);
            }
        }

        private Vector3 RandomDirectionInCone(
            Vector3 forward,
            Vector3 upHint,
            float halfAngle)
        {
            if (forward.sqrMagnitude <
                0.001f)
            {
                forward = transform.forward;
            }

            forward.Normalize();

            if (halfAngle <= 0.01f)
                return forward;

            Vector3 up =
                Vector3.ProjectOnPlane(
                    upHint,
                    forward);

            if (up.sqrMagnitude <
                0.001f)
            {
                up =
                    Vector3.ProjectOnPlane(
                        Vector3.up,
                        forward);
            }

            if (up.sqrMagnitude <
                0.001f)
            {
                up = Vector3.right;
            }

            up.Normalize();

            Vector3 right =
                Vector3.Cross(
                    up,
                    forward)
                    .normalized;

            Vector2 disk =
                Random.insideUnitCircle;

            float radius =
                Mathf.Tan(
                    halfAngle *
                    Mathf.Deg2Rad);

            return
                (forward +
                 right * disk.x * radius +
                 up * disk.y * radius)
                .normalized;
        }

        private void SpawnMine(
            WeaponFireEvent fireEvent,
            WeaponPresentationProfile profile,
            RacerViewController racer,
            Transform origin)
        {
            if (racer == null ||
                origin == null)
            {
                return;
            }

            Vector3 position =
                origin.position -
                racer.transform.forward *
                1.5f;

            Vector3 rayOrigin =
                position +
                racer.transform.up *
                0.8f;

            RaycastHit[] groundHits =
                Physics.RaycastAll(
                    rayOrigin,
                    -racer.transform.up,
                    4f,
                    hitMask,
                    QueryTriggerInteraction.Ignore);

            float nearestGroundDistance =
                float.PositiveInfinity;

            for (int i = 0;
                 i < groundHits.Length;
                 i++)
            {
                RaycastHit groundHit =
                    groundHits[i];

                RacerViewController hitRacer =
                    groundHit.collider != null
                        ? groundHit.collider.GetComponentInParent<
                            RacerViewController>()
                        : null;

                if (hitRacer != null ||
                    groundHit.distance >=
                        nearestGroundDistance)
                {
                    continue;
                }

                nearestGroundDistance =
                    groundHit.distance;

                position =
                    groundHit.point +
                    groundHit.normal *
                    0.08f;
            }

            GameObject mineObject =
                new GameObject(
                    "Ballistic Mine");

            mineObject.transform.position =
                position;

            mineObject.transform.rotation =
                racer.transform.rotation;

            BallisticMineView mine =
                mineObject.AddComponent<
                    BallisticMineView>();

            mine.Initialize(
                runtime,
                profile,
                fireEvent.RacerId,
                fireEvent.Damage,
                fireEvent.ExplosionRadius,
                fireEvent.ArmingDelay,
                fireEvent.Lifetime);
        }

        private void ActivateRam(
            WeaponFireEvent fireEvent,
            WeaponPresentationProfile profile,
            RacerViewController racer)
        {
            if (racer == null)
                return;

            RamAttackState ram =
                racer.GetComponent<
                    RamAttackState>();

            if (ram == null)
            {
                ram =
                    racer.gameObject.AddComponent<
                        RamAttackState>();
            }

            ram.Activate(
                runtime,
                profile,
                fireEvent.Damage,
                fireEvent.LateralDistance,
                fireEvent.DashDuration,
                fireEvent.ImpactPush);
        }

        private void FireFlameCone(
            WeaponFireEvent fireEvent,
            WeaponPresentationProfile profile,
            RacerViewController shooter,
            Transform origin)
        {
            if (shooter == null ||
                origin == null)
            {
                return;
            }

            float range =
                Mathf.Max(
                    0.01f,
                    fireEvent.Range);

            float halfAngle =
                fireEvent.SpreadAngle > 0f
                    ? fireEvent.SpreadAngle * 0.5f
                    : 24f;

            Vector3 forward =
                shooter.transform.forward;

            Vector3 right =
                shooter.transform.right;

            Vector3 left =
                -right;

            SpawnFlameBurstVisual(
                origin,
                forward,
                right,
                left,
                range,
                halfAngle);

            IReadOnlyList<RaceParticipant> participants =
                runtime.Director.State.Participants;

            for (int i = 0;
                 i < participants.Count;
                 i++)
            {
                RaceParticipant participant =
                    participants[i];

                if (participant == null ||
                    participant.RacerId ==
                        fireEvent.RacerId)
                {
                    continue;
                }

                if (!runtime.TryGetRacerView(
                        participant.RacerId,
                        out RacerViewController victim) ||
                    !IsEnemy(
                        shooter,
                        victim))
                {
                    continue;
                }

                Vector3 toTarget =
                    victim.transform.position -
                    origin.position;

                float distance =
                    toTarget.magnitude;

                if (distance <= 0.01f ||
                    distance > range)
                {
                    continue;
                }

                Vector3 targetDirection =
                    toTarget /
                    distance;

                float angle =
                    Mathf.Min(
                        Vector3.Angle(
                            forward,
                            targetDirection),
                        Mathf.Min(
                            Vector3.Angle(
                                right,
                                targetDirection),
                            Vector3.Angle(
                                left,
                                targetDirection)));

                if (angle > halfAngle)
                    continue;

                if (fireEvent.Damage > 0f)
                {
                    runtime.Director.ApplyDamage(
                        fireEvent.RacerId,
                        victim.RacerId,
                        fireEvent.Damage,
                        DamageCause.Weapon);
                }

                AccumulateFlameExposure(
                    fireEvent,
                    profile,
                    victim);
            }
        }

        private void AccumulateFlameExposure(
            WeaponFireEvent fireEvent,
            WeaponPresentationProfile profile,
            RacerViewController victim)
        {
            string key =
                fireEvent.RacerId +
                "|" +
                fireEvent.DefinitionId +
                "|" +
                victim.RacerId;

            if (!flameExposure.TryGetValue(
                    key,
                    out FlameExposureState exposure))
            {
                exposure =
                    new FlameExposureState();
            }

            float allowedGap =
                Mathf.Max(
                    flameExposureGrace,
                    fireEvent.FireInterval *
                    2.5f);

            if (Time.time -
                    exposure.LastTouchedTime >
                allowedGap)
            {
                exposure.Exposure = 0f;
            }

            float exposureStep =
                Mathf.Max(
                    0.02f,
                    fireEvent.FireInterval);

            exposure.Exposure +=
                exposureStep;

            exposure.LastTouchedTime =
                Time.time;

            flameExposure[key] =
                exposure;

            float required =
                Mathf.Max(
                    0f,
                    fireEvent.ExposureDuration);

            if (exposure.Exposure + 0.0001f <
                required)
            {
                return;
            }

            exposure.Exposure = 0f;
            flameExposure[key] = exposure;

            ApplyBurn(
                fireEvent,
                profile,
                victim);
        }

        private void ApplyBurn(
            WeaponFireEvent fireEvent,
            WeaponPresentationProfile profile,
            RacerViewController victim)
        {
            if (victim == null ||
                fireEvent.EffectDuration <= 0f ||
                fireEvent.StatusDamagePerSecond <= 0f)
            {
                return;
            }

            string key =
                fireEvent.RacerId +
                "|" +
                victim.RacerId;

            if (!activeBurns.TryGetValue(
                    key,
                    out BurnState burn))
            {
                burn =
                    new BurnState();
            }

            burn.AttackerRacerId =
                fireEvent.RacerId;

            burn.VictimRacerId =
                victim.RacerId;

            burn.DamagePerSecond =
                fireEvent.StatusDamagePerSecond;

            burn.ExpiresAt =
                Time.time +
                fireEvent.EffectDuration;

            activeBurns[key] =
                burn;

            BurningStatusView visual =
                victim.GetComponent<
                    BurningStatusView>();

            if (visual == null)
            {
                visual =
                    victim.gameObject.AddComponent<
                        BurningStatusView>();
            }

            visual.Refresh(
                fireEvent.EffectDuration);

            WeaponWorldFeedback.Play(
                profile,
                victim.transform.position,
                victim.transform.rotation);
        }

        private void UpdateBurns(
            float deltaTime)
        {
            if (deltaTime <= 0f ||
                activeBurns.Count == 0)
            {
                return;
            }

            stateKeyScratch.Clear();

            foreach (KeyValuePair<string, BurnState> pair
                     in activeBurns)
            {
                BurnState burn =
                    pair.Value;

                if (burn == null ||
                    Time.time >=
                        burn.ExpiresAt ||
                    !runtime.TryGetRacerView(
                        burn.VictimRacerId,
                        out RacerViewController victim) ||
                    victim == null ||
                    victim.Participant == null ||
                    victim.Participant.Status !=
                        RaceParticipantStatus.Racing ||
                    victim.Participant.Vehicle == null ||
                    victim.Participant.Vehicle.IsDestroyed)
                {
                    stateKeyScratch.Add(
                        pair.Key);

                    continue;
                }

                burn.TickAccumulator +=
                    deltaTime;

                float tick =
                    Mathf.Max(
                        0.02f,
                        burnDamageTickInterval);

                while (burn.TickAccumulator >=
                    tick)
                {
                    runtime.Director.ApplyDamage(
                        burn.AttackerRacerId,
                        burn.VictimRacerId,
                        burn.DamagePerSecond *
                        tick,
                        DamageCause.Weapon);

                    burn.TickAccumulator -=
                        tick;
                }
            }

            for (int i = 0;
                 i < stateKeyScratch.Count;
                 i++)
            {
                activeBurns.Remove(
                    stateKeyScratch[i]);
            }
        }

        private void PruneFlameExposure()
        {
            if (flameExposure.Count == 0)
                return;

            stateKeyScratch.Clear();

            foreach (KeyValuePair<string, FlameExposureState> pair
                     in flameExposure)
            {
                if (Time.time -
                        pair.Value.LastTouchedTime >
                    Mathf.Max(
                        1f,
                        flameExposureGrace * 2f))
                {
                    stateKeyScratch.Add(
                        pair.Key);
                }
            }

            for (int i = 0;
                 i < stateKeyScratch.Count;
                 i++)
            {
                flameExposure.Remove(
                    stateKeyScratch[i]);
            }
        }

        private void SpawnFlameBurstVisual(
            Transform origin,
            Vector3 forward,
            Vector3 right,
            Vector3 left,
            float range,
            float halfAngle)
        {
            GameObject root =
                new GameObject(
                    "Flamethrower Burst");

            root.transform.position =
                origin.position;

            CreateFlameJet(
                root.transform,
                forward,
                range,
                halfAngle);

            CreateFlameJet(
                root.transform,
                right,
                range,
                halfAngle);

            CreateFlameJet(
                root.transform,
                left,
                range,
                halfAngle);

            Destroy(
                root,
                0.55f);
        }

        private void CreateFlameJet(
            Transform parent,
            Vector3 direction,
            float range,
            float halfAngle)
        {
            if (direction.sqrMagnitude <
                0.001f)
            {
                return;
            }

            GameObject jet =
                new GameObject(
                    "Flame Jet");

            jet.transform.SetParent(
                parent,
                false);

            jet.transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    transform.up);

            ParticleSystem particles =
                jet.AddComponent<
                    ParticleSystem>();

            ParticleSystem.MainModule main =
                particles.main;

            main.loop = false;
            main.duration = 0.18f;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    0.16f,
                    0.28f);

            main.startSpeed =
                new ParticleSystem.MinMaxCurve(
                    range * 2.2f,
                    range * 3.4f);

            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    0.14f,
                    0.42f);

            main.startColor =
                new ParticleSystem.MinMaxGradient(
                    new Color(
                        1f,
                        0.28f,
                        0.03f,
                        0.9f),
                    new Color(
                        1f,
                        0.82f,
                        0.12f,
                        0.75f));

            ParticleSystem.EmissionModule emission =
                particles.emission;

            emission.rateOverTime = 0f;

            emission.SetBursts(
                new[]
                {
                    new ParticleSystem.Burst(
                        0f,
                        (short)18)
                });

            ParticleSystem.ShapeModule shape =
                particles.shape;

            shape.shapeType =
                ParticleSystemShapeType.Cone;

            shape.angle =
                Mathf.Clamp(
                    halfAngle,
                    2f,
                    45f);

            shape.radius = 0.08f;

            particles.Play(true);
        }

        private bool IsEnemy(
            RacerViewController shooter,
            RacerViewController candidate)
        {
            if (shooter == null ||
                candidate == null ||
                shooter == candidate ||
                !candidate.IsInitialized ||
                candidate.Participant == null ||
                candidate.Participant.Status !=
                    RaceParticipantStatus.Racing ||
                candidate.Participant.Vehicle == null ||
                candidate.Participant.Vehicle.IsDestroyed)
            {
                return false;
            }

            if (runtime?.Director?.State.Deathmatch?.Mode ==
                DeathmatchVictoryMode.Individual)
            {
                return true;
            }

            return
                shooter.Participant == null ||
                !string.Equals(
                    shooter.Participant.TeamId,
                    candidate.Participant.TeamId,
                    System.StringComparison.Ordinal);
        }

        private class FlameExposureState
        {
            public float Exposure;
            public float LastTouchedTime;
        }

        private class BurnState
        {
            public string AttackerRacerId;
            public string VictimRacerId;
            public float DamagePerSecond;
            public float ExpiresAt;
            public float TickAccumulator;
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