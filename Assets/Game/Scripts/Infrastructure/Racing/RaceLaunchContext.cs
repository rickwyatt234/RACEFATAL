using RaceFatal.Racing;

namespace RaceFatal.Infrastructure.Racing
{
    public class RaceLaunchContext
    {
        public RaceDirector PendingRace {
            get;
            private set;
        }

        public bool HasPendingRace =>
            PendingRace != null;

        public void SetPendingRace(
            RaceDirector director)
        {
            PendingRace =
                director;
        }

        public RaceDirector ConsumePendingRace()
        {
            RaceDirector result =
                PendingRace;

            PendingRace = null;

            return result;
        }

        public void Clear()
        {
            PendingRace = null;
        }
    }
}