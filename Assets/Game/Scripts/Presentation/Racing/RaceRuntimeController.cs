using System;
using System.Collections.Generic;
using RaceFatal.Infrastructure;
using RaceFatal.Infrastructure.Input;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    public class RaceRuntimeController :
        MonoBehaviour
    {
        private readonly Dictionary<
            string,
            RacerViewController>
            racerViews =
                new Dictionary<
                    string,
                    RacerViewController>();

        private RaceDirector raceDirector;

        private IRaceInputService input;

        private string playerRacerId;

        public RaceDirector Director =>
            raceDirector;

        public bool IsInitialized =>
            raceDirector != null;

        public void Initialize(
            RaceDirector director,
            string playerId)
        {
            raceDirector = director
                ?? throw new ArgumentNullException(
                    nameof(director));

            playerRacerId = playerId;

            GameContext context =
                Bootstrap.BootstrapController.GameContext;

            input = context.InputService;

            raceDirector.RacerDestroyed +=
                OnRacerDestroyed;

            raceDirector.StartRace();
        }

        private void Update()
        {
            if (raceDirector == null)
                return;

            float deltaTime =
                Time.deltaTime;

            HandlePlayerEquipmentInput();

            raceDirector.Tick(
                deltaTime);
        }

        public void RegisterRacerView(
            RacerViewController racer)
        {
            if (racer == null ||
                !racer.IsInitialized)
            {
                return;
            }

            racerViews[
                racer.RacerId] = racer;
        }

        public bool TryGetRacerView(
            string racerId,
            out RacerViewController view)
        {
            return racerViews.TryGetValue(
                racerId,
                out view);
        }

        private void HandlePlayerEquipmentInput()
        {
            if (input == null ||
                string.IsNullOrEmpty(
                    playerRacerId))
            {
                return;
            }

            if (input.NextEquipmentPressed)
            {
                raceDirector
                    .SelectNextEquipment(
                        playerRacerId);
            }

            if (input.PreviousEquipmentPressed)
            {
                raceDirector
                    .SelectPreviousEquipment(
                        playerRacerId);
            }

            if (input.EquipmentPressed)
            {
                raceDirector
                    .BeginEquipmentActivation(
                        playerRacerId);
            }

            if (input.EquipmentReleased)
            {
                raceDirector
                    .EndEquipmentActivation(
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

            view.gameObject.SendMessage(
                "OnRaceDestroyed",
                SendMessageOptions
                    .DontRequireReceiver);
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