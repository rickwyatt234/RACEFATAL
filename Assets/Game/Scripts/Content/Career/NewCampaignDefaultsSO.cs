using RaceFatal.Content.Vehicles;
using UnityEngine;

namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(
        fileName = "NewCampaignDefaults",
        menuName = "RaceFatal/Career/New Campaign Defaults")]
    public class NewCampaignDefaultsSO :
        ScriptableObject
    {
        [Header("Starting Roster")]
        [SerializeField]
        private RacerDefinitionSO partner;

        [Header("Starting Bikes")]
        [SerializeField]
        private BikeBuildDefinitionSO
            playerStarterBuild;

        [SerializeField]
        private BikeBuildDefinitionSO
            partnerStarterBuild;

        public RacerDefinitionSO Partner =>
            partner;

        public BikeBuildDefinitionSO
            PlayerStarterBuild =>
                playerStarterBuild;

        public BikeBuildDefinitionSO
            PartnerStarterBuild =>
                partnerStarterBuild;

        public bool TryGetDefinitionIds(
            out string partnerDefinitionId,
            out string playerStarterBuildId,
            out string partnerStarterBuildId,
            out string errorMessage)
        {
            partnerDefinitionId =
                partner != null
                    ? partner.Id
                    : null;

            playerStarterBuildId =
                playerStarterBuild != null
                    ? playerStarterBuild.Id
                    : null;

            partnerStarterBuildId =
                partnerStarterBuild != null
                    ? partnerStarterBuild.Id
                    : null;

            if (partner == null)
            {
                errorMessage =
                    "New campaign defaults have no partner racer assigned.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    partnerDefinitionId))
            {
                errorMessage =
                    "The default partner racer has no definition ID.";

                return false;
            }

            if (playerStarterBuild == null)
            {
                errorMessage =
                    "New campaign defaults have no player starter build assigned.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    playerStarterBuildId))
            {
                errorMessage =
                    "The default player starter build has no definition ID.";

                return false;
            }

            if (partnerStarterBuild == null)
            {
                errorMessage =
                    "New campaign defaults have no partner starter build assigned.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    partnerStarterBuildId))
            {
                errorMessage =
                    "The default partner starter build has no definition ID.";

                return false;
            }

            errorMessage =
                null;

            return true;
        }
    }
}
