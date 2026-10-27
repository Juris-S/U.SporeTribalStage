using UnityEngine;
using UnityEngine.InputSystem;
using SporeTribal.Inputs;

namespace SporeTribal.CameraControl
{
    [RequireComponent(typeof(Camera))]
    public class RTSCameraController : MonoBehaviour
    {
        [SerializeField] private RTSInputController input;

        [Header("Optics")]
        [SerializeField] private float fieldOfView = 60f;
        [SerializeField] private float nearClip = 0.1f;
        [SerializeField] private float farClip = 1000f;

        [Header("Zoom and pitch")]
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 70f;
        [Tooltip("Fraction of the current distance covered by one scroll notch.")]
        [SerializeField, Range(0.02f, 0.5f)] private float zoomStep = 0.12f;
        [Tooltip("Pitch at minDistance (x) and maxDistance (y); lerped in between.")]
        [SerializeField] private Vector2 pitchRange = new Vector2(25f, 65f);

        [Header("Panning")]
        [Tooltip("Key pan speed (m/s) at referenceDistance; scales linearly with distance, never below half.")]
        [SerializeField] private float basePanSpeed = 15f;
        [SerializeField] private float referenceDistance = 15f;
        [SerializeField] private float dragPanSpeed = 0.0025f;
        [SerializeField] private float orbitSpeed = 0.25f;

        [Header("Edge scroll")]
        [SerializeField] private bool edgeScroll = true;
        [SerializeField] private float edgeMarginPixels = 16f;

        [Header("Smoothing (seconds)")]
        [Tooltip("Time to reach full pan speed once a key is held.")]
        [SerializeField] private float panAccelTime = 0.1f;
        [Tooltip("Time for the pan to coast to a stop after release.")]
        [SerializeField] private float velocityDecayTime = 0.2f;
        [SerializeField] private float anchorSmoothTime = 0.05f;
        [SerializeField] private float zoomSmoothTime = 0.08f;
        [SerializeField] private float azimuthSmoothTime = 0.05f;

        public Vector3 Focus => focus;

        private Camera cam;

        private Vector3 focus;
        private float yaw;
        private float distance;

        private Vector3 targetFocus;
        private float targetYaw;
        private float targetDistance;

        private Vector3 focusVelocity;
        private float yawVelocity;
        private float distanceVelocity;

        private Vector2 keyInput;
        private Vector3 panVelocity;
        private Vector3 panVelocitySmoothing;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.fieldOfView = fieldOfView;
            cam.nearClipPlane = nearClip;
            cam.farClipPlane = farClip;
        }

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
            distance = Mathf.Clamp(ground.Raycast(ray, out float hit) ? hit : referenceDistance, minDistance, maxDistance);
            focus = ray.GetPoint(distance);
            yaw = transform.eulerAngles.y;

            targetFocus = focus;
            targetYaw = yaw;
            targetDistance = distance;
        }

        private void OnPanDragged(Vector2 delta)
        {
            Move(-delta.x * dragPanSpeed * targetDistance, -delta.y * dragPanSpeed * targetDistance);
        }

        private void OnPanKeyed(Vector2 dir) => keyInput = dir;

        private void OnOrbitDragged(Vector2 delta) => targetYaw += delta.x * orbitSpeed;

        private void OnZoomed(float direction)
        {
            targetDistance = Mathf.Clamp(targetDistance * (1f - direction * zoomStep), minDistance, maxDistance);
        }

        public void Orbit(float yawDegrees) => targetYaw += yawDegrees;

        public void FocusOn(Vector3 worldPoint) => targetFocus = new Vector3(worldPoint.x, 0f, worldPoint.z);

        private void Move(float right, float forward)
        {
            targetFocus += Quaternion.Euler(0f, targetYaw, 0f) * new Vector3(right, 0f, forward);
        }

        private Vector2 ReadEdgeScroll()
        {
            Mouse mouse = Mouse.current;
            if (!edgeScroll || mouse == null || !Application.isFocused) return Vector2.zero;

            Vector2 p = mouse.position.ReadValue();
            if (p.x < 0f || p.y < 0f || p.x > Screen.width || p.y > Screen.height) return Vector2.zero;

            var dir = Vector2.zero;
            if (p.x <= edgeMarginPixels) dir.x -= 1f;
            if (p.x >= Screen.width - edgeMarginPixels) dir.x += 1f;
            if (p.y <= edgeMarginPixels) dir.y -= 1f;
            if (p.y >= Screen.height - edgeMarginPixels) dir.y += 1f;
            return dir;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            Vector2 pan = keyInput + ReadEdgeScroll();
            if (pan.sqrMagnitude > 1f) pan.Normalize();
            keyInput = Vector2.zero;

            float speed = basePanSpeed * Mathf.Max(0.5f, targetDistance / referenceDistance);
            Vector3 desired = Quaternion.Euler(0f, targetYaw, 0f) * new Vector3(pan.x, 0f, pan.y) * speed;
            float smoothTime = pan == Vector2.zero ? velocityDecayTime : panAccelTime;
            panVelocity = Vector3.SmoothDamp(panVelocity, desired, ref panVelocitySmoothing, smoothTime, Mathf.Infinity, dt);
            targetFocus += panVelocity * dt;

            focus = Vector3.SmoothDamp(focus, targetFocus, ref focusVelocity, anchorSmoothTime, Mathf.Infinity, dt);
            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, azimuthSmoothTime, Mathf.Infinity, dt);
            distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVelocity, zoomSmoothTime, Mathf.Infinity, dt);

            float pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, Mathf.InverseLerp(minDistance, maxDistance, distance));
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }
    }
}
