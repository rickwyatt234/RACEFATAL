using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    public class TrackSurfaceProbe : MonoBehaviour
    {
        [SerializeField] private LayerMask trackMask;
        [Min(0.01f)] [SerializeField] private float sphereRadius = 0.25f;
        [Min(0f)] [SerializeField] private float startOffset = 0.25f;
        [Min(0.1f)] [SerializeField] private float probeDistance = 2f;
        [Tooltip("Distance from the root to each front/rear suspension sample.")]
        [Min(0f)] [SerializeField] private float longitudinalOffset = 0.8f;
        [Tooltip("Filters small collision-normal changes. Higher values follow tight curves faster.")]
        [Min(0f)] [SerializeField] private float normalResponse = 50f;
        [Tooltip("Reject surfaces facing away from the expected road up direction.")]
        [Range(0f, 89f)] [SerializeField] private float maximumNormalAngle = 70f;
        [Min(1)] [SerializeField] private int pathSearchRadius = 10;
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private Rigidbody body;
        private TrackProgressPath path;
        private int currentSegment = -1;
        private bool hasHistory;
        private bool continuousContact;
        public bool HasSurface { get; private set; }
        public Vector3 SurfaceNormal { get; private set; } = Vector3.up;
        // Spatially averaged support normal for physics. Temporal filtering here
        // would delay centripetal correction and let fast bikes leave crests.
        public Vector3 ContactNormal { get; private set; } = Vector3.up;
        public Vector3 SurfacePoint { get; private set; }
        // Root-to-road height along ContactNormal, NOT the sphere's travel distance.
        public float SurfaceDistance { get; private set; }
        public Vector3 LastSurfaceNormal { get; private set; } = Vector3.up;

        private void Awake() => body = GetComponent<Rigidbody>();
        private void OnEnable() => ResetTracking();

        public void Initialize(TrackProgressPath progressPath)
        {
            path = progressPath;
            ResetTracking();
        }

        public void ResetTracking()
        {
            currentSegment = -1;
            hasHistory = false;
            continuousContact = false;
            HasSurface = false;
            SurfaceNormal = ContactNormal = LastSurfaceNormal = body != null ? body.rotation * Vector3.up : transform.up;
            SurfaceDistance = 0f;
        }

        public bool Sample()
        {
            // Use the physics pose: an interpolated Transform can be one tick behind.
            Vector3 position = body != null ? body.position : transform.position;
            Quaternion rotation = body != null ? body.rotation : transform.rotation;
            Vector3 up = rotation * Vector3.up;
            Vector3 expectedUp = up;
            bool hasPathFrame = path != null && path.SampleCount > 0 && path.HasAuthoredOrientations;
            if (hasPathFrame)
            {
                float progress = path.GetProgress(position, currentSegment, pathSearchRadius, out currentSegment);
                expectedUp = path.GetRotationAtProgress(progress) * Vector3.up;
            }

            Vector3 normalSum = Vector3.zero;
            Vector3 pointSum = Vector3.zero;
            float weight = 0f;
            Vector3 offset = rotation * Vector3.forward * longitudinalOffset;
            Gather(position, up, expectedUp, 2f, ref normalSum, ref pointSum, ref weight);
            Gather(position + offset, up, expectedUp, 1f, ref normalSum, ref pointSum, ref weight);
            Gather(position - offset, up, expectedUp, 1f, ref normalSum, ref pointSum, ref weight);

            // Path banking is only a search hint. It never supplies imaginary contact
            // over a gap, steers the bike, or pulls it toward the centreline.
            if (weight == 0f && hasPathFrame && Vector3.Dot(up, expectedUp) > 0f)
            {
                Gather(position, expectedUp, expectedUp, 2f, ref normalSum, ref pointSum, ref weight);
                Gather(position + offset, expectedUp, expectedUp, 1f, ref normalSum, ref pointSum, ref weight);
                Gather(position - offset, expectedUp, expectedUp, 1f, ref normalSum, ref pointSum, ref weight);
            }

            HasSurface = weight > 0f;
            if (!HasSurface)
            {
                continuousContact = false;
                return false;
            }

            ContactNormal = normalSum.normalized;
            float response = normalResponse <= 0f ? 1f : 1f - Mathf.Exp(-normalResponse * Time.fixedDeltaTime);
            SurfaceNormal = hasHistory && continuousContact
                ? Vector3.Slerp(SurfaceNormal, ContactNormal, response).normalized : ContactNormal;
            SurfacePoint = pointSum / weight;
            SurfaceDistance = Mathf.Max(0f, Vector3.Dot(position - SurfacePoint, ContactNormal));
            LastSurfaceNormal = ContactNormal;
            hasHistory = continuousContact = true;
            return true;
        }

        private void Gather(Vector3 samplePosition, Vector3 castUp, Vector3 expectedUp, float weight,
            ref Vector3 normalSum, ref Vector3 pointSum, ref float totalWeight)
        {
            // Lift the starting sphere clear of shallow suspension/curved roads.
            // Extend the sweep equally so its far end (and capture range) stays put.
            int count = Physics.SphereCastNonAlloc(samplePosition + castUp * (startOffset + sphereRadius), sphereRadius,
                -castUp, hits, probeDistance + sphereRadius, trackMask, QueryTriggerInteraction.Ignore);
            float minimumDot = Mathf.Cos(maximumNormalAngle * Mathf.Deg2Rad);
            int best = -1;
            float distance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == null || (body != null && hits[i].collider.attachedRigidbody == body)) continue;
                Vector3 normal = hits[i].normal;
                if (normal.sqrMagnitude < 0.5f || Vector3.Dot(normal, expectedUp) < minimumDot) continue;
                if (hits[i].distance >= distance) continue;
                best = i;
                distance = hits[i].distance;
            }
            if (best < 0) return;
            normalSum += hits[best].normal * weight;
            pointSum += hits[best].point * weight;
            totalWeight += weight;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            for (int i = -1; i <= 1; i++)
            {
                Vector3 origin = transform.position + transform.forward * (i * longitudinalOffset) + transform.up * (startOffset + sphereRadius);
                Gizmos.DrawWireSphere(origin, sphereRadius);
                Gizmos.DrawRay(origin, -transform.up * (probeDistance + sphereRadius));
            }
            if (HasSurface)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(SurfacePoint, SurfaceNormal);
            }
        }
    }
}
