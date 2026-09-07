using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure.Input;
using RaceFatal.Infrastructure.Racing;
using RaceFatal.Racing;
using RaceFatal.Vehicles;

namespace RaceFatal.Infrastructure
{
    public sealed class GameContext
    {
        public CareerManager Career { get; }

        public GameDatabase Database { get; }

        public VehicleFactory Vehicles { get; }

        public EquipmentFactory Equipment { get; }

        public BikeBuildFactory BikeBuilds { get; }

        public WorldFactory Worlds { get; }

        public GameSessionManager Sessions { get; }

        public BikePerformanceCalculator
            Performance { get; }

        public RaceParticipantFactory
            Participants { get; }

        public RaceFactory Races { get; }

        public RaceEntryBuilder
            RaceEntries { get; }

        public RacePreparationService
            RacePreparation { get; }

        public RaceLaunchContext
            RaceLaunch { get; }

        public IRaceInputService Input { get; }

        public GameContext(
            CareerManager career,
            GameDatabase database,
            VehicleFactory vehicles,
            EquipmentFactory equipment,
            BikeBuildFactory bikeBuilds,
            WorldFactory worlds,
            GameSessionManager sessions,
            BikePerformanceCalculator performance,
            RaceParticipantFactory participants,
            RaceFactory races,
            RaceEntryBuilder raceEntries,
            RacePreparationService racePreparation,
            RaceLaunchContext raceLaunch,
            IRaceInputService input)
        {
            Career = career;
            Database = database;

            Vehicles = vehicles;
            Equipment = equipment;
            BikeBuilds = bikeBuilds;

            Worlds = worlds;
            Sessions = sessions;

            Performance = performance;
            Participants = participants;

            Races = races;
            RaceEntries = raceEntries;
            RacePreparation = racePreparation;

            RaceLaunch = raceLaunch;

            Input = input;
        }
    }
}