/*
    GOOD FOR LEANING THE BIKE VISUALS BASED ON STEERING INPUT
    MAY ADD MOVING COCKPIT SHELL, SUSPENSION APPEARANCE, STEERING ANIMATIONS, ETC. LATER
*/


using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    public class BikeVisualController :
        MonoBehaviour
    {
        [SerializeField]
        private BikeMotor motor;

        [SerializeField]
        private Transform visualRoot;

        [Min(0f)]
        [SerializeField]
        private float maximumLeanAngle =
            30f;

        [Min(0f)]
        [SerializeField]
        private float leanSpeed =
            8f;

        private Quaternion baseRotation;

        private void Awake()
        {
            if (visualRoot != null)
            {
                baseRotation =
                    visualRoot.localRotation;
            }
        }

        private void LateUpdate()
        {
            if (motor == null ||
                visualRoot == null)
            {
                return;
            }

            float speedFactor =
                Mathf.Clamp01(
                    motor.SpeedKph /
                    50f);

            float lean =
                -motor.SteeringInput *
                maximumLeanAngle *
                speedFactor;

            Quaternion target =
                baseRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    lean);

            visualRoot.localRotation =
                Quaternion.Slerp(
                    visualRoot.localRotation,
                    target,
                    leanSpeed *
                    Time.deltaTime);
        }
    }
}