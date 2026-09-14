using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class CountermeasureFlareView : MonoBehaviour
    {
        [Header("Movement")]
        [Min(0f)][SerializeField] private float velocityDrag = 0.4f;

        [Header("Lifetime")]
        [Min(0.05f)][SerializeField] private float defaultLifetime = 1.5f;

        private Vector3 velocity;
        private float remainingLifetime;
        private bool initialized;

        public void Initialize(
            Vector3 inheritedVelocity,
            Vector3 ejectionDirection,
            float ejectionSpeed,
            float lifetime)
        {
            Vector3 direction =
                ejectionDirection.sqrMagnitude > 0.001f
                    ? ejectionDirection.normalized
                    : -transform.forward;

            velocity =
                inheritedVelocity +
                direction *
                Mathf.Max(
                    0f,
                    ejectionSpeed);

            remainingLifetime =
                lifetime > 0f
                    ? lifetime
                    : defaultLifetime;

            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
                return;

            float deltaTime =
                Time.deltaTime;

            transform.position +=
                velocity *
                deltaTime;

            if (velocityDrag > 0f)
            {
                float factor =
                    Mathf.Exp(
                        -velocityDrag *
                        deltaTime);

                velocity *= factor;
            }

            if (velocity.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        velocity.normalized,
                        transform.up);
            }

            remainingLifetime -=
                deltaTime;

            if (remainingLifetime <= 0f)
                Destroy(gameObject);
        }
    }
}