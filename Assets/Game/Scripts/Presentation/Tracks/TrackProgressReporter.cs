using RaceFatal.Presentation.Racing;
using UnityEngine;
using RaceFatal.Racing;

namespace RaceFatal.Presentation.Tracks
{
    [RequireComponent(typeof(RacerViewController))]
    public class TrackProgressReporter : MonoBehaviour
    {
        [Header("Tracking")]
        [Tooltip("How many nearby path segments may be checked on each update.")]
        [Min(1)][SerializeField] private int segmentSearchRadius = 10;

        [Tooltip("How often course progress is reported.")]
        [Min(0.01f)][SerializeField] private float reportInterval = 0.05f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private float debugPathProgress;
        [SerializeField] private float debugParticipantCourseProgress;
        [SerializeField] private float debugLapTraversalProgress;
        [SerializeField] private int debugCompletedLaps;
        [SerializeField] private int debugCurrentSegment = -1;

        private RacerViewController racer;
        private RaceRuntimeController runtime;
        private TrackProgressPath path;

        private int currentSegment = -1;
        private float reportTimer;

        private void Awake()
        {
            racer =
                GetComponent<RacerViewController>();
        }

        public void Initialize(
            RaceRuntimeController raceRuntime,
            TrackProgressPath progressPath)
        {
            runtime = raceRuntime;
            path = progressPath;

            ResetTracking();

            debugInitialized =
                runtime != null &&
                path != null;
        }

        public void ResetTracking()
        {
            currentSegment = -1;
            reportTimer = 0f;

            debugCurrentSegment = -1;
        }

        private void FixedUpdate()
        {
            if (runtime == null ||
                path == null ||
                racer == null ||
                !racer.IsInitialized)
            {
                return;
            }

            /*
             * Don't cache a path segment while the racer
             * is still being spawned/moved onto the grid.
             */
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

            runtime.Director.ReportCourseProgress(
                racer.RacerId,
                progress);

            RaceParticipant participant =
                racer.Participant;

            debugPathProgress = progress;
            debugCurrentSegment = currentSegment;

            if (participant != null)
            {
                debugParticipantCourseProgress =
                    participant.CourseProgress;

                debugLapTraversalProgress =
                    participant.LapTraversalProgress;

                debugCompletedLaps =
                    participant.CompletedLaps;
            }
        }
    }
}