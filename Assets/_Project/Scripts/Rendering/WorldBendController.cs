using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using SporeTribal.Inputs;
using SporeTribal.Utilities;

namespace SporeTribal.Rendering
{
    [ExecuteAlways]
    [DefaultExecutionOrder(100)]
    public class WorldBendController : MonoBehaviour
    {
        [Tooltip("Master switch. Turn off for flat footage; also toggled by the hotkey in play mode.")]
        [SerializeField] private bool bendEnabled = true;
        [Tooltip("Off keeps the Game and Scene views flat while editing; the bend only applies in play mode.")]
        [SerializeField] private bool bendInEditMode;
        [Tooltip("Off keeps the Scene view flat so it is easy to place things, even while playing.")]
        [SerializeField] private bool bendInSceneView;
        [Tooltip("Spore uses 500. Smaller = stronger curve.")]
        [SerializeField] private float planetRadius = 500f;
        [SerializeField] private float groundHeight;
        [Tooltip("Half-size in view space of the widened culling box. Must cover everything that can bend into view.")]
        [SerializeField] private float cullingExtent = 150f;

        public bool BendEnabled
        {
            get => bendEnabled;
            set => bendEnabled = value;
        }

        private float Amount => 1f / (2f * Mathf.Max(planetRadius, 1f));

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            if (Application.isPlaying) GameInput.Controls.DevTools.ToggleWorldBend.performed += OnToggle;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            if (Application.isPlaying) GameInput.Controls.DevTools.ToggleWorldBend.performed -= OnToggle;

            Shader.SetGlobalFloat(WorldBend.AmountId, 0f);
            WorldBend.SetState(0f, Vector3.zero);
            WorldBend.NotifyCamera(0f, Vector3.zero);
        }

        private void OnToggle(InputAction.CallbackContext _) => bendEnabled = !bendEnabled;

        private void LateUpdate()
        {
            Camera cam = ScreenRaycast.Camera;
            if (cam != null && ShouldBend(cam)) WorldBend.SetState(Amount, WorldBend.GroundPivot(cam, groundHeight));
            else WorldBend.SetState(0f, Vector3.zero);
        }

        private bool ShouldBend(Camera cam)
        {
            if (!bendEnabled) return false;

            switch (cam.cameraType)
            {
                case CameraType.Game: return Application.isPlaying || bendInEditMode;
                case CameraType.SceneView: return (Application.isPlaying || bendInEditMode) && bendInSceneView;
                default: return false;
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (!ShouldBend(cam))
            {
                Shader.SetGlobalFloat(WorldBend.AmountId, 0f);
                WorldBend.NotifyCamera(0f, Vector3.zero);
                return;
            }

            Vector3 pivot = WorldBend.GroundPivot(cam, groundHeight);
            Shader.SetGlobalFloat(WorldBend.AmountId, Amount);
            Shader.SetGlobalVector(WorldBend.PivotId, pivot);
            WorldBend.NotifyCamera(Amount, pivot);
            cam.cullingMatrix = Matrix4x4.Ortho(-cullingExtent, cullingExtent, -cullingExtent, cullingExtent,
                cam.nearClipPlane, cam.farClipPlane) * cam.worldToCameraMatrix;
        }

        private static void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            cam.ResetCullingMatrix();
        }
    }
}
