using UnityEngine;
using SporeTribal.Inputs;

namespace SporeTribal.CameraControl
{
    public class RTSCameraController : MonoBehaviour
    {
        [SerializeField] private RTSInputController input;
        [SerializeField] private float keyPanSpeed = 0.8f;
        [SerializeField] private float dragPanSpeed = 0.0025f;
        [SerializeField] private float orbitSpeed = 0.25f;
        [SerializeField] private float zoomStep = 2f;
        [SerializeField] private Vector2 distanceRange = new Vector2(5f, 40f);
        [SerializeField] private Vector2 pitchRange = new Vector2(20f, 80f);
        [Tooltip("Higher = snappier. 0 disables smoothing.")]
        [SerializeField] private float smoothing = 12f;

        private Vector3 focus;
        private float yaw;
        private float pitch;
        private float distance;

        private Vector3 targetFocus;
        private float targetYaw;
        private float targetPitch;
        private float targetDistance;

        private void OnEnable()
        {
            input.PanDragged += OnPanDragged;
            input.PanKeyed += OnPanKeyed;
            input.OrbitDragged += OnOrbitDragged;
            input.Zoomed += OnZoomed;
        }

        private void OnDisable()
        {
            input.PanDragged -= OnPanDragged;
            input.PanKeyed -= OnPanKeyed;
            input.OrbitDragged -= OnOrbitDragged;
            input.Zoomed -= OnZoomed;
        }

        private void Start()
        {
            var ground = new Plane(Vector3.up, Vector3.zero);
            var ray = new Ray(transform.position, transform.forward);
            distance = ground.Raycast(ray, out float hit) ? hit : 15f;
            focus = ray.GetPoint(distance);
            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;

            targetFocus = focus;
            targetYaw = yaw;
            targetPitch = pitch;
            targetDistance = distance;
        }

        private void OnPanDragged(Vector2 delta)
        {
            Move(-delta.x * dragPanSpeed * targetDistance, -delta.y * dragPanSpeed * targetDistance);
        }

        private void OnPanKeyed(Vector2 dir)
        {
            float step = keyPanSpeed * targetDistance * Time.unscaledDeltaTime;
            Move(dir.x * step, dir.y * step);
        }

        private void OnOrbitDragged(Vector2 delta)
        {
            targetYaw += delta.x * orbitSpeed;
            targetPitch = Mathf.Clamp(targetPitch - delta.y * orbitSpeed, pitchRange.x, pitchRange.y);
        }

        private void OnZoomed(float direction)
        {
            targetDistance = Mathf.Clamp(targetDistance - direction * zoomStep, distanceRange.x, distanceRange.y);
        }

        public void Orbit(float yawDegrees) => targetYaw += yawDegrees;

        private void Move(float right, float forward)
        {
            targetFocus += Quaternion.Euler(0f, targetYaw, 0f) * new Vector3(right, 0f, forward);
        }

        private void LateUpdate()
        {
            float t = smoothing <= 0f ? 1f : 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);

            focus = Vector3.Lerp(focus, targetFocus, t);
            yaw = Mathf.Lerp(yaw, targetYaw, t);
            pitch = Mathf.Lerp(pitch, targetPitch, t);
            distance = Mathf.Lerp(distance, targetDistance, t);

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }
    }
}
