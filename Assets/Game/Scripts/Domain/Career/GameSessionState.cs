using System;

namespace RaceFatal.Career
{
    public class GameSessionState
    {
        public TeamState PlayerTeam { get; }

        public CareerRun CareerRun {
            get;
            private set;
        }

        public WorldState World { get; }

        public string DefaultPartnerRacerId { get; }

        public string DefaultPlayerBikeId { get; }

        public string DefaultPartnerBikeId { get; }

        public GameSessionState(
            TeamState playerTeam,
            CareerRun careerRun,
            WorldState world,
            string defaultPartnerRacerId,
            string defaultPlayerBikeId,
            string defaultPartnerBikeId)
        {
            PlayerTeam =
                playerTeam
                ?? throw new ArgumentNullException(
                    nameof(playerTeam));

            CareerRun =
                careerRun
                ?? throw new ArgumentNullException(
                    nameof(careerRun));

            World =
                world
                ?? throw new ArgumentNullException(
                    nameof(world));

            DefaultPartnerRacerId =
                defaultPartnerRacerId;

            DefaultPlayerBikeId =
                defaultPlayerBikeId;

            DefaultPartnerBikeId =
                defaultPartnerBikeId;
        }

        internal void SetCareerRun(
            CareerRun careerRun)
        {
            CareerRun =
                careerRun;
        }
    }
}