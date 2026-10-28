using SporeTribal.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.CameraControl
{
    public class CameraAutoOrbit : MonoBehaviour
    {
        [SerializeField] private RTSCameraController cameraController;
        [SerializeField] private RTSInputController input;
        [Tooltip("Start speed, top speed and how fast the arrow keys change it.")]
        [SerializeField] private CameraProfile profile;

        public bool IsOrbiting { get; private set; }
        public float Speed { get; private set; }

        private static TribalControls.CameraActions Actions => GameInput.Controls.Camera;

        private void OnEnable() => Actions.ToggleAutoOrbit.performed += OnToggle;

        private void OnDisable()
        {
            Actions.ToggleAutoOrbit.performed -= OnToggle;
            SetOrbiting(false);
        }

        private void OnToggle(InputAction.CallbackContext _)
        {
            if (Speed == 0f) Speed = profile.OrbitStartSpeed;
            SetOrbiting(!IsOrbiting);
        }

        private void Update()
        {
            if (GameInput.IsLocked) SetOrbiting(false);
            if (!IsOrbiting) return;

            float steer = Actions.Pan.ReadValue<Vector2>().x;
            float maxSpeed = profile.OrbitMaxSpeed;
            Speed = Mathf.Clamp(Speed + steer * profile.OrbitSpeedRamp * Time.unscaledDeltaTime, -maxSpeed, maxSpeed);
            cameraController.Orbit(Speed * Time.unscaledDeltaTime);
        }

        private void SetOrbiting(bool orbiting)
        {
            IsOrbiting = orbiting;
            if (input != null) input.KeyPanEnabled = !orbiting;
        }
    }
}
