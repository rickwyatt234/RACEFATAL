using System;
using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Shared;

namespace RaceFatal.Racing
{
    public class RaceFactory
    {
        private readonly RaceGridValidator gridValidator;

        public RaceFactory(
            RaceGridValidator gridValidator)
        {
            this.gridValidator =
                gridValidator;
        }

        public Result<RaceDirector> Create(
            RaceDefinition definition,
            IReadOnlyList<RaceParticipant> participants,
            CareerManager careerManager)
        {
            Result<bool> validationResult = gridValidator.ValidateRaceGrid(definition, new List<RaceParticipant>(participants));
            if (!validationResult.IsSuccess)
            {
                return Result<RaceDirector>.Failure(validationResult.ErrorMessage);
            }

            RaceState state = new RaceState(definition, participants);
            RaceDirector director = new RaceDirector(state, careerManager);

            return Result<RaceDirector>.Success(director);
        }
    }
}