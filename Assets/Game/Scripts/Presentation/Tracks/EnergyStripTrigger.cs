using System.Collections.Generic;
using RaceFatal.Energy;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    [RequireComponent(typeof(Collider))]
    public class EnergyStripTrigger : MonoBehaviour
    {
        [Header("Recharge")]
        [Tooltip("Amount of Energy restored per second while a racer remains on the strip.")]
        [Min(0f)][SerializeField] private float rechargePerSecond = 5f;

        [Header("Runtime Debug")]
        [SerializeField] private int debugRacersOnStrip;
        [SerializeField] private float debugEnergyRestoredThisFrame;
        [SerializeField] private float debugTotalEnergyRestored;

        private readonly Dictionary<RacerViewController, HashSet<Collider>> overlappingRacers =
            new Dictionary<RacerViewController, HashSet<Collider>>();

        private readonly List<RacerViewController> staleRacers =
            new List<RacerViewController>();

        private Collider triggerCollider;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();

            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning(
                    $"{nameof(EnergyStripTrigger)} on '{name}' requires its Collider to be a trigger. " +
                    "Enabling Is Trigger automatically.",
                    this);

                triggerCollider.isTrigger = true;
            }
        }

        private void OnDisable()
        {
            overlappingRacers.Clear();
            staleRacers.Clear();

            debugRacersOnStrip = 0;
            debugEnergyRestoredThisFrame = 0f;
        }

        private void FixedUpdate()
        {
            debugEnergyRestoredThisFrame = 0f;

            if (rechargePerSecond <= 0f || overlappingRacers.Count == 0)
            {
                debugRacersOnStrip = overlappingRacers.Count;
                return;
            }

            float rechargeAmount =
                rechargePerSecond *
                Time.fixedDeltaTime;

            staleRacers.Clear();

            foreach (KeyValuePair<RacerViewController, HashSet<Collider>> pair in overlappingRacers)
            {
                RacerViewController racerView = pair.Key;

                if (racerView == null ||
                    pair.Value == null ||
                    pair.Value.Count == 0)
                {
                    staleRacers.Add(racerView);
                    continue;
                }

                if (!racerView.IsInitialized ||
                    racerView.Participant == null ||
                    racerView.Participant.Vehicle == null)
                {
                    continue;
                }

                EnergyPool energyPool =
                    racerView.Participant.Vehicle.EnergyPool;

                if (energyPool == null)
                    continue;

                float restored =
                    energyPool.Recharge(
                        rechargeAmount);

                debugEnergyRestoredThisFrame += restored;
                debugTotalEnergyRestored += restored;
            }

            for (int i = 0; i < staleRacers.Count; i++)
            {
                RacerViewController racerView =
                    staleRacers[i];

                if (racerView != null)
                    overlappingRacers.Remove(racerView);
            }

            debugRacersOnStrip =
                overlappingRacers.Count;
        }

        private void OnTriggerEnter(Collider other)
        {
            RacerViewController racerView =
                other.GetComponentInParent<RacerViewController>();

            if (racerView == null)
                return;

            if (!overlappingRacers.TryGetValue(
                    racerView,
                    out HashSet<Collider> colliders))
            {
                colliders =
                    new HashSet<Collider>();

                overlappingRacers.Add(
                    racerView,
                    colliders);
            }

            colliders.Add(
                other);

            debugRacersOnStrip =
                overlappingRacers.Count;
        }

        private void OnTriggerExit(Collider other)
        {
            RacerViewController racerView =
                other.GetComponentInParent<RacerViewController>();

            if (racerView == null)
                return;

            if (!overlappingRacers.TryGetValue(
                    racerView,
                    out HashSet<Collider> colliders))
            {
                return;
            }

            colliders.Remove(
                other);

            if (colliders.Count == 0)
            {
                overlappingRacers.Remove(
                    racerView);
            }

            debugRacersOnStrip =
                overlappingRacers.Count;
        }
    }
}