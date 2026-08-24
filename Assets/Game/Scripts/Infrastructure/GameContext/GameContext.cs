using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Racing;
using RaceFatal.Vehicles;

namespace RaceFatal.Infrastructure
{
    public class GameContext
    {
        public CareerManager CareerManager { get; }
        public GameDatabase GameDatabase { get; }
        public VehicleFactory VehicleFactory { get; }
        public EquipmentFactory EquipmentFactory { get; }
        public RaceParticipantFactory RaceParticipantFactory { get; }
        public RaceFactory RaceFactory { get; }
        // public ISaveRepository SaveRepository { get; }
        // public IInputService InputService { get; }
        // public IAudioService AudioService { get; }
        // public SeededRandom Random { get; }

        public GameContext(
            CareerManager careerManager,
            GameDatabase gameDatabase,
            VehicleFactory vehicleFactory,
            EquipmentFactory equipmentFactory,
            RaceParticipantFactory raceParticipantFactory,
            RaceFactory raceFactory)
            // ISaveRepository saveRepository,
            // IInputService inputService,
            // IAudioService audioService,
            // SeededRandom random)
        {
            CareerManager = careerManager;
            GameDatabase = gameDatabase;
            VehicleFactory = vehicleFactory;
            EquipmentFactory = equipmentFactory;
            RaceParticipantFactory = raceParticipantFactory;
            RaceFactory = raceFactory;
            // SaveRepository = saveRepository;
            // InputService = inputService;
            // AudioService = audioService;
            // Random = random;
        }
    }
}
