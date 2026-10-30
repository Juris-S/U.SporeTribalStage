using System;
using System.Collections.Generic;
using System.Text;
using SporeTribal.CameraControl;
using SporeTribal.Culture;
using SporeTribal.Economy;
using SporeTribal.Inputs;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace SporeTribal.Debugging
{
    public class CultureDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool startVisible;
        [SerializeField] private CameraAutoOrbit orbit;
        [SerializeField] private float shiftStep = 0.25f;

        [Header("Health FX (selected villagers)")]
        [SerializeField] private int hitDamage = 5;

        private const float StatusSeconds = 3f;
        private const float ColourRowSpacing = 2f;
        private const float SkinSpread = 0.382f;
        private const float ColourRowSearch = 4f;

        private readonly StringBuilder text = new StringBuilder();
        private readonly List<VillagerUnit> selected = new List<VillagerUnit>();
        private GUIStyle style;
        private bool visible;
        private int subject = -1;
        private string status;
        private float statusUntil;

        private static TribalControls.DevToolsActions Actions => GameInput.Controls.DevTools;

        private InputAction[] activityActions;

        private void Awake()
        {
            visible = startVisible;
            activityActions = new[] { Actions.CultureActivity1, Actions.CultureActivity2 };
        }

        private void OnEnable()
        {
            Actions.ToggleCultureOverlay.performed += OnToggleOverlay;
            Actions.CycleCultureSubject.performed += OnCycleSubject;
            Actions.ShiftAggressive.performed += OnShiftAggressive;
            Actions.ShiftFriendly.performed += OnShiftFriendly;
            Actions.CultureActivity1.performed += OnActivity1;
            Actions.CultureActivity2.performed += OnActivity2;
            Actions.HitSelected.performed += OnHitSelected;
            Actions.KillSelected.performed += OnKillSelected;
        }

        private void OnDisable()
        {
            Actions.ToggleCultureOverlay.performed -= OnToggleOverlay;
            Actions.CycleCultureSubject.performed -= OnCycleSubject;
            Actions.ShiftAggressive.performed -= OnShiftAggressive;
            Actions.ShiftFriendly.performed -= OnShiftFriendly;
            Actions.CultureActivity1.performed -= OnActivity1;
            Actions.CultureActivity2.performed -= OnActivity2;
            Actions.HitSelected.performed -= OnHitSelected;
            Actions.KillSelected.performed -= OnKillSelected;
        }

        private void OnToggleOverlay(InputAction.CallbackContext _) => visible = !visible;
        private void OnCycleSubject(InputAction.CallbackContext _) => subject = subject + 1 >= TribeCulture.All.Count ? -1 : subject + 1;
        private void OnShiftAggressive(InputAction.CallbackContext _) => ForSubjects(c => c.Shift(-shiftStep), "more aggressive");
        private void OnShiftFriendly(InputAction.CallbackContext _) => ForSubjects(c => c.Shift(shiftStep), "more friendly");
        private void OnActivity1(InputAction.CallbackContext _) => Trigger(0);
        private void OnActivity2(InputAction.CallbackContext _) => Trigger(1);
        private void OnHitSelected(InputAction.CallbackContext _) => DamageSelected(hitDamage);
        private void OnKillSelected(InputAction.CallbackContext _) => DamageSelected(int.MaxValue);

        private void ForceEnd(bool win, bool ally)
        {
            var match = FindAnyObjectByType<MatchDirector>();
            if (match == null) { Report("No MatchDirector component in the scene: add one and assign the player Village"); return; }
            if (match.IsOver) { Report("Match already over"); return; }
            if (!win) { match.Player.Surrender(); return; }
            foreach (Village v in FindObjectsByType<Village>())
            {
                if (v == match.Player) continue;
                if (ally) v.DeclareAlliance(match.Player); else v.Surrender();
            }
        }

        private void SpawnColourRow()
        {
            var director = FindAnyObjectByType<TribeProgressionDirector>();
            var match = FindAnyObjectByType<MatchDirector>();
            if (director == null || match == null) { Report("Needs a TribeProgressionDirector and a MatchDirector in the scene"); return; }
            VillagerSpawner spawner = null;
            foreach (VillagerSpawner candidate in FindObjectsByType<VillagerSpawner>())
                if (candidate.Village == match.Player) { spawner = candidate; break; }
            if (spawner == null) { Report("No spawner for the player village"); return; }

            Camera camera = Camera.main;
            if (camera == null || !Physics.Raycast(camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out RaycastHit view, float.MaxValue, LayerMask.GetMask("Ground")))
            {
                Report("Screen centre is not over the ground");
                return;
            }

            var palette = director.Palette;
            float skinStart = UnityEngine.Random.value;
            Vector3 origin = view.point;
            for (int i = 0; i < palette.Count; i++)
            {
                Vector3 at = origin + new Vector3((i - (palette.Count - 1) * 0.5f) * ColourRowSpacing, 0f, 0f);
                if (NavMesh.SamplePosition(at, out NavMeshHit ground, ColourRowSearch, NavMesh.AllAreas)) at = ground.position;
                spawner.SpawnAt(at).PreviewTribeColour(palette[i].colour, Mathf.Repeat(skinStart + i * SkinSpread, 1f));
            }
            Report($"Spawned {palette.Count} villagers, one per tribe colour");
        }

        private void DamageSelected(int amount)
        {
            selected.Clear();
            foreach (VillagerUnit unit in VillagerUnit.All)
                if (unit.IsSelected && unit.Health.IsAlive) selected.Add(unit);
            foreach (VillagerUnit unit in selected) unit.Health.Damage(amount, null);

            string verb = amount == int.MaxValue ? "Killed" : "Hit";
            Report(selected.Count == 0 ? "No selected villager" : $"{verb} {selected.Count} selected");
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

        private void ForSubjects(Action<TribeCulture> action, string describe)
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

        private static string KeyOf(InputAction action) => action.GetBindingDisplayString();

        private void OnGUI()
        {
            if (!visible) return;
            style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, richText = true, padding = new RectOffset(10, 10, 8, 8) };

            BuildOverlayText();
            string overlay = text.ToString();
            Vector2 size = style.CalcSize(new GUIContent(overlay));
            GUI.Box(new Rect(12f, 12f, size.x, size.y), overlay, style);
            DrawMatchButtons(12f + size.y + 6f);
        }

        private void BuildOverlayText()
        {
            text.Clear();
            var cultures = TribeCulture.All;
            text.Append("<b>Culture</b>  subject: ")
                .Append(subject < 0 ? "All" : cultures[Mathf.Min(subject, cultures.Count - 1)].name)
                .Append($"  [{KeyOf(Actions.CycleCultureSubject)}]\n");
            foreach (TribeCulture culture in cultures)
            {
                var director = culture.GetComponent<IdleActivityDirector>();
                text.Append(culture.name).Append(":  ").Append(culture.Alignment.ToString("+0.00;-0.00;0.00"))
                    .Append("  ").Append(culture.Temper);
                if (director != null && director.Current != null) text.Append("  <i>").Append(director.Current.Label).Append("</i>");
                text.Append('\n');
            }
            text.Append($"[{KeyOf(Actions.ShiftAggressive)}] aggressive  [{KeyOf(Actions.ShiftFriendly)}] friendly  [{KeyOf(Actions.ToggleCultureOverlay)}] hide\n");
            for (int i = 0; i < activityActions.Length; i++)
                text.Append($"[{KeyOf(activityActions[i])}] {ActivityLabel(i)}  ");
            text.Append($"\n[{KeyOf(Actions.HitSelected)}] hit selected  [{KeyOf(Actions.KillSelected)}] kill selected");
            if (orbit != null)
                text.Append($"\n[{KeyOf(GameInput.Controls.Camera.ToggleAutoOrbit)}] orbit {(orbit.IsOrbiting ? orbit.Speed.ToString("0") + " deg/s  (Left/Right)" : "off")}");
            if (Time.unscaledTime < statusUntil) text.Append("\n<color=#ffd24a>").Append(status).Append("</color>");
        }

        private void DrawMatchButtons(float y)
        {
            if (GUI.Button(new Rect(12f, y, 110f, 26f), "Win: conquest")) ForceEnd(true, false);
            if (GUI.Button(new Rect(126f, y, 110f, 26f), "Win: alliance")) ForceEnd(true, true);
            if (GUI.Button(new Rect(240f, y, 110f, 26f), "Lose")) ForceEnd(false, false);
            if (GUI.Button(new Rect(12f, y + 30f, 170f, 26f), "Spawn all tribe colours")) SpawnColourRow();
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
