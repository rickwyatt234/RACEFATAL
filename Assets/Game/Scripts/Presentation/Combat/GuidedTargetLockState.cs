using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(
        typeof(RacerViewController))]
    public class GuidedTargetLockState :
        MonoBehaviour
    {
        [Header("Runtime Debug")]
        [SerializeField]
        private string debugTarget =
            "None";

        [SerializeField]
        private float debugProgress;

        [SerializeField]
        private bool debugLocked;

        private RacerViewController owner;

        private RacerViewController currentTarget;
        private TargetLockThreatReceiver targetReceiver;

        private float lockTimer;

        public RacerViewController CurrentTarget =>
            currentTarget;

        public float LockProgress {
            get;
            private set;
        }

        public bool IsLocked {
            get;
            private set;
        }

        private string SourceId
        {
            get
            {
                if (owner != null &&
                    !string.IsNullOrWhiteSpace(
                        owner.RacerId))
                {
                    return owner.RacerId;
                }

                return
                    GetInstanceID()
                        .ToString();
            }
        }

        private void Awake()
        {
            owner =
                GetComponent<
                    RacerViewController>();
        }

        public void TrackCandidate(
            RacerViewController candidate,
            float deltaTime,
            float requiredDuration)
        {
            if (candidate == null ||
                candidate == owner)
            {
                ClearTarget();
                return;
            }

            if (candidate !=
                currentTarget)
            {
                ChangeTarget(
                    candidate);
            }

            lockTimer +=
                Mathf.Max(
                    0f,
                    deltaTime);

            requiredDuration =
                Mathf.Max(
                    0f,
                    requiredDuration);

            LockProgress =
                requiredDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        lockTimer /
                        requiredDuration);

            IsLocked =
                LockProgress >= 1f;

            if (targetReceiver != null)
            {
                targetReceiver.SetThreat(
                    SourceId,
                    IsLocked);
            }

            UpdateDebug();
        }

        public bool TryGetLockedTarget(
            out RacerViewController target)
        {
            target =
                IsLocked
                    ? currentTarget
                    : null;

            return target != null;
        }

        public void ClearTarget()
        {
            ClearTargetRegistration();

            currentTarget =
                null;

            lockTimer =
                0f;

            LockProgress =
                0f;

            IsLocked =
                false;

            UpdateDebug();
        }

        private void ChangeTarget(
            RacerViewController target)
        {
            ClearTargetRegistration();

            currentTarget =
                target;

            lockTimer =
                0f;

            LockProgress =
                0f;

            IsLocked =
                false;

            targetReceiver =
                currentTarget.GetComponent<
                    TargetLockThreatReceiver>();

            if (targetReceiver != null)
            {
                targetReceiver.SetThreat(
                    SourceId,
                    false);
            }

            UpdateDebug();
        }

        private void ClearTargetRegistration()
        {
            if (targetReceiver != null)
            {
                targetReceiver.ClearThreat(
                    SourceId);
            }

            targetReceiver =
                null;
        }

        private void OnDisable()
        {
            ClearTarget();
        }

        private void OnDestroy()
        {
            ClearTarget();
        }

        private void UpdateDebug()
        {
            debugTarget =
                currentTarget != null
                    ? currentTarget.RacerId
                    : "None";

            debugProgress =
                LockProgress;

            debugLocked =
                IsLocked;
        }
    }
}