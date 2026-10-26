using UnityEngine;

namespace SporeTribal.Inputs
{
    public class SelectionBoxView : MonoBehaviour
    {
        [SerializeField] private RTSInputController input;
        [SerializeField] private Color fill = new Color(0.95f, 0.85f, 0.4f, 0.12f);
        [SerializeField] private Color border = new Color(0.95f, 0.85f, 0.4f, 1f);
        [SerializeField] private float borderThickness = 2f;

        private void OnGUI()
        {
            if (!input.IsDragging) return;

            Rect r = input.DragRect;
            var box = new Rect(r.xMin, Screen.height - r.yMax, r.width, r.height);
            float t = borderThickness;

            Draw(box, fill);
            Draw(new Rect(box.xMin, box.yMin, box.width, t), border);
            Draw(new Rect(box.xMin, box.yMax - t, box.width, t), border);
            Draw(new Rect(box.xMin, box.yMin, t, box.height), border);
            Draw(new Rect(box.xMax - t, box.yMin, t, box.height), border);
        }

        private static void Draw(Rect rect, Color colour)
        {
            GUI.color = colour;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
