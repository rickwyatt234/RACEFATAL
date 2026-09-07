namespace RaceFatal.Career
{
    public class NewGameRequest
    {
        public string TeamName { get; }

        public string PrimaryColor { get; }

        public string SecondaryColor { get; }

        public string PlayerName { get; }

        public string PartnerDefinitionId { get; }

        public string PlayerStarterBuildId { get; }

        public string PartnerStarterBuildId { get; }

        public NewGameRequest(
            string teamName,
            string primaryColor,
            string secondaryColor,
            string playerName,
            string partnerDefinitionId,
            string playerStarterBuildId,
            string partnerStarterBuildId)
        {
            TeamName = teamName;
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;

            PlayerName = playerName;

            PartnerDefinitionId =
                partnerDefinitionId;

            PlayerStarterBuildId =
                playerStarterBuildId;

            PartnerStarterBuildId =
                partnerStarterBuildId;
        }
    }
}