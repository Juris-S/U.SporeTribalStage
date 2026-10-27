using UnityEngine;

namespace SporeTribal.Villagers
{
    public sealed class VillagerVisualRig : MonoBehaviour
    {
        private Transform root;
        private float feet;

        public float Tilt { get; set; }

        public void Init(Transform root, float feet)
        {
            this.root = root;
            this.feet = feet;
            Follow();
        }

        public void Follow()
        {
            transform.SetPositionAndRotation(root.TransformPoint(0f, feet, 0f), root.rotation * Quaternion.Euler(0f, 0f, Tilt));
            transform.localScale = root.lossyScale;
        }

        private void LateUpdate() => Follow();
    }
}
