using System.Collections.Generic;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class MissileThreatReceiver : MonoBehaviour
    {
        [Header("Runtime Debug")]
        [SerializeField] private int debugThreatCount;
        [SerializeField] private string debugNearestMissile = "None";
        [SerializeField] private float debugNearestDistance;

        private readonly List<GuidedProjectileView> threats =
            new List<GuidedProjectileView>();

        public int ThreatCount
        {
            get
            {
                PruneThreats();
                return threats.Count;
            }
        }

        public bool HasIncomingMissile =>
            ThreatCount > 0;

        public void RegisterThreat(
            GuidedProjectileView missile)
        {
            if (missile == null ||
                threats.Contains(missile))
            {
                return;
            }

            threats.Add(missile);
            UpdateDebug();
        }

        public void UnregisterThreat(
            GuidedProjectileView missile)
        {
            if (missile == null)
                return;

            threats.Remove(missile);
            UpdateDebug();
        }

        public bool TryGetNearestThreat(
            out GuidedProjectileView nearest,
            out float distance)
        {
            PruneThreats();

            nearest = null;
            distance = float.PositiveInfinity;

            for (int i = 0; i < threats.Count; i++)
            {
                GuidedProjectileView missile =
                    threats[i];

                if (missile == null ||
                    !missile.IsActiveThreat)
                {
                    continue;
                }

                float currentDistance =
                    Vector3.Distance(
                        transform.position,
                        missile.transform.position);

                if (currentDistance >= distance)
                    continue;

                distance = currentDistance;
                nearest = missile;
            }

            debugNearestMissile =
                nearest != null
                    ? nearest.name
                    : "None";

            debugNearestDistance =
                nearest != null
                    ? distance
                    : 0f;

            return nearest != null;
        }

        public List<GuidedProjectileView> GetThreatSnapshot()
        {
            PruneThreats();

            return new List<GuidedProjectileView>(
                threats);
        }

        private void PruneThreats()
        {
            for (int i = threats.Count - 1; i >= 0; i--)
            {
                GuidedProjectileView missile =
                    threats[i];

                if (missile == null ||
                    !missile.IsActiveThreat)
                {
                    threats.RemoveAt(i);
                }
            }

            UpdateDebug();
        }

        private void UpdateDebug()
        {
            debugThreatCount =
                threats.Count;
        }
    }
}