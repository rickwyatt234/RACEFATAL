using UnityEngine;
using RaceFatal.Content;
using RaceFatal.Data;
using RaceFatal.Career;
using RaceFatal.Vehicles;
using RaceFatal.Racing;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure;
using RaceFatal.Infrastructure.Input;

namespace RaceFatal.Presentation.Bootstrap
{
    public class BootstrapController : MonoBehaviour
    {
        public static GameContext GameContext { get; private set; }
        [SerializeField] private GameContentCatalogSO gameContentCatalog;

        private void Awake()
        {
            if (GameContext != null)
            {
                Debug.LogWarning("GameContext already exists. BootstrapController should only be initialized once.");
                return;
            }

            DontDestroyOnLoad(gameObject);

            GameContext = InitializeGameContext();
        }

        private GameContext InitializeGameContext()
        {
            GameDatabase gameDatabase = GameDatabaseFactory.CreateGameDatabase(gameContentCatalog);
            CharacterFactory characterFactory = new CharacterFactory();
            CareerManager careerManager = new CareerManager(characterFactory);
            VehicleFactory vehicleFactory = new VehicleFactory();
            BikePerformanceCalculator bikePerformanceCalculator = new BikePerformanceCalculator(gameDatabase);
            RaceParticipantFactory participantFactory = new RaceParticipantFactory(gameDatabase, bikePerformanceCalculator);
            EquipmentFactory equipmentFactory = new EquipmentFactory();

            RaceEligibilityService eligibilityService = new RaceEligibilityService();
            RaceGridValidator gridValidator = new RaceGridValidator(eligibilityService);
            RaceFactory raceFactory = new RaceFactory(gridValidator);
            UnityRaceInputService inputService = GetComponent<UnityRaceInputService>();
            if (inputService == null)
            {
                inputService = gameObject.AddComponent<UnityRaceInputService>();
            }

            return new GameContext(
                careerManager,
                gameDatabase,
                vehicleFactory,
                equipmentFactory,
                bikePerformanceCalculator,
                participantFactory,
                raceFactory,
                inputService);
        }
    }   
}

