using System;
using RaceFatal.Racing;

namespace RaceFatal.Career
{
    public class RaceRewardPolicy
    {
        public RaceReward Calculate(
            int position,
            RaceParticipantStatus status)
        {
            RaceReward baseReward =
                GetBaseReward(position);

            if (status != RaceParticipantStatus.Destroyed &&
                status != RaceParticipantStatus.Retired)
            {
                return baseReward;
            }

            /*
             * A DNF still earns something from participation and
             * progress, but substantially less than finishing.
             */
            return new RaceReward(
                Scale(baseReward.Credits, 0.40f),
                Scale(baseReward.TeamFame, 0.50f),
                Scale(baseReward.ResearchPoints, 0.50f),
                Scale(baseReward.CharacterFame, 0.25f));
        }

        private RaceReward GetBaseReward(
            int position)
        {
            if (position <= 1)
            {
                return new RaceReward(
                    10000,
                    100,
                    50,
                    100);
            }

            if (position == 2)
            {
                return new RaceReward(
                    8000,
                    80,
                    40,
                    80);
            }

            if (position == 3)
            {
                return new RaceReward(
                    6500,
                    65,
                    35,
                    65);
            }

            if (position <= 6)
            {
                return new RaceReward(
                    5000,
                    40,
                    25,
                    45);
            }

            if (position <= 9)
            {
                return new RaceReward(
                    3500,
                    20,
                    15,
                    30);
            }

            return new RaceReward(
                2500,
                10,
                10,
                20);
        }

        private int Scale(
            int value,
            float multiplier)
        {
            return (int)Math.Round(
                value * multiplier,
                MidpointRounding.AwayFromZero);
        }
    }
}