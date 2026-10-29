using System.Collections.Generic;
using SporeTribal.Economy;
using UnityEngine;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    public class ProgressionBar : MonoBehaviour
    {
        private const float FillSpeed = 0.4f;
        private static readonly Color Unknown = new Color(0.42f, 0.42f, 0.44f);

        private TribeProgressionDirector progression;
        private VisualElement root;
        private VisualElement fill;
        private VisualElement markerTrack;
        private readonly List<VisualElement> markers = new List<VisualElement>();
        private readonly List<VisualElement> beads = new List<VisualElement>();
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
                TribeProgressionDirector.RivalStatus status = progression.StatusOf(i);
                VisualElement marker = markers[i];
                beads[i].style.backgroundColor = status.Resolved ? status.Colour : Unknown;
                marker.EnableInClassList("progress-marker--done", status.Resolved);
                if (status.Resolved == resolvedShown[i]) continue;
                resolvedShown[i] = status.Resolved;
                if (status.Resolved) HudLayer.Punch(marker, 0.6f);
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
            for (int i = 0; i < total; i++)
            {
                var marker = new VisualElement { pickingMode = PickingMode.Ignore };
                marker.AddToClassList("progress-marker");
                marker.style.left = Length.Percent((i + 1f) / total * 100f);
                var bead = new VisualElement { pickingMode = PickingMode.Ignore };
                bead.AddToClassList("progress-marker__bead");
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
