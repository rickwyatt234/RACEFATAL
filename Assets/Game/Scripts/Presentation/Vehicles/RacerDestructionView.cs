using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;
using UnityEngine.Audio;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(RacerViewController))]
    [RequireComponent(typeof(Rigidbody))]
    public class RacerDestructionView : MonoBehaviour
    {
        #region Primary Explosion

        [Header("Primary Explosion")]
        [Tooltip("Point from which destruction effects are spawned.")]
        [SerializeField] private Transform explosionOrigin;
        [SerializeField] private ParticleSystem explosionPrefab;

        #endregion

        #region Secondary Explosion

        [Header("Secondary Explosion")]
        [SerializeField] private ParticleSystem secondaryExplosionPrefab;
        [Min(0f)][SerializeField] private float secondaryExplosionDelay = 0.15f;

        #endregion

        #region Residual Effect

        [Header("Residual Fire / Smoke")]
        [SerializeField] private ParticleSystem residualEffectPrefab;
        [Min(0f)][SerializeField] private float residualEffectDelay = 0.1f;
        [Min(0.1f)][SerializeField] private float residualEffectLifetime = 6f;

        #endregion

        #region Audio

        [Header("Explosion Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] explosionClips;
        [SerializeField] private AudioClip[] secondaryExplosionClips;

        [Range(0f, 1f)][SerializeField] private float explosionVolume = 1f;
        [Range(0f, 0.25f)][SerializeField] private float pitchVariation = 0.04f;

        [Header("Audio Mixer Routing")]
        [SerializeField] private AudioMixerGroup explosionGroup;

        [Header("Player / Opponent Mix")]
        [Range(0f, 2f)][SerializeField] private float playerVolumeMultiplier = 1f;
        [Range(0f, 2f)][SerializeField] private float opponentVolumeMultiplier = 0.7f;
        [Range(0f, 1f)][SerializeField] private float playerSpatialBlend = 0.2f;
        [Range(0f, 1f)][SerializeField] private float opponentSpatialBlend = 1f;

        #endregion

        #region Debris

        [Header("Optional Debris")]
        [SerializeField] private bool spawnDebris = true;
        [SerializeField] private GameObject[] debrisPrefabs;

        [Min(0)][SerializeField] private int debrisCount = 3;
        [Min(0f)][SerializeField] private float debrisMinimumSpeed = 3f;
        [Min(0f)][SerializeField] private float debrisMaximumSpeed = 10f;
        [Min(0f)][SerializeField] private float debrisTorque = 8f;
        [Min(0.1f)][SerializeField] private float debrisLifetime = 4f;

        #endregion

        #region Preservation

        [Header("Additional Preserve On Destruction")]
        [Tooltip("Optional additional child objects that should survive destruction. The player camera is handled automatically.")]
        [SerializeField] private Transform[] preserveOnDestruction;

        [SerializeField] private bool preserveOnlyForPlayer = true;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugDestroyed;
        [SerializeField] private bool debugIsPlayer;
        [SerializeField] private bool debugPlayerCameraPrepared;
        [SerializeField] private int debugPreservedObjectCount;
        [SerializeField] private string debugMixerGroup = "Unassigned";

        #endregion

        #region Runtime

        private RacerViewController racerView;
        private PlayerCockpitView cockpitView;
        private Rigidbody body;

        private bool destroyed;
        private bool isPlayer;

        private int lastExplosionClip = -1;
        private int lastSecondaryClip = -1;

        #endregion

        #region Unity

        private void Awake()
        {
            racerView =
                GetComponent<RacerViewController>();

            cockpitView =
                GetComponentInChildren<
                    PlayerCockpitView>(true);

            body =
                GetComponent<Rigidbody>();

            if (explosionOrigin == null)
                explosionOrigin = transform;

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;

                if (explosionGroup != null)
                {
                    audioSource.outputAudioMixerGroup =
                        explosionGroup;

                    debugMixerGroup =
                        explosionGroup.name;
                }
            }
        }

        #endregion

        #region Destruction

        public void OnRaceDestroyed()
        {
            if (destroyed)
                return;

            destroyed = true;
            debugDestroyed = true;

            ResolveRole();

            Vector3 position =
                GetExplosionPosition();

            Quaternion rotation =
                GetExplosionRotation();

            Vector3 inheritedVelocity =
                body != null
                    ? body.linearVelocity
                    : Vector3.zero;

            Vector3 localUp =
                explosionOrigin != null
                    ? explosionOrigin.up
                    : transform.up;

            PreparePlayerCamera(
                position);

            PreserveAdditionalObjects();

            SpawnParticleEffect(
                explosionPrefab,
                position,
                rotation,
                0f);

            PlayRandomClipDetached(
                explosionClips,
                ref lastExplosionClip,
                explosionVolume,
                position,
                0f);

            SpawnParticleEffect(
                secondaryExplosionPrefab,
                position,
                rotation,
                secondaryExplosionDelay);

            PlayRandomClipDetached(
                secondaryExplosionClips,
                ref lastSecondaryClip,
                explosionVolume * 0.8f,
                position,
                secondaryExplosionDelay);

            SpawnParticleEffect(
                residualEffectPrefab,
                position,
                rotation,
                residualEffectDelay,
                residualEffectLifetime);

            if (spawnDebris)
            {
                SpawnDebris(
                    position,
                    localUp,
                    inheritedVelocity);
            }

            Destroy(
                gameObject);
        }

        private void ResolveRole()
        {
            isPlayer =
                racerView != null &&
                racerView.IsInitialized &&
                racerView.Participant != null &&
                racerView.Participant.Role ==
                RaceParticipantRole.Player;

            debugIsPlayer =
                isPlayer;
        }

        private void PreparePlayerCamera(
            Vector3 destructionPosition)
        {
            if (!isPlayer ||
                cockpitView == null)
            {
                return;
            }

            cockpitView.PrepareForDestruction(
                destructionPosition);

            debugPlayerCameraPrepared =
                true;
        }

        #endregion

        #region Preservation

        private void PreserveAdditionalObjects()
        {
            debugPreservedObjectCount = 0;

            if (preserveOnDestruction == null ||
                preserveOnDestruction.Length == 0)
            {
                return;
            }

            if (preserveOnlyForPlayer &&
                !isPlayer)
            {
                return;
            }

            for (int i = 0;
                 i < preserveOnDestruction.Length;
                 i++)
            {
                Transform target =
                    preserveOnDestruction[i];

                if (target == null ||
                    target == transform)
                {
                    continue;
                }

                if (!target.IsChildOf(transform))
                    continue;

                if (HasPreservedAncestor(
                        target,
                        i))
                {
                    continue;
                }

                target.SetParent(
                    null,
                    true);

                debugPreservedObjectCount++;
            }
        }

        private bool HasPreservedAncestor(
            Transform target,
            int targetIndex)
        {
            for (int i = 0;
                 i < preserveOnDestruction.Length;
                 i++)
            {
                if (i == targetIndex)
                    continue;

                Transform possibleAncestor =
                    preserveOnDestruction[i];

                if (possibleAncestor == null ||
                    possibleAncestor == target)
                {
                    continue;
                }

                if (target.IsChildOf(
                        possibleAncestor))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Particles

        private void SpawnParticleEffect(
            ParticleSystem prefab,
            Vector3 position,
            Quaternion rotation,
            float additionalDelay,
            float forcedLifetime = 0f)
        {
            if (prefab == null)
                return;

            ParticleSystem effect =
                Instantiate(
                    prefab,
                    position,
                    rotation);

            ParticleSystem[] systems =
                effect.GetComponentsInChildren<
                    ParticleSystem>(true);

            for (int i = 0;
                 i < systems.Length;
                 i++)
            {
                ParticleSystem system =
                    systems[i];

                if (system == null)
                    continue;

                system.Stop(
                    false,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear);

                system.Clear(false);

                ParticleSystem.MainModule main =
                    system.main;

                float existingDelay =
                    main.startDelay.constantMax;

                main.startDelay =
                    existingDelay +
                    additionalDelay;
            }

            effect.Play(true);

            float cleanupTime =
                forcedLifetime > 0f
                    ? additionalDelay +
                      forcedLifetime
                    : GetParticleLifetime(
                        effect);

            Destroy(
                effect.gameObject,
                Mathf.Max(
                    0.5f,
                    cleanupTime));
        }

        private float GetParticleLifetime(
            ParticleSystem root)
        {
            if (root == null)
                return 3f;

            ParticleSystem[] systems =
                root.GetComponentsInChildren<
                    ParticleSystem>(true);

            float longest = 0f;

            for (int i = 0;
                 i < systems.Length;
                 i++)
            {
                ParticleSystem system =
                    systems[i];

                if (system == null)
                    continue;

                ParticleSystem.MainModule main =
                    system.main;

                float lifetime =
                    main.duration +
                    main.startDelay.constantMax +
                    main.startLifetime.constantMax;

                longest =
                    Mathf.Max(
                        longest,
                        lifetime);
            }

            return Mathf.Max(
                0.5f,
                longest + 0.25f);
        }

        #endregion

        #region Debris

        private void SpawnDebris(
            Vector3 position,
            Vector3 localUp,
            Vector3 inheritedVelocity)
        {
            if (debrisPrefabs == null ||
                debrisPrefabs.Length == 0 ||
                debrisCount <= 0)
            {
                return;
            }

            for (int i = 0;
                 i < debrisCount;
                 i++)
            {
                GameObject prefab =
                    debrisPrefabs[
                        Random.Range(
                            0,
                            debrisPrefabs.Length)];

                if (prefab == null)
                    continue;

                GameObject debris =
                    Instantiate(
                        prefab,
                        position,
                        Random.rotation);

                Rigidbody debrisBody =
                    debris.GetComponent<
                        Rigidbody>();

                if (debrisBody != null)
                {
                    Vector3 direction =
                        (
                            Random.onUnitSphere +
                            localUp * 0.75f
                        ).normalized;

                    float speed =
                        Random.Range(
                            debrisMinimumSpeed,
                            debrisMaximumSpeed);

                    debrisBody.linearVelocity =
                        inheritedVelocity +
                        direction *
                        speed;

                    debrisBody.angularVelocity =
                        Random.insideUnitSphere *
                        debrisTorque;
                }

                Destroy(
                    debris,
                    debrisLifetime);
            }
        }

        #endregion

        #region Audio

        private void PlayRandomClipDetached(
            AudioClip[] clips,
            ref int previousIndex,
            float baseVolume,
            Vector3 position,
            float delay)
        {
            if (audioSource == null ||
                clips == null ||
                clips.Length == 0)
            {
                return;
            }

            int index =
                SelectClipIndex(
                    clips,
                    previousIndex);

            if (index < 0)
                return;

            AudioClip clip =
                clips[index];

            if (clip == null)
                return;

            previousIndex = index;

            GameObject audioObject =
                new GameObject(
                    "DestructionAudio");

            audioObject.transform.position =
                position;

            AudioSource detachedSource =
                audioObject.AddComponent<
                    AudioSource>();

            CopyAudioSettings(
                audioSource,
                detachedSource);

            float roleMultiplier =
                isPlayer
                    ? playerVolumeMultiplier
                    : opponentVolumeMultiplier;

            detachedSource.clip =
                clip;

            detachedSource.volume =
                Mathf.Clamp01(
                    baseVolume *
                    roleMultiplier);

            detachedSource.pitch =
                Random.Range(
                    1f - pitchVariation,
                    1f + pitchVariation);

            detachedSource.spatialBlend =
                isPlayer
                    ? playerSpatialBlend
                    : opponentSpatialBlend;

            detachedSource.loop = false;
            detachedSource.playOnAwake = false;

            detachedSource.PlayDelayed(
                delay);

            float pitch =
                Mathf.Max(
                    0.01f,
                    Mathf.Abs(
                        detachedSource.pitch));

            float cleanupDelay =
                delay +
                clip.length /
                pitch +
                0.25f;

            Destroy(
                audioObject,
                cleanupDelay);
        }

        private void CopyAudioSettings(
            AudioSource source,
            AudioSource destination)
        {
            destination.outputAudioMixerGroup =
                source.outputAudioMixerGroup;

            destination.priority =
                source.priority;

            destination.rolloffMode =
                source.rolloffMode;

            destination.minDistance =
                source.minDistance;

            destination.maxDistance =
                source.maxDistance;

            destination.dopplerLevel =
                source.dopplerLevel;

            destination.spread =
                source.spread;

            destination.reverbZoneMix =
                source.reverbZoneMix;

            destination.bypassEffects =
                source.bypassEffects;

            destination.bypassListenerEffects =
                source.bypassListenerEffects;

            destination.bypassReverbZones =
                source.bypassReverbZones;
        }

        private int SelectClipIndex(
            AudioClip[] clips,
            int previousIndex)
        {
            int validCount = 0;

            for (int i = 0;
                 i < clips.Length;
                 i++)
            {
                if (clips[i] != null)
                    validCount++;
            }

            if (validCount == 0)
                return -1;

            int ordinal =
                Random.Range(
                    0,
                    validCount);

            int selected =
                GetValidClipIndex(
                    clips,
                    ordinal);

            if (validCount > 1 &&
                selected ==
                previousIndex)
            {
                ordinal =
                    (ordinal + 1) %
                    validCount;

                selected =
                    GetValidClipIndex(
                        clips,
                        ordinal);
            }

            return selected;
        }

        private int GetValidClipIndex(
            AudioClip[] clips,
            int ordinal)
        {
            int current = 0;

            for (int i = 0;
                 i < clips.Length;
                 i++)
            {
                if (clips[i] == null)
                    continue;

                if (current == ordinal)
                    return i;

                current++;
            }

            return -1;
        }

        #endregion

        #region Helpers

        private Vector3 GetExplosionPosition()
        {
            return explosionOrigin != null
                ? explosionOrigin.position
                : transform.position;
        }

        private Quaternion GetExplosionRotation()
        {
            return explosionOrigin != null
                ? explosionOrigin.rotation
                : transform.rotation;
        }

        #endregion
    }
}