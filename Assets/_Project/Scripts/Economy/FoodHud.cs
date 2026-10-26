using UnityEngine;
using UnityEngine.UIElements;

namespace SporeTribal.Economy
{
    [ExecuteAlways, RequireComponent(typeof(UIDocument))]
    public class FoodHud : MonoBehaviour
    {
        [SerializeField] private Village village;
        [SerializeField] private Texture2D icon;
        [SerializeField] private Color gradientStart = new Color(1f, 0.35f, 0.4f, 0.75f);
        [SerializeField] private Color gradientEnd = new Color(1f, 0.35f, 0.4f, 0f);

        private Label count;
        private Texture2D gradient;
        private int shown = -1;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            count = root.Q<Label>("food-count");
            root.Q("food-bar").style.backgroundImage = new StyleBackground(BuildGradient());
            root.Q("food-icon").style.backgroundImage = new StyleBackground(icon);
            root.pickingMode = PickingMode.Ignore;
            shown = -1;
        }

        private void OnDisable()
        {
            if (gradient != null) DestroyImmediate(gradient);
        }

        private void Update()
        {
            if (count == null) return;
            int food = village != null ? village.Food : 0;
            if (food == shown) return;
            shown = food;
            count.text = shown.ToString();
        }

        private Texture2D BuildGradient()
        {
            const int width = 64;
            if (gradient != null) DestroyImmediate(gradient);
            gradient = new Texture2D(width, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            for (int x = 0; x < width; x++) gradient.SetPixel(x, 0, Color.Lerp(gradientStart, gradientEnd, x / (width - 1f)));
            gradient.Apply();
            return gradient;
        }
    }
}
