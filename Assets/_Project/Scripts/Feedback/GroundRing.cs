using UnityEngine;
using UnityEngine.Rendering;

namespace SporeTribal.Feedback
{
    public static class GroundRing
    {
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly Quaternion FlatOnGround = Quaternion.Euler(90f, 0f, 0f);

        private static MaterialPropertyBlock block;

        public static MeshRenderer Create(string name, Material material, Color rim, Transform parent = null)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.name = name;
            ring.transform.SetParent(parent, false);
            ring.transform.rotation = FlatOnGround;

            MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            SetRim(renderer, rim);
            return renderer;
        }

        public static void SetRim(Renderer renderer, Color rim)
        {
            block ??= new MaterialPropertyBlock();
            block.SetColor(RimColorId, rim);
            renderer.SetPropertyBlock(block);
        }
    }
}
