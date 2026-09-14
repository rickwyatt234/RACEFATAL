using RaceFatal.Content.Career;
using RaceFatal.Presentation.Combat;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(RacerViewController))]
    public class BikeRuntimeController : MonoBehaviour
    {
        #region References

        [Header("Core")]
        [SerializeField] private RacerViewController racerView;

        [Header("Driver Roots")]
        [Tooltip("Child object containing player-only racing systems.")]
        [SerializeField] private GameObject playerDriverRoot;

        [Tooltip("Child object containing AI-only racing systems.")]
        [SerializeField] private GameObject aiDriverRoot;

        [Header("Drivers")]
        [SerializeField] private BikeController playerController;
        [SerializeField] private AIDriverController aiDriver;

        [Header("Presentation")]
        [SerializeField] private WeaponChargePresentationController weaponChargePresentation;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugParticipantInitialized;
        [SerializeField] private bool debugDriverConfigured;
        [SerializeField] private bool debugDriverControlEnabled;

        [SerializeField] private string debugRacerId = "None";
        [SerializeField] private string debugRole = "None";
        [SerializeField] private string debugFailure = "None";

        #endregion

        #region Properties

        public RacerViewController RacerView =>
            racerView;

        public RaceParticipant Participant =>
            racerView != null
                ? racerView.Participant
                : null;

        public bool IsParticipantInitialized =>
            racerView != null &&
            racerView.IsInitialized &&
            racerView.Participant != null;

        public bool IsDriverConfigured =>
            debugDriverConfigured;

        public bool DriverControlEnabled
        {
            get
            {
                if (!debugDriverConfigured ||
                    Participant == null)
                {
                    return false;
                }

                if (Participant.Role ==
                    RaceParticipantRole.Player)
                {
                    return
                        playerDriverRoot != null &&
                        playerDriverRoot.activeSelf &&
                        playerController != null &&
                        playerController.enabled;
                }

                return
                    aiDriverRoot != null &&
                    aiDriverRoot.activeSelf &&
                    aiDriver != null &&
                    aiDriver.enabled;
            }
        }

        #endregion

        #region Unity

        private void Awake()
        {
            CacheReferences();
        }

        #endregion

        #region Participant Initialization

        public bool InitializeParticipant(
            RaceParticipant raceParticipant,
            RaceWeaponPresenter weaponPresenter)
        {
            CacheReferences();

            if (raceParticipant == null)
            {
                return Fail(
                    "RaceParticipant is required.");
            }

            if (racerView == null)
            {
                return Fail(
                    "RacerViewController is missing.");
            }

            if (!ValidateDriverHierarchy())
                return false;

            racerView.Initialize(
                raceParticipant);

            if (weaponChargePresentation != null)
            {
                weaponChargePresentation.Initialize(
                    weaponPresenter);
            }

            SetDriverRootsForRole();

            playerController.enabled =
                false;

            aiDriver.enabled =
                false;

            debugParticipantInitialized =
                true;

            debugDriverConfigured =
                false;

            debugDriverControlEnabled =
                false;

            debugRacerId =
                Participant.RacerId;

            debugRole =
                Participant.Role.ToString();

            debugFailure =
                "None";

            return true;
        }

        #endregion

        #region Driver Configuration

        public bool ConfigureDriver(
            RaceRuntimeController raceRuntime,
            TrackRuntimeController trackRuntime,
            RacerDefinitionSO aiDefinition)
        {
            if (!IsParticipantInitialized)
            {
                return Fail(
                    "Participant has not been initialized.");
            }

            if (raceRuntime == null)
            {
                return Fail(
                    "RaceRuntimeController is required.");
            }

            if (trackRuntime == null)
            {
                return Fail(
                    "TrackRuntimeController is required.");
            }

            SetDriverRootsForRole();

            if (Participant.Role ==
                RaceParticipantRole.Player)
            {
                return ConfigurePlayerDriver();
            }

            return ConfigureAIDriver(
                raceRuntime,
                trackRuntime,
                aiDefinition);
        }

        private bool ConfigurePlayerDriver()
        {
            playerDriverRoot.SetActive(
                true);

            aiDriverRoot.SetActive(
                false);

            aiDriver.enabled =
                false;

            playerController.enabled =
                true;

            debugDriverConfigured =
                true;

            debugDriverControlEnabled =
                true;

            debugRole =
                RaceParticipantRole.Player
                    .ToString();

            debugFailure =
                "None";

            return true;
        }

        private bool ConfigureAIDriver(
            RaceRuntimeController raceRuntime,
            TrackRuntimeController trackRuntime,
            RacerDefinitionSO racerDefinition)
        {
            if (racerDefinition == null)
            {
                return Fail(
                    $"AI racer content for " +
                    $"'{Participant.RacerId}' is required.");
            }

            playerDriverRoot.SetActive(
                false);

            aiDriverRoot.SetActive(
                true);

            playerController.enabled =
                false;

            aiDriver.enabled =
                false;

            bool initialized =
                aiDriver.Initialize(
                    Participant,
                    raceRuntime,
                    trackRuntime.ProgressPath,
                    racerDefinition.Pace,
                    racerDefinition.Aggression,
                    racerDefinition.OvertakingSkill,
                    racerDefinition.DefensiveSkill,
                    racerDefinition.WeaponAggression);

            if (!initialized)
            {
                return Fail(
                    $"AI initialization failed for " +
                    $"'{Participant.RacerId}'.");
            }

            aiDriver.enabled =
                true;

            debugDriverConfigured =
                true;

            debugDriverControlEnabled =
                true;

            debugRole =
                Participant.Role.ToString();

            debugFailure =
                "None";

            return true;
        }

        #endregion

        #region Driver Control

        public void SetDriverControlEnabled(
            bool enabled)
        {
            if (!debugDriverConfigured ||
                Participant == null)
            {
                return;
            }

            if (Participant.Role ==
                RaceParticipantRole.Player)
            {
                if (playerController != null &&
                    playerDriverRoot != null &&
                    playerDriverRoot.activeSelf)
                {
                    playerController.enabled =
                        enabled;
                }
            }
            else
            {
                if (aiDriver != null &&
                    aiDriverRoot != null &&
                    aiDriverRoot.activeSelf)
                {
                    aiDriver.enabled =
                        enabled;
                }
            }

            debugDriverControlEnabled =
                DriverControlEnabled;
        }

        private void SetDriverRootsForRole()
        {
            bool isPlayer =
                Participant != null &&
                Participant.Role ==
                    RaceParticipantRole.Player;

            if (playerDriverRoot != null)
            {
                playerDriverRoot.SetActive(
                    isPlayer);
            }

            if (aiDriverRoot != null)
            {
                aiDriverRoot.SetActive(
                    !isPlayer);
            }
        }

        #endregion

        #region Validation

        private bool ValidateDriverHierarchy()
        {
            if (playerDriverRoot == null)
            {
                return Fail(
                    "Player Driver Root is not assigned.");
            }

            if (aiDriverRoot == null)
            {
                return Fail(
                    "AI Driver Root is not assigned.");
            }

            if (playerDriverRoot == gameObject)
            {
                return Fail(
                    "Player Driver Root must be a child object, not the bike root.");
            }

            if (aiDriverRoot == gameObject)
            {
                return Fail(
                    "AI Driver Root must be a child object, not the bike root.");
            }

            if (!playerDriverRoot.transform.IsChildOf(
                    transform))
            {
                return Fail(
                    "Player Driver Root must belong to this bike hierarchy.");
            }

            if (!aiDriverRoot.transform.IsChildOf(
                    transform))
            {
                return Fail(
                    "AI Driver Root must belong to this bike hierarchy.");
            }

            if (playerController == null)
            {
                return Fail(
                    "BikeController is missing from the Player Driver Root.");
            }

            if (aiDriver == null)
            {
                return Fail(
                    "AIDriverController is missing from the AI Driver Root.");
            }

            if (!playerController.transform.IsChildOf(
                    playerDriverRoot.transform))
            {
                return Fail(
                    "BikeController must be located under the Player Driver Root.");
            }

            if (!aiDriver.transform.IsChildOf(
                    aiDriverRoot.transform))
            {
                return Fail(
                    "AIDriverController must be located under the AI Driver Root.");
            }

            return true;
        }

        #endregion

        #region Reference Resolution

        private void CacheReferences()
        {
            if (racerView == null)
            {
                racerView =
                    GetComponent<
                        RacerViewController>();
            }

            if (playerController == null)
            {
                playerController =
                    GetComponentInChildren<
                        BikeController>(true);
            }

            if (aiDriver == null)
            {
                aiDriver =
                    GetComponentInChildren<
                        AIDriverController>(true);
            }

            if (weaponChargePresentation == null)
            {
                weaponChargePresentation =
                    GetComponentInChildren<
                        WeaponChargePresentationController>(
                            true);
            }
        }

        #endregion

        #region Failure

        private bool Fail(
            string message)
        {
            debugFailure =
                message;

            debugDriverConfigured =
                false;

            debugDriverControlEnabled =
                false;

            Debug.LogError(
                $"{nameof(BikeRuntimeController)}: {message}",
                this);

            return false;
        }

        #endregion
    }
}