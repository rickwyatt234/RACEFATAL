using System.Collections.Generic;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(RacerViewController))]
    [RequireComponent(typeof(MissileThreatReceiver))]
    public class AutomaticCountermeasureController : MonoBehaviour
    {
        #region Configuration

        [Header("Countermeasure Presentation")]
        [Tooltip("Shared presentation profiles for all countermeasure definitions.")]
        [SerializeField] private CountermeasureRuntimeCatalogSO catalog;

        [Header("Flare Origins")]
        [Tooltip("Bike-specific rear-mounted flare origins. Alternated when multiple flares deploy.")]
        [SerializeField] private Transform[] flareOrigins;

        #endregion

        #region Audio

        [Header("Optional Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] deploymentClips;

        [Range(0f, 1f)]
        [SerializeField] private float deploymentVolume = 1f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugResolved;
        [SerializeField] private bool debugHasCountermeasure;
        [SerializeField] private bool debugProfileResolved;

        [SerializeField] private string debugDefinitionId = "None";
        [SerializeField] private string debugProfile = "None";
        [SerializeField] private string debugCountermeasureType = "None";

        [SerializeField] private float debugCooldown;
        [SerializeField] private float debugTriggerDistance;
        [SerializeField] private float debugDefeatRadius;

        [SerializeField] private int debugRemainingUses;
        [SerializeField] private int debugMaximumUses;

        [SerializeField] private int debugThreatCount;
        [SerializeField] private float debugNearestThreatDistance;
        [SerializeField] private int debugLastMissilesCountered;

        [SerializeField] private string debugState = "Resolving";

        #endregion

        #region Runtime

        private RacerViewController racerView;
        private MissileThreatReceiver threatReceiver;
        private Rigidbody body;

        private RaceParticipant participant;
        private RaceEquipmentSystem equipment;

        private CountermeasureDefinition activeDefinition;
        private CountermeasureRuntimeProfileSO activeProfile;

        private string activeDefinitionId;
        private bool resolved;

        #endregion

        #region Unity

        private void Awake()
        {
            racerView =
                GetComponent<RacerViewController>();

            threatReceiver =
                GetComponent<MissileThreatReceiver>();

            body =
                GetComponent<Rigidbody>();

            if (audioSource != null)
            {
                audioSource.playOnAwake =
                    false;

                audioSource.loop =
                    false;
            }
        }

        private void Update()
        {
            if (!resolved)
            {
                TryResolve();
                return;
            }

            if (participant == null ||
                participant.Vehicle == null ||
                participant.Status !=
                    RaceParticipantStatus.Racing ||
                participant.Vehicle.IsDestroyed)
            {
                debugState =
                    "Inactive";

                return;
            }

            if (activeDefinition == null ||
                activeProfile == null ||
                equipment == null)
            {
                debugState =
                    "No Countermeasure";

                return;
            }

            debugThreatCount =
                threatReceiver.ThreatCount;

            UpdateUsageDebug();

            if (!threatReceiver.TryGetNearestThreat(
                    out GuidedProjectileView nearest,
                    out float nearestDistance))
            {
                debugNearestThreatDistance =
                    0f;

                debugState =
                    "Armed";

                return;
            }

            debugNearestThreatDistance =
                nearestDistance;

            if (nearestDistance >
                activeDefinition.TriggerDistance)
            {
                debugState =
                    "Threat Detected";

                return;
            }

            if (equipment.GetCountermeasureRemainingUses(
                    activeDefinitionId) <= 0)
            {
                debugState =
                    "Depleted";

                return;
            }

            if (!equipment.TryTriggerCountermeasure(
                    activeDefinitionId))
            {
                debugState =
                    "Cooldown";

                return;
            }

            DeployCountermeasure();
        }

        #endregion

        #region Resolution

        private void TryResolve()
        {
            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null)
            {
                return;
            }

            participant =
                racerView.Participant;

            equipment =
                participant.Vehicle?
                    .EquipmentSystem;

            if (equipment == null)
                return;

            resolved =
                true;

            debugResolved =
                true;

            if (!equipment.TryGetFirstCountermeasureDefinition(
                    out activeDefinition))
            {
                debugHasCountermeasure =
                    false;

                debugDefinitionId =
                    "None";

                debugProfileResolved =
                    false;

                debugProfile =
                    "None";

                debugState =
                    "No Countermeasure";

                return;
            }

            activeDefinitionId =
                activeDefinition.Id;

            debugHasCountermeasure =
                true;

            debugDefinitionId =
                activeDefinitionId;

            debugCountermeasureType =
                activeDefinition
                    .CountermeasureType
                    .ToString();

            debugCooldown =
                activeDefinition.Cooldown;

            debugTriggerDistance =
                activeDefinition.TriggerDistance;

            debugDefeatRadius =
                activeDefinition.DefeatRadius;

            if (catalog == null)
            {
                debugProfileResolved =
                    false;

                debugState =
                    "No Presentation Catalog";

                Debug.LogWarning(
                    $"{nameof(AutomaticCountermeasureController)} " +
                    $"on '{name}' has no countermeasure presentation " +
                    "catalog assigned.",
                    this);

                return;
            }

            if (!catalog.TryGetProfile(
                    activeDefinitionId,
                    out activeProfile))
            {
                debugProfileResolved =
                    false;

                debugProfile =
                    "None";

                debugState =
                    "No Presentation Profile";

                Debug.LogWarning(
                    $"No countermeasure presentation profile exists for " +
                    $"definition '{activeDefinitionId}'.",
                    this);

                return;
            }

            debugProfileResolved =
                true;

            debugProfile =
                activeProfile.name;

            UpdateUsageDebug();

            debugState =
                "Armed";
        }

        #endregion

        #region Deployment

        private void DeployCountermeasure()
        {
            SpawnFlares();

            /*
             * Countermeasure escape direction deliberately uses
             * local bike up. This keeps missile defeat correct
             * on walls, loops and inverted track sections.
             */
            Vector3 escapeUp =
                transform.up;

            List<GuidedProjectileView> threats =
                threatReceiver.GetThreatSnapshot();

            int defeatedCount = 0;

            for (int i = 0;
                 i < threats.Count;
                 i++)
            {
                GuidedProjectileView missile =
                    threats[i];

                if (missile == null ||
                    !missile.IsActiveThreat)
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        transform.position,
                        missile.transform.position);

                if (distance >
                    activeDefinition.DefeatRadius)
                {
                    continue;
                }

                missile.DefeatByCountermeasure(
                    escapeUp);

                defeatedCount++;
            }

            PlayDeploymentAudio();

            debugLastMissilesCountered =
                defeatedCount;

            UpdateUsageDebug();

            debugState =
                equipment.GetCountermeasureRemainingUses(
                    activeDefinitionId) > 0
                    ? "Deployed"
                    : "Depleted";
        }

        #endregion

        #region Flares

        private void SpawnFlares()
        {
            if (activeProfile == null ||
                activeProfile.FlarePrefab == null)
            {
                return;
            }

            Vector3 inheritedVelocity =
                body != null
                    ? body.linearVelocity
                    : Vector3.zero;

            for (int i = 0;
                 i < activeProfile.FlareCount;
                 i++)
            {
                Transform origin =
                    ResolveFlareOrigin(
                        i);

                Vector3 position =
                    origin != null
                        ? origin.position
                        : transform.position;

                Quaternion rotation =
                    origin != null
                        ? origin.rotation
                        : transform.rotation;

                Vector3 back =
                    -(origin != null
                        ? origin.forward
                        : transform.forward);

                Vector3 right =
                    origin != null
                        ? origin.right
                        : transform.right;

                Vector3 up =
                    origin != null
                        ? origin.up
                        : transform.up;

                float horizontal =
                    Mathf.Tan(
                        activeProfile.HorizontalSpread *
                        Mathf.Deg2Rad) *
                    Random.Range(
                        -1f,
                        1f);

                float vertical =
                    Mathf.Tan(
                        activeProfile.UpwardAngle *
                        Mathf.Deg2Rad);

                Vector3 direction =
                    (
                        back +
                        right * horizontal +
                        up * vertical
                    ).normalized;

                CountermeasureFlareView flare =
                    Instantiate(
                        activeProfile.FlarePrefab,
                        position,
                        rotation);

                flare.Initialize(
                    inheritedVelocity,
                    direction,
                    activeProfile.FlareEjectionSpeed,
                    activeProfile.FlareLifetime);
            }
        }

        private Transform ResolveFlareOrigin(
            int index)
        {
            if (flareOrigins == null ||
                flareOrigins.Length == 0)
            {
                return null;
            }

            return flareOrigins[
                index %
                flareOrigins.Length];
        }

        #endregion

        #region Audio

        private void PlayDeploymentAudio()
        {
            if (audioSource == null ||
                deploymentClips == null ||
                deploymentClips.Length == 0)
            {
                return;
            }

            int validCount = 0;

            for (int i = 0;
                 i < deploymentClips.Length;
                 i++)
            {
                if (deploymentClips[i] != null)
                    validCount++;
            }

            if (validCount == 0)
                return;

            int target =
                Random.Range(
                    0,
                    validCount);

            int current = 0;

            AudioClip selectedClip =
                null;

            for (int i = 0;
                 i < deploymentClips.Length;
                 i++)
            {
                AudioClip clip =
                    deploymentClips[i];

                if (clip == null)
                    continue;

                if (current == target)
                {
                    selectedClip =
                        clip;

                    break;
                }

                current++;
            }

            if (selectedClip == null)
                return;

            audioSource.PlayOneShot(
                selectedClip,
                deploymentVolume);
        }

        #endregion

        #region Debug

        private void UpdateUsageDebug()
        {
            if (equipment == null ||
                string.IsNullOrWhiteSpace(
                    activeDefinitionId))
            {
                debugRemainingUses =
                    0;

                debugMaximumUses =
                    0;

                return;
            }

            debugRemainingUses =
                equipment.GetCountermeasureRemainingUses(
                    activeDefinitionId);

            debugMaximumUses =
                equipment.GetCountermeasureMaximumUses(
                    activeDefinitionId);
        }

        #endregion
    }
}