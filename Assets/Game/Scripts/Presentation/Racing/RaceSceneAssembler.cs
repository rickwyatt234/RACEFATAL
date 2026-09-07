using RaceFatal.Content;
using RaceFatal.Content.Tracks;
using RaceFatal.Content.Vehicles;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.Combat;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    public class RaceSceneAssembler :
        MonoBehaviour
    {
        [Header("Runtime")]

        [SerializeField]
        private RaceRuntimeController
            raceRuntime;

        [SerializeField]
        private RaceWeaponPresenter
            weaponPresenter;

        [Header("Spawn Roots")]

        [SerializeField]
        private Transform trackRoot;

        [SerializeField]
        private Transform racerRoot;

        private GameObject spawnedTrack;

        public bool IsBuilt {
            get;
            private set;
        }

        public bool Build(
            RaceDirector director)
        {
            if (IsBuilt)
            {
                RaceStartupTrace.Warning(
                    "RaceSceneAssembler has already " +
                    "built a race.",
                    this);

                return false;
            }

            if (director == null)
            {
                RaceStartupTrace.Fail(
                    "RaceDirector is required.",
                    this);

                return false;
            }

            if (raceRuntime == null)
            {
                RaceStartupTrace.Fail(
                    "RaceRuntimeController is not assigned.",
                    this);

                return false;
            }

            // -------------------------------------------------
            // CONTENT CATALOG
            // -------------------------------------------------

            GameContentCatalogSO catalog =
                BootstrapController
                    .ContentCatalog;

            if (catalog == null)
            {
                RaceStartupTrace.Fail(
                    "GameContentCatalog is not available.",
                    this);

                return false;
            }

            // -------------------------------------------------
            // RACE / TRACK
            // -------------------------------------------------

            RaceDefinition race =
                director.State.RaceDefinition;

            if (race == null)
            {
                RaceStartupTrace.Fail(
                    "RaceState has no RaceDefinition.",
                    this);

                return false;
            }

            TrackDefinitionSO trackContent =
                catalog.FindTrackContent(
                    race.TrackId);

            if (trackContent == null)
            {
                RaceStartupTrace.Fail(
                    $"Track content '{race.TrackId}' " +
                    "was not found.",
                    this);

                return false;
            }

            RaceStartupTrace.Mark(
                $"Track content resolved: " +
                $"'{trackContent.Id}'.");

            if (trackContent.TrackPrefab == null)
            {
                RaceStartupTrace.Fail(
                    $"Track '{race.TrackId}' does not " +
                    "have a Track Prefab assigned.",
                    trackContent);

                return false;
            }

            // -------------------------------------------------
            // SPAWN TRACK
            // -------------------------------------------------

            Transform parent =
                trackRoot != null
                    ? trackRoot
                    : transform;

            spawnedTrack =
                Instantiate(
                    trackContent.TrackPrefab,
                    parent);

            spawnedTrack.name =
                $"Track_{trackContent.Id}";

            RaceStartupTrace.Mark(
                $"Track prefab instantiated: " +
                $"'{spawnedTrack.name}'.",
                spawnedTrack);

            TrackRuntimeController
                trackRuntime =
                    spawnedTrack
                        .GetComponentInChildren<
                            TrackRuntimeController>(
                            true);

            if (trackRuntime == null)
            {
                RaceStartupTrace.Fail(
                    $"Track prefab " +
                    $"'{trackContent.name}' does not " +
                    $"contain a " +
                    $"{nameof(TrackRuntimeController)}.",
                    spawnedTrack);

                Destroy(
                    spawnedTrack);

                return false;
            }

            RaceStartupTrace.Mark(
                "TrackRuntimeController found.",
                trackRuntime);

            // -------------------------------------------------
            // FIND PLAYER
            // -------------------------------------------------

            RaceParticipant player =
                FindPlayer(
                    director);

            if (player == null)
            {
                RaceStartupTrace.Fail(
                    "Race does not contain a " +
                    "player participant.",
                    this);

                Destroy(
                    spawnedTrack);

                return false;
            }

            // -------------------------------------------------
            // GRID VALIDATION
            // -------------------------------------------------

            int participantCount =
                director.State
                    .Participants.Count;

            if (trackRuntime.GridSlotCount <
                participantCount)
            {
                RaceStartupTrace.Fail(
                    $"Track provides only " +
                    $"{trackRuntime.GridSlotCount} " +
                    $"grid slots, but race contains " +
                    $"{participantCount} participants.",
                    spawnedTrack);

                Destroy(
                    spawnedTrack);

                return false;
            }

            // -------------------------------------------------
            // RUNTIME
            // -------------------------------------------------

            try
            {
                raceRuntime.Initialize(
                    director,
                    player.RacerId,
                    trackRuntime);
            }
            catch (System.Exception exception)
            {
                RaceStartupTrace.Fail(
                    "RaceRuntimeController failed " +
                    "to initialize:\n" +
                    exception,
                    this);

                Destroy(
                    spawnedTrack);

                return false;
            }

            RaceStartupTrace.Mark(
                "RaceRuntimeController initialized.");

            // -------------------------------------------------
            // RACERS
            // -------------------------------------------------

            if (!SpawnRacers(
                    director,
                    catalog,
                    trackRuntime))
            {
                RaceStartupTrace.Fail(
                    "One or more racer GameObjects " +
                    "failed to spawn.",
                    this);

                return false;
            }

            RaceStartupTrace.Mark(
                $"All {participantCount} bike " +
                "GameObjects spawned and placed.");

            // -------------------------------------------------
            // WEAPON PRESENTATION
            // -------------------------------------------------

            if (weaponPresenter != null)
            {
                weaponPresenter.Initialize(
                    raceRuntime);

                RaceStartupTrace.Mark(
                    "RaceWeaponPresenter initialized.");
            }
            else
            {
                RaceStartupTrace.Warning(
                    "No RaceWeaponPresenter is assigned.",
                    this);
            }

            IsBuilt = true;

            return true;
        }

        public void StartRace()
        {
            if (!IsBuilt)
            {
                RaceStartupTrace.Warning(
                    "Cannot start race because the " +
                    "scene has not been built.",
                    this);

                return;
            }

            raceRuntime.StartRace();
        }

        private bool SpawnRacers(
            RaceDirector director,
            GameContentCatalogSO catalog,
            TrackRuntimeController trackRuntime)
        {
            for (int i = 0;
                 i < director.State
                     .Participants.Count;
                 i++)
            {
                RaceParticipant participant =
                    director.State
                        .Participants[i];

                BikeDefinitionSO bikeContent =
                    catalog.FindBikeContent(
                        participant.Bike
                            .BikeDefinitionId);

                if (bikeContent == null)
                {
                    RaceStartupTrace.Fail(
                        $"Bike content " +
                        $"'{participant.Bike.BikeDefinitionId}' " +
                        "was not found.",
                        this);

                    return false;
                }

                if (bikeContent.BikePrefab == null)
                {
                    RaceStartupTrace.Fail(
                        $"Bike '{bikeContent.Id}' does " +
                        "not have a prefab assigned.",
                        bikeContent);

                    return false;
                }

                // ---------------------------------------------
                // SPAWN
                // ---------------------------------------------

                Transform parent =
                    racerRoot != null
                        ? racerRoot
                        : transform;

                GameObject bikeObject =
                    Instantiate(
                        bikeContent.BikePrefab,
                        parent);

                bikeObject.name =
                    $"Racer_{participant.RacerId}";

                // ---------------------------------------------
                // VIEW
                // ---------------------------------------------

                RacerViewController view =
                    bikeObject.GetComponent<
                        RacerViewController>();

                if (view == null)
                {
                    RaceStartupTrace.Fail(
                        $"Bike prefab " +
                        $"'{bikeContent.name}' requires " +
                        $"a {nameof(RacerViewController)}.",
                        bikeObject);

                    Destroy(
                        bikeObject);

                    return false;
                }

                view.Initialize(
                    participant);

                // ---------------------------------------------
                // CONTROLLERS
                // ---------------------------------------------

                BikeController playerController =
                    bikeObject.GetComponent<
                        BikeController>();

                AIDriverController aiDriver =
                    bikeObject.GetComponent<
                        AIDriverController>();

                if (playerController == null)
                {
                    RaceStartupTrace.Fail(
                        $"Bike prefab " +
                        $"'{bikeContent.name}' requires " +
                        $"a {nameof(BikeController)}.",
                        bikeObject);

                    Destroy(
                        bikeObject);

                    return false;
                }

                if (aiDriver == null)
                {
                    RaceStartupTrace.Fail(
                        $"Bike prefab " +
                        $"'{bikeContent.name}' requires " +
                        $"an {nameof(AIDriverController)}.",
                        bikeObject);

                    Destroy(
                        bikeObject);

                    return false;
                }

                // ---------------------------------------------
                // REGISTER
                // ---------------------------------------------

                raceRuntime.RegisterRacerView(
                    view);

                // ---------------------------------------------
                // GRID
                // ---------------------------------------------

                bool placed =
                    raceRuntime.PlaceRacerOnGrid(
                        participant.RacerId,
                        i);

                if (!placed)
                {
                    RaceStartupTrace.Fail(
                        $"Could not place racer " +
                        $"'{participant.RacerId}' " +
                        $"in grid slot {i}.",
                        bikeObject);

                    return false;
                }

                // ---------------------------------------------
                // CONTROL OWNERSHIP
                // ---------------------------------------------

                if (participant.Role ==
                    RaceParticipantRole.Player)
                {
                    /*
                     * Only the player-input controller may
                     * write controls to the player's motor.
                     */

                    playerController.enabled =
                        true;

                    aiDriver.enabled =
                        false;

                    RaceStartupTrace.Mark(
                        $"Racer '{participant.RacerId}' " +
                        "assigned PLAYER control.",
                        bikeObject);
                }
                else
                {
                    /*
                     * AI racers must not have BikeController
                     * writing player input into BikeMotor.
                     */

                    playerController.enabled =
                        false;

                    aiDriver.enabled =
                        true;

                    bool aiInitialized =
                        aiDriver.Initialize(
                            participant,
                            raceRuntime,
                            trackRuntime.ProgressPath);

                    if (!aiInitialized)
                    {
                        RaceStartupTrace.Fail(
                            $"AI initialization failed for " +
                            $"racer '{participant.RacerId}'.",
                            bikeObject);

                        return false;
                    }

                    RaceStartupTrace.Mark(
                        $"Racer '{participant.RacerId}' " +
                        "assigned AI control.",
                        bikeObject);
                }

                // ---------------------------------------------
                // DIAGNOSTICS
                // ---------------------------------------------

                RaceStartupTrace.Mark(
                    $"Grid {i:00}: " +
                    $"Racer '{participant.RacerId}', " +
                    $"Bike='{participant.Bike.BikeDefinitionId}', " +
                    $"Role={participant.Role}.",
                    bikeObject);
            }

            return true;
        }

        private static RaceParticipant
            FindPlayer(
                RaceDirector director)
        {
            foreach (RaceParticipant participant
                     in director.State.Participants)
            {
                if (participant.Role ==
                    RaceParticipantRole.Player)
                {
                    return participant;
                }
            }

            return null;
        }
    }
}