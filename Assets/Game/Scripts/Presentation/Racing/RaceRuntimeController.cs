using System;
using System.Collections.Generic;
using RaceFatal.Infrastructure;
using RaceFatal.Infrastructure.Input;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using UnityEngine;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Combat;
using RaceFatal.Shared;

namespace RaceFatal.Presentation.Racing
{
    public class RaceRuntimeController : MonoBehaviour
    {
        [Header("Combat Balance")] [Tooltip("Damage dealt when one non-player racer damages another. " + "Player damage dealt and received is unaffected.")] [Range(0f,
            1f)] [SerializeField] private float aiVsAIDamageMultiplier = 0.45f;
        [Header("Audience Favor")] [SerializeField] private AudienceSettings audienceSettings = new AudienceSettings();
        private TrackRuntimeController trackRuntime;
        private readonly Dictionary<string, RacerViewController> racerViews = new Dictionary<string, RacerViewController>();
        private RaceDirector raceDirector;
        private IRaceInputService input;
        private string playerRacerId;
        public RaceDirector Director => raceDirector;
        public string PlayerRacerId => playerRacerId;
        public bool IsInitialized => raceDirector != null;
        public bool HasStarted => raceDirector != null && raceDirector.State.IsStarted;
        public bool IsRaceActive => raceDirector != null && raceDirector.State.IsStarted && !raceDirector.State.IsFinished;

        private void Awake()
        {
            if (GetComponent<RacePauseController>() == null)
                gameObject.AddComponent<RacePauseController>();
        }

        public void Initialize(RaceDirector director, string playerId, TrackRuntimeController track)
        {
            raceDirector = director ?? throw new ArgumentNullException(nameof(director));
            raceDirector.AIVsAIDamageMultiplier = Mathf.Clamp01(aiVsAIDamageMultiplier);
            raceDirector.ConfigureAudience(audienceSettings);
            trackRuntime = track ?? throw new ArgumentNullException(nameof(track));
            playerRacerId = playerId;
            GameContext context = BootstrapController.Context;
            input = context?.Input;
            raceDirector.RacerDestroyed += OnRacerDestroyed;
            raceDirector.RacerRetired += OnRacerRetired;
            raceDirector.RaceCompleted += OnRaceCompleted;
            if (!trackRuntime.Initialize(this))
            {
                throw new InvalidOperationException("Track runtime failed to initialize.");
            }

            foreach (RacerViewController view in racerViews.Values)
            {
                trackRuntime.BindRacer(view);
            }
        }

        public void StartRace()
        {
            if (raceDirector == null)
                return;
            raceDirector.StartRace();
        }

        private void Update()
        {
            if (RacePauseController.IsGameplayBlocked)
                return;
            if (raceDirector == null)
                return;
            HandlePlayerEquipmentInput();
            foreach (var view in racerViews.Values)
            {
                if (view == null)
                    continue;
                var motor = view.GetComponent<BikeMotor>();
                raceDirector.ReportSpeed(view.RacerId, motor != null ? motor.SpeedKph : 0);
            }

            raceDirector.Tick(Time.deltaTime);
        }

        public void RegisterRacerView(RacerViewController racer)
        {
            if (racer == null || !racer.IsInitialized)
            {
                return;
            }

            racerViews[racer.RacerId] = racer;
            if (trackRuntime != null && IsInitialized)
            {
                trackRuntime.BindRacer(racer);
            }
        }

        public bool TryGetRacerView(string racerId, out RacerViewController view)
        {
            return racerViews.TryGetValue(racerId, out view);
        }

        public bool PlaceRacerOnGrid(string racerId, int slot)
        {
            if (trackRuntime == null)
                return false;
            if (!TryGetRacerView(racerId, out RacerViewController view))
            {
                return false;
            }

            return trackRuntime.PlaceRacerAtGridSlot(view, slot);
        }

        public void StopRacerControl(string racerId, float brakeInput = 1f)
        {
            if (!TryGetRacerView(racerId, out RacerViewController view))
            {
                return;
            }

            RaceParticipant participant = view.Participant;
            if (participant?.Vehicle?.EquipmentSystem != null)
            {
                participant.Vehicle.EquipmentSystem.SetBoostActive(false);
            }

            BikeController playerController = view.GetComponent<BikeController>();
            AIDriverController aiDriver = view.GetComponent<AIDriverController>();
            BikeMotor motor = view.GetComponent<BikeMotor>();
            if (playerController != null)
                playerController.enabled = false;
            if (aiDriver != null)
                aiDriver.enabled = false;
            if (motor != null)
            {
                motor.SetControls(0f, Mathf.Clamp01(brakeInput), 0f);
            }
        }

        public void StopAllVehicleControl(float brakeInput = 1f)
        {
            foreach (RacerViewController view in racerViews.Values)
            {
                if (view == null || !view.IsInitialized)
                {
                    continue;
                }

                StopRacerControl(view.RacerId, brakeInput);
            }
        }

        private void HandlePlayerEquipmentInput()
        {
            if (input == null || string.IsNullOrEmpty(playerRacerId))
            {
                return;
            }

            if (!TryGetRacerView(playerRacerId, out RacerViewController playerView))
            {
                return;
            }

            RaceParticipant participant = playerView.Participant;
            if (participant?.Vehicle?.EquipmentSystem == null)
                return;
            bool raceActive = IsRaceActive && participant.Status == RaceParticipantStatus.Racing && !participant.Vehicle.IsDestroyed;
            participant.Vehicle.EquipmentSystem.SetBoostActive(raceActive && input.BoostHeld);
            if (!raceActive)
                return;
            if (input.NextEquipmentPressed)
            {
                raceDirector.SelectNextEquipment(playerRacerId);
            }

            if (input.PreviousEquipmentPressed)
            {
                raceDirector.SelectPreviousEquipment(playerRacerId);
            }

            if (input.EquipmentPressed)
            {
                RaceEquipmentSystem equipment = participant.Vehicle.EquipmentSystem;
                WeaponDefinition weapon = equipment.SelectedWeaponDefinition;
                bool canActivate = true;
                if (weapon != null && weapon.AimMode == WeaponAimMode.Targeted && weapon.DeliveryMode == WeaponDeliveryMode.GuidedProjectile)
                {
                    GuidedTargetLockState lockState = playerView.GetComponent<GuidedTargetLockState>();
                    canActivate = lockState != null && lockState.IsLocked;
                }

                if (canActivate)
                {
                    raceDirector.BeginEquipmentActivation(playerRacerId);
                }
            }

            if (input.EquipmentReleased)
            {
                raceDirector.EndEquipmentActivation(playerRacerId);
            }
        }

        private void OnRacerRetired(RaceParticipant participant) => StopRacerControl(participant.RacerId);
        private void OnRaceCompleted(RaceResult result) => StopAllVehicleControl();
        private void OnRacerDestroyed(RaceParticipant participant)
        {
            participant.Vehicle.EquipmentSystem.SetBoostActive(false);
            if (!TryGetRacerView(participant.RacerId, out RacerViewController view))
            {
                return;
            }

            view.gameObject.SendMessage("OnRaceDestroyed", SendMessageOptions.DontRequireReceiver);
        }

        private void OnDestroy()
        {
            if (raceDirector != null)
            {
                raceDirector.RacerRetired -= OnRacerRetired;
                raceDirector.RaceCompleted -= OnRaceCompleted;
                raceDirector.RacerDestroyed -= OnRacerDestroyed;
            }
        }
    }
}
