using RaceFatal.Combat;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(RacerViewController))]
    public sealed class RacerCollisionDamage :
        MonoBehaviour
    {
        [SerializeField]
        private RaceRuntimeController runtime;

        [SerializeField]
        private float minimumImpactSpeed =
            5f;

        [SerializeField]
        private float damageMultiplier =
            0.5f;

        private RacerViewController racer;

        private void Awake()
        {
            racer =
                GetComponent<
                    RacerViewController>();
        }

        private void OnCollisionEnter(
            Collision collision)
        {
            RacerViewController other =
                collision.collider
                    .GetComponentInParent<
                        RacerViewController>();

            if (other == null)
                return;

            if (other.RacerId ==
                racer.RacerId)
            {
                return;
            }

            float impact =
                collision.relativeVelocity
                    .magnitude;

            if (impact <
                minimumImpactSpeed)
            {
                return;
            }

            float damage =
                (impact -
                 minimumImpactSpeed) *
                damageMultiplier;

            runtime.Director.ApplyDamage(
                racer.RacerId,
                other.RacerId,
                damage,
                DamageCause.Collision);
        }
    }
}