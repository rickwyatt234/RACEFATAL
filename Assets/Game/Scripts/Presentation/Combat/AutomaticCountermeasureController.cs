using System;
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
        [Serializable]
        private class CountermeasureModelSettings
        {
            [Tooltip("Must exactly match the installed CountermeasureDefinition ID.")]
            [SerializeField] private string definitionId;

            [Tooltip("Number of automatic deployments available for one race.")]
            [Min(1)][SerializeField] private int startingUses = 4;

            [Tooltip("Nearest missile distance at which this unit automatically deploys.")]
            [Min(0.1f)][SerializeField] private float triggerDistance = 25f;

            [Tooltip("All missiles targeting this racer within this radius are defeated by the deployment.")]
            [Min(0.1f)][SerializeField] private float defeatRadius = 35f;

            [Header("Flare Presentation")]
            [SerializeField] private CountermeasureFlareView flarePrefab;
            [Min(1)][SerializeField] private int flareCount = 2;
            [Min(0f)][SerializeField] private float flareEjectionSpeed = 30f;
            [Range(0f, 60f)][SerializeField] private float horizontalSpread = 15f;
            [Range(-30f, 60f)][SerializeField] private float upwardAngle = 8f;
            [Min(0.1f)][SerializeField] private float flareLifetime = 1.5f;

            public string DefinitionId => definitionId;
            public int StartingUses => startingUses;
            public float TriggerDistance => triggerDistance;
            public float DefeatRadius => defeatRadius;

            public CountermeasureFlareView FlarePrefab => flarePrefab;
            public int FlareCount => flareCount;
            public float FlareEjectionSpeed => flareEjectionSpeed;
            public float HorizontalSpread => horizontalSpread;
            public float UpwardAngle => upwardAngle;
            public float FlareLifetime => flareLifetime;
        }

        [Header("Countermeasure Models")]
        [SerializeField] private List<CountermeasureModelSettings> models =
            new List<CountermeasureModelSettings>();

        [Header("Flare Origins")]
        [Tooltip("Rear-mounted flare origins. Alternated when multiple flares deploy.")]
        [SerializeField] private Transform[] flareOrigins;

        [Header("Optional Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] deploymentClips;
        [Range(0f, 1f)][SerializeField] private float deploymentVolume = 1f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugResolved;
        [SerializeField] private bool debugHasCountermeasure;
        [SerializeField] private string debugDefinitionId = "None";
        [SerializeField] private int debugRemainingUses;
        [SerializeField] private int debugMaximumUses;
        [SerializeField] private int debugThreatCount;
        [SerializeField] private float debugNearestThreatDistance;
        [SerializeField] private int debugLastMissilesCountered;
        [SerializeField] private string debugState = "Resolving";

        private RacerViewController racerView;
        private MissileThreatReceiver threatReceiver;
        private Rigidbody body;

        private RaceParticipant participant;
        private RaceEquipmentSystem equipment;
        private CountermeasureModelSettings activeSettings;

        private string activeDefinitionId;
        private bool resolved;

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
                audioSource.playOnAwake = false;
                audioSource.loop = false;
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
                participant.Status != RaceParticipantStatus.Racing ||
                participant.Vehicle.IsDestroyed)
            {
                debugState = "Inactive";
                return;
            }

            if (activeSettings == null ||
                equipment == null)
            {
                debugState = "No Countermeasure";
                return;
            }

            debugThreatCount =
                threatReceiver.ThreatCount;

            UpdateUsageDebug();

            if (!threatReceiver.TryGetNearestThreat(
                    out GuidedProjectileView nearest,
                    out float nearestDistance))
            {
                debugNearestThreatDistance = 0f;
                debugState = "Armed";
                return;
            }

            debugNearestThreatDistance =
                nearestDistance;

            if (nearestDistance >
                activeSettings.TriggerDistance)
            {
                debugState = "Threat Detected";
                return;
            }

            if (equipment.GetCountermeasureRemainingUses(
                    activeDefinitionId) <= 0)
            {
                debugState = "Depleted";
                return;
            }

            if (!equipment.TryTriggerCountermeasure(
                    activeDefinitionId))
            {
                debugState = "Cooldown";
                return;
            }

            DeployCountermeasure();
        }

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

            resolved = true;
            debugResolved = true;

            if (!equipment.TryGetFirstCountermeasureDefinitionId(
                    out activeDefinitionId))
            {
                debugHasCountermeasure = false;
                debugState = "No Countermeasure";
                return;
            }

            debugHasCountermeasure = true;
            debugDefinitionId = activeDefinitionId;

            activeSettings =
                FindModelSettings(
                    activeDefinitionId);

            if (activeSettings == null)
            {
                debugState = "No Model Settings";

                Debug.LogWarning(
                    $"No automatic countermeasure settings are configured for " +
                    $"definition '{activeDefinitionId}'.",
                    this);

                return;
            }

            equipment.ConfigureCountermeasureUses(
                activeDefinitionId,
                activeSettings.StartingUses);

            UpdateUsageDebug();

            debugState = "Armed";
        }

        private CountermeasureModelSettings FindModelSettings(
            string definitionId)
        {
            for (int i = 0; i < models.Count; i++)
            {
                CountermeasureModelSettings model =
                    models[i];

                if (model == null)
                    continue;

                if (string.Equals(
                        model.DefinitionId,
                        definitionId,
                        StringComparison.Ordinal))
                {
                    return model;
                }
            }

            return null;
        }

        private void DeployCountermeasure()
        {
            SpawnFlares();

            Vector3 escapeUp =
                transform.up;

            List<GuidedProjectileView> threats =
                threatReceiver.GetThreatSnapshot();

            int defeatedCount = 0;

            for (int i = 0; i < threats.Count; i++)
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
                    activeSettings.DefeatRadius)
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

        private void SpawnFlares()
        {
            if (activeSettings.FlarePrefab == null)
                return;

            Vector3 inheritedVelocity =
                body != null
                    ? body.linearVelocity
                    : Vector3.zero;

            for (int i = 0;
                 i < activeSettings.FlareCount;
                 i++)
            {
                Transform origin =
                    ResolveFlareOrigin(i);

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
                        activeSettings.HorizontalSpread *
                        Mathf.Deg2Rad) *
                    UnityEngine.Random.Range(
                        -1f,
                        1f);

                float vertical =
                    Mathf.Tan(
                        activeSettings.UpwardAngle *
                        Mathf.Deg2Rad);

                Vector3 direction =
                    (back +
                     right * horizontal +
                     up * vertical)
                    .normalized;

                CountermeasureFlareView flare =
                    Instantiate(
                        activeSettings.FlarePrefab,
                        position,
                        rotation);

                flare.Initialize(
                    inheritedVelocity,
                    direction,
                    activeSettings.FlareEjectionSpeed,
                    activeSettings.FlareLifetime);
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

        private void PlayDeploymentAudio()
        {
            if (audioSource == null ||
                deploymentClips == null ||
                deploymentClips.Length == 0)
            {
                return;
            }

            AudioClip clip =
                deploymentClips[
                    UnityEngine.Random.Range(
                        0,
                        deploymentClips.Length)];

            if (clip == null)
                return;

            audioSource.PlayOneShot(
                clip,
                deploymentVolume);
        }

        private void UpdateUsageDebug()
        {
            if (equipment == null ||
                string.IsNullOrWhiteSpace(
                    activeDefinitionId))
            {
                return;
            }

            debugRemainingUses =
                equipment.GetCountermeasureRemainingUses(
                    activeDefinitionId);

            debugMaximumUses =
                equipment.GetCountermeasureMaximumUses(
                    activeDefinitionId);
        }
    }
}