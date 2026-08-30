using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    public class TrackSurfaceProbe : MonoBehaviour
    {
        [SerializeField]
        private LayerMask trackMask;

        [Min(0.01f)]
        [SerializeField]
        private float sphereRadius = 0.25f;

        [Min(0f)]
        [SerializeField]
        private float startOffset = 0.25f;

        [Min(0.1f)]
        [SerializeField]
        private float probeDistance = 2f;

        public bool HasSurface { get; private set; }

        public Vector3 SurfaceNormal { get; private set; } =
            Vector3.up;

        public Vector3 SurfacePoint { get; private set; }

        public float SurfaceDistance { get; private set; }


        public Vector3 LastSurfaceNormal { get; private set; } =
            Vector3.up;

        public bool Sample()
        {
            Vector3 origin =
                transform.position +
                transform.up * startOffset;

            Vector3 direction =
                -transform.up;

            if (Physics.SphereCast(
                    origin,
                    sphereRadius,
                    direction,
                    out RaycastHit hit,
                    probeDistance,
                    trackMask,
                    QueryTriggerInteraction.Ignore))
            {
                HasSurface = true;
                SurfaceNormal = hit.normal.normalized;
                LastSurfaceNormal = SurfaceNormal;
                SurfacePoint = hit.point;
                SurfaceDistance = hit.distance;

                return true;
            }

            HasSurface = false;

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin =
                transform.position +
                transform.up * startOffset;

            Vector3 direction =
                -transform.up;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                origin,
                sphereRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(
                origin,
                direction * probeDistance);
        }
    }
}

