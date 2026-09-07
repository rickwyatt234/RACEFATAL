using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(RacerViewController))]
    public class RaceVehicleDebugView : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string racerId;
        [SerializeField] private string role;

        [Header("Energy")]
        [SerializeField] private float currentEnergy;
        [SerializeField] private float maximumEnergy;
        [SerializeField] private float energyPercent;

        [Header("Booster")]
        [SerializeField] private bool hasBooster;
        [SerializeField] private bool boosterActive;
        [SerializeField] private float speedMultiplier;
        [SerializeField] private float accelerationMultiplier;
        [SerializeField] private float handlingMultiplier;

        private RacerViewController racerView;

        private void Awake()
        {
            racerView =
                GetComponent<RacerViewController>();
        }

        private void Update()
        {
            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant?.Vehicle == null)
            {
                return;
            }

            var participant =
                racerView.Participant;

            var vehicle =
                participant.Vehicle;

            var energy =
                vehicle.EnergyPool;

            var equipment =
                vehicle.EquipmentSystem;

            racerId =
                participant.RacerId;

            role =
                participant.Role.ToString();

            currentEnergy =
                energy.CurrentEnergy;

            maximumEnergy =
                energy.MaxEnergy;

            energyPercent =
                maximumEnergy > 0f
                    ? currentEnergy /
                      maximumEnergy
                    : 0f;

            hasBooster =
                equipment.HasBooster;

            boosterActive =
                equipment.IsBoosterActive;

            speedMultiplier =
                equipment.SpeedMultiplier;

            accelerationMultiplier =
                equipment.AccelerationMultiplier;

            handlingMultiplier =
                equipment.HandlingMultiplier;
        }
    }
}