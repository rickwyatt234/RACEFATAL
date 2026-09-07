using System.Collections.Generic;
using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    public class EnergyStripAIAnchor : MonoBehaviour
    {
        [Header("Reference")]
        [Tooltip("Optional point representing the center of the usable Energy Strip. Defaults to this transform.")]
        [SerializeField] private Transform targetPoint;

        [Tooltip("Path segment search radius used when locating this strip on the progress path.")]
        [Min(1)][SerializeField] private int pathSearchRadius = 30;

        private static readonly List<EnergyStripAIAnchor> activeAnchors =
            new List<EnergyStripAIAnchor>();

        private TrackProgressPath cachedPath;
        private bool cacheValid;
        private float cachedProgress;
        private float cachedLateralOffset;

        public static IReadOnlyList<EnergyStripAIAnchor> ActiveAnchors =>
            activeAnchors;

        public Vector3 Position =>
            targetPoint != null
                ? targetPoint.position
                : transform.position;

        public Vector3 SurfaceNormal =>
            targetPoint != null
                ? targetPoint.up
                : transform.up;

        private void OnEnable()
        {
            if (!activeAnchors.Contains(this))
                activeAnchors.Add(this);

            InvalidateCache();
        }

        private void OnDisable()
        {
            activeAnchors.Remove(this);
            InvalidateCache();
        }

        private void OnValidate()
        {
            InvalidateCache();
        }

        public bool TryGetTrackData(
            TrackProgressPath path,
            out float progress,
            out float lateralOffset)
        {
            progress = 0f;
            lateralOffset = 0f;

            if (path == null ||
                path.TotalLength <= 0f)
            {
                return false;
            }

            if (cacheValid &&
                cachedPath == path)
            {
                progress = cachedProgress;
                lateralOffset = cachedLateralOffset;
                return true;
            }

            int segment;

            progress =
                path.GetProgress(
                    Position,
                    -1,
                    pathSearchRadius,
                    out segment);

            Vector3 pathCenter =
                path.GetPositionAtProgress(
                    progress);

            Vector3 pathForward =
                path.GetForwardAtProgress(
                    progress);

            Vector3 surfaceNormal =
                SurfaceNormal.normalized;

            pathForward =
                Vector3.ProjectOnPlane(
                    pathForward,
                    surfaceNormal);

            if (pathForward.sqrMagnitude <
                0.001f)
            {
                return false;
            }

            pathForward.Normalize();

            Vector3 pathRight =
                Vector3.Cross(
                    surfaceNormal,
                    pathForward);

            if (pathRight.sqrMagnitude <
                0.001f)
            {
                return false;
            }

            pathRight.Normalize();

            Vector3 centerToStrip =
                Vector3.ProjectOnPlane(
                    Position - pathCenter,
                    surfaceNormal);

            lateralOffset =
                Vector3.Dot(
                    centerToStrip,
                    pathRight);

            cachedPath = path;
            cachedProgress = progress;
            cachedLateralOffset = lateralOffset;
            cacheValid = true;

            return true;
        }

        private void InvalidateCache()
        {
            cachedPath = null;
            cacheValid = false;
            cachedProgress = 0f;
            cachedLateralOffset = 0f;
        }
    }
}