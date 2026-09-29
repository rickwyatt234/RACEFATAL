using System.Collections.Generic;
using RaceFatal.Combat;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(RacerViewController))]
    [RequireComponent(typeof(BikeMotor))]
    public class RamAttackState : MonoBehaviour
    {
        private readonly HashSet<string>
            hitRacers =
                new HashSet<string>();

        private RaceRuntimeController runtime;
        private RacerViewController racer;
        private BikeMotor motor;
        private WeaponPresentationProfile profile;

        private float activeUntil;
        private float damage;
        private float impactPush;

        private TrailRenderer dashTrail;

        private void Awake()
        {
            racer =
                GetComponent<
                    RacerViewController>();

            motor =
                GetComponent<
                    BikeMotor>();
        }

        public bool Activate(
            RaceRuntimeController raceRuntime,
            WeaponPresentationProfile presentation,
            float ramDamage,
            float lateralDistance,
            float dashDuration,
            float push)
        {
            runtime = raceRuntime;
            profile = presentation;

            if (racer == null ||
                motor == null ||
                !racer.IsInitialized)
            {
                return false;
            }

            float steering =
                motor.SteeringInput;

            if (Mathf.Abs(
                    steering) <
                0.05f)
            {
                return false;
            }

            damage =
                Mathf.Max(
                    0f,
                    ramDamage);

            impactPush =
                Mathf.Max(
                    0f,
                    push);

            float direction =
                Mathf.Sign(
                    steering);

            float safeDuration =
                Mathf.Max(
                    0.06f,
                    dashDuration);

            hitRacers.Clear();

            activeUntil =
                Time.time +
                safeDuration +
                0.12f;

            motor.ApplyLateralDash(
                direction *
                Mathf.Max(
                    0f,
                    lateralDistance),
                safeDuration);

            BeginDashTrail(
                safeDuration);

            return true;
        }

        private void OnCollisionEnter(
            Collision collision)
        {
            if (runtime == null ||
                !runtime.IsRaceActive ||
                Time.time >
                    activeUntil ||
                collision == null ||
                collision.collider == null)
            {
                return;
            }

            RacerViewController other =
                collision.collider.GetComponentInParent<
                    RacerViewController>();

            if (!IsEnemy(
                    other) ||
                !hitRacers.Add(
                    other.RacerId))
            {
                return;
            }

            runtime?.Director?.ApplyDamage(
                racer.RacerId,
                other.RacerId,
                damage,
                DamageCause.Weapon, isRamAttack: true);

            BikeMotor otherMotor =
                other.GetComponent<
                    BikeMotor>();

            if (otherMotor != null &&
                impactPush > 0f)
            {
                Vector3 pushDirection =
                    other.transform.position -
                    transform.position;

                if (pushDirection.sqrMagnitude <
                    0.001f)
                {
                    pushDirection =
                        motor.SteeringInput >= 0f
                            ? transform.right
                            : -transform.right;
                }

                otherMotor.ApplyExternalVelocityChange(
                    pushDirection,
                    impactPush);
            }

            Vector3 contactPoint =
                collision.contactCount > 0
                    ? collision.GetContact(0).point
                    : other.transform.position;

            Vector3 contactNormal =
                collision.contactCount > 0
                    ? collision.GetContact(0).normal
                    : (transform.position -
                       other.transform.position)
                        .normalized;

            Quaternion rotation =
                contactNormal.sqrMagnitude >
                    0.001f
                    ? Quaternion.LookRotation(
                        contactNormal)
                    : Quaternion.identity;

            WeaponWorldFeedback.Play(
                profile,
                contactPoint,
                rotation);
        }

        private bool IsEnemy(
            RacerViewController candidate)
        {
            if (candidate == null ||
                candidate == racer ||
                !candidate.IsInitialized ||
                candidate.Participant == null ||
                candidate.Participant.Vehicle == null ||
                candidate.Participant.Vehicle.IsDestroyed ||
                candidate.Participant.Status !=
                    RaceParticipantStatus.Racing)
            {
                return false;
            }

            if (runtime?.Director?.State.Deathmatch?.Mode ==
                DeathmatchVictoryMode.Individual)
            {
                return true;
            }

            return
                racer?.Participant == null ||
                !string.Equals(
                    racer.Participant.TeamId,
                    candidate.Participant.TeamId,
                    System.StringComparison.Ordinal);
        }

        private void BeginDashTrail(
            float duration)
        {
            if (dashTrail == null)
            {
                GameObject trailObject =
                    new GameObject(
                        "Ram Dash Trail");

                trailObject.transform.SetParent(
                    transform,
                    false);

                dashTrail =
                    trailObject.AddComponent<
                        TrailRenderer>();

                dashTrail.time = 0.14f;
                dashTrail.startWidth = 0.3f;
                dashTrail.endWidth = 0f;
                dashTrail.minVertexDistance = 0.04f;

                Shader shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit");

                if (shader == null)
                    shader = Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    dashTrail.material =
                        new Material(
                            shader);
                }

                dashTrail.startColor =
                    new Color(
                        0.25f,
                        0.8f,
                        1f,
                        0.85f);

                dashTrail.endColor =
                    new Color(
                        0.25f,
                        0.8f,
                        1f,
                        0f);
            }

            dashTrail.Clear();
            dashTrail.emitting = true;

            CancelInvoke(
                nameof(
                    StopDashTrail));

            Invoke(
                nameof(
                    StopDashTrail),
                Mathf.Max(
                    0.05f,
                    duration));
        }

        private void StopDashTrail()
        {
            if (dashTrail != null)
                dashTrail.emitting = false;
        }
    }
}
