using SporeTribal.AI;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Inputs;
using SporeTribal.Utilities;
using UnityEngine;

namespace SporeTribal.UI
{
    public class PerformanceHud : MonoBehaviour
    {
        [SerializeField] private SocialPerformanceDirector director;
        [SerializeField] private RTSInputController input;
        [SerializeField] private float heightAboveCaller = 3f;
        [SerializeField] private float bannerSeconds = 4f;
        [SerializeField] private Color meterColour = new Color(0.95f, 0.75f, 0.2f);

        private static readonly Vector2 MeterSize = new Vector2(320f, 20f);
        private static readonly Vector2 ButtonSize = new Vector2(104f, 52f);
        private static readonly Vector2 BalloonSize = new Vector2(64f, 64f);
        private static readonly InstrumentKind[] Kinds = { InstrumentKind.Horn, InstrumentKind.Maraca, InstrumentKind.Didgeridoo };
        private const float Margin = 12f;

        private System.Func<Vector2, bool> previousBlocker;
        private Rect panel;
        private string banner;
        private float bannerUntil;
        private float flashUntil;
        private bool flashGood;
        private GUIStyle label;

        private void OnEnable()
        {
            director.Ended += OnEnded;
            director.Answered += OnAnswered;
            director.Started += OnStarted;
        }

        private void Start()
        {
            previousBlocker = input.PointerBlocked;
            input.PointerBlocked = Blocked;
        }

        private void OnDisable()
        {
            director.Ended -= OnEnded;
            director.Answered -= OnAnswered;
            director.Started -= OnStarted;
            if (input != null && input.PointerBlocked == Blocked) input.PointerBlocked = previousBlocker;
        }

        private bool Blocked(Vector2 screen)
        {
            if (director.IsActive && panel.Contains(new Vector2(screen.x, Screen.height - screen.y))) return true;
            return previousBlocker != null && previousBlocker(screen);
        }

        private void OnStarted(Village target) => Show("The band heads out to perform.");
        private void OnAnswered(bool good, InstrumentKind kind)
        {
            flashGood = good;
            flashUntil = Time.time + 0.4f;
        }

        private void OnEnded(SocialPerformanceDirector.Outcome outcome, Village target)
        {
            switch (outcome)
            {
                case SocialPerformanceDirector.Outcome.Allied:
                    Show("The rival tribe is now your ally!");
                    break;
                case SocialPerformanceDirector.Outcome.Pleased:
                    Show("The crowd loved it. They like you more.");
                    break;
                case SocialPerformanceDirector.Outcome.Disappointed:
                    Show("The crowd leaves disappointed.");
                    break;
                default:
                    Show("The performance was interrupted.");
                    break;
            }
        }

        private void Show(string text)
        {
            banner = text;
            bannerUntil = Time.time + bannerSeconds;
        }

        private void OnGUI()
        {
            if (label == null) label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };

            DrawBanner();
            DrawRelationship();
            if (!director.IsActive) { panel = Rect.zero; return; }

            DrawTray();
            DrawBalloon();
        }

        private void DrawBanner()
        {
            if (banner == null || Time.time > bannerUntil) return;
            GUI.Box(new Rect((Screen.width - 460f) * 0.5f, 64f, 460f, 36f), banner);
        }

        private void DrawRelationship()
        {
            Village rival = director.Rival;
            if (rival == null) return;
            RelationshipTier tier = rival.TierTowards(director.Village);
            string soothed = rival.IsSoothed ? " (soothed)" : string.Empty;
            string text = $"Rival: {tier} ({rival.RelationshipTo(director.Village)}){soothed}   G gift {director.SmallGift}, Shift+G gift {director.LargeGift}, H soothing song";
            GUI.Label(new Rect(Margin, Screen.height - 28f, 640f, 24f), text);
        }

        private void DrawTray()
        {
            float width = ButtonSize.x * Kinds.Length + 8f * (Kinds.Length - 1);
            panel = new Rect((Screen.width - width) * 0.5f, Screen.height - ButtonSize.y - MeterSize.y - 48f, width, ButtonSize.y + MeterSize.y + 12f);

            var meter = new Rect((Screen.width - MeterSize.x) * 0.5f, panel.y, MeterSize.x, MeterSize.y);
            GUI.DrawTexture(meter, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, 0.6f), 0f, 0f);
            Color fill = Time.time < flashUntil ? (flashGood ? Color.green : Color.red) : meterColour;
            GUI.DrawTexture(new Rect(meter.x + 2f, meter.y + 2f, (meter.width - 4f) * director.Progress, meter.height - 4f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, fill, 0f, 0f);

            for (int i = 0; i < Kinds.Length; i++)
            {
                InstrumentKind kind = Kinds[i];
                int count = director.MatchingCount(kind);
                var rect = new Rect(panel.x + i * (ButtonSize.x + 8f), meter.yMax + 12f, ButtonSize.x, ButtonSize.y);
                bool was = GUI.enabled;
                GUI.enabled = count > 0;
                if (GUI.Button(rect, $"{i + 1}  {kind}\nx{count}")) director.Submit(kind);
                GUI.enabled = was;
            }
        }

        private void DrawBalloon()
        {
            if (director.Current != SocialPerformanceDirector.State.Calling || director.Caller == null) return;
            Vector3 screen = ScreenRaycast.Camera.WorldToScreenPoint(director.Caller.transform.position + Vector3.up * heightAboveCaller);
            if (screen.z <= 0f) return;

            var box = new Rect(screen.x - BalloonSize.x * 0.5f, Screen.height - screen.y - BalloonSize.y, BalloonSize.x, BalloonSize.y);
            GUI.Box(box, GUIContent.none);
            ToolDefinition tool = director.ToolFor(director.Demanded);
            Sprite icon = tool != null ? tool.BalloonIcon : null;
            if (icon != null)
            {
                Rect uv = icon.textureRect;
                var normalised = new Rect(uv.x / icon.texture.width, uv.y / icon.texture.height, uv.width / icon.texture.width, uv.height / icon.texture.height);
                GUI.DrawTextureWithTexCoords(new Rect(box.x + 6f, box.y + 6f, box.width - 12f, box.height - 12f), icon.texture, normalised);
            }
            else GUI.Label(box, director.Demanded.ToString(), label);

            var bar = new Rect(box.x, box.yMax + 2f, box.width, 8f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, 0.6f), 0f, 0f);
            float left = director.CallRemaining01;
            GUI.DrawTexture(new Rect(bar.x + 1f, bar.y + 1f, (bar.width - 2f) * left, bar.height - 2f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.Lerp(Color.red, Color.green, left), 0f, 0f);
        }
    }
}
