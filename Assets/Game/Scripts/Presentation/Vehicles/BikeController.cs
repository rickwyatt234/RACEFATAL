using RaceFatal.Infrastructure.Input;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(BikeMotor))]
    [RequireComponent(typeof(RacerViewController))]
    public sealed class BikeController :
        MonoBehaviour
    {
        private BikeMotor motor;

        private RacerViewController racerView;

        private IRaceInputService input;

        private void Awake()
        {
            motor =
                GetComponent<BikeMotor>();

            racerView =
                GetComponent<RacerViewController>();
        }

        private void Start()
        {
            if (BootstrapController.GameContext != null)
            {
                input =
                    BootstrapController
                        .GameContext
                        .InputService;
            }
        }

        private void Update()
        {
            if (input == null)
                return;

            if (!racerView.IsInitialized)
                return;

            RaceParticipant participant =
                racerView.Participant;

            // Only the player's bike reads player input.
            if (participant.Role !=
                RaceParticipantRole.Player)
            {
                return;
            }

            motor.SetPerformance(
                participant
                    .Vehicle
                    .Performance);

            motor.SetRuntimeModifiers(
                participant
                    .Vehicle
                    .EquipmentSystem
                    .SpeedMultiplier,

                participant
                    .Vehicle
                    .EquipmentSystem
                    .AccelerationMultiplier,

                participant
                    .Vehicle
                    .EquipmentSystem
                    .HandlingMultiplier);

            motor.SetControls(
                input.Throttle,
                input.Brake,
                input.Steering);
        }
    }
}