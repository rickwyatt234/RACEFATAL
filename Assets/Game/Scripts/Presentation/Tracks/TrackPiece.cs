using System.Collections.Generic;
using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    public class TrackPiece : MonoBehaviour
    {
        [Header("Connections")]

        [SerializeField]
        private Transform entry;

        [SerializeField]
        private Transform exit;

        [Header("Optional Centerline")]

        [Tooltip(
            "Optional parent containing intermediate centerline " +
            "points in sibling order. Do not include Entry or Exit.")]
        [SerializeField]
        private Transform pathRoot;

        public Transform Entry => entry;

        public Transform Exit => exit;

        public bool IsConfigured =>
            entry != null &&
            exit != null;

        /// <summary>
        /// Appends:
        ///
        /// Entry
        /// -> intermediate Path children
        /// -> Exit
        ///
        /// using world-space positions.
        /// </summary>
        public void GetControlPoints(
            List<Vector3> destination)
        {
            destination.Clear();

            if (!IsConfigured)
                return;

            destination.Add(
                entry.position);

            if (pathRoot != null)
            {
                for (int i = 0;
                     i < pathRoot.childCount;
                     i++)
                {
                    destination.Add(
                        pathRoot
                            .GetChild(i)
                            .position);
                }
            }

            destination.Add(
                exit.position);
        }

        [ContextMenu("Auto Assign Named Children")]
        private void AutoAssignNamedChildren()
        {
            if (entry == null)
            {
                entry =
                    transform.Find("Entry");
            }

            if (exit == null)
            {
                exit =
                    transform.Find("Exit");
            }

            if (pathRoot == null)
            {
                pathRoot =
                    transform.Find("Path");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!IsConfigured)
                return;

            var points =
                new List<Vector3>();

            GetControlPoints(points);

            for (int i = 0;
                 i < points.Count - 1;
                 i++)
            {
                Gizmos.DrawLine(
                    points[i],
                    points[i + 1]);
            }

            Gizmos.DrawSphere(
                entry.position,
                0.15f);

            Gizmos.DrawSphere(
                exit.position,
                0.15f);
        }
    }
}