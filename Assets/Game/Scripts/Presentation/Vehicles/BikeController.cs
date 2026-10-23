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
        public bool IsReady => motor != null && racerView != null;

        private void Awake()
        {
            motor = GetComponentInParent<BikeMotor>();
            racerView = GetComponentInParent<RacerViewController>();
        }

        private void Start()
        {
            ResolveInput();
        }

        private void Update()
        {
            if (RacePauseController.IsGameplayBlocked)
                return;
            if (input == null)
            {
                ResolveInput();
                if (input == null)
                    return;
            }

            if (motor == null || racerView == null || !racerView.IsInitialized)
            {
                return;
            }

            RaceParticipant participant = racerView.Participant;
            if (participant == null || participant.Role != RaceParticipantRole.Player || participant.Vehicle == null)
            {
                return;
            }

            motor.SetPerformance(participant.Vehicle.Performance);
            motor.SetRuntimeModifiers(participant.Vehicle.EquipmentSystem.SpeedMultiplier, participant.Vehicle.EquipmentSystem.AccelerationMultiplier, participant.Vehicle.TemporaryHandlingMultiplier);
            if (RaceFatal.Presentation.Tactics.TacticalRaceController.IsActive)
            {
                var path = FindFirstObjectByType<RaceFatal.Presentation.Tracks.TrackProgressPath>();
                if (path != null && path.TotalLength > 0f)
                {
                    int segment;
                    float progress = path.GetProgress(motor.transform.position,-1,path.SegmentCount,out segment);
                    float look = path.AdvanceProgressByDistance(progress,Mathf.Max(15f,motor.SpeedMetersPerSecond*0.5f));
                    Vector3 direction = path.GetPositionAtProgress(look)-motor.transform.position;
                    float angle = Vector3.SignedAngle(motor.transform.forward,Vector3.ProjectOnPlane(direction,motor.transform.up),motor.transform.up);
                    motor.SetControls(0.35f,Mathf.Abs(angle)>30f?0.5f:0f,Mathf.Clamp(angle/30f,-1f,1f));
                    return;
                }
            }
            motor.SetControls(input.Throttle, input.Brake, input.Steering);
        }

        private void OnDisable()
        {
            if (motor == null)
                return;
            motor.SetControls(0f, 0f, 0f);
        }

        private void ResolveInput()
        {
            if (BootstrapController.Context == null)
                return;
            input = BootstrapController.Context.Input;
        }
    }
}
