using System.Collections.Generic;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure.Input;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.Combat;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using RaceFatal.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Vehicles
{
    public class PlayerCockpitTargetingHUD : MonoBehaviour
    {
        [System.Serializable]
        private class IdBoxView
        {
            public RectTransform root;
            public TextMeshProUGUI racerName;
            public TextMeshProUGUI teamName;
        }

        private struct TargetCandidate
        {
            public RacerViewController View;
            public Vector2 HudUv;
            public float Distance;
            public float CenterScore;
        }

#region Inspector
        [Header("Projection")]
        [Tooltip("The visible flat HUD projection Quad in front of the cockpit.")]
        [SerializeField]
        private Transform hudProjectionPlane;
        [Tooltip("MeshFilter belonging to the HUD projection Quad.")] [SerializeField] private MeshFilter hudProjectionMesh;
        [Range(0f,
            0.25f)] [Tooltip("Allows targets slightly outside the exact projection-plane bounds.")] [SerializeField] private float projectionBoundsMargin = 0.03f;
        [Header("Rocket Targeting")] [Tooltip("Empty RectTransform root containing every visual piece of the rocket reticle.")] [SerializeField] private
            RectTransform rocketReticle;
        [Tooltip("Text displayed once the selected target has achieved hard lock.")] [SerializeField] private TextMeshProUGUI rocketLockText;
        [Tooltip("Optional CanvasGroup placed on the empty rocket-reticle root.")] [SerializeField] private CanvasGroup rocketReticleCanvasGroup;
        [Tooltip("Individual UI Images that make up the reticle. " + "If empty, they are automatically discovered below Rocket Reticle.")] [SerializeField] private List<Image> rocketReticleImages =
            new List<Image>();
        [SerializeField] private Color acquiringReticleColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color lockedReticleColor = Color.white;
        [Tooltip("Makes every reticle child use the same Unity layer as the reticle root.")] [SerializeField] private bool synchronizeReticleChildLayers = true;
        [Header("Target Selection")] [Tooltip("If enabled, selecting the rocket launcher automatically chooses " + "the visible target nearest the center of the windshield. " + "Once selected, the target remains sticky until invalid or manually cycled.")] [SerializeField] private bool autoSelectFirstTarget =
            false;
        [Header("Targeting Audio")] [Tooltip("Dedicated looping AudioSource used while acquiring a target.")] [SerializeField] private
            AudioSource targetAcquireAudioSource;
        [Tooltip("Dedicated AudioSource used for the completed lock-on sound.")] [SerializeField] private AudioSource targetLockAudioSource;
        [Tooltip("Looping sound played while a target is being acquired.")] [SerializeField] private AudioClip targetAcquireClip;
        [Tooltip("One-shot sound played when hard lock is achieved.")] [SerializeField] private AudioClip targetLockedClip;
        [Tooltip("Mixer group used for cockpit targeting sounds. " +
            "The UI mixer group is recommended.")] [SerializeField] private AudioMixerGroup targetingAudioMixerGroup;
        [Range(0f, 1f)] [SerializeField] private float targetAcquireVolume = 0.65f;
        [Range(0f, 1f)] [SerializeField] private float targetLockedVolume = 1f;
        [Tooltip("Gradually raises the pitch of the acquisition loop as lock progresses.")] [SerializeField] private bool increaseAcquirePitchWithProgress =
            true;
        [Range(0.1f, 3f)] [SerializeField] private float acquireStartPitch = 0.9f;
        [Range(0.1f, 3f)] [SerializeField] private float acquireEndPitch = 1.15f;
        [Header("ID Boxes")] [SerializeField] private List<IdBoxView> idBoxes = new List<IdBoxView>();
        [SerializeField] private Vector2 idBoxOffset = new Vector2(80f, 35f);
        [Header("Warning")] [SerializeField] private GameObject warningRoot;
        [SerializeField] private TextMeshProUGUI warningText;
        [Header("Target Visibility")] [Tooltip("Local point on another bike used for targeting and ID positioning.")] [SerializeField] private Vector3 targetLocalOffset =
            new Vector3(0f, 0.9f, 0f);
        [Tooltip("If enabled, track geometry can obstruct targeting.")] [SerializeField] private bool requireLineOfSight = true;
        [Tooltip("Layers that may block visibility between the cockpit camera and another racer.")] [SerializeField] private LayerMask lineOfSightMask = ~0;
#endregion
#region Runtime Debug
        [Header("Runtime Debug")]
        [SerializeField]
        private string debugTarget = "None";
        [SerializeField] private float debugLockProgress;
        [SerializeField] private bool debugLocked;
        [Header("Targeting Debug")] [SerializeField] private string debugSelectedWeapon = "None";
        [SerializeField] private bool debugNeedsTargetLock;
        [SerializeField] private int debugCandidateCount;
        [SerializeField] private int debugInRangeCandidateCount;
        [SerializeField] private int debugProjectedCandidateCount;
        [SerializeField] private int debugVisibleCandidateCount;
        [SerializeField] private int debugAvailableTargetCount;
        [SerializeField] private string debugSelectedTarget = "None";
        [SerializeField] private string debugTargetingFailure = "None";
#endregion
#region Runtime
        private readonly RaycastHit[] visibilityHits = new RaycastHit[32];
        private readonly List<TargetCandidate> availableTargets = new List<TargetCandidate>();
        private RacerViewController racerView;
        private PlayerCockpitView cockpitView;
        private GuidedTargetLockState lockState;
        private TargetLockThreatReceiver lockThreatReceiver;
        private MissileThreatReceiver missileThreatReceiver;
        private RaceRuntimeController raceRuntime;
        private IRaceInputService input;
        private RacerViewController selectedTarget;
        private RacerViewController focusedRacer;
        private readonly List<TargetCandidate> focusCandidates = new List<TargetCandidate>();
        private bool wasTargetLocked;
#endregion
#region Unity
        private void Awake()
        {
            racerView = GetComponentInParent<RacerViewController>();
            cockpitView = GetComponent<PlayerCockpitView>();
            lockState = GetComponentInParent<GuidedTargetLockState>();
            lockThreatReceiver = GetComponentInParent<TargetLockThreatReceiver>();
            missileThreatReceiver = GetComponentInParent<MissileThreatReceiver>();
            ResolveProjectionReferences();
            ResolveReticleReferences();
            ConfigureTargetingAudio();
            HideTargeting();
            HideAllIdBoxes();
            SetWarning(null);
            ResetTargetingDebug();
        }

        private void Start()
        {
            ResolveInput();
        }

        private void Update()
        {
            if (RacePauseController.IsGameplayBlocked)
                return;
            if (input == null)
            {
                ResolveInput();
            }

            if (!TryResolve())
            {
                ClearSelectedTarget();
                HideAllIdBoxes();
                SetWarning(null);
                return;
            }

            if (input != null && input.FocusHeld)
            {
                UpdateIdBoxes();
            }
            else
            {
                focusedRacer = null;
                HideAllIdBoxes();
            }

            UpdateGuidedTargeting();
            UpdateWarning();
        }

        private void OnDisable()
        {
            focusedRacer = null;
            ClearSelectedTarget();
            HideAllIdBoxes();
            SetWarning(null);
            StopAcquireAudio();
        }

#endregion
#region Initialization
        private void ResolveInput()
        {
            if (BootstrapController.Context == null)
                return;
            input = BootstrapController.Context.Input;
        }

        private void ResolveProjectionReferences()
        {
            if (hudProjectionMesh == null && hudProjectionPlane != null)
            {
                hudProjectionMesh = hudProjectionPlane.GetComponent<MeshFilter>();
            }

            if (hudProjectionPlane == null && hudProjectionMesh != null)
            {
                hudProjectionPlane = hudProjectionMesh.transform;
            }
        }

        private void ResolveReticleReferences()
        {
            if (rocketReticle == null)
                return;
            if (rocketReticleCanvasGroup == null)
            {
                rocketReticleCanvasGroup = rocketReticle.GetComponent<CanvasGroup>();
            }

            if (rocketReticleImages.Count == 0)
            {
                Image[] discoveredImages = rocketReticle.GetComponentsInChildren<Image>(true);
                rocketReticleImages.AddRange(discoveredImages);
            }

            if (synchronizeReticleChildLayers)
            {
                SetLayerRecursively(rocketReticle.gameObject, rocketReticle.gameObject.layer);
            }
        }

        private void ConfigureTargetingAudio()
        {
            if (targetAcquireAudioSource != null)
            {
                targetAcquireAudioSource.playOnAwake = false;
                targetAcquireAudioSource.loop = true;
                targetAcquireAudioSource.spatialBlend = 0f;
                targetAcquireAudioSource.volume = targetAcquireVolume;
                targetAcquireAudioSource.pitch = acquireStartPitch;
                if (targetingAudioMixerGroup != null)
                {
                    targetAcquireAudioSource.outputAudioMixerGroup = targetingAudioMixerGroup;
                }
            }

            if (targetLockAudioSource != null)
            {
                targetLockAudioSource.playOnAwake = false;
                targetLockAudioSource.loop = false;
                targetLockAudioSource.spatialBlend = 0f;
                if (targetingAudioMixerGroup != null)
                {
                    targetLockAudioSource.outputAudioMixerGroup = targetingAudioMixerGroup;
                }
            }
        }

        private bool TryResolve()
        {
            if (racerView == null || !racerView.IsInitialized || racerView.Participant == null)
            {
                debugTargetingFailure = "Player racer not initialized";
                return false;
            }

            if (racerView.Participant.Role != RaceParticipantRole.Player)
            {
                debugTargetingFailure = "Racer is not the player";
                return false;
            }

            if (cockpitView == null || !cockpitView.IsActivePlayerView || cockpitView.CockpitCamera == null)
            {
                debugTargetingFailure = "Cockpit view/camera unavailable";
                return false;
            }

            if (lockState == null)
            {
                lockState = GetComponentInParent<GuidedTargetLockState>();
                if (lockState == null)
                {
                    debugTargetingFailure = "GuidedTargetLockState missing";
                    return false;
                }
            }

            if (raceRuntime == null)
            {
                raceRuntime = FindFirstObjectByType<RaceRuntimeController>();
            }

            if (raceRuntime == null || raceRuntime.Director == null)
            {
                debugTargetingFailure = "Race runtime unavailable";
                return false;
            }

            ResolveProjectionReferences();
            if (hudProjectionMesh == null || hudProjectionMesh.sharedMesh == null)
            {
                debugTargetingFailure = "HUD Projection Mesh missing";
                return false;
            }

            return true;
        }

#endregion
#region Guided Targeting
        private void UpdateGuidedTargeting()
        {
            ResetTargetingDebug();
            RaceVehicleState vehicle = racerView.Participant.Vehicle;
            if (vehicle == null || vehicle.EquipmentSystem == null)
            {
                debugTargetingFailure = "Vehicle/equipment system unavailable";
                ClearSelectedTarget();
                return;
            }

            RaceEquipmentSystem equipment = vehicle.EquipmentSystem;
            WeaponDefinition weapon = equipment.SelectedWeaponDefinition;
            debugSelectedWeapon = weapon != null ? weapon.DisplayName : "None";
            debugNeedsTargetLock = weapon != null && weapon.AimMode == WeaponAimMode.Targeted && weapon.DeliveryMode == WeaponDeliveryMode.GuidedProjectile;
            if (!debugNeedsTargetLock)
            {
                debugTargetingFailure = "Selected weapon does not require guided lock";
                ClearSelectedTarget();
                return;
            }

            BuildAvailableTargetList(weapon);
            debugAvailableTargetCount = availableTargets.Count;
            if (selectedTarget != null && !TryGetCandidate(selectedTarget, out _))
            {
                selectedTarget = null;
                lockState.ClearTarget();
                StopAcquireAudio();
                wasTargetLocked = false;
                HideTargeting();
            }

            bool cyclePressed = input != null && input.NextTargetPressed && !input.FocusHeld;
            if (input != null && input.FocusHeld)
            {
                // Inspect and lock the same racer in focus mode. A teammate or
                // an out-of-range inspection target must not create a lock.
                SelectTarget(TryGetCandidate(focusedRacer, out _) ? focusedRacer : null);
            }
            else if (cyclePressed)
            {
                CycleTarget();
            }
            else if (selectedTarget == null && autoSelectFirstTarget && availableTargets.Count > 0)
            {
                SelectClosestToCenter();
            }

            if (selectedTarget == null)
            {
                lockState.ClearTarget();
                StopAcquireAudio();
                wasTargetLocked = false;
                HideTargeting();
                if (availableTargets.Count > 0)
                {
                    debugTargetingFailure = input == null ? "Targets available - input unavailable" : "Targets available - awaiting selection";
                }
                else
                {
                    debugTargetingFailure = ResolveNoTargetFailure();
                }

                return;
            }

            if (!TryGetCandidate(selectedTarget, out TargetCandidate selectedCandidate))
            {
                debugTargetingFailure = "Selected target unavailable";
                ClearSelectedTarget();
                return;
            }

            lockState.TrackCandidate(selectedTarget, Time.deltaTime, weapon.TargetLockDuration);
            UpdateTargetingAudio();
            PositionAtHudUv(rocketReticle, selectedCandidate.HudUv, Vector2.zero);
            ShowTargeting();
            UpdateReticleAppearance();
            if (rocketLockText != null)
            {
                rocketLockText.text = lockState.IsLocked ? "TARGET LOCKED" : string.Empty;
            }

            debugTarget = selectedTarget.RacerId;
            debugSelectedTarget = selectedTarget.RacerId;
            debugLockProgress = lockState.LockProgress;
            debugLocked = lockState.IsLocked;
            debugTargetingFailure = lockState.IsLocked ? "None" : "Acquiring lock";
        }

        private void BuildAvailableTargetList(WeaponDefinition weapon)
        {
            availableTargets.Clear();
            foreach (RaceParticipant candidate in raceRuntime.Director.State.Participants)
            {
                if (!IsValidEnemy(candidate))
                {
                    continue;
                }

                debugCandidateCount++;
                if (!raceRuntime.TryGetRacerView(candidate.RacerId, out RacerViewController view))
                {
                    continue;
                }

                if (view == null)
                    continue;
                Vector3 targetPoint = view.transform.TransformPoint(targetLocalOffset);
                float distance = Vector3.Distance(cockpitView.CockpitCamera.transform.position, targetPoint);
                if (distance > weapon.Range)
                {
                    continue;
                }

                debugInRangeCandidateCount++;
                if (!TryProjectToHud(targetPoint, out Vector2 uv))
                {
                    continue;
                }

                debugProjectedCandidateCount++;
                if (requireLineOfSight && !HasLineOfSight(view, targetPoint))
                {
                    continue;
                }

                debugVisibleCandidateCount++;
                Vector2 fromCenter = uv - new Vector2(0.5f, 0.5f);
                TargetCandidate target = new TargetCandidate {  View = view,  HudUv = uv,  Distance = distance,  CenterScore = fromCenter.sqrMagnitude };
                availableTargets.Add(target);
            }

            availableTargets.Sort(CompareTargetCandidates);
        }

        private int CompareTargetCandidates(TargetCandidate a, TargetCandidate b)
        {
            int horizontal = a.HudUv.x.CompareTo(b.HudUv.x);
            if (horizontal != 0)
                return horizontal;
            return a.Distance.CompareTo(b.Distance);
        }

        private bool TryGetCandidate(RacerViewController target, out TargetCandidate candidate)
        {
            candidate = default;
            if (target == null)
                return false;
            for (int i = 0; i < availableTargets.Count; i++)
            {
                TargetCandidate current = availableTargets[i];
                if (current.View != target)
                {
                    continue;
                }

                candidate = current;
                return true;
            }

            return false;
        }

        private void CycleTarget()
        {
            if (availableTargets.Count == 0)
            {
                ClearSelectedTarget();
                return;
            }

            if (selectedTarget == null)
            {
                SelectClosestToCenter();
                return;
            }

            int currentIndex = -1;
            for (int i = 0; i < availableTargets.Count; i++)
            {
                if (availableTargets[i].View != selectedTarget)
                {
                    continue;
                }

                currentIndex = i;
                break;
            }

            if (currentIndex < 0)
            {
                SelectClosestToCenter();
                return;
            }

            int nextIndex = currentIndex + 1;
            if (nextIndex >= availableTargets.Count)
            {
                nextIndex = 0;
            }

            SelectTarget(availableTargets[nextIndex].View);
        }

        private void SelectClosestToCenter()
        {
            if (availableTargets.Count == 0)
            {
                ClearSelectedTarget();
                return;
            }

            int bestIndex = 0;
            float bestScore = availableTargets[0].CenterScore;
            for (int i = 1; i < availableTargets.Count; i++)
            {
                float score = availableTargets[i].CenterScore;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestIndex = i;
            }

            SelectTarget(availableTargets[bestIndex].View);
        }

        private void SelectTarget(RacerViewController target)
        {
            if (target == selectedTarget)
            {
                return;
            }

            selectedTarget = target;
            lockState?.ClearTarget();
            StopAcquireAudio();
            wasTargetLocked = false;
        }

        private void ClearSelectedTarget()
        {
            selectedTarget = null;
            availableTargets.Clear();
            lockState?.ClearTarget();
            StopAcquireAudio();
            wasTargetLocked = false;
            HideTargeting();
            debugSelectedTarget = "None";
        }

        private string ResolveNoTargetFailure()
        {
            if (debugCandidateCount <= 0)
            {
                return "No valid enemy racers";
            }

            if (debugInRangeCandidateCount <= 0)
            {
                return "No enemies inside weapon range";
            }

            if (debugProjectedCandidateCount <= 0)
            {
                return "No enemies intersect HUD projection";
            }

            if (requireLineOfSight && debugVisibleCandidateCount <= 0)
            {
                return "All projected enemies failed line of sight";
            }

            return "No available target";
        }

        private bool IsValidEnemy(RaceParticipant candidate)
        {
            RaceParticipant player = racerView.Participant;
            if (candidate == null || candidate == player)
            {
                return false;
            }

            if (candidate.Status != RaceParticipantStatus.Racing)
            {
                return false;
            }

            if (candidate.Vehicle == null || candidate.Vehicle.IsDestroyed)
            {
                return false;
            }

            return raceRuntime?.Director?.State.Deathmatch?.Mode == DeathmatchVictoryMode.Individual || !string.Equals(candidate.TeamId, player.TeamId, System.StringComparison.Ordinal);
        }

#endregion
#region Targeting Audio
        private void UpdateTargetingAudio()
        {
            if (lockState == null || selectedTarget == null)
            {
                StopAcquireAudio();
                wasTargetLocked = false;
                return;
            }

            bool locked = lockState.IsLocked;
            if (!locked)
            {
                StartAcquireAudio();
                if (increaseAcquirePitchWithProgress && targetAcquireAudioSource != null)
                {
                    targetAcquireAudioSource.pitch = Mathf.Lerp(acquireStartPitch, acquireEndPitch, Mathf.Clamp01(lockState.LockProgress));
                }
            }
            else if (!wasTargetLocked)
            {
                StopAcquireAudio();
                PlayLockedAudio();
            }

            wasTargetLocked = locked;
        }

        private void StartAcquireAudio()
        {
            if (targetAcquireAudioSource == null || targetAcquireClip == null)
            {
                return;
            }

            if (targetAcquireAudioSource.isPlaying)
                return;
            targetAcquireAudioSource.clip = targetAcquireClip;
            targetAcquireAudioSource.volume = targetAcquireVolume;
            targetAcquireAudioSource.pitch = acquireStartPitch;
            targetAcquireAudioSource.loop = true;
            targetAcquireAudioSource.Play();
        }

        private void StopAcquireAudio()
        {
            if (targetAcquireAudioSource == null)
                return;
            if (targetAcquireAudioSource.isPlaying)
            {
                targetAcquireAudioSource.Stop();
            }

            targetAcquireAudioSource.pitch = acquireStartPitch;
        }

        private void PlayLockedAudio()
        {
            if (targetLockAudioSource == null || targetLockedClip == null)
            {
                return;
            }

            targetLockAudioSource.PlayOneShot(targetLockedClip, targetLockedVolume);
        }

#endregion
#region ID Boxes
        private void UpdateIdBoxes()
        {
            HideAllIdBoxes();
            focusCandidates.Clear();
            foreach (RaceParticipant participant in raceRuntime.Director.State.Participants)
            {
                if (participant == null || participant == racerView.Participant)
                {
                    continue;
                }

                if (participant.Status != RaceParticipantStatus.Racing || participant.Vehicle == null || participant.Vehicle.IsDestroyed)
                {
                    continue;
                }

                if (!raceRuntime.TryGetRacerView(participant.RacerId, out RacerViewController view))
                {
                    continue;
                }

                if (view == null)
                {
                    continue;
                }
                Vector3 point = view.transform.TransformPoint(targetLocalOffset);
                if (!TryProjectToHud(point, out Vector2 uv) || (requireLineOfSight && !HasLineOfSight(view, point)))
                    continue;
                focusCandidates.Add(new TargetCandidate { View = view, HudUv = uv,
                    CenterScore = (uv - new Vector2(0.5f, 0.5f)).sqrMagnitude });
            }
            // Stable roster order makes Tab predictable even when racers cross
            // each other on screen. Keep the current focus until it leaves view.
            focusCandidates.Sort((a, b) => string.CompareOrdinal(a.View.Participant.RacerId, b.View.Participant.RacerId));
            int index = focusCandidates.FindIndex(candidate => candidate.View == focusedRacer);
            if (index < 0 && focusCandidates.Count > 0)
            {
                index = focusCandidates.FindIndex(candidate => candidate.View == selectedTarget);
                if (index < 0)
                {
                    index = 0;
                    for (int i = 1; i < focusCandidates.Count; i++)
                        if (focusCandidates[i].CenterScore < focusCandidates[index].CenterScore) index = i;
                }
            }
            else if (index >= 0 && input != null && input.NextTargetPressed)
                index = (index + 1) % focusCandidates.Count;
            focusedRacer = index >= 0 ? focusCandidates[index].View : null;
            if (focusedRacer != null)
                TryShowIdBoxForView(focusedRacer, 0);
        }

        private bool TryShowIdBoxForView(RacerViewController view, int boxIndex)
        {
            if (view == null || boxIndex < 0 || boxIndex >= idBoxes.Count)
            {
                return false;
            }

            RaceParticipant participant = view.Participant;
            if (participant == null)
                return false;
            Vector3 targetPoint = view.transform.TransformPoint(targetLocalOffset);
            if (!TryProjectToHud(targetPoint, out Vector2 uv))
            {
                return false;
            }

            if (requireLineOfSight && !HasLineOfSight(view, targetPoint))
            {
                return false;
            }

            IdBoxView box = idBoxes[boxIndex];
            if (box == null || box.root == null)
            {
                return false;
            }

            PositionAtHudUv(box.root, uv, idBoxOffset);
            box.root.gameObject.SetActive(true);
            if (box.racerName != null)
            {
                string racerName = participant.Racer != null ? participant.Racer.Name : participant.RacerId;
                box.racerName.text = string.IsNullOrWhiteSpace(racerName) ? string.Empty : racerName.ToUpperInvariant();
            }

            if (box.teamName != null)
            {
                box.teamName.text = string.IsNullOrWhiteSpace(participant.TeamName) ? string.Empty : participant.TeamName.ToUpperInvariant();
            }

            return true;
        }

        private void HideAllIdBoxes()
        {
            for (int i = 0; i < idBoxes.Count; i++)
            {
                HideIdBox(idBoxes[i]);
            }
        }

        private void HideIdBox(IdBoxView box)
        {
            if (box?.root == null)
                return;
            box.root.gameObject.SetActive(false);
        }

#endregion
#region HUD Projection
        private bool TryProjectToHud(Vector3 worldPoint, out Vector2 uv)
        {
            uv = default;
            if (cockpitView == null || cockpitView.CockpitCamera == null || hudProjectionMesh == null || hudProjectionMesh.sharedMesh == null)
            {
                return false;
            }

            Camera camera = cockpitView.CockpitCamera;
            Vector3 targetViewport = camera.WorldToViewportPoint(worldPoint);
            if (targetViewport.z <= 0f)
            {
                return false;
            }

            Transform projectionTransform = hudProjectionMesh.transform;
            Bounds bounds = hudProjectionMesh.sharedMesh.bounds;
            Vector3 localBottomLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.center.z);
            Vector3 localBottomRight = new Vector3(bounds.max.x, bounds.min.y, bounds.center.z);
            Vector3 localTopLeft = new Vector3(bounds.min.x, bounds.max.y, bounds.center.z);
            Vector3 localTopRight = new Vector3(bounds.max.x, bounds.max.y, bounds.center.z);
            Vector3 bottomLeft = camera.WorldToViewportPoint(projectionTransform.TransformPoint(localBottomLeft));
            Vector3 bottomRight = camera.WorldToViewportPoint(projectionTransform.TransformPoint(localBottomRight));
            Vector3 topLeft = camera.WorldToViewportPoint(projectionTransform.TransformPoint(localTopLeft));
            Vector3 topRight = camera.WorldToViewportPoint(projectionTransform.TransformPoint(localTopRight));
            if (bottomLeft.z <= 0f || bottomRight.z <= 0f || topLeft.z <= 0f || topRight.z <= 0f)
            {
                return false;
            }

            float minimumX = Mathf.Min(bottomLeft.x, bottomRight.x, topLeft.x, topRight.x);
            float maximumX = Mathf.Max(bottomLeft.x, bottomRight.x, topLeft.x, topRight.x);
            float minimumY = Mathf.Min(bottomLeft.y, bottomRight.y, topLeft.y, topRight.y);
            float maximumY = Mathf.Max(bottomLeft.y, bottomRight.y, topLeft.y, topRight.y);
            float width = maximumX - minimumX;
            float height = maximumY - minimumY;
            if (width <= 0.0001f || height <= 0.0001f)
            {
                return false;
            }

            float marginX = width * projectionBoundsMargin;
            float marginY = height * projectionBoundsMargin;
            if (targetViewport.x < minimumX - marginX || targetViewport.x > maximumX + marginX || targetViewport.y < minimumY - marginY || targetViewport.y > maximumY + marginY)
            {
                return false;
            }

            uv = new Vector2(Mathf.Clamp01(Mathf.InverseLerp(minimumX, maximumX, targetViewport.x)), Mathf.Clamp01(Mathf.InverseLerp(minimumY, maximumY, targetViewport.y)));
            return true;
        }

        private bool HasLineOfSight(RacerViewController target, Vector3 targetPoint)
        {
            if (!requireLineOfSight)
                return true;
            if (cockpitView == null || cockpitView.CockpitCamera == null)
            {
                return false;
            }

            Camera camera = cockpitView.CockpitCamera;
            Vector3 origin = camera.transform.position;
            Vector3 toTarget = targetPoint - origin;
            float distance = toTarget.magnitude;
            if (distance <= 0.001f)
                return true;
            if (lineOfSightMask.value == 0)
                return true;
            int hitCount = Physics.RaycastNonAlloc(origin, toTarget / distance, visibilityHits, distance, lineOfSightMask, QueryTriggerInteraction.Ignore);
            Collider nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = visibilityHits[i];
                if (hit.collider == null)
                    continue;
                RacerViewController hitRacer = hit.collider.GetComponentInParent<RacerViewController>();
                if (hitRacer == racerView)
                {
                    continue;
                }

                if (hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                nearest = hit.collider;
            }

            if (nearest == null)
                return true;
            RacerViewController nearestRacer = nearest.GetComponentInParent<RacerViewController>();
            return nearestRacer == target;
        }

        private void PositionAtHudUv(RectTransform rect, Vector2 uv, Vector2 offset)
        {
            if (rect == null)
                return;
            rect.anchorMin = uv;
            rect.anchorMax = uv;
            rect.anchoredPosition = offset;
        }

#endregion
#region Warnings
        private void UpdateWarning()
        {
            if (missileThreatReceiver != null && missileThreatReceiver.HasIncomingMissile)
            {
                SetWarning("MISSILE INCOMING");
                return;
            }

            if (lockThreatReceiver != null && lockThreatReceiver.HasHardLock)
            {
                SetWarning("MISSILE LOCK");
                return;
            }

            if (lockThreatReceiver != null && lockThreatReceiver.HasLockThreat)
            {
                SetWarning("LOCK WARNING");
                return;
            }

            SetWarning(null);
        }

        private void SetWarning(string message)
        {
            bool visible = !string.IsNullOrWhiteSpace(message);
            if (warningRoot != null)
            {
                warningRoot.SetActive(visible);
            }

            if (warningText != null)
            {
                warningText.text = visible ? message : string.Empty;
            }
        }

#endregion
#region Reticle Presentation
        private void ShowTargeting()
        {
            if (rocketReticle != null)
            {
                rocketReticle.gameObject.SetActive(true);
            }

            if (rocketReticleCanvasGroup != null)
            {
                rocketReticleCanvasGroup.alpha = 1f;
            }
        }

        private void UpdateReticleAppearance()
        {
            if (lockState == null)
                return;
            Color targetColor = lockState.IsLocked ? lockedReticleColor : acquiringReticleColor;
            for (int i = 0; i < rocketReticleImages.Count; i++)
            {
                Image image = rocketReticleImages[i];
                if (image == null)
                    continue;
                image.color = targetColor;
            }

            if (rocketReticleCanvasGroup != null)
            {
                rocketReticleCanvasGroup.alpha = 1f;
            }
        }

        private void HideTargeting()
        {
            if (rocketReticleCanvasGroup != null)
            {
                rocketReticleCanvasGroup.alpha = 0f;
            }

            if (rocketReticle != null)
            {
                rocketReticle.gameObject.SetActive(false);
            }

            if (rocketLockText != null)
            {
                rocketLockText.text = string.Empty;
            }

            debugTarget = "None";
            debugLockProgress = 0f;
            debugLocked = false;
        }

#endregion
#region Debug
        private void ResetTargetingDebug()
        {
            debugSelectedWeapon = "None";
            debugNeedsTargetLock = false;
            debugCandidateCount = 0;
            debugInRangeCandidateCount = 0;
            debugProjectedCandidateCount = 0;
            debugVisibleCandidateCount = 0;
            debugAvailableTargetCount = 0;
            debugSelectedTarget = selectedTarget != null ? selectedTarget.RacerId : "None";
            debugTargetingFailure = "None";
        }

#endregion
#region Utilities
        private void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
                return;
            root.layer = layer;
            Transform rootTransform = root.transform;
            for (int i = 0; i < rootTransform.childCount; i++)
            {
                Transform child = rootTransform.GetChild(i);
                if (child == null)
                    continue;
                SetLayerRecursively(child.gameObject, layer);
            }
        }
#endregion
    }
}
