using RaceFatal.Combat;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(Collider))]
    public class ProjectileView :
        MonoBehaviour
    {
        [SerializeField]
        private float maximumLifetime =
            10f;

        private RaceRuntimeController runtime;

        private string attackerRacerId;

        private float damage;

        private float speed;

        private float remainingRange;

        private Vector3 previousPosition;

        private float lifetime;

        private bool initialized;

        public void Initialize(
            RaceRuntimeController raceRuntime,
            string attackerId,
            float projectileDamage,
            float projectileSpeed,
            float maximumRange)
        {
            runtime = raceRuntime;

            attackerRacerId =
                attackerId;

            damage =
                projectileDamage;

            speed =
                projectileSpeed;

            remainingRange =
                maximumRange;

            previousPosition =
                transform.position;

            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
                return;

            float movement =
                speed * Time.deltaTime;

            transform.position +=
                transform.forward *
                movement;

            remainingRange -= movement;

            lifetime +=
                Time.deltaTime;

            if (remainingRange <= 0f ||
                lifetime >= maximumLifetime)
            {
                Destroy(gameObject);
            }

            previousPosition =
                transform.position;
        }

        private void OnTriggerEnter(
            Collider other)
        {
            if (!initialized)
                return;

            RacerViewController victim =
                other.GetComponentInParent<
                    RacerViewController>();

            if (victim == null)
                return;

            if (victim.RacerId ==
                attackerRacerId)
            {
                return;
            }

            runtime.Director.ApplyDamage(
                attackerRacerId,
                victim.RacerId,
                damage,
                DamageCause.Weapon);

            Destroy(gameObject);
        }
    }
}