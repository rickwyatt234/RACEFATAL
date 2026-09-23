using System;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public class GameSessionState
    {
        public TeamState PlayerTeam { get; }

        public CareerRun CareerRun {
            get;
            private set;
        }

        public bool HasCareerRun =>
            CareerRun != null;

        public WorldState World { get; }

        public string DefaultPartnerRacerId { get; }

        public string DefaultPlayerBikeId { get; }

        public string DefaultPartnerBikeId { get; }

        public string SelectedPlayerBikeId { get; private set; }
        public string SelectedPartnerBikeId { get; private set; }

        public GameSessionState(
            TeamState playerTeam,
            CareerRun careerRun,
            WorldState world,
            string defaultPartnerRacerId,
            string defaultPlayerBikeId,
            string defaultPartnerBikeId,
            string selectedPlayerBikeId = null,
            string selectedPartnerBikeId = null)
        {
            PlayerTeam =
                playerTeam
                ?? throw new ArgumentNullException(
                    nameof(playerTeam));

            CareerRun =
                careerRun;

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

            SelectedPlayerBikeId =
                string.IsNullOrWhiteSpace(selectedPlayerBikeId)
                    ? defaultPlayerBikeId
                    : selectedPlayerBikeId;

            SelectedPartnerBikeId =
                string.IsNullOrWhiteSpace(selectedPartnerBikeId)
                    ? defaultPartnerBikeId
                    : selectedPartnerBikeId;
        }

        public Result SelectPlayerBike(string bikeId)
        {
            Result validation = ValidateBikeAssignment(bikeId);
            if (!validation.IsSuccess)
                return validation;

            if (bikeId == SelectedPartnerBikeId)
            {
                Result otherValidation =
                    ValidateBikeAssignment(SelectedPlayerBikeId);
                if (!otherValidation.IsSuccess)
                    return Result.Failure(
                        "Cannot swap assignments: " +
                        otherValidation.ErrorMessage);

                SelectedPartnerBikeId = SelectedPlayerBikeId;
            }

            SelectedPlayerBikeId = bikeId;
            return Result.Success();
        }

        public Result SelectPartnerBike(string bikeId)
        {
            Result validation = ValidateBikeAssignment(bikeId);
            if (!validation.IsSuccess)
                return validation;

            if (bikeId == SelectedPlayerBikeId)
            {
                Result otherValidation =
                    ValidateBikeAssignment(SelectedPartnerBikeId);
                if (!otherValidation.IsSuccess)
                    return Result.Failure(
                        "Cannot swap assignments: " +
                        otherValidation.ErrorMessage);

                SelectedPlayerBikeId = SelectedPartnerBikeId;
            }

            SelectedPartnerBikeId = bikeId;
            return Result.Success();
        }

        private Result ValidateBikeAssignment(string bikeId)
        {
            if (string.IsNullOrWhiteSpace(bikeId))
                return Result.Failure("Select an owned bike.");

            var bike = PlayerTeam.Garage.FindBike(bikeId);
            if (bike == null)
                return Result.Failure("Selected bike is not owned by your team.");

            if (bike.IsDestroyed)
                return Result.Failure("A destroyed bike cannot be assigned.");

            return Result.Success();
        }

        internal void SetCareerRun(
            CareerRun careerRun)
        {
            CareerRun =
                careerRun;
        }
    }
}
