using UnityEngine;
using TMPro;

namespace RaceFatal.Presentation.Racing
{
    public class RaceStandingsRowView : MonoBehaviour
    {
        [Header("Columns")]
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI racerNameText;
        [SerializeField] private TextMeshProUGUI teamNameText;
        [SerializeField] private TextMeshProUGUI raceTimeText;

        public void Render(
            int position,
            string racerName,
            string teamName,
            string raceTime)
        {
            gameObject.SetActive(true);

            if (positionText != null)
                positionText.text = position.ToString("00");

            if (racerNameText != null)
            {
                racerNameText.text =
                    string.IsNullOrWhiteSpace(racerName)
                        ? "UNKNOWN"
                        : racerName.ToUpperInvariant();
            }

            if (teamNameText != null)
            {
                teamNameText.text =
                    string.IsNullOrWhiteSpace(teamName)
                        ? "UNKNOWN"
                        : teamName.ToUpperInvariant();
            }

            if (raceTimeText != null)
                raceTimeText.text = raceTime;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}