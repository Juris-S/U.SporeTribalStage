using UnityEngine;
using UnityEngine.Rendering;

namespace SporeTribal.Rendering
{
    [ExecuteAlways]
    public class DistanceFog : MonoBehaviour
    {
        private static readonly int CenterId = Shader.PropertyToID("_DistanceFogCenter");
        private static readonly int ParamsId = Shader.PropertyToID("_DistanceFogParams");
        private static readonly int ColorId = Shader.PropertyToID("_DistanceFogColor");

        [SerializeField] private Color color = new Color(0.72f, 0.80f, 0.88f);
        [Tooltip("Ground distance from the look-at point that stays completely clear.")]
        [SerializeField] private float clearRadius = 45f;
        [Tooltip("Ground distance where the fog reaches full strength.")]
        [SerializeField] private float fullRadius = 120f;
        [Range(0f, 1f)]
        [SerializeField] private float maxOpacity = 0.9f;
        [SerializeField] private float groundHeight;

        private void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Shader.SetGlobalVector(ParamsId, Vector4.zero);
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            Shader.SetGlobalVector(CenterId, WorldBend.GroundPivot(cam, groundHeight));
            Shader.SetGlobalVector(ParamsId, new Vector4(clearRadius, Mathf.Max(fullRadius, clearRadius + 0.01f), maxOpacity, 0f));
            Shader.SetGlobalColor(ColorId, color);
        }
    }
}
