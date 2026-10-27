using UnityEngine;

namespace SporeTribal.Villagers
{
    public sealed class VillagerVisualRig : MonoBehaviour
    {
        private Transform root;
        private Transform model;
        private float modelScale = 1f;
        private float feet;

        public float Tilt { get; set; }

        public void Init(Transform root, float feet)
        {
            this.root = root;
            this.feet = feet;
            Follow();
        }

        public void SetModel(Transform model, float scale)
        {
            this.model = model;
            modelScale = scale;
            Follow();
        }

        public void Follow()
        {
            transform.SetPositionAndRotation(root.TransformPoint(0f, feet, 0f), root.rotation * Quaternion.Euler(0f, 0f, Tilt));
            transform.localScale = root.lossyScale;
            if (model == null) return;
            Vector3 s = transform.localScale;
            model.localScale = new Vector3(modelScale / s.x, modelScale / s.y, modelScale / s.z);
        }

        private void LateUpdate() => Follow();
    }
}
