using RaceFatal.Content;
using RaceFatal.Content.Career;
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
    public class RaceSceneAssembler : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private RaceRuntimeController raceRuntime;
        [SerializeField] private RaceWeaponPresenter weaponPresenter;
        [SerializeField] private RaceStartSequenceController raceStartSequence;

        [Header("Spawn Roots")]
        [SerializeField] private Transform trackRoot;
        [SerializeField] private Transform racerRoot;

        private GameObject spawnedTrack;

        public bool IsBuilt { get; private set; }

        public bool Build(RaceDirector director)
        {
            if (IsBuilt)
            {
                RaceStartupTrace.Warning(
                    "RaceSceneAssembler has already built a race.",
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

            GameContentCatalogSO catalog =
                BootstrapController.ContentCatalog;

            if (catalog == null)
            {
                RaceStartupTrace.Fail(
                    "GameContentCatalog is not available.",
                    this);

                return false;
            }

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
                    $"Track content '{race.TrackId}' was not found.",
                    this);

                return false;
            }

            RaceStartupTrace.Mark(
                $"Track content resolved: '{trackContent.Id}'.");

            if (trackContent.TrackPrefab == null)
            {
                RaceStartupTrace.Fail(
                    $"Track '{race.TrackId}' does not have a Track Prefab assigned.",
                    trackContent);

                return false;
            }

            Transform trackParent =
                trackRoot != null
                    ? trackRoot
                    : transform;

            spawnedTrack =
                Instantiate(
                    trackContent.TrackPrefab,
                    trackParent);

            spawnedTrack.name =
                $"Track_{trackContent.Id}";

            RaceStartupTrace.Mark(
                $"Track prefab instantiated: '{spawnedTrack.name}'.",
                spawnedTrack);

            TrackRuntimeController trackRuntime =
                spawnedTrack.GetComponentInChildren<
                    TrackRuntimeController>(true);

            if (trackRuntime == null)
            {
                RaceStartupTrace.Fail(
                    $"Track prefab '{trackContent.name}' does not contain a " +
                    $"{nameof(TrackRuntimeController)}.",
                    spawnedTrack);

                Destroy(spawnedTrack);
                return false;
            }

            RaceStartupTrace.Mark(
                "TrackRuntimeController found.",
                trackRuntime);

            RaceParticipant player =
                FindPlayer(
                    director);

            if (player == null)
            {
                RaceStartupTrace.Fail(
                    "Race does not contain a player participant.",
                    this);

                Destroy(spawnedTrack);
                return false;
            }

            int participantCount =
                director.State.Participants.Count;

            if (trackRuntime.GridSlotCount <
                participantCount)
            {
                RaceStartupTrace.Fail(
                    $"Track provides only {trackRuntime.GridSlotCount} grid slots, " +
                    $"but race contains {participantCount} participants.",
                    spawnedTrack);

                Destroy(spawnedTrack);
                return false;
            }

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
                    "RaceRuntimeController failed to initialize:\n" +
                    exception,
                    this);

                Destroy(spawnedTrack);
                return false;
            }

            RaceStartupTrace.Mark(
                "RaceRuntimeController initialized.");

            if (!SpawnRacers(
                    director,
                    catalog,
                    trackRuntime))
            {
                RaceStartupTrace.Fail(
                    "One or more racer GameObjects failed to spawn.",
                    this);

                return false;
            }

            RaceStartupTrace.Mark(
                $"All {participantCount} bike GameObjects spawned and placed.");

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
                    "Cannot start race because the scene has not been built.",
                    this);

                return;
            }

            if (raceStartSequence == null)
            {
                RaceStartupTrace.Warning(
                    "No RaceStartSequenceController is assigned. " +
                    "Starting race immediately.",
                    this);

                raceRuntime.StartRace();
                return;
            }

            raceStartSequence.Begin(
                raceRuntime);
        }

        private bool SpawnRacers(
            RaceDirector director,
            GameContentCatalogSO catalog,
            TrackRuntimeController trackRuntime)
        {
            for (int i = 0;
                 i < director.State.Participants.Count;
                 i++)
            {
                RaceParticipant participant =
                    director.State.Participants[i];

                BikeDefinitionSO bikeContent =
                    catalog.FindBikeContent(
                        participant.Bike.BikeDefinitionId);

                if (bikeContent == null)
                {
                    RaceStartupTrace.Fail(
                        $"Bike content '{participant.Bike.BikeDefinitionId}' " +
                        "was not found.",
                        this);

                    return false;
                }

                if (bikeContent.BikePrefab == null)
                {
                    RaceStartupTrace.Fail(
                        $"Bike '{bikeContent.Id}' does not have a prefab assigned.",
                        bikeContent);

                    return false;
                }

                Transform racerParent =
                    racerRoot != null
                        ? racerRoot
                        : transform;

                GameObject bikeObject =
                    Instantiate(
                        bikeContent.BikePrefab,
                        racerParent);

                bikeObject.name =
                    $"Racer_{participant.RacerId}";

                BikeRuntimeController bikeRuntime =
                    bikeObject.GetComponent<
                        BikeRuntimeController>();

                if (bikeRuntime == null)
                {
                    RaceStartupTrace.Fail(
                        $"Bike prefab '{bikeContent.name}' requires a " +
                        $"{nameof(BikeRuntimeController)}.",
                        bikeObject);

                    Destroy(bikeObject);
                    return false;
                }

                bool participantInitialized =
                    bikeRuntime.InitializeParticipant(
                        participant,
                        weaponPresenter);

                if (!participantInitialized)
                {
                    RaceStartupTrace.Fail(
                        $"Bike runtime initialization failed for racer " +
                        $"'{participant.RacerId}'.",
                        bikeObject);

                    Destroy(bikeObject);
                    return false;
                }

                RacerViewController view =
                    bikeRuntime.RacerView;

                if (view == null)
                {
                    RaceStartupTrace.Fail(
                        $"Bike runtime for racer '{participant.RacerId}' " +
                        "did not provide a RacerViewController.",
                        bikeObject);

                    Destroy(bikeObject);
                    return false;
                }

                raceRuntime.RegisterRacerView(
                    view);

                bool placed =
                    raceRuntime.PlaceRacerOnGrid(
                        participant.RacerId,
                        i);

                if (!placed)
                {
                    RaceStartupTrace.Fail(
                        $"Could not place racer '{participant.RacerId}' " +
                        $"in grid slot {i}.",
                        bikeObject);

                    Destroy(bikeObject);
                    return false;
                }

                RacerDefinitionSO racerContent = null;

                if (participant.Role !=
                    RaceParticipantRole.Player)
                {
                    racerContent =
                        catalog.FindRacerContent(
                            participant.RacerId);

                    if (racerContent == null)
                    {
                        RaceStartupTrace.Fail(
                            $"AI racer content '{participant.RacerId}' " +
                            "was not found.",
                            bikeObject);

                        Destroy(bikeObject);
                        return false;
                    }
                }

                bool driverConfigured =
                    bikeRuntime.ConfigureDriver(
                        raceRuntime,
                        trackRuntime,
                        racerContent);

                if (!driverConfigured)
                {
                    RaceStartupTrace.Fail(
                        $"Driver configuration failed for racer " +
                        $"'{participant.RacerId}'.",
                        bikeObject);

                    Destroy(bikeObject);
                    return false;
                }

                if (participant.Role ==
                    RaceParticipantRole.Player)
                {
                    RaceStartupTrace.Mark(
                        $"Racer '{participant.RacerId}' assigned PLAYER control.",
                        bikeObject);
                }
                else
                {
                    RaceStartupTrace.Mark(
                        $"Racer '{participant.RacerId}' assigned AI control. " +
                        $"Pace={racerContent.Pace:F2}, " +
                        $"Aggression={racerContent.Aggression:F2}, " +
                        $"OvertakingSkill={racerContent.OvertakingSkill:F2}, " +
                        $"DefensiveSkill={racerContent.DefensiveSkill:F2}, " +
                        $"WeaponAggression={racerContent.WeaponAggression:F2}.",
                        bikeObject);
                }

                RaceStartupTrace.Mark(
                    $"Grid {i:00}: " +
                    $"Racer='{participant.RacerId}', " +
                    $"Bike='{participant.Bike.BikeDefinitionId}', " +
                    $"Role={participant.Role}.",
                    bikeObject);
            }

            return true;
        }

        private static RaceParticipant FindPlayer(
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