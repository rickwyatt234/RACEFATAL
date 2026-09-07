using System.Collections.Generic;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(RacerViewController))]
    public class AIRacerSensor : MonoBehaviour
    {
        #region Detection

        [Header("Forward Detection")]
        [Tooltip("Maximum distance ahead at which another racer may be considered traffic.")]
        [Min(1f)][SerializeField] private float detectionRange = 45f;

        [Tooltip("Half-width of the corridor used to decide whether another racer is directly ahead.")]
        [Min(0.1f)][SerializeField] private float forwardLaneHalfWidth = 2.25f;

        [Tooltip("Vertical tolerance used when comparing nearby racers.")]
        [Min(0.1f)][SerializeField] private float verticalTolerance = 4f;

        [Header("Passing Space")]
        [Tooltip("Distance ahead that must be clear on the chosen passing side.")]
        [Min(0f)][SerializeField] private float sideFrontClearance = 14f;

        [Tooltip("Distance behind that must be clear before moving into a passing lane.")]
        [Min(0f)][SerializeField] private float sideRearClearance = 8f;

        [Tooltip("Half-width of the area considered occupied around a proposed passing lane.")]
        [Min(0.1f)][SerializeField] private float sideLaneHalfWidth = 1.4f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private string racerId;
        [SerializeField] private string debugAheadRacer;
        [SerializeField] private float debugAheadDistance;
        [SerializeField] private float debugAheadLateralOffset;
        [SerializeField] private float debugClosingSpeed;

        #endregion

        #region Runtime

        private static readonly List<AIRacerSensor> Registry =
            new List<AIRacerSensor>();

        private Rigidbody body;
        private RacerViewController racerView;

        public static IReadOnlyList<AIRacerSensor> ActiveSensors =>
            Registry;

        public AIRacerSensor AheadRacer { get; private set; }
        public float AheadDistance { get; private set; }
        public float AheadLateralOffset { get; private set; }
        public float AheadClosingSpeed { get; private set; }

        public string RacerId => racerId;
        public RacerViewController RacerView => racerView;

        public float SpeedMetersPerSecond =>
            body != null
                ? body.linearVelocity.magnitude
                : 0f;

        #endregion

        #region Unity

        private void Awake()
        {
            body =
                GetComponent<Rigidbody>();

            racerView =
                GetComponent<RacerViewController>();
        }

        private void OnEnable()
        {
            if (!Registry.Contains(this))
                Registry.Add(this);
        }

        private void OnDisable()
        {
            Registry.Remove(this);
        }

        #endregion

        #region Identity

        public void SetIdentity(string id)
        {
            racerId = id;
        }

        #endregion

        #region Scanning

        public void Scan()
        {
            AheadRacer = null;
            AheadDistance = float.PositiveInfinity;
            AheadLateralOffset = 0f;
            AheadClosingSpeed = 0f;

            for (int i = Registry.Count - 1; i >= 0; i--)
            {
                AIRacerSensor candidate =
                    Registry[i];

                if (candidate == null)
                {
                    Registry.RemoveAt(i);
                    continue;
                }

                if (candidate == this ||
                    !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                Vector3 localPosition =
                    transform.InverseTransformPoint(
                        candidate.transform.position);

                if (Mathf.Abs(localPosition.y) >
                    verticalTolerance)
                {
                    continue;
                }

                if (localPosition.z <= 0f ||
                    localPosition.z > detectionRange)
                {
                    continue;
                }

                if (Mathf.Abs(localPosition.x) >
                    forwardLaneHalfWidth)
                {
                    continue;
                }

                if (localPosition.z >=
                    AheadDistance)
                {
                    continue;
                }

                AheadRacer =
                    candidate;

                AheadDistance =
                    localPosition.z;

                AheadLateralOffset =
                    localPosition.x;

                AheadClosingSpeed =
                    SpeedMetersPerSecond -
                    candidate.SpeedMetersPerSecond;
            }

            debugAheadRacer =
                AheadRacer != null
                    ? AheadRacer.RacerId
                    : "None";

            debugAheadDistance =
                AheadRacer != null
                    ? AheadDistance
                    : 0f;

            debugAheadLateralOffset =
                AheadLateralOffset;

            debugClosingSpeed =
                AheadClosingSpeed;
        }

        public bool IsSideClear(
            int side,
            float lateralPassDistance)
        {
            side =
                side < 0
                    ? -1
                    : 1;

            float targetX =
                side *
                Mathf.Abs(
                    lateralPassDistance);

            for (int i = Registry.Count - 1;
                 i >= 0;
                 i--)
            {
                AIRacerSensor candidate =
                    Registry[i];

                if (candidate == null)
                {
                    Registry.RemoveAt(i);
                    continue;
                }

                if (candidate == this ||
                    !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                Vector3 localPosition =
                    transform.InverseTransformPoint(
                        candidate.transform.position);

                if (Mathf.Abs(localPosition.y) >
                    verticalTolerance)
                {
                    continue;
                }

                if (localPosition.z <
                    -sideRearClearance)
                {
                    continue;
                }

                if (localPosition.z >
                    sideFrontClearance)
                {
                    continue;
                }

                if (Mathf.Abs(
                        localPosition.x -
                        targetX) <=
                    sideLaneHalfWidth)
                {
                    return false;
                }
            }

            return true;
        }

        public float GetLongitudinalDistanceTo(
            AIRacerSensor other)
        {
            if (other == null)
                return float.PositiveInfinity;

            return transform
                .InverseTransformPoint(
                    other.transform.position)
                .z;
        }

        #endregion
    }
}