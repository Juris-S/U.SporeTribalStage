using PrimeTween;
using SporeTribal.Economy;
using UnityEngine;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    [ExecuteAlways, RequireComponent(typeof(UIDocument))]
    public class FoodHud : MonoBehaviour
    {
        private const float FlashSeconds = 0.45f;
        private const float PunchScale = 0.35f;

        [SerializeField] private Village village;
        [SerializeField] private Texture2D icon;
        [Tooltip("Shared HUD stylesheet (Hud.uss), applied to everything HudLayer adds.")]
        [SerializeField] private StyleSheet hudStyle;
        [SerializeField] private Color gainColour = new Color(0.55f, 1f, 0.55f);
        [SerializeField] private Color spendColour = new Color(1f, 0.45f, 0.45f);

        private Label count;
        private VisualElement iconElement;
        private Tween flash;
        private Color flashFrom;
        private int shown = -1;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            count = root.Q<Label>("food-count");
            iconElement = root.Q("food-icon");
            HudLayer.ApplyBackdrop(root.Q("food-bar"));
            iconElement.style.backgroundImage = new StyleBackground(icon);
            root.pickingMode = PickingMode.Ignore;
            shown = -1;
        }

        public StyleSheet HudStyle => hudStyle;

        private void OnDisable() => flash.Stop();

        private void Update()
        {
            if (count == null) return;
            int food = village != null ? village.Food : 0;
            if (food == shown) return;
            bool changed = shown >= 0 && Application.isPlaying;
            bool gained = food > shown;
            shown = food;
            count.text = shown.ToString();
            if (changed) Flash(gained);
        }

        private void Flash(bool gained)
        {
            flash.Stop();
            flashFrom = gained ? gainColour : spendColour;
            flash = Tween.Custom(this, 0f, 1f, FlashSeconds, static (hud, t) => hud.ApplyFlash(t), Ease.OutQuad);
        }

        private void ApplyFlash(float t)
        {
            count.style.color = Color.Lerp(flashFrom, Color.white, t);
            float s = 1f + PunchScale * (1f - t) * (1f - t);
            count.style.scale = new Scale(new Vector2(s, s));
            float iconScale = 1f + PunchScale * 0.5f * (1f - t) * (1f - t);
            iconElement.style.scale = new Scale(new Vector2(iconScale, iconScale));
        }
    }
}
