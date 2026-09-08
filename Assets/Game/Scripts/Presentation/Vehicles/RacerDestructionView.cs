using System.Collections;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(RacerViewController))]
    [RequireComponent(typeof(Rigidbody))]
    public class RacerDestructionView : MonoBehaviour
    {
        [Header("Visuals")]
        [Tooltip("Point from which the destruction explosion is spawned.")]
        [SerializeField] private Transform explosionOrigin;

        [Tooltip("Explosion particle prefab spawned when the racer is destroyed.")]
        [SerializeField] private ParticleSystem explosionPrefab;

        [Tooltip("The intact bike model. Do not include the player camera or HUD in this object.")]
        [SerializeField] private GameObject visualRoot;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip explosionClip;

        [Header("Systems")]
        [Tooltip("Bike behaviours that should stop when the racer is destroyed.")]
        [SerializeField] private Behaviour[] disableOnDestruction;

        [Tooltip("Bike colliders to disable after destruction. Leave empty to automatically use all child colliders.")]
        [SerializeField] private Collider[] destructionColliders;

        [Header("Destruction Timing")]
        [Tooltip("Delay before the intact bike model disappears.")]
        [Min(0f)][SerializeField] private float visualHideDelay = 0.12f;

        [Tooltip("Delay before the destroyed bike stops interacting physically with other racers.")]
        [Min(0f)][SerializeField] private float colliderDisableDelay = 0.08f;

        [Tooltip("Delay before the bike Rigidbody is completely frozen.")]
        [Min(0f)][SerializeField] private float freezeDelay = 0.3f;

        [Header("Behaviour")]
        [SerializeField] private bool hideVisualRoot = true;
        [SerializeField] private bool disableColliders = true;
        [SerializeField] private bool freezeBody = true;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugDestroyed;
        [SerializeField] private bool debugExplosionPlayed;
        [SerializeField] private bool debugVisualHidden;
        [SerializeField] private bool debugCollidersDisabled;
        [SerializeField] private bool debugBodyFrozen;

        private RacerViewController racerView;
        private Rigidbody body;

        private bool destroyed;

        private void Awake()
        {
            racerView = GetComponent<RacerViewController>();
            body = GetComponent<Rigidbody>();

            if (explosionOrigin == null)
                explosionOrigin = transform;

            if (destructionColliders == null ||
                destructionColliders.Length == 0)
            {
                destructionColliders =
                    GetComponentsInChildren<Collider>(true);
            }
        }

        /// <summary>
        /// Called by RaceRuntimeController when this participant
        /// reaches its destruction state.
        /// </summary>
        public void OnRaceDestroyed()
        {
            if (destroyed)
                return;

            destroyed = true;
            debugDestroyed = true;

            DisableRaceSystems();
            PlayExplosion();

            if (hideVisualRoot)
            {
                StartCoroutine(
                    HideVisualAfterDelay());
            }

            if (disableColliders)
            {
                StartCoroutine(
                    DisableCollidersAfterDelay());
            }

            if (freezeBody)
            {
                StartCoroutine(
                    FreezeBodyAfterDelay());
            }
        }

        private void DisableRaceSystems()
        {
            if (disableOnDestruction == null)
                return;

            for (int i = 0;
                 i < disableOnDestruction.Length;
                 i++)
            {
                Behaviour behaviour =
                    disableOnDestruction[i];

                if (behaviour == null ||
                    behaviour == this)
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }

        private void PlayExplosion()
        {
            Vector3 position =
                explosionOrigin != null
                    ? explosionOrigin.position
                    : transform.position;

            Quaternion rotation =
                explosionOrigin != null
                    ? explosionOrigin.rotation
                    : transform.rotation;

            if (explosionPrefab != null)
            {
                ParticleSystem explosion =
                    Instantiate(
                        explosionPrefab,
                        position,
                        rotation);

                explosion.Play(true);

                debugExplosionPlayed = true;
            }

            if (audioSource != null &&
                explosionClip != null)
            {
                audioSource.PlayOneShot(
                    explosionClip);
            }
        }

        private IEnumerator HideVisualAfterDelay()
        {
            if (visualHideDelay > 0f)
            {
                yield return new WaitForSeconds(
                    visualHideDelay);
            }

            if (visualRoot != null)
                visualRoot.SetActive(false);

            debugVisualHidden = true;
        }

        private IEnumerator DisableCollidersAfterDelay()
        {
            if (colliderDisableDelay > 0f)
            {
                yield return new WaitForSeconds(
                    colliderDisableDelay);
            }

            if (destructionColliders != null)
            {
                for (int i = 0;
                     i < destructionColliders.Length;
                     i++)
                {
                    Collider collider =
                        destructionColliders[i];

                    if (collider != null)
                        collider.enabled = false;
                }
            }

            debugCollidersDisabled = true;
        }

        private IEnumerator FreezeBodyAfterDelay()
        {
            if (freezeDelay > 0f)
            {
                yield return new WaitForSeconds(
                    freezeDelay);
            }

            if (body != null)
            {
                body.linearVelocity =
                    Vector3.zero;

                body.angularVelocity =
                    Vector3.zero;

                body.isKinematic =
                    true;
            }

            debugBodyFrozen = true;
        }
    }
}