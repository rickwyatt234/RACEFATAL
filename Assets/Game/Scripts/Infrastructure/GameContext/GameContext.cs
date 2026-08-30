using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Racing;
using RaceFatal.Vehicles;
using RaceFatal.Infrastructure.Input;

namespace RaceFatal.Infrastructure
{
    public class GameContext
    {
        public CareerManager CareerManager { get; }
        public GameDatabase GameDatabase { get; }
        public VehicleFactory VehicleFactory { get; }
        public EquipmentFactory EquipmentFactory { get; }
        public BikePerformanceCalculator BikePerformanceCalculator { get; }
        public RaceParticipantFactory RaceParticipantFactory { get; }
        public RaceFactory RaceFactory { get; }
        public IRaceInputService InputService { get; }


        public GameContext(
            CareerManager careerManager,
            GameDatabase gameDatabase,
            VehicleFactory vehicleFactory,
            EquipmentFactory equipmentFactory,
            BikePerformanceCalculator bikePerformanceCalculator,
            RaceParticipantFactory raceParticipantFactory,
            RaceFactory raceFactory,
            IRaceInputService inputService)

        {
            CareerManager = careerManager;
            GameDatabase = gameDatabase;
            VehicleFactory = vehicleFactory;
            EquipmentFactory = equipmentFactory;
            BikePerformanceCalculator = bikePerformanceCalculator;
            RaceParticipantFactory = raceParticipantFactory;
            RaceFactory = raceFactory;
            InputService = inputService;
        }
    }
}
