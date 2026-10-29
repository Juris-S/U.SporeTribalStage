using UnityEngine;

namespace SporeTribal.CameraControl
{
    [CreateAssetMenu(menuName = "Tribal/Camera Profile", fileName = "CameraProfile")]
    public class CameraProfile : ScriptableObject
    {
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

        [Header("Bounds")]
        [Tooltip("The focus point cannot leave this circle around the world origin (metres). Keep it inside the island shore.")]
        [SerializeField] private float maxFocusRadius = 140f;

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

        [Header("Auto orbit")]
        [Tooltip("Degrees per second when the orbit is first switched on.")]
        [SerializeField] private float orbitStartSpeed = 8f;
        [SerializeField] private float orbitMaxSpeed = 60f;
        [Tooltip("Degrees per second gained for every second an arrow key is held.")]
        [SerializeField] private float orbitSpeedRamp = 20f;

        public float FieldOfView => fieldOfView;
        public float NearClip => nearClip;
        public float FarClip => farClip;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public float ZoomStep => zoomStep;
        public Vector2 PitchRange => pitchRange;
        public float BasePanSpeed => basePanSpeed;
        public float ReferenceDistance => referenceDistance;
        public float DragPanSpeed => dragPanSpeed;
        public float OrbitSpeed => orbitSpeed;
        public float MaxFocusRadius => maxFocusRadius;
        public bool EdgeScroll => edgeScroll;
        public float EdgeMarginPixels => edgeMarginPixels;
        public float PanAccelTime => panAccelTime;
        public float VelocityDecayTime => velocityDecayTime;
        public float AnchorSmoothTime => anchorSmoothTime;
        public float ZoomSmoothTime => zoomSmoothTime;
        public float AzimuthSmoothTime => azimuthSmoothTime;
        public float OrbitStartSpeed => orbitStartSpeed;
        public float OrbitMaxSpeed => orbitMaxSpeed;
        public float OrbitSpeedRamp => orbitSpeedRamp;
    }
}
