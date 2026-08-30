using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    public class EnergyStripTrigger :
        MonoBehaviour
    {
        [SerializeField]
        private RaceRuntimeController runtime;

        [SerializeField]
        private float rechargeAmount =
            25f;

        private void OnTriggerEnter(
            Collider other)
        {
            RacerViewController racer =
                other.GetComponentInParent<
                    RacerViewController>();

            if (racer == null)
                return;

            runtime.Director.RechargeEnergy(
                racer.RacerId,
                rechargeAmount);
        }
    }
}