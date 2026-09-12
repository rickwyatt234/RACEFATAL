using System.Collections.Generic;
using RaceFatal.Racing;
using UnityEngine;
using TMPro;

namespace RaceFatal.Presentation.Racing
{
    public class RaceStandingsView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI headingText;

        [Tooltip("Parent containing Row01 through Row12.")]
        [SerializeField] private Transform rowsRoot;

        [SerializeField] private List<RaceStandingsRowView> rows =
            new List<RaceStandingsRowView>();

        [Header("Live Refresh")]
        [Min(0.02f)][SerializeField] private float refreshInterval = 0.1f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private bool debugLiveMode;
        [SerializeField] private int debugCachedRows;
        [SerializeField] private int debugVisibleRows;
        [SerializeField] private int debugParticipantCount;
        [SerializeField] private int debugResultCount;

        private RaceRuntimeController raceRuntime;

        private float refreshTimer;
        private bool liveMode;

        private void Awake()
        {
            CacheRows();
            HideAllRows();
        }

        private void OnEnable()
        {
            if (rows.Count == 0)
                CacheRows();
        }

        private void Update()
        {
            if (!liveMode ||
                raceRuntime == null ||
                raceRuntime.Director == null)
            {
                return;
            }

            refreshTimer -=
                Time.deltaTime;

            if (refreshTimer > 0f)
                return;

            refreshTimer =
                refreshInterval;

            RefreshLive();
        }

        public void Initialize(
            RaceRuntimeController runtime)
        {
            raceRuntime = runtime;

            if (rows.Count == 0)
                CacheRows();

            debugInitialized =
                raceRuntime != null &&
                raceRuntime.Director != null;
        }

        public void ShowLive()
        {
            if (rows.Count == 0)
                CacheRows();

            if (raceRuntime == null ||
                raceRuntime.Director == null)
            {
                Debug.LogWarning(
                    "RaceStandingsView cannot show live standings because it has not been initialized.",
                    this);

                return;
            }

            if (rows.Count == 0)
            {
                Debug.LogError(
                    "RaceStandingsView has no RaceStandingsRowView rows.",
                    this);

                return;
            }

            liveMode = true;
            debugLiveMode = true;

            refreshTimer = 0f;

            if (headingText != null)
                headingText.text = "LIVE RACE STANDINGS";

            RefreshLive();
        }

        public void ShowFinal(
            RaceResult result)
        {
            if (rows.Count == 0)
                CacheRows();

            liveMode = false;
            debugLiveMode = false;

            if (headingText != null)
                headingText.text = "FINAL RESULTS";

            if (rows.Count == 0)
            {
                Debug.LogError(
                    "RaceStandingsView has no RaceStandingsRowView rows.",
                    this);

                return;
            }

            RefreshFinal(
                result);
        }

        private void CacheRows()
        {
            rows.Clear();

            Transform searchRoot =
                rowsRoot != null
                    ? rowsRoot
                    : transform;

            RaceStandingsRowView[] foundRows =
                searchRoot.GetComponentsInChildren<RaceStandingsRowView>(true);

            rows.AddRange(
                foundRows);

            debugCachedRows =
                rows.Count;

            if (rows.Count == 0)
            {
                Debug.LogWarning(
                    "RaceStandingsView found no standings rows. Assign Rows Root or add RaceStandingsRowView components beneath it.",
                    this);
            }
        }

        private void RefreshLive()
        {
            if (raceRuntime?.Director?.State == null)
                return;

            RaceState state =
                raceRuntime.Director.State;

            IReadOnlyList<RaceParticipant> order =
                state.GetCurrentOrder();

            debugParticipantCount =
                order.Count;

            int visible =
                Mathf.Min(
                    rows.Count,
                    order.Count);

            for (int i = 0;
                 i < visible;
                 i++)
            {
                RaceParticipant participant =
                    order[i];

                if (rows[i] == null)
                    continue;

                rows[i].Render(
                    i + 1,
                    participant.Racer.Name,
                    participant.TeamName,
                    GetLiveRaceTimeText(participant));
            }

            HideRowsAfter(
                visible);

            debugVisibleRows =
                visible;
        }

        private void RefreshFinal(
            RaceResult result)
        {
            if (result == null)
            {
                debugResultCount = 0;
                HideAllRows();
                return;
            }

            debugResultCount =
                result.Standings.Count;

            int visible =
                Mathf.Min(
                    rows.Count,
                    result.Standings.Count);

            for (int i = 0;
                 i < visible;
                 i++)
            {
                RaceResultEntry entry =
                    result.Standings[i];

                if (rows[i] == null)
                    continue;

                rows[i].Render(
                    entry.Position,
                    entry.RacerName,
                    entry.TeamName,
                    GetFinalRaceTimeText(entry));
            }

            HideRowsAfter(
                visible);

            debugVisibleRows =
                visible;
        }

        private string GetLiveRaceTimeText(
            RaceParticipant participant)
        {
            if (participant.FinishTimeSeconds.HasValue)
            {
                return FormatRaceTime(
                    participant.FinishTimeSeconds.Value);
            }

            if (participant.Status ==
                RaceParticipantStatus.Finished)
            {
                return participant.WasFastResolved
                    ? "RESOLVED"
                    : "--:--.---";
            }

            if (participant.Status ==
                    RaceParticipantStatus.Destroyed ||
                participant.Status ==
                    RaceParticipantStatus.Retired)
            {
                return "DNF";
            }

            if (participant.Status ==
                RaceParticipantStatus.Racing)
            {
                return "RACING";
            }

            return "--:--.---";
        }

        private string GetFinalRaceTimeText(
            RaceResultEntry entry)
        {
            if (entry.RaceTimeSeconds.HasValue)
            {
                return FormatRaceTime(
                    entry.RaceTimeSeconds.Value);
            }

            if (entry.WasFastResolved)
                return "RESOLVED";

            if (entry.Status ==
                    RaceParticipantStatus.Destroyed ||
                entry.Status ==
                    RaceParticipantStatus.Retired)
            {
                return "DNF";
            }

            return "--:--.---";
        }

        private string FormatRaceTime(
            float totalSeconds)
        {
            int totalMilliseconds =
                Mathf.Max(
                    0,
                    Mathf.RoundToInt(
                        totalSeconds * 1000f));

            int minutes =
                totalMilliseconds / 60000;

            int seconds =
                totalMilliseconds / 1000 % 60;

            int milliseconds =
                totalMilliseconds % 1000;

            return
                $"{minutes:00}:{seconds:00}.{milliseconds:000}";
        }

        private void HideRowsAfter(
            int firstHiddenIndex)
        {
            for (int i = firstHiddenIndex;
                 i < rows.Count;
                 i++)
            {
                if (rows[i] != null)
                    rows[i].Hide();
            }
        }

        private void HideAllRows()
        {
            HideRowsAfter(0);

            debugVisibleRows = 0;
        }
    }
}