using PrimeTween;
using SporeTribal.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    public static class Toasts
    {
        private const float PopSeconds = 0.4f;
        private const float FadeSeconds = 0.5f;
        private const float DropPixels = 28f;
        private const float StartScale = 0.85f;

        public static void Show(string text, float seconds = 5f, SfxId sound = SfxId.None)
        {
            VisualElement stack = HudLayer.ToastStack;
            if (stack == null) return;

            var toast = new VisualElement { pickingMode = PickingMode.Ignore };
            toast.AddToClassList("toast");
            HudLayer.ApplyBackdrop(toast);
            Label label = HudLayer.MakeLabel(text, "toast__text");
            label.enableRichText = true;
            toast.Add(label);
            toast.style.opacity = 0f;
            stack.Add(toast);

            float hold = Mathf.Max(0f, seconds - PopSeconds - FadeSeconds);
            Sequence.Create(useUnscaledTime: true)
                .Chain(Tween.Custom(toast, 0f, 1f, PopSeconds, static (el, pop) => Pop(el, pop), Ease.OutBack))
                .Group(Tween.Custom(toast, 0f, 1f, PopSeconds / 3f, static (el, a) => el.style.opacity = a))
                .ChainDelay(hold)
                .Chain(Tween.Custom(toast, 1f, 0f, FadeSeconds, static (el, a) => el.style.opacity = a))
                .OnComplete(toast, static el => el.RemoveFromHierarchy());

            if (sound != SfxId.None) AudioManager.PlayUi(sound);
        }

        private static void Pop(VisualElement toast, float pop)
        {
            toast.style.translate = new Translate(0f, -(1f - pop) * DropPixels);
            float s = Mathf.LerpUnclamped(StartScale, 1f, pop);
            toast.style.scale = new Scale(new Vector2(s, s));
        }
    }
}
