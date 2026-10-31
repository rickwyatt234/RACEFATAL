using UnityEngine;

namespace RaceFatal.Presentation.Tactics
{
    // Input must run early, but camera blends must follow PlayerCockpitView's
    // order-1000 LateUpdate to sample its final pose without a one-frame lag.
    [DefaultExecutionOrder(1200)]
    [DisallowMultipleComponent]
    public sealed class TacticalCameraPoseDriver : MonoBehaviour
    {
        private TacticalRaceController controller;
        private void Awake() { controller = GetComponent<TacticalRaceController>(); }
        private void LateUpdate()
        {
            if (controller != null) controller.UpdateCameraPose();
        }
    }
}
