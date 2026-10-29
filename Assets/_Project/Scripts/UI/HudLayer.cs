using System.Collections.Generic;
using PrimeTween;
using SporeTribal.Audio;
using SporeTribal.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    public static class HudLayer
    {
        private const int BackdropWidth = 80;
        private const int BackdropEdge = 32;
        private const float BackdropOpacity = 0.7f;
        private const float PopFromScale = 0.85f;

        private static FoodHud host;
        private static VisualElement layer;
        private static VisualElement toastStack;
        private static Texture2D backdrop;
        private static readonly List<VisualElement> blockers = new List<VisualElement>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            host = null;
            layer = null;
            toastStack = null;
            backdrop = null;
            blockers.Clear();
        }

        public static VisualElement Root
        {
            get
            {
                if (layer != null && layer.panel != null) return layer;
                if (host == null) host = Object.FindAnyObjectByType<FoodHud>();
                if (host == null) return null;

                VisualElement root = host.GetComponent<UIDocument>().rootVisualElement;
                VisualElement container = root?.Q("food-hud")?.parent;
                if (container == null) return null;

                layer = new VisualElement { name = "hud-layer", pickingMode = PickingMode.Ignore };
                layer.AddToClassList("hud-layer");
                if (host.HudStyle != null) layer.styleSheets.Add(host.HudStyle);
                root.Add(layer);

                toastStack = new VisualElement { name = "toast-stack", pickingMode = PickingMode.Ignore };
                toastStack.AddToClassList("toast-stack");
                layer.Add(toastStack);
                return layer;
            }
        }

        public static VisualElement ToastStack => Root != null ? toastStack : null;

        public static void ApplyBackdrop(VisualElement element)
        {
            backdrop ??= BuildBackdrop();
            element.AddToClassList("hud-backdrop");
            element.style.backgroundImage = new StyleBackground(backdrop);
        }

        public static Button MakeButton(string text, System.Action onClick, params string[] classes)
        {
            var button = new Button(() =>
            {
                AudioManager.PlayUi(SfxId.ButtonClick);
                onClick();
            }) { text = text };
            button.AddToClassList("hud-button");
            foreach (string c in classes) button.AddToClassList(c);
            return button;
        }

        public static void AddBlocker(VisualElement element)
        {
            if (!blockers.Contains(element)) blockers.Add(element);
        }

        public static void RemoveBlocker(VisualElement element) => blockers.Remove(element);

        public static bool IsOverBlocker(Vector2 screen)
        {
            foreach (VisualElement element in blockers)
            {
                if (element.panel == null || element.resolvedStyle.display == DisplayStyle.None) continue;
                Vector2 point = RuntimePanelUtils.ScreenToPanel(element.panel, new Vector2(screen.x, Screen.height - screen.y));
                if (element.worldBound.Contains(point)) return true;
            }
            return false;
        }

        public static Label MakeLabel(string text, params string[] classes)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("hud-label");
            foreach (string c in classes) label.AddToClassList(c);
            return label;
        }

        public static bool TryWorldToPanel(Vector3 world, VisualElement within, out Vector2 point)
        {
            Vector3 screen = ScreenRaycast.WorldToScreen(world);
            point = within.panel != null ? RuntimePanelUtils.ScreenToPanel(within.panel, new Vector2(screen.x, Screen.height - screen.y)) : default;
            return screen.z > 0f && within.panel != null;
        }

        public static void PopIn(VisualElement element, float seconds = 0.25f, float from = PopFromScale, float delay = 0f)
        {
            Tween.StopAll(element);
            element.style.display = DisplayStyle.Flex;
            Tween.Custom(element, from, 1f, seconds, static (el, s) => el.style.scale = new Scale(new Vector2(s, s)), Ease.OutBack, startDelay: delay, useUnscaledTime: true)
                .OnComplete(element, static el => el.style.scale = StyleKeyword.Null);
            Tween.Custom(element, 0f, 1f, seconds * 0.4f, static (el, a) => el.style.opacity = a, Ease.OutQuad, startDelay: delay, useUnscaledTime: true);
        }

        public static void FadeOut(VisualElement element, float seconds = 0.12f)
        {
            Tween.StopAll(element);
            Tween.Custom(element, element.resolvedStyle.opacity, 0f, seconds, static (el, a) => el.style.opacity = a, Ease.Linear, useUnscaledTime: true)
                .OnComplete(element, static el => el.style.display = DisplayStyle.None);
        }

        public static void Punch(VisualElement element, float amount = 0.12f, float seconds = 0.25f)
        {
            Tween.StopAll(element);
            Tween.Custom(element, 1f + amount, 1f, seconds, static (el, s) => el.style.scale = new Scale(new Vector2(s, s)), Ease.OutBack, useUnscaledTime: true)
                .OnComplete(element, static el => el.style.scale = StyleKeyword.Null);
        }

        public static void Shake(VisualElement element)
        {
            Tween.StopAll(element);
            Tween.Custom(element, 0f, 1f, 0.3f, static (el, t) => el.style.translate = new Translate(Mathf.Sin(t * Mathf.PI * 6f) * (1f - t) * 8f, 0f), Ease.Linear, useUnscaledTime: true)
                .OnComplete(element, static el => el.style.translate = StyleKeyword.Null);
        }

        private static Texture2D BuildBackdrop()
        {
            var texture = new Texture2D(BackdropWidth, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            for (int x = 0; x < BackdropWidth; x++)
            {
                float edge = Mathf.Min(x + 0.5f, BackdropWidth - x - 0.5f) / BackdropEdge;
                texture.SetPixel(x, 0, new Color(0f, 0f, 0f, BackdropOpacity * Mathf.SmoothStep(0f, 1f, edge)));
            }
            texture.Apply();
            return texture;
        }
    }
}
