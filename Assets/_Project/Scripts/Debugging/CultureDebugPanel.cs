using System.Text;
using SporeTribal.CameraControl;
using SporeTribal.Culture;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.Debugging
{
    public class CultureDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool startVisible;
        [SerializeField] private CameraAutoOrbit orbit;

        [Header("Keys")]
        [SerializeField] private Key toggleOverlayKey = Key.F9;
        [Tooltip("Cycles the subject: each tribe in turn, then all of them.")]
        [SerializeField] private Key cycleSubjectKey = Key.Tab;
        [Tooltip("Triggers each tribe's activity at the same index (Activities list on its IdleActivityDirector).")]
        [SerializeField] private Key[] activityKeys = { Key.F5, Key.F6 };
        [SerializeField] private Key aggressiveKey = Key.F7;
        [SerializeField] private Key friendlyKey = Key.F8;
        [SerializeField] private float shiftStep = 0.25f;

        private const float StatusSeconds = 3f;

        private readonly StringBuilder text = new StringBuilder();
        private GUIStyle style;
        private bool visible;
        private int subject = -1;
        private string status;
        private float statusUntil;

        private void Awake() => visible = startVisible;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[toggleOverlayKey].wasPressedThisFrame) visible = !visible;
            if (keyboard[cycleSubjectKey].wasPressedThisFrame) subject = subject + 1 >= TribeCulture.All.Count ? -1 : subject + 1;
            if (keyboard[aggressiveKey].wasPressedThisFrame) ForSubjects(c => c.Shift(-shiftStep), "more aggressive");
            if (keyboard[friendlyKey].wasPressedThisFrame) ForSubjects(c => c.Shift(shiftStep), "more friendly");
            for (int i = 0; i < activityKeys.Length; i++)
                if (keyboard[activityKeys[i]].wasPressedThisFrame) Trigger(i);
        }

        private void Trigger(int index)
        {
            ForSubjects(culture =>
            {
                var director = culture.GetComponent<IdleActivityDirector>();
                if (director == null || index >= director.Activities.Count) return;
                string label = director.Activities[index].Label;
                Report(director.TryStart(director.Activities[index])
                    ? $"{culture.name}: {label}"
                    : $"{culture.name}: {label} not started: {director.LastRefusal}");
            }, null);
        }

        private void ForSubjects(System.Action<TribeCulture> action, string describe)
        {
            var cultures = TribeCulture.All;
            for (int i = 0; i < cultures.Count; i++)
            {
                if (subject >= 0 && subject != i) continue;
                action(cultures[i]);
                if (describe != null) Report($"{cultures[i].name}: {describe}");
            }
        }

        private void Report(string message)
        {
            status = message;
            statusUntil = Time.unscaledTime + StatusSeconds;
            Debug.Log($"[Culture] {message}");
        }

        private void OnGUI()
        {
            if (!visible) return;
            style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, richText = true, padding = new RectOffset(10, 10, 8, 8) };

            text.Clear();
            var cultures = TribeCulture.All;
            text.Append("<b>Culture</b>  subject: ")
                .Append(subject < 0 ? "All" : cultures[Mathf.Min(subject, cultures.Count - 1)].name)
                .Append($"  [{cycleSubjectKey}]\n");
            foreach (TribeCulture culture in cultures)
            {
                var director = culture.GetComponent<IdleActivityDirector>();
                text.Append(culture.name).Append(":  ").Append(culture.Alignment.ToString("+0.00;-0.00;0.00"))
                    .Append("  ").Append(culture.Temper);
                if (director != null && director.Current != null) text.Append("  <i>").Append(director.Current.Label).Append("</i>");
                text.Append('\n');
            }
            text.Append($"[{aggressiveKey}] aggressive  [{friendlyKey}] friendly  [{toggleOverlayKey}] hide\n");
            for (int i = 0; i < activityKeys.Length; i++)
                text.Append($"[{activityKeys[i]}] {ActivityLabel(i)}  ");
            if (orbit != null)
                text.Append($"\n[{orbit.ToggleKey}] orbit {(orbit.IsOrbiting ? orbit.Speed.ToString("0") + " deg/s  (Left/Right)" : "off")}");
            if (Time.unscaledTime < statusUntil) text.Append("\n<color=#ffd24a>").Append(status).Append("</color>");

            Vector2 size = style.CalcSize(new GUIContent(text.ToString()));
            GUI.Box(new Rect(12f, 12f, size.x, size.y), text.ToString(), style);
        }

        private static string ActivityLabel(int index)
        {
            foreach (TribeCulture culture in TribeCulture.All)
            {
                var director = culture.GetComponent<IdleActivityDirector>();
                if (director != null && index < director.Activities.Count) return director.Activities[index].Label;
            }
            return "-";
        }
    }
}
