using RaceFatal.Infrastructure.Input;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    public class BikeController : MonoBehaviour
    {
        private BikeMotor motor;
        private RacerViewController racerView;
        private IRaceInputService input;

        public bool IsReady =>
            motor != null &&
            racerView != null;

        private void Awake()
        {
            motor =
                GetComponentInParent<BikeMotor>();

            racerView =
                GetComponentInParent<RacerViewController>();
        }

        private void Start()
        {
            ResolveInput();
        }

        private void Update()
        {
            if (input == null)
            {
                ResolveInput();

                if (input == null)
                    return;
            }

            if (motor == null ||
                racerView == null ||
                !racerView.IsInitialized)
            {
                return;
            }

            RaceParticipant participant =
                racerView.Participant;

            if (participant == null ||
                participant.Role !=
                    RaceParticipantRole.Player ||
                participant.Vehicle == null)
            {
                return;
            }

            motor.SetPerformance(
                participant.Vehicle.Performance);

            motor.SetRuntimeModifiers(
                participant.Vehicle
                    .EquipmentSystem
                    .SpeedMultiplier,
                participant.Vehicle
                    .EquipmentSystem
                    .AccelerationMultiplier,
                participant.Vehicle
                    .EquipmentSystem
                    .HandlingMultiplier);

            motor.SetControls(
                input.Throttle,
                input.Brake,
                input.Steering);
        }

        private void OnDisable()
        {
            if (motor == null)
                return;

            motor.SetControls(
                0f,
                0f,
                0f);
        }

        private void ResolveInput()
        {
            if (BootstrapController.Context == null)
                return;

            input =
                BootstrapController
                    .Context
                    .Input;
        }
    }
}