using UnityEngine;
using TMPro;

namespace RaceFatal.Presentation.Racing
{
    public class RaceStandingsRowView : MonoBehaviour
    {
        [Header("Columns")] [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI racerNameText;
        [SerializeField] private TextMeshProUGUI teamNameText;
        [SerializeField] private TextMeshProUGUI raceTimeText;
        public void Render(int position, string racerName, string teamName, string raceTime, bool survival = false)
        {
            gameObject.SetActive(true);
            if (positionText != null)
                positionText.text = position.ToString("00");
            if (racerNameText != null)
            {
                racerNameText.text = string.IsNullOrWhiteSpace(racerName) ? "UNKNOWN" : racerName.ToUpperInvariant();
            }

            if (teamNameText != null)
            {
                teamNameText.text = string.IsNullOrWhiteSpace(teamName) ? "UNKNOWN" : teamName.ToUpperInvariant();
            }

            if (raceTimeText != null)
            {
                raceTimeText.text = raceTime;
            }
        }

        [SerializeField] private bool layoutAuthored;
#if UNITY_EDITOR
        public void EditorAuthor()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Editor only");
            if (layoutAuthored) return;
            if (raceTimeText != null) { raceTimeText.enableAutoSizing=true; raceTimeText.fontSizeMin=10; raceTimeText.fontSizeMax=24; }
            layoutAuthored=true;
        }
#endif
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
