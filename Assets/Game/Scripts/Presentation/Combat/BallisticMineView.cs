using System.Collections.Generic;
using RaceFatal.Combat;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class BallisticMineView : MonoBehaviour
    {
        private readonly HashSet<string> damagedRacerIds = new HashSet<string>();
        private RaceRuntimeController runtime;
        private WeaponPresentationProfile profile;
        private string attackerRacerId;
        private float damage;
        private float explosionRadius;
        private float armedTime;
        private float expireTime;
        private BoxCollider trigger;
        private Light warningLight;
        private Material visualMaterial;
        private bool initialized;
        private bool detonated;
        public void Initialize(RaceRuntimeController raceRuntime, WeaponPresentationProfile presentation, string attackerId, float mineDamage, float radius, float armingDelay, float lifetime)
        {
            runtime = raceRuntime;
            profile = presentation;
            attackerRacerId = attackerId;
            damage = Mathf.Max(0f, mineDamage);
            explosionRadius = Mathf.Max(0.75f, radius);
            armedTime = Time.time + Mathf.Max(0f, armingDelay);
            expireTime = Time.time + (lifetime > 0f ? lifetime : 20f);
            EnsureTrigger();
            BuildVisual();
            initialized = true;
        }

        private void Update()
        {
            if (!initialized || detonated)
            {
                return;
            }

            if (runtime == null || !runtime.IsRaceActive)
            {
                Destroy(gameObject);
                return;
            }

            if (Time.time >= expireTime)
            {
                Destroy(gameObject);
                return;
            }

            if (warningLight != null)
            {
                float armedRatio = Time.time >= armedTime ? 1f : 0.35f;
                float pulse = 0.78f + Mathf.Sin(Time.time * 5f) * 0.12f;
                warningLight.intensity = armedRatio * pulse;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized || detonated || Time.time < armedTime || other == null)
            {
                return;
            }

            RacerViewController racer = other.GetComponentInParent<RacerViewController>();
            if (!IsEnemy(racer))
            {
                return;
            }

            Detonate();
        }

        private void Detonate()
        {
            if (detonated)
                return;
            detonated = true;
            damagedRacerIds.Clear();
            Collider[] overlaps = Physics.OverlapBox(transform.position, trigger.size * 0.5f, transform.rotation, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider hit = overlaps[i];
                if (hit == null)
                    continue;
                RacerViewController victim = hit.GetComponentInParent<RacerViewController>();
                if (!IsEnemy(victim) || !damagedRacerIds.Add(victim.RacerId))
                {
                    continue;
                }

                runtime?.Director?.ApplyDamage(attackerRacerId, victim.RacerId, damage, DamageCause.Weapon);
            }

            WeaponWorldFeedback.Play(profile, transform.position, transform.rotation);
            Destroy(gameObject);
        }

        private bool IsEnemy(RacerViewController candidate)
        {
            if (candidate == null || !candidate.IsInitialized || candidate.Participant == null || candidate.Participant.Vehicle == null || candidate.Participant.Vehicle.IsDestroyed || candidate.Participant.Status != RaceParticipantStatus.Racing || candidate.RacerId == attackerRacerId)
            {
                return false;
            }

            if (runtime == null || runtime.Director == null)
            {
                return true;
            }

            if (!runtime.TryGetRacerView(attackerRacerId, out RacerViewController attacker) || attacker == null || attacker.Participant == null)
            {
                return true;
            }

            if (runtime.Director.State.Deathmatch?.Mode == DeathmatchVictoryMode.Individual)
            {
                return true;
            }

            return !string.Equals(attacker.Participant.TeamId, candidate.Participant.TeamId, System.StringComparison.Ordinal);
        }

        private void EnsureTrigger()
        {
            trigger = GetComponent<BoxCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<BoxCollider>();
            }

            trigger.isTrigger = true;
            trigger.size = Vector3.one * Mathf.Max(0.65f, explosionRadius * 0.45f);
        }

        private void BuildVisual()
        {
            // Use the prefab for the visual representation of the mine if available
            GameObject visual = profile != null && profile.DeployedMineVfxPrefab != null ? Instantiate(profile.DeployedMineVfxPrefab) : new GameObject(GameObject.CreatePrimitive(PrimitiveType.Sphere).name);
            visual.name = "Mine Visual";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 0.22f;
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                visualCollider.enabled = false;
                Destroy(visualCollider);
            }

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("HDRP/Unlit");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                if (shader != null)
                {
                    Material material = new Material(shader);
                    Color baseColor = new Color(0.08f, 0.12f, 0.15f, 1f);
                    Color emission = new Color(0.05f, 0.4f, 0.7f, 1f);
                    material.color = baseColor;
                    if (material.HasProperty("_UnlitColor")) material.SetColor("_UnlitColor", baseColor);
                    if (material.HasProperty("_EmissiveColor")) material.SetColor("_EmissiveColor", emission * 2f);
                    material.EnableKeyword("_EMISSION");
                    if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
                    visualMaterial = material;
                    renderer.sharedMaterial = material;
                }
            }

            if (profile != null && profile.DeployedMineVfxPrefab != null)
            {
                GameObject marker = Instantiate(profile.DeployedMineVfxPrefab, transform, false);
                marker.name = "Deployed Mine Marker";
                marker.transform.localPosition = profile.DeployedMineVfxOffset;
                marker.transform.localScale = profile.DeployedMineVfxScale;
                marker.SetActive(true);
                // Loop all particle systems in the marker to ensure they play over and over again
                foreach (ParticleSystem particles in marker.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (particles.main.loop)
                        particles.Play();
                }

                return;
            }

            GameObject lightObject = new GameObject("Mine Indicator");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.up * 0.05f;
            warningLight = lightObject.AddComponent<Light>();
            warningLight.type = LightType.Point;
            warningLight.range = 1.1f;
            warningLight.intensity = 0.35f;
            warningLight.color = new Color(0.2f, 0.7f, 1f, 1f);
        }

        private void OnDestroy()
        {
            if (visualMaterial != null) Destroy(visualMaterial);
        }
    }
}
