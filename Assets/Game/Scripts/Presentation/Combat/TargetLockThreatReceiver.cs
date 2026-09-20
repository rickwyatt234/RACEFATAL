using System.Collections.Generic;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class TargetLockThreatReceiver :
        MonoBehaviour
    {
        [Header("Runtime Debug")]
        [SerializeField]
        private int debugThreatCount;

        [SerializeField]
        private bool debugHardLock;

        private readonly Dictionary<string, bool>
            threats =
                new Dictionary<string, bool>();

        public bool HasLockThreat =>
            threats.Count > 0;

        public bool HasHardLock
        {
            get
            {
                foreach (bool locked
                         in threats.Values)
                {
                    if (locked)
                        return true;
                }

                return false;
            }
        }

        public void SetThreat(
            string sourceRacerId,
            bool locked)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceRacerId))
            {
                return;
            }

            threats[sourceRacerId] =
                locked;

            UpdateDebug();
        }

        public void ClearThreat(
            string sourceRacerId)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceRacerId))
            {
                return;
            }

            threats.Remove(
                sourceRacerId);

            UpdateDebug();
        }

        private void OnDisable()
        {
            threats.Clear();
            UpdateDebug();
        }

        private void UpdateDebug()
        {
            debugThreatCount =
                threats.Count;

            debugHardLock =
                HasHardLock;
        }
    }
}