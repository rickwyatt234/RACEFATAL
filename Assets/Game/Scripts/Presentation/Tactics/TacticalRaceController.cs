using System.Collections.Generic;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Tactics
{
    [DefaultExecutionOrder(-100)]
    public sealed class TacticalRaceController : MonoBehaviour
    {
        public static TacticalRaceController Active { get; private set; }
        public static bool IsActive => Active != null && Active.IsOpen;
        [Header("Input")] public KeyCode toggleKey = KeyCode.Space;
        public KeyCode forwardKey = KeyCode.W;
        public KeyCode backwardKey = KeyCode.S;
        public KeyCode cycleKey = KeyCode.Tab;
        public KeyCode selectKey = KeyCode.Return;
        public KeyCode recenterKey = KeyCode.Home;
        public KeyCode resetOrientationKey = KeyCode.Q;
        [Header("Orbit")] [Min(0.01f)] public float orbitDegreesPerPixel = 0.2f;
        [Range(10f, 80f)] public float minimumOrbitElevation = 20f;
        private float orbitYaw;
        private float orbitElevation = 90f;
        private Vector3 previousMousePosition;
        private bool orbitDragging;
        [Header("Camera")] [Range(0.001f, 0.1f)] public float tacticalTimeScale = 0.01f;
        [Min(1f)] public float panMetersPerSecond = 150f;
        [Min(5f)] public float minimumHeight = 5f;
        [Min(10f)] public float maximumHeight = 220f;
        [Header("Camera Transitions")]
        [Min(0.01f)] public float enterBlendDuration = 0.5f;
        [Min(0.01f)] public float exitBlendDuration = 0.5f;
        [Min(0f)] public float trackOrientationResponse = 12f;
        private Quaternion trackFrame;
        private bool hasTrackFrame;
        private bool entering;
        private bool exiting;
        private float blendElapsed;
        private Vector3 blendPosition;
        private Quaternion blendRotation;
        private float blendFieldOfView;
        public bool IsOpen { get; private set; }
        public RacerViewController Selected { get; private set; }
        public RacerViewController Player { get; private set; }
        public PartnerOrderController Partner { get; private set; }
        public Camera TacticalCamera => tacticalCamera;
        public float Height => height;
        public bool WheelOpen { get; private set; }
        public string TaggedRacerId { get; private set; }
        public Vector3? PositionPreview { get; private set; }
        private readonly List<RacerViewController> racers = new List<RacerViewController>();
        public IReadOnlyList<RacerViewController> Racers => racers;
        private TrackProgressPath path;
        private RaceRuntimeController runtime;
        private Camera tacticalCamera;
        private Camera cockpitCamera;
        private bool cockpitWasEnabled;
        private float progress;
        private float height = 80f;
        private bool following;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private TacticalRaceUI ui;

        private void Awake()
        {
            Active = this;
            if (GetComponent<TacticalCameraPoseDriver>() == null) gameObject.AddComponent<TacticalCameraPoseDriver>();
        }
        private void OnEnable() { Active = this; }
        private void Start()
        {
            runtime = GetComponent<RaceRuntimeController>();
            ui = GetComponent<TacticalRaceUI>() ?? gameObject.AddComponent<TacticalRaceUI>();
            ui.Initialize(this);
        }
        private void RefreshRacers()
        {
            racers.Clear();
            if (runtime?.Director == null) return;
            foreach (var p in runtime.Director.State.Participants)
            {
                if (!runtime.TryGetRacerView(p.RacerId, out var view) || view == null) continue;
                if (p.Role == RaceParticipantRole.Player) Player = view;
                if (p.Role == RaceParticipantRole.PlayerPartner) Partner = view.GetComponentInChildren<PartnerOrderController>();
                if (p.Status == RaceParticipantStatus.Racing && !p.Vehicle.IsDestroyed) racers.Add(view);
            }
            racers.Sort((a,b) => { int c = b.Participant.CompletedLaps.CompareTo(a.Participant.CompletedLaps); if (c == 0) c = b.Participant.CourseProgress.CompareTo(a.Participant.CourseProgress); return c != 0 ? c : string.CompareOrdinal(a.RacerId,b.RacerId); });
        }
        private void Update()
        {
            if (RacePauseController.IsGameplayBlocked) { orbitDragging = false; return; }
            if (runtime == null || !runtime.IsRaceActive)
            { if (IsOpen || exiting) Close(); return; }
            if (UnityEngine.Input.GetKeyDown(toggleKey))
            { if (IsOpen) Close(false); else Open(); }
            if (!IsOpen) return;
            if (Partner == null) RefreshRacers();
            if (Player == null || Player.Participant.Vehicle.IsDestroyed || Player.Participant.Status != RaceParticipantStatus.Racing)
            { Close(); return; }
            if (UnityEngine.Input.GetKeyDown(cycleKey)) Cycle();
            if (UnityEngine.Input.GetKeyDown(recenterKey)) Recenter();
            if (UnityEngine.Input.GetKeyDown(resetOrientationKey)) ResetOrientation();
            UpdateOrbit();
            if (UnityEngine.Input.GetKeyDown(selectKey) && Selected != null) WheelOpen = !WheelOpen;
            if (UnityEngine.Input.GetMouseButtonDown(1)) WheelOpen = false;
            float pan = (UnityEngine.Input.GetKey(forwardKey) ? 1f : 0f) - (UnityEngine.Input.GetKey(backwardKey) ? 1f : 0f);
            if (pan != 0f) { following = false; Selected = null; WheelOpen = false; PositionPreview = null; progress = path.AdvanceProgressByDistance(progress,pan * panMetersPerSecond * Time.unscaledDeltaTime); }
            height = Mathf.Clamp(height * Mathf.Exp(-UnityEngine.Input.mouseScrollDelta.y * 0.12f),minimumHeight,maximumHeight);
            if (Selected != null && Selected.Participant.Status != RaceParticipantStatus.Racing) { Selected = null; WheelOpen = false; following = false; }
        }
        internal void UpdateCameraPose()
        {
            if (!isActiveAndEnabled || RacePauseController.IsGameplayBlocked || tacticalCamera == null) return;
            if (exiting)
            {
                if (cockpitCamera == null) { FinishCameraExit(); return; }
                float t = AdvanceBlend(exitBlendDuration);
                ApplyBlendedPose(cockpitCamera.transform.position, cockpitCamera.transform.rotation, cockpitCamera.fieldOfView, t);
                if (t >= 1f) FinishCameraExit();
                return;
            }
            if (!IsOpen || path == null) return;
            if (following && Selected != null) progress = ProgressOf(Selected);
            Vector3 center = following && Selected != null ? Selected.transform.position : path.GetPositionAtProgress(progress);
            Quaternion desiredFrame = path.GetRotationAtProgress(progress);
            float response = trackOrientationResponse <= 0f ? 1f : 1f - Mathf.Exp(-trackOrientationResponse * Time.unscaledDeltaTime);
            trackFrame = hasTrackFrame ? Quaternion.Slerp(trackFrame, desiredFrame, response) : desiredFrame;
            hasTrackFrame = true;
            Vector3 forward = trackFrame * Vector3.forward;
            Vector3 normal = trackFrame * Vector3.up;
            Vector3 heading = Quaternion.AngleAxis(orbitYaw, normal) * forward;
            float elevation = orbitElevation * Mathf.Deg2Rad;
            Vector3 offset = normal * Mathf.Sin(elevation) - heading * Mathf.Cos(elevation);
            Vector3 targetPosition = center + offset * height;
            Quaternion targetRotation = Quaternion.LookRotation(-offset, heading);
            if (entering)
            {
                float t = AdvanceBlend(enterBlendDuration);
                ApplyBlendedPose(targetPosition, targetRotation, cockpitCamera.fieldOfView, t);
                if (t >= 1f) entering = false;
            }
            else tacticalCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
        }
        private float AdvanceBlend(float duration)
        {
            blendElapsed += Time.unscaledDeltaTime;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(blendElapsed / Mathf.Max(0.01f, duration)));
        }
        private void CaptureBlendPose(Camera source)
        {
            blendElapsed = 0f;
            blendPosition = source.transform.position;
            blendRotation = source.transform.rotation;
            blendFieldOfView = source.fieldOfView;
        }
        private void ApplyBlendedPose(Vector3 position, Quaternion rotation, float fieldOfView, float t)
        {
            tacticalCamera.transform.SetPositionAndRotation(Vector3.Lerp(blendPosition, position, t), Quaternion.Slerp(blendRotation, rotation, t));
            tacticalCamera.fieldOfView = Mathf.Lerp(blendFieldOfView, fieldOfView, t);
        }
        private void FinishCameraExit()
        {
            entering = exiting = false;
            if (tacticalCamera != null) tacticalCamera.enabled = false;
            if (cockpitCamera != null) cockpitCamera.enabled = cockpitWasEnabled;
        }
        private void UpdateOrbit()
        {
            if (!UnityEngine.Input.GetMouseButton(2)) { orbitDragging = false; return; }
            Vector3 mouse = UnityEngine.Input.mousePosition;
            if (orbitDragging)
            {
                Vector3 delta = mouse - previousMousePosition;
                orbitYaw = Mathf.Repeat(orbitYaw + delta.x * orbitDegreesPerPixel, 360f);
                orbitElevation = Mathf.Clamp(orbitElevation - delta.y * orbitDegreesPerPixel, minimumOrbitElevation, 90f);
            }
            previousMousePosition = mouse;
            orbitDragging = true;
        }
        public void ResetOrientation() { orbitYaw = 0f; orbitElevation = 90f; orbitDragging = false; }
        private float ProgressOf(RacerViewController view)
        {
            // Seed a local search from the race's tracked segment; a global nearest
            // search can switch branches where corkscrews/stacked roads cross.
            int seed = Mathf.FloorToInt(view.Participant.CourseProgress * path.SegmentCount) % path.SegmentCount;
            int segment;
            return path.GetProgress(view.transform.position, seed, 3, out segment);
        }
        public void Open()
        {
            if (IsOpen) return;
            // The race assembler spawns the track after this component's early Start.
            // Resolve the initialized race's path on entry, not from the startup scene.
            if (runtime == null) runtime = GetComponent<RaceRuntimeController>();
            path = runtime != null ? runtime.ProgressPath : null;
            RefreshRacers();
            if (Player == null || path == null || path.TotalLength <= 0f)
            {
                Debug.LogWarning("Cannot open tactical view: the player or initialized race track is unavailable.", this);
                return;
            }
            var cockpit = Player.GetComponentInChildren<PlayerCockpitView>(true);
            cockpitCamera = cockpit != null ? cockpit.CockpitCamera : Camera.main;
            if (cockpitCamera == null)
            {
                Debug.LogWarning("Cannot open tactical view: no player camera is available.", this);
                return;
            }
            if (tacticalCamera == null)
            {
                var go = new GameObject("TacticalCamera"); go.tag = "MainCamera"; go.transform.SetParent(transform);
                tacticalCamera = go.AddComponent<Camera>();
            }
            CaptureBlendPose(exiting ? tacticalCamera : cockpitCamera);
            bool wasExiting = exiting;
            exiting = false;
            entering = true;
            hasTrackFrame = false;
            tacticalCamera.CopyFrom(cockpitCamera); tacticalCamera.targetTexture = null;
            tacticalCamera.rect = new Rect(0f,0f,1f,1f); tacticalCamera.depth = cockpitCamera.depth + 10f;
            tacticalCamera.nearClipPlane = 0.3f; tacticalCamera.farClipPlane = Mathf.Max(1000f,cockpitCamera.farClipPlane);
            if (!wasExiting) cockpitWasEnabled = cockpitCamera.enabled;
            cockpitCamera.enabled = false;
            tacticalCamera.enabled = true;
            ApplyBlendedPose(blendPosition, blendRotation, blendFieldOfView, 0f);
            previousCursorLock = Cursor.lockState; previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            IsOpen = true; ResetOrientation(); Recenter();
            Player.Participant.Vehicle.EquipmentSystem.CancelSelectedActivation();
            Player.Participant.Vehicle.EquipmentSystem.SetBoostActive(false);
        }
        public void Close(bool immediate = true)
        {
            if (!IsOpen) { if (immediate && exiting) FinishCameraExit(); return; }
            orbitDragging = false;
            IsOpen = false; WheelOpen = false; PositionPreview = null;
            if (!immediate && tacticalCamera != null && cockpitCamera != null)
            {
                CaptureBlendPose(tacticalCamera);
                entering = false;
                exiting = true;
            }
            else FinishCameraExit();
            Cursor.lockState = previousCursorLock; Cursor.visible = previousCursorVisible;
        }
        public void Recenter() { if (Player != null && path != null) progress = ProgressOf(Player); following = false; Selected = null; WheelOpen = false; }
        public void Cycle()
        {
            RefreshRacers();
            var visible = new List<RacerViewController>();
            foreach (var view in racers)
            {
                if (view == Player) continue;
                Vector3 vp = tacticalCamera.WorldToViewportPoint(view.transform.position);
                if (vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f) visible.Add(view);
            }
            if (visible.Count == 0) return;
            int index = visible.IndexOf(Selected);
            Select(visible[(index + 1) % visible.Count],false);
        }
        public void Select(RacerViewController view, bool openWheel = true)
        {
            if (view == null || view == Player || view.Participant.Status != RaceParticipantStatus.Racing) return;
            Selected = view; following = true; WheelOpen = openWheel;
        }
        public void Tag() { TaggedRacerId = Selected?.RacerId; WheelOpen = false; }
        public void Command(PartnerOrder order)
        {
            if (Partner != null && Selected != null)
                Partner.Issue(order,Selected.Participant.Role == RaceParticipantRole.PlayerPartner ? null : Selected);
            WheelOpen = false; PositionPreview = null;
        }
        public void Preview(int side)
        {
            PositionPreview = Selected != null ? Selected.transform.position + Selected.transform.right * side * 2.5f : (Vector3?)null;
        }
        public void ClearPreview() { PositionPreview = null; }
        private void OnDisable() { Close(); if (Active == this) Active = null; }
    }
}
