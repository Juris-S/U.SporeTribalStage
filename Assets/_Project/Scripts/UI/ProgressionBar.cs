using System.Collections.Generic;
using SporeTribal.Economy;
using UnityEngine;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    public class ProgressionBar : MonoBehaviour
    {
        private const float FillSpeed = 0.25f;
        private const float ReachEpsilon = 0.01f;
        private const float PunchAmount = 0.18f;
        private const float PunchSeconds = 0.6f;
        private static readonly string[] IconNames = { "tent", "fire", "flag", "tower", "home" };
        private static readonly Color Unknown = new Color(0.42f, 0.42f, 0.44f);

        private TribeProgressionDirector progression;
        private VisualElement root;
        private VisualElement fill;
        private VisualElement markerTrack;
        private readonly List<VisualElement> markers = new List<VisualElement>();
        private readonly List<VisualElement> beads = new List<VisualElement>();
        private Color[] resolvedColours = System.Array.Empty<Color>();
        private bool[] recorded = System.Array.Empty<bool>();
        private int slot;
        private readonly List<bool> resolvedShown = new List<bool>();
        private float shown;

        private void OnDisable()
        {
            if (root == null) return;
            root.RemoveFromHierarchy();
            root = null;
        }

        private void Update()
        {
            if (progression == null) progression = FindAnyObjectByType<TribeProgressionDirector>();
            if (progression == null || progression.RivalCount == 0 || !EnsureUi()) return;

            int total = progression.RivalCount;
            float target = (float)progression.Resolved / total;
            shown = Mathf.MoveTowards(shown, target, FillSpeed * Time.unscaledDeltaTime);
            fill.style.width = Length.Percent(shown * 100f);

            for (int i = 0; i < total; i++)
            {
                if (recorded[i]) continue;
                TribeProgressionDirector.RivalStatus status = progression.StatusOf(i);
                if (!status.Resolved) continue;
                recorded[i] = true;
                resolvedColours[slot++] = status.Colour;
            }

            for (int i = 0; i < total; i++)
            {
                VisualElement marker = markers[i];
                bool revealed = i < slot && shown >= (i + 1f) / total - ReachEpsilon;
                beads[i].style.backgroundColor = revealed ? resolvedColours[i] : Unknown;
                marker.EnableInClassList("progress-marker--done", revealed);
                if (revealed == resolvedShown[i]) continue;
                resolvedShown[i] = revealed;
                if (revealed) HudLayer.Punch(marker, PunchAmount, PunchSeconds);
            }
        }

        private bool EnsureUi()
        {
            VisualElement layer = HudLayer.Root;
            if (layer == null) return false;
            if (root != null && root.panel != null) return true;

            root = new VisualElement { name = "progression-bar", pickingMode = PickingMode.Ignore };
            root.AddToClassList("progress");
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.AddToClassList("progress__track");
            fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("progress__fill");
            track.Add(fill);
            markerTrack = new VisualElement { pickingMode = PickingMode.Ignore };
            markerTrack.AddToClassList("progress__markers");
            track.Add(markerTrack);
            root.Add(track);
            layer.Add(root);

            markers.Clear();
            beads.Clear();
            resolvedShown.Clear();
            int total = progression.RivalCount;
            resolvedColours = new Color[total];
            recorded = new bool[total];
            slot = 0;
            for (int i = 0; i < total; i++)
            {
                var marker = new VisualElement { pickingMode = PickingMode.Ignore };
                marker.AddToClassList("progress-marker");
                marker.style.left = Length.Percent((i + 1f) / total * 100f);
                var bead = new VisualElement { pickingMode = PickingMode.Ignore };
                bead.AddToClassList("progress-marker__bead");
                var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
                glyph.AddToClassList("progress-marker__glyph");
                Texture2D icon = Resources.Load<Texture2D>("HudIcons/" + IconNames[i % IconNames.Length]);
                if (icon != null) glyph.style.backgroundImage = new StyleBackground(icon);
                bead.Add(glyph);
                marker.Add(bead);
                markerTrack.Add(marker);
                markers.Add(marker);
                beads.Add(bead);
                resolvedShown.Add(false);
            }
            shown = 0f;
            return true;
        }
    }
}
