using UnityEngine;

namespace NightRush
{
    /// <summary>Feeds player input into a CarController.</summary>
    public class PlayerInput : MonoBehaviour
    {
        CarController car;

        void Awake() { car = GetComponent<CarController>(); }

        void Update()
        {
            car.Steer = InputBridge.Steer();
            car.Throttle = InputBridge.Throttle();
            car.Handbrake = InputBridge.Handbrake();
            car.NitroHeld = InputBridge.Nitro();
        }
    }
}
