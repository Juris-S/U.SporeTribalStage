using SporeTribal.Buildings;
using SporeTribal.Combat;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.UI
{
    public class HealthBars : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(44f, 6f);
        [SerializeField] private float heightAboveUnit = 2.4f;
        [Tooltip("Seconds a bar stays visible after the last hit (selected units always show theirs while hurt).")]
        [SerializeField] private float showSeconds = 4f;
        [Tooltip("Damaged buildings always show their bar, this much wider than a villager's.")]
        [SerializeField] private float buildingWidthScale = 2f;

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            Camera cam = ScreenRaycast.Camera;
            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                Health health = unit.Health;
                if (health.Current >= health.Max) continue;
                if (!unit.IsSelected && Time.time - health.LastHitTime > showSeconds) continue;
                Draw(cam, unit.transform.position + Vector3.up * heightAboveUnit, size, health.Fraction);
            }

            var buildingSize = new Vector2(size.x * buildingWidthScale, size.y);
            foreach (Building building in Building.All)
            {
                Health health = building.Health;
                if (!building.IsStanding || health.Current >= health.Max) continue;
                Bounds bounds = building.GetComponentInChildren<Collider>() is Collider c ? c.bounds : new Bounds(building.transform.position, Vector3.one);
                Draw(cam, new Vector3(bounds.center.x, bounds.max.y + 0.5f, bounds.center.z), buildingSize, health.Fraction);
            }
        }

        private static void Draw(Camera cam, Vector3 world, Vector2 size, float fraction)
        {
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f) return;

            var back = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y, size.x, size.y);
            GUI.DrawTexture(back, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, 0.6f), 0f, 0f);
            var fill = new Rect(back.x + 1f, back.y + 1f, (back.width - 2f) * fraction, back.height - 2f);
            GUI.DrawTexture(fill, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.Lerp(Color.red, Color.green, fraction), 0f, 0f);
        }
    }
}
