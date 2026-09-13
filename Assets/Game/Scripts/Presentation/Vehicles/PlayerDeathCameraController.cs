using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    public class PlayerDeathCameraController : MonoBehaviour
    {
        private Vector3 focusPosition;

        private Vector3 startPosition;
        private Quaternion startRotation;

        private Vector3 targetPosition;

        private float holdDuration;
        private float driftDuration;
        private float lookResponse;

        private float timer;

        private bool initialized;

        public void Initialize(
            Vector3 deathPosition,
            float holdTime,
            float movementDuration,
            float backwardDistance,
            float upwardDistance,
            float rotationResponse)
        {
            focusPosition = deathPosition;

            holdDuration = Mathf.Max(0f, holdTime);
            driftDuration = Mathf.Max(0.01f, movementDuration);
            lookResponse = Mathf.Max(0f, rotationResponse);

            startPosition = transform.position;
            startRotation = transform.rotation;

            Vector3 backward =
                -transform.forward *
                backwardDistance;

            Vector3 upward =
                transform.up *
                upwardDistance;

            targetPosition =
                startPosition +
                backward +
                upward;

            timer = 0f;
            initialized = true;
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            timer += Time.deltaTime;

            if (timer <= holdDuration)
            {
                transform.SetPositionAndRotation(
                    startPosition,
                    startRotation);

                return;
            }

            float driftTime =
                timer -
                holdDuration;

            float normalized =
                Mathf.Clamp01(
                    driftTime /
                    driftDuration);

            float smooth =
                normalized *
                normalized *
                (3f - 2f * normalized);

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    smooth);

            UpdateLookDirection();
        }

        private void UpdateLookDirection()
        {
            Vector3 direction =
                focusPosition -
                transform.position;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    transform.up);

            if (lookResponse <= 0f)
            {
                transform.rotation =
                    targetRotation;

                return;
            }

            float factor =
                1f -
                Mathf.Exp(
                    -lookResponse *
                    Time.deltaTime);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    factor);
        }
    }
}