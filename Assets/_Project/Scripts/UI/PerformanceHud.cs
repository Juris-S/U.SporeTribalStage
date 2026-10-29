using PrimeTween;
using SporeTribal.AI;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Inputs;
using SporeTribal.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class PerformanceHud : MonoBehaviour
    {
        [SerializeField] private SocialPerformanceDirector director;
        [SerializeField] private RTSInputController input;
        [SerializeField] private float heightAboveCaller = 3f;
        [SerializeField] private float bannerSeconds = 4f;
        [SerializeField] private float meterSeconds = 0.45f;
        [SerializeField] private bool hintsStartVisible;
        [Tooltip("Tint of an instrument's silhouette while it cannot be played.")]
        [SerializeField] private Color idleTint = new Color(1f, 1f, 1f, 0.35f);

        private static readonly InstrumentKind[] Kinds = { InstrumentKind.Horn, InstrumentKind.Maraca, InstrumentKind.Didgeridoo };

        private struct Slot
        {
            public InstrumentKind Kind;
            public Button Button;
            public VisualElement Icon;
            public Label Count;
        }

        private readonly Slot[] slots = new Slot[3];
        private System.Func<Vector2, bool> previousBlocker;
        private VisualElement root;
        private VisualElement tray;
        private VisualElement meterFill;
        private VisualElement balloon;
        private VisualElement balloonIcon;
        private Label relationship;
        private Tween meterTween;
        private float shownProgress;
        private float meterTarget = -1f;
        private float flashUntil;
        private bool flashGood;
        private int shownRelationship = int.MinValue;
        private RelationshipTier shownTier;
        private bool shownSoothed;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            root.pickingMode = PickingMode.Ignore;
            root.Q("perf-root").pickingMode = PickingMode.Ignore;
            relationship = root.Q<Label>("perf-relationship");
            tray = root.Q("perf-tray");
            meterFill = root.Q("perf-meter-fill");
            balloon = root.Q("perf-balloon");
            balloonIcon = root.Q("perf-balloon-icon");
            relationship.pickingMode = balloon.pickingMode = PickingMode.Ignore;
            relationship.style.display = hintsStartVisible ? DisplayStyle.Flex : DisplayStyle.None;
            BuildButtons(root.Q("perf-buttons"));

            director.Ended += OnEnded;
            director.Answered += OnAnswered;
            director.Started += OnStarted;
            GameInput.Controls.Interface.ToggleHints.performed += OnToggleHints;
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
            GameInput.Controls.Interface.ToggleHints.performed -= OnToggleHints;
            if (input != null && input.PointerBlocked == Blocked) input.PointerBlocked = previousBlocker;
        }

        private void BuildButtons(VisualElement parent)
        {
            parent.Clear();
            for (int i = 0; i < Kinds.Length; i++)
            {
                InstrumentKind kind = Kinds[i];
                var button = new Button(() => director.Submit(kind));
                button.AddToClassList("perf__button");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("perf__button-icon");
                var key = new Label(GameInput.InstrumentAction(kind).GetBindingDisplayString()) { pickingMode = PickingMode.Ignore };
                key.AddToClassList("perf__button-key");
                var count = new Label { pickingMode = PickingMode.Ignore };
                count.AddToClassList("perf__button-count");
                button.Add(icon);
                button.Add(key);
                button.Add(count);
                parent.Add(button);
                slots[i] = new Slot { Kind = kind, Button = button, Icon = icon, Count = count };
            }
        }

        private bool Blocked(Vector2 screen)
        {
            if (root != null && root.panel != null && director.IsActive)
            {
                Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screen.x, Screen.height - screen.y));
                if (tray.worldBound.Contains(panelPoint)) return true;
            }
            return previousBlocker != null && previousBlocker(screen);
        }

        private void OnToggleHints(InputAction.CallbackContext _)
        {
            if (relationship == null) return;
            bool hidden = relationship.resolvedStyle.display == DisplayStyle.None;
            relationship.style.display = hidden ? DisplayStyle.Flex : DisplayStyle.None;
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
                    Show($"The {target.ColouredName} tribe is now your ally!");
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
            Toasts.Show(text, bannerSeconds);
        }

        private void Update()
        {
            if (root == null) return;
            UpdateRelationship();

            bool active = director.IsActive;
            tray.EnableInClassList("perf__tray--visible", active);
            if (!active)
            {
                balloon.EnableInClassList("perf__balloon--visible", false);
                meterTween.Stop();
                meterTarget = -1f;
                return;
            }
            UpdateTray();
            UpdateBalloon();
        }

        private void UpdateRelationship()
        {
            Village rival = director.Rival;
            if (rival == null) return;
            RelationshipTier tier = rival.TierTowards(director.Village);
            int value = rival.RelationshipTo(director.Village);
            bool soothed = rival.IsSoothed;
            if (value == shownRelationship && tier == shownTier && soothed == shownSoothed) return;
            shownRelationship = value;
            shownTier = tier;
            shownSoothed = soothed;
            relationship.text = $"Rival: {tier} ({value}){(soothed ? " (soothed)" : string.Empty)}   G gift {director.SmallGift}, Shift+G gift {director.LargeGift}, H soothing song";
        }

        private void UpdateTray()
        {
            float target = director.ProjectedProgress;
            if (!Mathf.Approximately(target, meterTarget))
            {
                if (meterTween.isAlive) meterTween.Stop();
                else if (meterTarget < 0f) shownProgress = target;
                meterTarget = target;
                bool playing = director.Current == SocialPerformanceDirector.State.Playing;
                meterTween = Tween.Custom(this, shownProgress, target, playing ? director.PlayRemaining : meterSeconds,
                    static (hud, v) => hud.SetMeter(v), playing ? Ease.Linear : Ease.OutCubic);
            }
            bool flashing = Time.time < flashUntil;
            meterFill.EnableInClassList("perf__meter-fill--good", flashing && flashGood);
            meterFill.EnableInClassList("perf__meter-fill--bad", flashing && !flashGood);

            bool ready = director.Current == SocialPerformanceDirector.State.Calling;
            foreach (Slot slot in slots)
            {
                int count = director.MatchingCount(slot.Kind);
                bool held = count > 0;
                slot.Button.style.display = held ? DisplayStyle.Flex : DisplayStyle.None;
                if (!held) continue;

                slot.Button.SetEnabled(ready);
                slot.Button.EnableInClassList("perf__button--ready", ready);
                slot.Count.text = count > 1 ? $"x{count}" : string.Empty;

                ToolDefinition tool = director.ToolFor(slot.Kind);
                if (tool == null) continue;
                slot.Icon.style.backgroundImage = new StyleBackground(ready ? tool.Icon : tool.SilhouetteIcon);
                slot.Icon.style.unityBackgroundImageTintColor = ready ? Color.white : idleTint;
            }
        }

        private void SetMeter(float value)
        {
            shownProgress = value;
            meterFill.style.width = Length.Percent(value * 100f);
        }

        private void UpdateBalloon()
        {
            bool calling = director.Current == SocialPerformanceDirector.State.Calling && director.Caller != null;
            Vector3 world = calling ? director.Caller.transform.position + Vector3.up * heightAboveCaller : Vector3.zero;
            if (!calling || ScreenRaycast.WorldToScreen(world).z <= 0f)
            {
                balloon.EnableInClassList("perf__balloon--visible", false);
                return;
            }
            Vector2 panelPoint = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, world, ScreenRaycast.Camera);
            balloon.style.left = panelPoint.x - balloon.resolvedStyle.width * 0.5f;
            balloon.style.top = panelPoint.y - balloon.resolvedStyle.height;
            balloon.EnableInClassList("perf__balloon--visible", true);

            ToolDefinition tool = director.ToolFor(director.Demanded);
            balloonIcon.style.backgroundImage = new StyleBackground(tool != null ? tool.Icon : null);
        }
    }
}
