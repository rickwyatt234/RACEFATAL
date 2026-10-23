using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Tactics
{
    public enum PartnerOrder { FormUp, Cover, Tail, Attack, PressureLeft, PressureRight, Ram, Disengage }
    public enum PartnerAggression { Passive, Balanced, Aggressive }

    // Produces intentions for the existing driver. Never teleports or overrides road safety.
    public sealed class PartnerOrderController : MonoBehaviour
    {
        public bool HasPlayer => player != null;
        public PartnerOrder Order { get; private set; }
        public PartnerAggression Aggression { get; private set; } = PartnerAggression.Passive;
        public bool ConserveEnergy { get; private set; } = true;
        public int FormationSide { get; private set; } = 1;
        public RacerViewController Target { get; private set; }
        public string Acknowledgment { get; private set; } = "Forming up";
        public bool AllowAttack => !survival && !ConserveEnergy && Target != null &&
            (Order == PartnerOrder.Attack || Order == PartnerOrder.Ram || Order == PartnerOrder.PressureLeft || Order == PartnerOrder.PressureRight || Order == PartnerOrder.Cover || (Order == PartnerOrder.FormUp && Aggression != PartnerAggression.Passive));
        public bool AllowBoost => !survival && !ConserveEnergy && Order != PartnerOrder.FormUp && Order != PartnerOrder.Tail;
        private bool survival;
        private RacerViewController self;
        private RacerViewController player;
        private TrackProgressPath path;

        public void Initialize(RacerViewController ownView, RacerViewController playerView, TrackProgressPath track)
        {
            if (self?.Participant != null) self.Participant.Vehicle.EquipmentSystem.WeaponFired -= OnWeaponFired;
            self = ownView; player = playerView; path = track;
            if (self?.Participant != null) self.Participant.Vehicle.EquipmentSystem.WeaponFired += OnWeaponFired;
        }
        private void OnWeaponFired(RaceFatal.Equipment.WeaponFireEvent shot)
        {
            if (Order == PartnerOrder.Ram && shot.DeliveryMode == RaceFatal.Shared.WeaponDeliveryMode.Ram) Issue(PartnerOrder.FormUp);
        }
        private void OnDestroy()
        { if (self?.Participant != null) self.Participant.Vehicle.EquipmentSystem.WeaponFired -= OnWeaponFired; }
        public void SetAggression(PartnerAggression value) { Aggression = value; Acknowledgment = "Aggression: " + value; }
        public void SetConservation(bool value) { ConserveEnergy = value; Acknowledgment = value ? "Conserving energy" : "Energy use authorized"; }
        public void SetFormationSide(int side) { FormationSide = side < 0 ? -1 : 1; }
        public void Issue(PartnerOrder order, RacerViewController target = null)
        {
            Order = order; Target = target;
            if (order == PartnerOrder.Attack || order == PartnerOrder.Ram || order == PartnerOrder.PressureLeft || order == PartnerOrder.PressureRight || order == PartnerOrder.Cover)
                ConserveEnergy = false;
            Acknowledgment = order + (target != null ? ": " + target.Participant.Racer.Name : "");
            self?.Participant?.Vehicle?.EquipmentSystem.CancelSelectedActivation();
        }
        public bool TryGetRamSteering(out float steering)
        {
            steering = 0f;
            if (Order != PartnerOrder.Ram || !AllowAttack || Target == null || self == null) return false;
            Vector3 delta=Target.transform.position-self.transform.position;
            if(delta.magnitude>5f || Mathf.Abs(Vector3.Dot(delta,self.transform.forward))>2f) return false;
            for(int i=0;i<self.Participant.Vehicle.EquipmentSystem.WeaponCount;i++)
                if(self.Participant.Vehicle.EquipmentSystem.TryGetWeaponSnapshot(i,out var weapon) && weapon.HasAmmo && weapon.Definition.DeliveryMode==RaceFatal.Shared.WeaponDeliveryMode.Ram)
                { steering=Mathf.Sign(Vector3.Dot(delta,self.transform.right))*0.75f;return true; }
            return false;
        }
        public void UpdateIntent()
        {
            if (self?.Participant == null) return;
            bool critical = self.Participant.Vehicle.Damage.DamageRatio >= 0.85f;
            if (critical && !survival) { survival = true; Acknowledgment = "Integrity critical — breaking off"; }
            if (Target != null && (Target.Participant == null || Target.Participant.Vehicle.IsDestroyed || Target.Participant.Status != RaceParticipantStatus.Racing))
                Issue(PartnerOrder.FormUp);
            if ((Order == PartnerOrder.Cover || (Order == PartnerOrder.FormUp && Aggression != PartnerAggression.Passive)) && player != null)
            {
                RacerViewController threat = null;
                float best = Aggression == PartnerAggression.Aggressive ? 50f : 25f;
                foreach (var sensor in RaceFatal.Presentation.Vehicles.AIRacerSensor.ActiveSensors)
                {
                    var view = sensor.GetComponentInParent<RacerViewController>();
                    if (view?.Participant == null || view.Participant.TeamId == self.Participant.TeamId || view.Participant.Status != RaceParticipantStatus.Racing) continue;
                    float distance = Vector3.Distance(view.transform.position, player.transform.position);
                    if (distance < best) { best = distance; threat = view; }
                }
                Target = threat;
            }
        }
        private RacerViewController Anchor => !survival && Order != PartnerOrder.Disengage && Order != PartnerOrder.Cover && Order != PartnerOrder.FormUp && Target != null ? Target : player;
        public string CombatTargetId => AllowAttack ? Target?.RacerId : null;
        public bool TryGetLane(float ownProgress, Vector3 normal, float halfWidth, out float lane)
        {
            lane = 0f;
            var anchor = Anchor;
            if (anchor == null || path == null) return false;
            int segment;
            float progress = path.GetProgress(anchor.transform.position, -1, path.SegmentCount, out segment);
            Vector3 forward = Vector3.ProjectOnPlane(path.GetForwardAtProgress(progress), normal).normalized;
            Vector3 right = Vector3.Cross(normal, forward).normalized;
            float anchorLane = Vector3.Dot(anchor.transform.position - path.GetPositionAtProgress(progress), right);
            float side = Order == PartnerOrder.PressureLeft ? -1 : Order == PartnerOrder.PressureRight ? 1 : FormationSide;
            float offset = Anchor == player || survival || Order == PartnerOrder.Disengage ? side * 2.5f :
                Order == PartnerOrder.PressureLeft || Order == PartnerOrder.PressureRight || Order == PartnerOrder.Ram ? side * 2f : 0f;
            lane = Mathf.Clamp(anchorLane + offset, -halfWidth, halfWidth);
            return true;
        }
        public float LimitSpeed(float ownProgress, float plannedSpeed)
        {
            var anchor = Anchor;
            if (anchor?.Participant == null || path == null) return plannedSpeed;
            int segment;
            float progress = path.GetProgress(anchor.transform.position, -1, path.SegmentCount, out segment);
            float gap = (Mathf.Repeat(progress - ownProgress + 0.5f, 1f) - 0.5f) * path.TotalLength;
            float desiredGap = Anchor == player || survival || Order == PartnerOrder.Disengage ? 7f :
                Order == PartnerOrder.PressureLeft || Order == PartnerOrder.PressureRight || Order == PartnerOrder.Ram ? 0f : 10f;
            // Ahead of the slot: gently drop back. Behind it: use normal legal race pace.
            float anchorSpeed = anchor.Participant.SpeedKph / 3.6f;
            float followingSpeed = Mathf.Max(0f, anchorSpeed + (gap - desiredGap) * 0.65f);
            return Mathf.Min(plannedSpeed, followingSpeed);
        }
    }
}
