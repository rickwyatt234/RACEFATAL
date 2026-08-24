using UnityEngine;
using RaceFatal.Content;
using RaceFatal.Data;
using RaceFatal.Career;
using RaceFatal.Vehicles;
using RaceFatal.Racing;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure;

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
            RaceParticipantFactory participantFactory = new RaceParticipantFactory(gameDatabase);
            EquipmentFactory equipmentFactory = new EquipmentFactory();

            RaceEligibilityService eligibilityService = new RaceEligibilityService();
            RaceGridValidator gridValidator = new RaceGridValidator(eligibilityService);
            RaceFactory raceFactory = new RaceFactory(gridValidator);

            return new GameContext(
                careerManager,
                gameDatabase,
                vehicleFactory,
                equipmentFactory,
                participantFactory,
                raceFactory);
        }
    }   
}

