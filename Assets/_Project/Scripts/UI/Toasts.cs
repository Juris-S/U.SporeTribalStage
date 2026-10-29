using System.Collections.Generic;
using SporeTribal.Audio;
using UnityEngine;

namespace SporeTribal.UI
{
    public class Toasts : MonoBehaviour
    {
        private const int FontSize = 22;
        private const float TopFraction = 0.12f;
        private const float Height = 50f;
        private const float Gap = 6f;
        private const float BackdropPadding = 200f;

        private struct Toast
        {
            public string Text;
            public float Start;
            public float Until;
        }

        private static Toasts instance;

        private readonly List<Toast> toasts = new List<Toast>();
        private GUIStyle style;

        public static void Show(string text, float seconds = 5f, SfxId sound = SfxId.None)
        {
            if (instance == null) instance = new GameObject("Toasts").AddComponent<Toasts>();
            float now = Time.unscaledTime;
            instance.toasts.Add(new Toast { Text = text, Start = now, Until = now + seconds });
            if (sound != SfxId.None) AudioManager.PlayUi(sound);
        }

        private void OnGUI()
        {
            float now = Time.unscaledTime;
            toasts.RemoveAll(toast => now >= toast.Until);
            if (toasts.Count == 0) return;
            style ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = FontSize, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

            float y = Screen.height * TopFraction;
            foreach (Toast toast in toasts)
            {
                var rect = new Rect(0f, y, Screen.width, Height);
                float spread = style.CalcSize(new GUIContent(toast.Text)).x * 0.5f + BackdropPadding;
                ToastBackdrop.Begin(rect, now - toast.Start, toast.Until - now);
                ToastBackdrop.Draw(new Rect(Screen.width * 0.5f - spread, y, spread * 2f, Height));
                GUI.Label(rect, toast.Text, style);
                ToastBackdrop.End();
                y += Height + Gap;
            }
        }
    }
}
