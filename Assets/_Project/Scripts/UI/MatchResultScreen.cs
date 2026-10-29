using System;
using PrimeTween;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    public static class MatchResultScreen
    {
        public static void Show(bool won, string title, string subtitle, Action onPlayAgain)
        {
            VisualElement layer = HudLayer.Root;
            if (layer == null) return;

            var screen = new VisualElement { pickingMode = PickingMode.Position };
            screen.AddToClassList("result");

            Label titleLabel = HudLayer.MakeLabel(title, "result__title", won ? "result__title--win" : "result__title--lose");
            Label subtitleLabel = HudLayer.MakeLabel(subtitle, "result__subtitle");
            var play = new Button(onPlayAgain) { text = "Play again" };
            play.AddToClassList("hud-button");
            play.AddToClassList("result__button");

            screen.Add(titleLabel);
            screen.Add(subtitleLabel);
            screen.Add(play);
            layer.Add(screen);

            Tween.Custom(screen, 0f, 1f, 0.4f, static (el, a) => el.style.opacity = a, Ease.OutQuad, useUnscaledTime: true);
            HudLayer.PopIn(titleLabel, 0.55f, 1.6f, 0.15f);
            HudLayer.PopIn(subtitleLabel, 0.4f, 0.9f, 0.55f);
            HudLayer.PopIn(play, 0.4f, 0.8f, 0.85f);
        }
    }
}
