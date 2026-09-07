using System.Collections.Generic;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    public class TrackRuntimeController :
        MonoBehaviour
    {
        [Header("Course")]

        [SerializeField]
        private TrackPathBuilder pathBuilder;

        [SerializeField]
        private TrackProgressPath progressPath;

        [SerializeField]
        private LapLineTrigger lapLine;

        [Header("Starting Grid")]

        [SerializeField]
        private List<Transform> gridSlots =
            new List<Transform>();

        private RaceRuntimeController runtime;

        public TrackProgressPath ProgressPath =>
            progressPath;

        public int GridSlotCount =>
            gridSlots.Count;

        public bool Initialize(
            RaceRuntimeController raceRuntime)
        {
            runtime =
                raceRuntime;

            if (pathBuilder == null)
            {
                Debug.LogError(
                    "TrackRuntimeController requires a " +
                    "TrackPathBuilder.",
                    this);

                return false;
            }

            if (progressPath == null)
            {
                Debug.LogError(
                    "TrackRuntimeController requires a " +
                    "TrackProgressPath.",
                    this);

                return false;
            }

            if (!pathBuilder.Build())
            {
                Debug.LogError(
                    "The circuit progress path could not be built.",
                    this);

                return false;
            }

            if (lapLine != null)
            {
                lapLine.Initialize(
                    runtime);
            }

            return true;
        }

        public void BindRacer(
            RacerViewController racer)
        {
            if (racer == null)
                return;

            TrackProgressReporter reporter =
                racer.GetComponent<
                    TrackProgressReporter>();

            if (reporter == null)
            {
                Debug.LogWarning(
                    $"Racer '{racer.name}' requires a " +
                    $"{nameof(TrackProgressReporter)}.",
                    racer);

                return;
            }

            reporter.Initialize(
                runtime,
                progressPath);
        }

        public bool PlaceRacerAtGridSlot(
            RacerViewController racer,
            int slotIndex)
        {
            if (racer == null)
                return false;

            if (slotIndex < 0 ||
                slotIndex >=
                gridSlots.Count)
            {
                return false;
            }

            Transform slot =
                gridSlots[
                    slotIndex];

            if (slot == null)
                return false;

            Rigidbody body =
                racer.GetComponent<
                    Rigidbody>();

            if (body != null)
            {
                body.position =
                    slot.position;

                body.rotation =
                    slot.rotation;

                body.linearVelocity =
                    Vector3.zero;

                body.angularVelocity =
                    Vector3.zero;
            }
            else
            {
                racer.transform
                    .SetPositionAndRotation(
                        slot.position,
                        slot.rotation);
            }

            TrackProgressReporter reporter =
                racer.GetComponent<
                    TrackProgressReporter>();

            if (reporter != null)
            {
                reporter.ResetTracking();
            }

            return true;
        }

        public Transform GetGridSlot(
            int index)
        {
            if (index < 0 ||
                index >=
                gridSlots.Count)
            {
                return null;
            }

            return gridSlots[index];
        }
    }
}