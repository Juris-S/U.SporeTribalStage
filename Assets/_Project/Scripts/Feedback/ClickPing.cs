using PrimeTween;
using UnityEngine;

namespace SporeTribal.Feedback
{
    public class ClickPing : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField, ColorUsage(true, true)] private Color color = new Color(1f, 0.95f, 0.6f, 1f);
        [SerializeField] private float duration = 0.45f;
        [SerializeField] private float startSize = 0.5f;
        [SerializeField] private float endSize = 2.2f;
        [SerializeField] private float lift = 0.03f;

        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static MaterialPropertyBlock block;

        public void Play(Vector3 point)
        {
            block ??= new MaterialPropertyBlock();
            GameObject ping = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(ping.GetComponent<Collider>());
            ping.name = "ClickPing";
            ping.transform.SetPositionAndRotation(point + Vector3.up * lift, Quaternion.Euler(90f, 0f, 0f));
            ping.transform.localScale = Vector3.one * startSize;

            MeshRenderer ring = ping.GetComponent<MeshRenderer>();
            ring.sharedMaterial = material;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            block.SetColor(RimColorId, color);
            ring.SetPropertyBlock(block);

            Tween.Scale(ping.transform, startSize, endSize, duration, Ease.OutCubic);
            Tween.Custom(ring, color, new TweenSettings<float>(0f, 1f, duration, Ease.Linear), static (r, c, k) =>
            {
                c.a *= 1f - k * k;
                block.SetColor(RimColorId, c);
                r.SetPropertyBlock(block);
            }).OnComplete(ping, static p => Destroy(p));
        }
    }
}
