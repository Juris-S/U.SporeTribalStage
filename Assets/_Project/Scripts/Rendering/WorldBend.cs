using UnityEngine;

namespace SporeTribal.Rendering
{
    public static class WorldBend
    {
        public static readonly int AmountId = Shader.PropertyToID("_WorldBendAmount");
        public static readonly int PivotId = Shader.PropertyToID("_WorldBendPivot");

        public static float Amount { get; private set; }
        public static Vector3 Pivot { get; private set; }
        public static bool IsActive => Amount > 0f;

        public static void SetState(float amount, Vector3 pivot)
        {
            Amount = amount;
            Pivot = pivot;
        }

        public static Vector3 Apply(Vector3 flat)
        {
            flat.y -= Drop(flat);
            return flat;
        }

        public static Vector3 Remove(Vector3 bent)
        {
            bent.y += Drop(bent);
            return bent;
        }

        public static Vector3 GroundPivot(Camera cam, float height)
        {
            var ground = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (ground.Raycast(ray, out float hit)) return ray.GetPoint(hit);

            Vector3 p = cam.transform.position;
            return new Vector3(p.x, height, p.z);
        }

        private static float Drop(Vector3 world)
        {
            float dx = world.x - Pivot.x;
            float dz = world.z - Pivot.z;
            return (dx * dx + dz * dz) * Amount;
        }
    }
}
