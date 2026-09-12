using System.Collections.Generic;
using RaceFatal.Presentation.Racing;
using UnityEngine;
using RaceFatal.Racing;

namespace RaceFatal.Presentation.Tracks
{
    [RequireComponent(typeof(Collider))]
    public class LapLineTrigger : MonoBehaviour
    {
        [Header("Crossing")]
        [Tooltip("Forward must point in the legal racing direction.")]
        [SerializeField] private Transform forwardReference;

        [Tooltip("Minimum velocity in the legal direction required for a crossing.")]
        [Min(0f)][SerializeField] private float minimumForwardSpeed = 1f;

        [Tooltip("Protects against multiple bike colliders registering the same crossing.")]
        [Min(0f)][SerializeField] private float crossingCooldown = 1f;

        [Header("Debug")]
        [SerializeField] private string debugLastRacer = "None";
        [SerializeField] private string debugLastCollider = "None";
        [SerializeField] private string debugLastResult = "No Crossing";
        [SerializeField] private float debugForwardSpeed;
        [SerializeField] private float debugTraversalBefore;
        [SerializeField] private float debugTraversalAfter;
        [SerializeField] private float debugCourseProgress;
        [SerializeField] private int debugLapsBefore;
        [SerializeField] private int debugLapsAfter;
        [SerializeField] private bool debugLapAccepted;
        [SerializeField] private int debugTriggerCount;
        [SerializeField] private int debugIgnoredNonRacerCount;

        private readonly Dictionary<string, float> lastCrossingTimes =
            new Dictionary<string, float>();

        private RaceRuntimeController runtime;

        public void Initialize(RaceRuntimeController raceRuntime)
        {
            runtime = raceRuntime;
            debugLastResult = runtime != null ? "Initialized" : "Initialization Failed";
        }

        private void OnTriggerEnter(Collider other)
        {
            RacerViewController racer =
                other.GetComponentInParent<RacerViewController>();

            if (racer == null)
            {
                debugIgnoredNonRacerCount++;
                return;
            }

            debugTriggerCount++;
            debugLastCollider = other != null ? other.name : "Null";

            if (runtime == null)
            {
                debugLastResult = "Rejected: Runtime Missing";
                return;
            }

            if (runtime.Director == null)
            {
                debugLastResult = "Rejected: Director Missing";
                return;
            }

            if (!runtime.HasStarted)
            {
                debugLastResult = "Rejected: Race Not Started";
                return;
            }

            if (!racer.IsInitialized)
            {
                debugLastResult = "Rejected: Racer Not Initialized";
                return;
            }

            debugLastRacer = racer.RacerId;

            Rigidbody body = racer.GetComponent<Rigidbody>();

            if (body == null)
            {
                debugLastResult = "Rejected: Rigidbody Missing";
                return;
            }

            Vector3 legalDirection =
                forwardReference != null
                    ? forwardReference.forward
                    : transform.forward;

            float forwardSpeed =
                Vector3.Dot(
                    body.linearVelocity,
                    legalDirection.normalized);

            debugForwardSpeed = forwardSpeed;

            if (forwardSpeed < minimumForwardSpeed)
            {
                debugLastResult =
                    $"Rejected: Wrong Direction ({forwardSpeed:0.0} m/s)";

                debugLapAccepted = false;
                return;
            }

            string racerId = racer.RacerId;

            if (lastCrossingTimes.TryGetValue(racerId, out float previousTime))
            {
                float elapsed = Time.time - previousTime;

                if (elapsed < crossingCooldown)
                {
                    debugLastResult =
                        $"Rejected: Cooldown ({elapsed:0.00}s)";

                    debugLapAccepted = false;
                    return;
                }
            }

            RaceParticipant participant =
                racer.Participant;

            debugTraversalBefore = participant.LapTraversalProgress;
            debugCourseProgress = participant.CourseProgress;
            debugLapsBefore = participant.CompletedLaps;

            lastCrossingTimes[racerId] = Time.time;

            runtime.Director.ReportLapCompleted(racerId);

            debugTraversalAfter = participant.LapTraversalProgress;
            debugLapsAfter = participant.CompletedLaps;

            debugLapAccepted =
                debugLapsAfter > debugLapsBefore;

            if (participant.Status == RaceParticipantStatus.Finished)
            {
                debugLapAccepted = true;
                debugLastResult =
                    $"RACE FINISHED - Position {participant.FinishPosition}";
            }
            else if (debugLapAccepted)
            {
                debugLastResult =
                    $"Lap Accepted - Lap {participant.CompletedLaps}";
            }
            else
            {
                debugLastResult =
                    $"Rejected By LapTracker - Traversal {debugTraversalBefore:0.000}";
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform reference =
                forwardReference != null
                    ? forwardReference
                    : transform;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                reference.position,
                reference.position + reference.forward * 5f);
        }
    }
}