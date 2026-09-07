using System.Collections.Generic;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    [RequireComponent(typeof(Collider))]
    public sealed class LapLineTrigger :
        MonoBehaviour
    {
        [Tooltip(
            "Forward must point in the legal racing direction.")]
        [SerializeField]
        private Transform forwardReference;

        [Tooltip(
            "Minimum velocity in the legal direction required for a crossing.")]
        [Min(0f)]
        [SerializeField]
        private float minimumForwardSpeed =
            1f;

        [Tooltip(
            "Protects against multiple bike colliders registering the same crossing.")]
        [Min(0f)]
        [SerializeField]
        private float crossingCooldown =
            1f;

        private readonly Dictionary<
            string,
            float>
            lastCrossingTimes =
                new Dictionary<
                    string,
                    float>();

        private RaceRuntimeController runtime;

        public void Initialize(
            RaceRuntimeController raceRuntime)
        {
            runtime =
                raceRuntime;
        }

        private void OnTriggerEnter(
            Collider other)
        {
            if (runtime == null ||
                runtime.Director == null ||
                !runtime.HasStarted)
            {
                return;
            }

            RacerViewController racer =
                other.GetComponentInParent<
                    RacerViewController>();

            if (racer == null ||
                !racer.IsInitialized)
            {
                return;
            }

            Rigidbody body =
                racer.GetComponent<
                    Rigidbody>();

            if (body == null)
                return;

            Vector3 legalDirection =
                forwardReference != null
                    ? forwardReference.forward
                    : transform.forward;

            float forwardSpeed =
                Vector3.Dot(
                    body.linearVelocity,
                    legalDirection);

            // Crossing backwards cannot confirm a lap.
            if (forwardSpeed <
                minimumForwardSpeed)
            {
                return;
            }

            string racerId =
                racer.RacerId;

            if (lastCrossingTimes
                .TryGetValue(
                    racerId,
                    out float previousTime))
            {
                if (Time.time -
                    previousTime <
                    crossingCooldown)
                {
                    return;
                }
            }

            lastCrossingTimes[
                racerId] =
                    Time.time;

            // LapTracker makes the final decision.
            //
            // This trigger does NOT itself mean:
            // "the racer completed a lap."
            runtime.Director
                .ReportLapCompleted(
                    racerId);
        }

        private void OnDrawGizmosSelected()
        {
            if (forwardReference != null)
            {
                Gizmos.color =
                    Color.green;

                Gizmos.DrawLine(
                    forwardReference.position,
                    forwardReference.position +
                        forwardReference.forward);
            }
        }
    }
}