using SporeTribal.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.CameraControl
{
    public class CameraAutoOrbit : MonoBehaviour
    {
        [SerializeField] private RTSCameraController cameraController;
        [SerializeField] private RTSInputController input;
        [SerializeField] private Key toggleKey = Key.O;
        [Tooltip("Degrees per second when the orbit is first switched on.")]
        [SerializeField] private float startSpeed = 8f;
        [SerializeField] private float maxSpeed = 60f;
        [Tooltip("Degrees per second gained for every second an arrow key is held.")]
        [SerializeField] private float speedRamp = 20f;

        public bool IsOrbiting { get; private set; }
        public float Speed { get; private set; }
        public Key ToggleKey => toggleKey;

        private void OnDisable() => SetOrbiting(false);

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[toggleKey].wasPressedThisFrame)
            {
                if (Speed == 0f) Speed = startSpeed;
                SetOrbiting(!IsOrbiting);
            }
            if (!IsOrbiting) return;

            float steer = (keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f);
            Speed = Mathf.Clamp(Speed + steer * speedRamp * Time.unscaledDeltaTime, -maxSpeed, maxSpeed);
            cameraController.Orbit(Speed * Time.unscaledDeltaTime);
        }

        private void SetOrbiting(bool orbiting)
        {
            IsOrbiting = orbiting;
            if (input != null) input.KeyPanEnabled = !orbiting;
        }
    }
}
