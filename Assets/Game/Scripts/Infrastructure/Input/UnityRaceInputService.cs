using UnityEngine;

// NOT MEANT TO BE IN FINAL
// REPLACE WITH PROPER INPUT SYSTEM LATER
namespace RaceFatal.Infrastructure.Input
{
    public sealed class UnityRaceInputService :
        MonoBehaviour,
        IRaceInputService
    {
        [Header("Temporary Input Bindings")]

        [SerializeField]
        private KeyCode throttleKey = KeyCode.W;
        [SerializeField]
        private KeyCode brakeKey = KeyCode.S;
        [SerializeField]
        private KeyCode steeringLeftKey = KeyCode.A;
        [SerializeField]
        private KeyCode steeringRightKey = KeyCode.D;
        [SerializeField]
        private KeyCode nextEquipment = KeyCode.E;
        [SerializeField]
        private KeyCode previousEquipment = KeyCode.Q;
        [SerializeField]
        private int activationMouseButton = 0;

        public bool EquipmentPressed =>
            UnityEngine.Input.GetMouseButtonDown(
                activationMouseButton);

        public bool EquipmentReleased =>
            UnityEngine.Input.GetMouseButtonUp(
                activationMouseButton);

        public bool NextEquipmentPressed =>
            UnityEngine.Input.GetKeyDown(
                nextEquipment);

        public bool PreviousEquipmentPressed =>
            UnityEngine.Input.GetKeyDown(
                previousEquipment);
        public float Throttle => UnityEngine.Input.GetKey(throttleKey) ? 1f : 0f;
        public float Brake => UnityEngine.Input.GetKey(brakeKey) ? 1f : 0f;
        public float Steering
        {
            get
            {
                float value = 0f;

                if (UnityEngine.Input.GetKey(
                        steeringLeftKey))
                {
                    value -= 1f;
                }

                if (UnityEngine.Input.GetKey(
                        steeringRightKey))
                {
                    value += 1f;
                }

                return value;
            }
        }
    }
}