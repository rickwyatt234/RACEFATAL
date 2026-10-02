namespace RaceFatal.Presentation.FrontEnd
{
    public class NewCampaignDraft
    {
        public RaceFatal.Career.GameSessionState PreparedSession { get; set; }
        public int SlotIndex { get; }
        public string TeamName { get; private set; }
        public string PrimaryColor { get; private set; }
        public string SecondaryColor { get; private set; }
        public string PlayerName { get; private set; }
        public bool HasTeamData => !string.IsNullOrWhiteSpace(TeamName) && !string.IsNullOrWhiteSpace(PrimaryColor) && !string.IsNullOrWhiteSpace(SecondaryColor);
        public bool HasRacerData => !string.IsNullOrWhiteSpace(PlayerName);
        public bool IsComplete => HasTeamData && HasRacerData;

        public NewCampaignDraft(int slotIndex, string defaultPrimaryColor = "#FFFFFF", string defaultSecondaryColor = "#202020")
        {
            SlotIndex = slotIndex;
            PrimaryColor = defaultPrimaryColor;
            SecondaryColor = defaultSecondaryColor;
        }

        public void SetTeam(string teamName, string primaryColor, string secondaryColor)
        {
            if (TeamName != teamName?.Trim() || PrimaryColor != primaryColor?.Trim() || SecondaryColor != secondaryColor?.Trim()) PreparedSession = null;
            TeamName = teamName?.Trim();
            PrimaryColor = primaryColor?.Trim();
            SecondaryColor = secondaryColor?.Trim();
        }

        public void SetPlayer(string playerName)
        {
            if (PlayerName != playerName?.Trim()) PreparedSession = null;
            PlayerName = playerName?.Trim();
        }
    }
}
