using System;
using System.Collections.Generic;
using RaceFatal.Infrastructure;
using RaceFatal.Infrastructure.Input;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    public class RaceRuntimeController : MonoBehaviour
    {
        private TrackRuntimeController trackRuntime;

        private readonly Dictionary<string, RacerViewController> racerViews =
            new Dictionary<string, RacerViewController>();

        private RaceDirector raceDirector;
        private IRaceInputService input;
        private string playerRacerId;

        public RaceDirector Director => raceDirector;
        public bool IsInitialized => raceDirector != null;

        public bool HasStarted =>
            raceDirector != null &&
            raceDirector.State.IsStarted;

        public void Initialize(
            RaceDirector director,
            string playerId,
            TrackRuntimeController track)
        {
            raceDirector = director
                ?? throw new ArgumentNullException(nameof(director));

            trackRuntime = track
                ?? throw new ArgumentNullException(nameof(track));

            playerRacerId = playerId;

            GameContext context =
                BootstrapController.Context;

            input = context?.Input;

            raceDirector.RacerDestroyed +=
                OnRacerDestroyed;

            if (!trackRuntime.Initialize(this))
            {
                throw new InvalidOperationException(
                    "Track runtime failed to initialize.");
            }

            foreach (RacerViewController view in racerViews.Values)
            {
                trackRuntime.BindRacer(
                    view);
            }
        }

        /// <summary>
        /// Only call this once racers have been spawned,
        /// initialized, registered and placed on the grid.
        /// </summary>
        public void StartRace()
        {
            if (raceDirector == null)
                return;

            raceDirector.StartRace();
        }

        private void Update()
        {
            if (raceDirector == null)
                return;

            HandlePlayerEquipmentInput();

            raceDirector.Tick(
                Time.deltaTime);
        }

        public void RegisterRacerView(
            RacerViewController racer)
        {
            if (racer == null ||
                !racer.IsInitialized)
            {
                return;
            }

            racerViews[racer.RacerId] =
                racer;

            if (trackRuntime != null &&
                IsInitialized)
            {
                trackRuntime.BindRacer(
                    racer);
            }
        }

        public bool TryGetRacerView(
            string racerId,
            out RacerViewController view)
        {
            return racerViews.TryGetValue(
                racerId,
                out view);
        }

        public bool PlaceRacerOnGrid(
            string racerId,
            int slot)
        {
            if (trackRuntime == null)
                return false;

            if (!TryGetRacerView(
                    racerId,
                    out RacerViewController view))
            {
                return false;
            }

            return trackRuntime.PlaceRacerAtGridSlot(
                view,
                slot);
        }

        private void HandlePlayerEquipmentInput()
        {
            if (input == null ||
                string.IsNullOrEmpty(playerRacerId))
            {
                return;
            }

            if (!TryGetRacerView(
                    playerRacerId,
                    out RacerViewController playerView))
            {
                return;
            }

            RaceParticipant participant =
                playerView.Participant;

            if (participant?.Vehicle?.EquipmentSystem == null)
                return;

            bool raceActive =
                HasStarted &&
                !raceDirector.State.IsFinished &&
                !participant.Vehicle.IsDestroyed;

            /*
             * Boost is a dedicated continuous control, completely
             * independent from selected weapons.
             */
            participant.Vehicle.EquipmentSystem.SetBoostActive(
                raceActive &&
                input.BoostHeld);

            if (!raceActive)
                return;

            if (input.NextEquipmentPressed)
            {
                raceDirector.SelectNextEquipment(
                    playerRacerId);
            }

            if (input.PreviousEquipmentPressed)
            {
                raceDirector.SelectPreviousEquipment(
                    playerRacerId);
            }

            if (input.EquipmentPressed)
            {
                raceDirector.BeginEquipmentActivation(
                    playerRacerId);
            }

            if (input.EquipmentReleased)
            {
                raceDirector.EndEquipmentActivation(
                    playerRacerId);
            }
        }

        private void OnRacerDestroyed(
            RaceParticipant participant)
        {
            if (!TryGetRacerView(
                    participant.RacerId,
                    out RacerViewController view))
            {
                return;
            }

            /*
             * Ensure a destroyed player cannot leave its booster
             * active in race state.
             */
            participant.Vehicle
                .EquipmentSystem
                .SetBoostActive(false);

            view.gameObject.SendMessage(
                "OnRaceDestroyed",
                SendMessageOptions.DontRequireReceiver);
        }

        private void OnDestroy()
        {
            if (raceDirector != null)
            {
                raceDirector.RacerDestroyed -=
                    OnRacerDestroyed;
            }
        }
    }
}