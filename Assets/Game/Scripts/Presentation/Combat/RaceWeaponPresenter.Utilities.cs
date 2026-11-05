using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Combat;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public partial class RaceWeaponPresenter
    {
        [SerializeField] private KeyCode ramKey = KeyCode.R;
        [SerializeField] private KeyCode siphonKey = KeyCode.G;
        [SerializeField] private KeyCode repulsionKey = KeyCode.H;

        private sealed class UtilityState
        {
            public float RamReady, SiphonReady, RepulsionReady, RuptureReady, SiphonEnds;
            public RacerViewController SiphonTarget;
        }

        private readonly Dictionary<string, UtilityState> utilityStates = new Dictionary<string, UtilityState>();
        private UtilityState UtilityStateFor(string id)
        {
            if (!utilityStates.TryGetValue(id, out var state))
                utilityStates.Add(id, state = new UtilityState());
            return state;
        }

        private void UpdateRaceUtilities()
        {
            foreach (var racer in runtime.Director.State.Participants)
            {
                if (racer.Status != RaceParticipantStatus.Racing || !racer.Vehicle.EquipmentSystem.WeaponsAllowed ||
                    !runtime.TryGetRacerView(racer.RacerId, out var view)) continue;
                var equipment = racer.Vehicle.EquipmentSystem;
                var state = UtilityStateFor(racer.RacerId);
                bool player = racer.Role == RaceParticipantRole.Player;
                RacerViewController nearest = null;
                float distance = 12f;
                float leechBudget = equipment.Utility(UtilityEffect.Leech) * Time.deltaTime;
                int leechTargets = 0;
                foreach (var other in runtime.Director.State.Participants)
                {
                    if (other.Status != RaceParticipantStatus.Racing || other.TeamId == racer.TeamId ||
                        !runtime.TryGetRacerView(other.RacerId, out var rival)) continue;
                    float range = Vector3.Distance(view.transform.position, rival.transform.position);
                    if (range < distance) { nearest = rival; distance = range; }
                    if (range <= 8f && leechBudget > 0f && other.Vehicle.EquipmentSystem.Shield?.Current > 0f && leechTargets < 2)
                    {
                        other.Vehicle.EquipmentSystem.Shield.AbsorbDamage(leechBudget);
                        leechTargets++;
                    }
                }

                if (Time.time >= state.RamReady && (player ? UnityEngine.Input.GetKeyDown(ramKey) : distance < 4f))
                {
                    var ram = view.GetComponent<RamAttackState>() ?? view.gameObject.AddComponent<RamAttackState>();
                    TryGetPresentationProfile("weapon_lateral_ram", out var profile);
                    if (ram.Activate(runtime, profile, 32f, 4.2f, .16f, 7.5f)) state.RamReady = Time.time + 2f;
                }

                float cooldownScale = 1f - racer.Perks.Bonus(RacerPerkEffect.TacticalOverdrive);
                if (nearest != null && equipment.Utility(UtilityEffect.Siphon) > 0f && Time.time >= state.SiphonReady &&
                    (!player || UnityEngine.Input.GetKeyDown(siphonKey)))
                {
                    state.SiphonTarget = nearest;
                    state.SiphonEnds = Time.time + 4f;
                    state.SiphonReady = Time.time + 12f * cooldownScale;
                }
                if (state.SiphonTarget != null)
                {
                    var victim = state.SiphonTarget.Participant;
                    if (Time.time >= state.SiphonEnds || victim.Status != RaceParticipantStatus.Racing ||
                        Vector3.Distance(view.transform.position, state.SiphonTarget.transform.position) > 12f)
                        state.SiphonTarget = null;
                    else
                    {
                        var pool = racer.Vehicle.EnergyPool;
                        float rate = equipment.Utility(UtilityEffect.Siphon) * (1f + racer.Perks.Bonus(RacerPerkEffect.SiphonStrength));
                        float amount = Mathf.Min(rate * Time.deltaTime, pool.MaxEnergy - pool.CurrentEnergy);
                        pool.Recharge(victim.Vehicle.EnergyPool.SpendUpTo(amount));
                    }
                }
                if (distance <= 10f && equipment.Utility(UtilityEffect.Repulsion) > 0f && Time.time >= state.RepulsionReady &&
                    (!player || UnityEngine.Input.GetKeyDown(repulsionKey)) &&
                    racer.Vehicle.EnergyPool.TrySpend(40f * (cooldownScale < 1f ? 1.05f : 1f)))
                {
                    state.RepulsionReady = Time.time + 12f * cooldownScale;
                    Pulse(view, false);
                }
            }
        }

        private void OnUtilityDamage(DamageEvent hit)
        {
            if (!hit.ShieldDepleted || !runtime.TryGetRacerView(hit.VictimRacerId, out var view) ||
                view.Participant.Vehicle.EquipmentSystem.Utility(UtilityEffect.Rupture) <= 0f) return;
            var state = UtilityStateFor(hit.VictimRacerId);
            if (Time.time < state.RuptureReady) return;
            state.RuptureReady = Time.time + 20f;
            Pulse(view, true);
        }

        private void Pulse(RacerViewController source, bool damaging)
        {
            foreach (var other in runtime.Director.State.Participants)
            {
                if (other.Status != RaceParticipantStatus.Racing || other.TeamId == source.Participant.TeamId ||
                    !runtime.TryGetRacerView(other.RacerId, out var rival)) continue;
                Vector3 direction = rival.transform.position - source.transform.position;
                if (direction.sqrMagnitude > 100f) continue;
                if (damaging)
                    runtime.Director.ApplyDamage(source.RacerId, other.RacerId,
                        other.Vehicle.EquipmentSystem.Shield?.Current > 0 ? 30f : 15f, DamageCause.Weapon);
                else
                    rival.GetComponent<BikeMotor>()?.ApplyExternalVelocityChange(direction.normalized,
                        10f * (1f - other.Vehicle.EquipmentSystem.Utility(UtilityEffect.Anchor)));
            }
        }
    }
}
