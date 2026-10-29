using UnityEngine;
using UnityEngine.InputSystem;
using SporeTribal.Inputs;

namespace SporeTribal.CameraControl
{
    [RequireComponent(typeof(Camera))]
    public class RTSCameraController : MonoBehaviour
    {
        private const float GroundProbeHeight = 100f;

        [SerializeField] private RTSInputController input;
        [SerializeField] private CameraProfile profile;

        public Vector3 Focus => focus;
        public float Zoom01 => Mathf.InverseLerp(profile.MinDistance, profile.MaxDistance, distance);

        private Camera cam;
        private int groundMask;

        private Vector3 focus;
        private float yaw;
        private float distance;

        private Vector3 targetFocus;
        private float targetYaw;
        private float targetDistance;

        private float glideSmoothTime;
        private Vector3 focusVelocity;
        private float yawVelocity;
        private float distanceVelocity;

        private Vector2 keyInput;
        private Vector3 panVelocity;
        private Vector3 panVelocitySmoothing;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            groundMask = LayerMask.GetMask("Ground");
            cam.fieldOfView = profile.FieldOfView;
            cam.nearClipPlane = profile.NearClip;
            cam.farClipPlane = profile.FarClip;
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
            float startDistance = ground.Raycast(ray, out float hit) ? hit : profile.ReferenceDistance;
            distance = Mathf.Clamp(startDistance, profile.MinDistance, profile.MaxDistance);
            focus = ray.GetPoint(distance);
            focus.y = GroundHeight(focus);
            yaw = transform.eulerAngles.y;

            targetFocus = focus;
            targetYaw = yaw;
            targetDistance = distance;
        }

        private void OnPanDragged(Vector2 delta)
        {
            glideSmoothTime = 0f;
            float scale = profile.DragPanSpeed * targetDistance;
            Move(-delta.x * scale, -delta.y * scale);
        }

        private void OnPanKeyed(Vector2 dir)
        {
            keyInput = dir;
            glideSmoothTime = 0f;
        }

        private void OnOrbitDragged(Vector2 delta) => targetYaw += delta.x * profile.OrbitSpeed;

        private void OnZoomed(float direction)
        {
            targetDistance = Mathf.Clamp(targetDistance * (1f - direction * profile.ZoomStep), profile.MinDistance, profile.MaxDistance);
        }

        public void Orbit(float yawDegrees) => targetYaw += yawDegrees;

        public void GlideTo(Vector3 worldPoint, float smoothTime = 0.35f)
        {
            FocusOn(worldPoint);
            glideSmoothTime = smoothTime;
        }

        public void FocusOn(Vector3 worldPoint) => targetFocus = new Vector3(worldPoint.x, 0f, worldPoint.z);

        private void Move(float right, float forward)
        {
            targetFocus += Quaternion.Euler(0f, targetYaw, 0f) * new Vector3(right, 0f, forward);
        }

        private Vector2 ReadEdgeScroll()
        {
            Mouse mouse = Mouse.current;
            if (!profile.EdgeScroll || GameInput.IsLocked || mouse == null || !Application.isFocused) return Vector2.zero;

            Vector2 p = mouse.position.ReadValue();
            if (p.x < 0f || p.y < 0f || p.x > Screen.width || p.y > Screen.height) return Vector2.zero;

            float margin = profile.EdgeMarginPixels;
            var dir = Vector2.zero;
            if (p.x <= margin) dir.x -= 1f;
            if (p.x >= Screen.width - margin) dir.x += 1f;
            if (p.y <= margin) dir.y -= 1f;
            if (p.y >= Screen.height - margin) dir.y += 1f;
            return dir;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            UpdatePan(dt);
            ClampToIsland();
            targetFocus.y = GroundHeight(targetFocus);

            if ((focus - targetFocus).sqrMagnitude < 0.04f) glideSmoothTime = 0f;
            focus = Vector3.SmoothDamp(focus, targetFocus, ref focusVelocity, Mathf.Max(profile.AnchorSmoothTime, glideSmoothTime), Mathf.Infinity, dt);
            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, profile.AzimuthSmoothTime, Mathf.Infinity, dt);
            distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVelocity, profile.ZoomSmoothTime, Mathf.Infinity, dt);

            float zoom01 = Mathf.InverseLerp(profile.MinDistance, profile.MaxDistance, distance);
            float pitch = Mathf.Lerp(profile.PitchRange.x, profile.PitchRange.y, zoom01);
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }

        private void ClampToIsland()
        {
            var flat = new Vector2(targetFocus.x, targetFocus.z);
            float max = profile.MaxFocusRadius;
            if (flat.sqrMagnitude <= max * max) return;

            flat = flat.normalized * max;
            targetFocus.x = flat.x;
            targetFocus.z = flat.y;
        }

        private float GroundHeight(Vector3 at)
        {
            var probe = new Ray(new Vector3(at.x, GroundProbeHeight, at.z), Vector3.down);
            return Physics.Raycast(probe, out RaycastHit hit, GroundProbeHeight * 2f, groundMask) ? hit.point.y : 0f;
        }

        private void UpdatePan(float dt)
        {
            Vector2 pan = keyInput + ReadEdgeScroll();
            if (pan.sqrMagnitude > 1f) pan.Normalize();
            keyInput = Vector2.zero;

            float speed = profile.BasePanSpeed * Mathf.Max(0.5f, targetDistance / profile.ReferenceDistance);
            Vector3 desired = Quaternion.Euler(0f, targetYaw, 0f) * new Vector3(pan.x, 0f, pan.y) * speed;
            float smoothTime = pan == Vector2.zero ? profile.VelocityDecayTime : profile.PanAccelTime;
            panVelocity = Vector3.SmoothDamp(panVelocity, desired, ref panVelocitySmoothing, smoothTime, Mathf.Infinity, dt);
            targetFocus += panVelocity * dt;
        }
    }
}
