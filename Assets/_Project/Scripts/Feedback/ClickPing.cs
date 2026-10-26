using System.Collections;
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

        public void Play(Vector3 point) => StartCoroutine(Animate(point));

        private IEnumerator Animate(Vector3 point)
        {
            GameObject ping = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(ping.GetComponent<Collider>());
            ping.name = "ClickPing";
            ping.transform.SetPositionAndRotation(point + Vector3.up * lift, Quaternion.Euler(90f, 0f, 0f));

            MeshRenderer ring = ping.GetComponent<MeshRenderer>();
            ring.sharedMaterial = material;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float eased = 1f - (1f - k) * (1f - k) * (1f - k);
                ping.transform.localScale = Vector3.one * Mathf.Lerp(startSize, endSize, eased);

                Color c = color;
                c.a *= 1f - k * k;
                block.SetColor(RimColorId, c);
                ring.SetPropertyBlock(block);
                yield return null;
            }

            Destroy(ping);
        }
    }
}
