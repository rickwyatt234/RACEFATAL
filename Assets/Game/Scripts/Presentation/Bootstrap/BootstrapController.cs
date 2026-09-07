using RaceFatal.Career;
using RaceFatal.Content;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure;
using RaceFatal.Infrastructure.Input;
using RaceFatal.Infrastructure.Racing;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using RaceFatal.Vehicles;
using UnityEngine;

namespace RaceFatal.Presentation.Bootstrap
{
    public class BootstrapController :
        MonoBehaviour
    {
        [Header("Content")]

        [SerializeField]
        private GameContentCatalogSO
            contentCatalog;

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