using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(RacerViewController))]
    public class WeaponChargePresentationController : MonoBehaviour
    {
        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private bool debugResolved;
        [SerializeField] private bool debugCharging;
        [SerializeField] private string debugWeapon = "None";
        [SerializeField] private string debugEquipment = "None";
        [SerializeField] private float debugChargeRatio;
        [SerializeField] private string debugFailure = "None";

        private RacerViewController racerView;
        private RaceEquipmentSystem equipment;
        private RaceWeaponPresenter weaponPresenter;

        private WeaponMountFeedbackView activeFeedback;
        private WeaponPresentationProfile activeProfile;
        private string activeEquipmentId;

        private void Awake()
        {
            racerView =
                GetComponent<RacerViewController>();
        }

        public void Initialize(
            RaceWeaponPresenter presenter)
        {
            weaponPresenter =
                presenter;

            debugInitialized =
                weaponPresenter != null;

            debugFailure =
                weaponPresenter != null
                    ? "None"
                    : "No RaceWeaponPresenter";
        }

        private void Update()
        {
            if (!TryResolveRuntime())
            {
                CancelPresentation();
                return;
            }

            if (weaponPresenter == null)
            {
                debugFailure =
                    "No RaceWeaponPresenter";

                CancelPresentation();
                return;
            }

            if (racerView.Participant.Status !=
                    RaceParticipantStatus.Racing ||
                racerView.Participant.Vehicle == null ||
                racerView.Participant.Vehicle.IsDestroyed)
            {
                debugFailure =
                    "Racer Inactive";

                CancelPresentation();
                return;
            }

            WeaponDefinition weapon =
                equipment.SelectedWeaponDefinition;

            if (weapon == null ||
                weapon.ActivationMode !=
                    EquipmentActivationMode.ChargeRelease ||
                !equipment.SelectedWeaponIsCharging)
            {
                debugFailure =
                    "None";

                CancelPresentation();
                return;
            }

            string equipmentId =
                equipment.SelectedEquipmentId;

            if (string.IsNullOrWhiteSpace(
                    equipmentId))
            {
                debugFailure =
                    "No Equipment ID";

                CancelPresentation();
                return;
            }

            if (!racerView.TryGetEquipmentMount(
                    equipmentId,
                    out BikeEquipmentMountBinding mount))
            {
                debugFailure =
                    "No Physical Mount";

                CancelPresentation();
                return;
            }

            if (!weaponPresenter.TryGetPresentationProfile(
                    weapon.Id,
                    out WeaponPresentationProfile profile))
            {
                debugFailure =
                    $"No Profile: {weapon.Id}";

                CancelPresentation();
                return;
            }

            WeaponMountFeedbackView feedback =
                mount.GetComponentInChildren<
                    WeaponMountFeedbackView>(true);

            if (feedback == null)
            {
                debugFailure =
                    "No Mount Feedback";

                CancelPresentation();
                return;
            }

            Transform origin =
                mount.EquipmentOrigin != null
                    ? mount.EquipmentOrigin
                    : mount.MountRoot;

            if (origin == null)
            {
                debugFailure =
                    "No Mount Origin";

                CancelPresentation();
                return;
            }

            bool presentationChanged =
                activeFeedback != feedback ||
                activeProfile != profile ||
                activeEquipmentId !=
                    equipmentId;

            if (presentationChanged)
            {
                CancelPresentation();

                activeFeedback =
                    feedback;

                activeProfile =
                    profile;

                activeEquipmentId =
                    equipmentId;

                activeFeedback.BeginCharge(
                    activeProfile,
                    origin);
            }

            float chargeRatio =
                equipment
                    .SelectedWeaponChargeRatio;

            activeFeedback.UpdateCharge(
                activeProfile,
                origin,
                chargeRatio);

            debugCharging = true;
            debugWeapon = weapon.DisplayName;
            debugEquipment = equipmentId;
            debugChargeRatio = chargeRatio;
            debugFailure = "None";
        }

        private bool TryResolveRuntime()
        {
            if (racerView == null)
            {
                racerView =
                    GetComponent<RacerViewController>();
            }

            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null ||
                racerView.Participant.Vehicle == null)
            {
                debugResolved = false;
                debugFailure =
                    "Racer Not Initialized";

                return false;
            }

            equipment =
                racerView.Participant
                    .Vehicle
                    .EquipmentSystem;

            if (equipment == null)
            {
                debugResolved = false;
                debugFailure =
                    "No Equipment System";

                return false;
            }

            debugResolved = true;
            return true;
        }

        private void CancelPresentation()
        {
            if (activeFeedback != null)
            {
                activeFeedback.CancelCharge();
            }

            activeFeedback = null;
            activeProfile = null;
            activeEquipmentId = null;

            debugCharging = false;
            debugWeapon = "None";
            debugEquipment = "None";
            debugChargeRatio = 0f;
        }

        private void OnDisable()
        {
            CancelPresentation();
        }

        private void OnDestroy()
        {
            CancelPresentation();
        }
    }
}