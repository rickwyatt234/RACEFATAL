using System.IO;
using RaceFatal.Career;
using RaceFatal.Content;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure;
using RaceFatal.Infrastructure.Input;
using RaceFatal.Infrastructure.Racing;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using RaceFatal.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaceFatal.Presentation.Bootstrap
{
    public class BootstrapController :
        MonoBehaviour
    {
        [Header("Content")]

        [SerializeField]
        private GameContentCatalogSO
            contentCatalog;

        [Header("Startup")]

        [SerializeField]
        private BootstrapStartupMode startupMode =
            BootstrapStartupMode.FrontEnd;

        [SerializeField]
        private string frontEndSceneName =
            "03_MainMenu";

        public static GameContext Context {
            get;
            private set;
        }

        public static GameContentCatalogSO
            ContentCatalog {
            get;
            private set;
        }

        private void Awake()
        {
            RaceStartupTrace.Reset();

            RaceStartupTrace.Mark(
                "BootstrapController.Awake()",
                this);

            if (Context != null)
            {
                RaceStartupTrace.Warning(
                    "A GameContext already exists. " +
                    "Destroying duplicate Bootstrap.",
                    this);

                Destroy(gameObject);

                return;
            }

            if (contentCatalog == null)
            {
                RaceStartupTrace.Fail(
                    "BootstrapController has no " +
                    "GameContentCatalog assigned.",
                    this);

                return;
            }

            PrototypeRaceLauncher prototypeLauncher =
                GetComponent<PrototypeRaceLauncher>();

            if (prototypeLauncher != null)
            {
                prototypeLauncher.enabled =
                    false;
            }

            DontDestroyOnLoad(
                gameObject);

            ContentCatalog =
                contentCatalog;

            RaceStartupTrace.Mark(
                "Creating GameContext...",
                this);

            Context =
                InitializeGameContext();

            if (Context == null)
            {
                RaceStartupTrace.Fail(
                    "InitializeGameContext() returned null.",
                    this);

                return;
            }

            RaceStartupTrace.Mark(
                "GameContext successfully created.",
                this);
        }

        private void Start()
        {
            if (Context == null)
                return;

            switch (startupMode)
            {
                case BootstrapStartupMode.FrontEnd:
                    LaunchFrontEnd();
                    break;

                case BootstrapStartupMode.PrototypeRace:
                    LaunchPrototypeRace();
                    break;

                case BootstrapStartupMode.StayInBootstrap:
                    RaceStartupTrace.Mark(
                        "Bootstrap startup mode is StayInBootstrap.",
                        this);
                    break;
            }
        }

        private void LaunchFrontEnd()
        {
            if (string.IsNullOrWhiteSpace(
                    frontEndSceneName))
            {
                RaceStartupTrace.Fail(
                    "Front-end scene name is empty.",
                    this);

                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(
                    frontEndSceneName))
            {
                RaceStartupTrace.Fail(
                    $"Front-end scene '{frontEndSceneName}' " +
                    "cannot be loaded. Verify that it is included " +
                    "in Build Profiles / Build Settings.",
                    this);

                return;
            }

            RaceStartupTrace.Mark(
                $"Loading front-end scene '{frontEndSceneName}'.",
                this);

            SceneManager.LoadScene(
                frontEndSceneName);
        }

        private void LaunchPrototypeRace()
        {
            PrototypeRaceLauncher launcher =
                GetComponent<PrototypeRaceLauncher>();

            if (launcher == null)
            {
                RaceStartupTrace.Fail(
                    "Bootstrap startup mode is PrototypeRace, " +
                    "but no PrototypeRaceLauncher is attached.",
                    this);

                return;
            }

            launcher.Launch();
        }

        private GameContext
            InitializeGameContext()
        {
            // -------------------------------------------------
            // DATABASE
            // -------------------------------------------------

            GameDatabase database =
                GameDatabaseFactory
                    .CreateGameDatabase(
                        contentCatalog);

            RaceStartupTrace.Mark(
                "GameDatabase created.",
                this);

            // -------------------------------------------------
            // CAREER
            // -------------------------------------------------

            CharacterFactory
                characterFactory =
                    new CharacterFactory();

            CareerManager careerManager =
                new CareerManager(
                    characterFactory);

            // -------------------------------------------------
            // VEHICLES / EQUIPMENT
            // -------------------------------------------------

            VehicleFactory vehicleFactory =
                new VehicleFactory();

            EquipmentFactory
                equipmentFactory =
                    new EquipmentFactory();

            BikeBuildFactory
                bikeBuildFactory =
                    new BikeBuildFactory(
                        database,
                        vehicleFactory,
                        equipmentFactory);

            WorldFactory worldFactory =
                new WorldFactory(
                    database,
                    bikeBuildFactory);

            // -------------------------------------------------
            // PERFORMANCE
            // -------------------------------------------------

            BikePerformanceCalculator
                performanceCalculator =
                    new BikePerformanceCalculator(
                        database);

            // -------------------------------------------------
            // RACING
            // -------------------------------------------------

            RaceParticipantFactory
                participantFactory =
                    new RaceParticipantFactory(
                        database,
                        performanceCalculator);

            RaceEligibilityService
                eligibilityService =
                    new RaceEligibilityService();

            RaceGridValidator
                gridValidator =
                    new RaceGridValidator(
                        eligibilityService);

            RaceFactory raceFactory =
                new RaceFactory(
                    gridValidator);

            RaceEntryBuilder
                raceEntryBuilder =
                    new RaceEntryBuilder(
                        participantFactory,
                        raceFactory,
                        careerManager);

            // -------------------------------------------------
            // GAME SESSION
            // -------------------------------------------------

            GameSessionManager
                sessionManager =
                    new GameSessionManager(
                        database,
                        careerManager,
                        worldFactory,
                        bikeBuildFactory);

            // -------------------------------------------------
            // SAVING
            // -------------------------------------------------

            string saveRoot =
                Path.Combine(
                    Application.persistentDataPath,
                    "RACEFATAL",
                    "Saves");

            ICampaignSaveRepository
                saveRepository =
                    new JsonCampaignSaveRepository(
                        saveRoot,
                        3);

            CampaignSaveMapper saveMapper =
                new CampaignSaveMapper(
                    database);

            CampaignSaveService saveService =
                new CampaignSaveService(
                    sessionManager,
                    saveMapper,
                    saveRepository);

            RacePreparationService
                racePreparation =
                    new RacePreparationService(
                        database,
                        sessionManager,
                        raceEntryBuilder);

            RaceLaunchContext
                raceLaunch =
                    new RaceLaunchContext();

            // -------------------------------------------------
            // INPUT
            // -------------------------------------------------

            UnityRaceInputService input =
                GetComponent<
                    UnityRaceInputService>();

            if (input == null)
            {
                input =
                    gameObject.AddComponent<
                        UnityRaceInputService>();
            }

            // -------------------------------------------------
            // COMPLETE CONTEXT
            // -------------------------------------------------

            RaceStartupTrace.Mark(
                "Core services created.",
                this);

            return new GameContext(
                careerManager,
                database,
                vehicleFactory,
                equipmentFactory,
                bikeBuildFactory,
                worldFactory,
                sessionManager,
                saveService,
                performanceCalculator,
                participantFactory,
                raceFactory,
                raceEntryBuilder,
                racePreparation,
                raceLaunch,
                input);
        }
    }
}
