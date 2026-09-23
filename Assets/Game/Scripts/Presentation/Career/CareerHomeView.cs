using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Vehicles;
using TMPro;
using UnityEngine;

namespace RaceFatal.Presentation.Career
{
    public class CareerHomeView :
        MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private TMP_Text teamNameText;
        [SerializeField] private TMP_Text racerNameText;

        [Header("Resources")]
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private TMP_Text fameText;
        [SerializeField] private TMP_Text researchPointsText;

        [Header("Current Setup")]
        [SerializeField] private TMP_Text currentBikeText;
        [SerializeField] private TMP_Text currentPartnerText;

        [Header("Career")]
        [SerializeField] private TMP_Text careerStatusText;

        public void Bind(
            GameSessionState session,
            GameDatabase database)
        {
            if (session == null)
            {
                Clear();
                return;
            }

            TeamState team =
                session.PlayerTeam;

            CareerRun run =
                session.CareerRun;

            RacerState player =
                ResolvePlayer(
                    session);

            RacerState partner =
                team?.Roster?.FindRacer(
                    session.DefaultPartnerRacerId);

            SetText(
                teamNameText,
                team != null
                    ? team.TeamName
                    : "NO TEAM");

            SetText(
                racerNameText,
                player != null
                    ? player.Name
                    : "NO ACTIVE RACER");

            SetText(
                creditsText,
                $"CREDITS  {(team?.Credits ?? 0):N0}");

            SetText(
                fameText,
                $"TEAM FAME  {(team?.Fame ?? 0):N0}");

            SetText(
                researchPointsText,
                $"RESEARCH POINTS  {(team?.ResearchPoints ?? 0):N0}");

            SetText(
                currentBikeText,
                "CURRENT BIKE  " +
                ResolveBikeName(
                    session,
                    database));

            SetText(
                currentPartnerText,
                "CURRENT PARTNER  " +
                (partner != null
                    ? partner.Name
                    : "NONE"));

            string careerStatus =
                run == null
                    ? "NO ACTIVE CAREER"
                    : run.IsActive
                        ? "ACTIVE"
                        : "INACTIVE";

            SetText(
                careerStatusText,
                $"CAREER STATUS  {careerStatus}");
        }

        private RacerState ResolvePlayer(
            GameSessionState session)
        {
            if (session?.CareerRun?.Player != null)
            {
                return session.CareerRun.Player;
            }

            if (session?.PlayerTeam?.Roster?.Racers == null)
            {
                return null;
            }

            for (int i =
                     session.PlayerTeam.Roster.Racers.Count - 1;
                 i >= 0;
                 i--)
            {
                RacerState racer =
                    session.PlayerTeam.Roster.Racers[i];

                if (racer != null &&
                    racer.IsPlayerCharacter)
                {
                    return racer;
                }
            }

            return null;
        }

        private string ResolveBikeName(
            GameSessionState session,
            GameDatabase database)
        {
            if (session?.PlayerTeam?.Garage == null ||
                string.IsNullOrWhiteSpace(
                    session.SelectedPlayerBikeId))
            {
                return "NONE";
            }

            BikeState bike =
                session.PlayerTeam.Garage.FindBike(
                    session.SelectedPlayerBikeId);

            if (bike == null)
            {
                return "NONE";
            }

            BikeDefinition definition =
                database?.GetBikeDefinition(
                    bike.BikeDefinitionId);

            if (definition != null &&
                !string.IsNullOrWhiteSpace(
                    definition.DisplayName))
            {
                return definition.DisplayName;
            }

            return string.IsNullOrWhiteSpace(
                bike.BikeDefinitionId)
                ? bike.BikeId
                : bike.BikeDefinitionId;
        }

        private void Clear()
        {
            SetText(teamNameText, "NO TEAM");
            SetText(racerNameText, "NO ACTIVE RACER");
            SetText(creditsText, "CREDITS  0");
            SetText(fameText, "TEAM FAME  0");
            SetText(researchPointsText, "RESEARCH POINTS  0");
            SetText(currentBikeText, "CURRENT BIKE  NONE");
            SetText(currentPartnerText, "CURRENT PARTNER  NONE");
            SetText(careerStatusText, "CAREER STATUS  UNAVAILABLE");
        }

        private void SetText(
            TMP_Text target,
            string value)
        {
            if (target != null)
            {
                target.text =
                    value ?? string.Empty;
            }
        }
    }
}
