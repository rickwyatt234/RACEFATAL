using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    [RequireComponent(typeof(RacerViewController))]
    public sealed class TrackProgressReporter :
        MonoBehaviour
    {
        [Tooltip(
            "How many nearby path segments may be checked on each update.")]
        [Min(1)]
        [SerializeField]
        private int segmentSearchRadius =
            10;

        [Tooltip(
            "How often course progress is reported.")]
        [Min(0.01f)]
        [SerializeField]
        private float reportInterval =
            0.05f;

        private RacerViewController racer;

        private RaceRuntimeController runtime;

        private TrackProgressPath path;

        private int currentSegment = -1;

        private float reportTimer;

        private void Awake()
        {
            racer =
                GetComponent<
                    RacerViewController>();
        }

        public void Initialize(
            RaceRuntimeController raceRuntime,
            TrackProgressPath progressPath)
        {
            runtime =
                raceRuntime;

            path =
                progressPath;

            ResetTracking();
        }

        public void ResetTracking()
        {
            currentSegment = -1;

            reportTimer = 0f;
        }

        private void FixedUpdate()
        {
            if (runtime == null ||
                path == null ||
                !racer.IsInitialized)
            {
                return;
            }

            // Important:
            // Don't cache a path segment while the racer
            // is still being spawned/moved onto the grid.
            if (!runtime.HasStarted)
                return;

            reportTimer -=
                Time.fixedDeltaTime;

            if (reportTimer > 0f)
                return;

            reportTimer =
                reportInterval;

            float progress =
                path.GetProgress(
                    transform.position,
                    currentSegment,
                    segmentSearchRadius,
                    out currentSegment);

            runtime.Director
                .ReportCourseProgress(
                    racer.RacerId,
                    progress);
        }
    }
}