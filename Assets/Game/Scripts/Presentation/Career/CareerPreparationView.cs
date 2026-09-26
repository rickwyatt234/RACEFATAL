using System.Collections.Generic;
using System.Linq;
using System.Text;
using RaceFatal.Career;
using RaceFatal.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public sealed class CareerPreparationView : MonoBehaviour
    {
        private CareerController owner;
        private GameContext context;
        private CareerRacesView races;
        private GameObject modal;
        private TMP_Text details, feedback, recoveryLabel;
        private Button recoverButton;
        private readonly List<Selectable> blocked = new List<Selectable>();
        private TeamRecoveryQuote pending;
        private bool confirming, busy, forEvent;

        public static void Create(CareerController owner, GameContext context, CareerRacesView races,
            Transform canvas, params GameObject[] roots)
        {
            var host = new GameObject("TeamPreparationController"); host.transform.SetParent(canvas, false);
            var view = host.AddComponent<CareerPreparationView>();
            view.owner = owner; view.context = context; view.races = races;
            view.Build(canvas);
            for (int i = 0; i < roots.Length; i++)
            {
                bool eventScreen = i == 0;
                if (roots[i] != null) CareerRuntimeUi.Button(roots[i].transform, "PREPARATION / RECOVERY",
                    new Vector2(1440, -95), new Vector2(380, 50), () => view.Open(eventScreen));
            }
        }
        private void Build(Transform canvas)
        {
            var panel = CareerRuntimeUi.Modal(canvas, "TeamPreparation", new Vector2(1200, 880), out modal);
            CareerRuntimeUi.Text(panel, "Heading", "TEAM // RACE PREPARATION", new Vector2(35, -30), new Vector2(1130, 60), 34);
            details = CareerRuntimeUi.Scroll(panel, "Checklist", new Vector2(35, -110), new Vector2(1130, 460));
            feedback = CareerRuntimeUi.Text(panel, "Feedback", "", new Vector2(35, -585), new Vector2(1130, 85));
            CareerRuntimeUi.Button(panel, "GARAGE", new Vector2(35, -685), new Vector2(265, 55), () => Navigate(CareerScreen.Garage));
            CareerRuntimeUi.Button(panel, "ROSTER", new Vector2(323, -685), new Vector2(265, 55), () => Navigate(CareerScreen.Roster));
            CareerRuntimeUi.Button(panel, "SHOP", new Vector2(611, -685), new Vector2(265, 55), () => Navigate(CareerScreen.Shop));
            CareerRuntimeUi.Button(panel, "CAREER HOME", new Vector2(899, -685), new Vector2(265, 55), () => Navigate(CareerScreen.Home));
            recoverButton = CareerRuntimeUi.Button(panel, "REVIEW TEAM RECOVERY", new Vector2(35, -770), new Vector2(700, 65), Recover);
            recoveryLabel = recoverButton.GetComponentInChildren<TMP_Text>();
            CareerRuntimeUi.Button(panel, "BACK / CLOSE", new Vector2(770, -770), new Vector2(395, 65), Back);
            modal.SetActive(false);
        }
        private void Open(bool eventScreen)
        {
            forEvent = eventScreen; confirming = false;
            modal.SetActive(true); modal.transform.SetAsLastSibling();
            foreach (var control in transform.parent.GetComponentsInChildren<Selectable>())
                if (control.interactable && !control.transform.IsChildOf(modal.transform))
                { blocked.Add(control); control.interactable = false; }
            Refresh();
        }
        private void Refresh()
        {
            var session = context.Sessions.Current;
            var race = forEvent ? races?.SelectedRaceDefinition : null;
            var text = new StringBuilder(race == null ? "TEAM EQUIPMENT AND ROSTER\n\n" : race.DisplayName + "\n\n");
            foreach (var check in TeamPreparationService.Inspect(session, race)) text.AppendLine(check + "\n");
            if (forEvent && races != null && races.SelectedEventId != null)
            {
                var entry = new CareerCalendarService(context.Database).CanEnter(session.PlayerTeam, races.SelectedEventId);
                text.AppendLine(entry.IsSuccess ? "[READY] Event entry fee and unlock requirements met." : "[ACTION] " + entry.ErrorMessage);
            }
            text.AppendLine("\nAll event rules, content and loadouts are checked again when entering. Recovery supplies a basic team; higher-class events may require further upgrades.");
            details.text = text.ToString();
            var quote = new TeamRecoveryService(context.Database).Quote(session);
            pending = quote.IsSuccess ? quote.Value : null;
            recoverButton.interactable = pending != null && context.Saves.HasActiveCampaign && !context.RaceLaunch.HasPendingRace;
            recoveryLabel.text = "REVIEW TEAM RECOVERY";
            feedback.text = quote.IsSuccess ? "Recovery can restore your minimum team setup. Review the plan before confirming." : quote.ErrorMessage;
            ResetScroll();
        }
        private void Recover()
        {
            if (busy || pending == null || context.RaceLaunch.HasPendingRace) return;
            if (!confirming)
            {
                confirming = true; details.text = pending.Description;
                recoveryLabel.text = "CONFIRM RECOVERY & SAVE";
                feedback.text = "Review the credit charge and assistance above. Back cancels this confirmation.";
                ResetScroll(); return;
            }
            busy = true;
            var saved = context.Saves.RecoverTeam(pending.Signature);
            busy = false; confirming = false; pending = null;
            Refresh(); owner.RefreshHome();
            feedback.text = saved.IsSuccess ? "TEAM RECOVERED AND CAMPAIGN SAVED. Review assignments in Garage and Roster." : saved.ErrorMessage;
        }
        private void Back() { if (busy) return; if (confirming) { confirming = false; Refresh(); } else Close(); }
        private void Navigate(CareerScreen screen) { if (busy) return; Close(); owner.ShowScreen(screen); }
        private void Close()
        {
            foreach (var control in blocked) if (control != null) control.interactable = true;
            blocked.Clear(); pending = null; confirming = false; modal.SetActive(false);
            owner.ShowScreen(owner.CurrentScreen);
        }
        private void ResetScroll() { Canvas.ForceUpdateCanvases(); details.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1; }
        private void OnDestroy() { if (modal != null) Destroy(modal); }
    }
}
