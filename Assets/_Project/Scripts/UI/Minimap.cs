using SporeTribal.CameraControl;
using SporeTribal.Economy;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    public class Minimap : MonoBehaviour
    {
        private const int TextureSize = 256;
        private const float MinRadius = 20f;
        private const float MaxRadius = 140f;
        private const float StartRadius = 45f;
        private const float ZoomStep = 1.4f;
        private const float ZoomSharpness = 10f;
        private const float RefreshSeconds = 0.1f;
        private const float CameraHeight = 200f;

        private Camera mapCamera;
        private RenderTexture texture;
        private RTSCameraController rig;
        private Village village;
        private VisualElement root;
        private VisualElement image;
        private VisualElement dots;
        private readonly System.Collections.Generic.List<VisualElement> dotPool = new System.Collections.Generic.List<VisualElement>();
        private VisualElement[] blockers = System.Array.Empty<VisualElement>();
        private Label count;
        private float radius = StartRadius;
        private float targetRadius = StartRadius;
        private float nextRefresh;
        private int shown = -1;

        private void OnDisable()
        {
            if (root != null)
            {
                foreach (VisualElement blocker in blockers) HudLayer.RemoveBlocker(blocker);
                root.RemoveFromHierarchy();
                root = null;
            }
            if (mapCamera != null) Destroy(mapCamera.gameObject);
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
        }

        private void Update()
        {
            if (!Resolve() || !EnsureUi()) return;

            int food = village.Food;
            if (food != shown)
            {
                if (shown >= 0) HudLayer.Punch(count, 0.25f);
                shown = food;
                count.text = food.ToString();
            }

            if (!Mathf.Approximately(radius, targetRadius))
            {
                float eased = Mathf.Lerp(Mathf.Log(radius), Mathf.Log(targetRadius), 1f - Mathf.Exp(-ZoomSharpness * Time.unscaledDeltaTime));
                radius = Mathf.Abs(eased - Mathf.Log(targetRadius)) < 0.001f ? targetRadius : Mathf.Exp(eased);
                nextRefresh = 0f;
            }

            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            Render();
        }

        private bool Resolve()
        {
            if (rig == null) rig = FindAnyObjectByType<RTSCameraController>();
            if (village == null)
            {
                MatchDirector match = FindAnyObjectByType<MatchDirector>();
                village = match != null ? match.Player : null;
            }
            return rig != null && village != null;
        }

        private bool EnsureUi()
        {
            VisualElement layer = HudLayer.Root;
            if (layer == null) return false;
            if (root != null && root.panel != null) return true;

            if (texture == null)
            {
                texture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32) { name = "Minimap", antiAliasing = 1, filterMode = FilterMode.Bilinear };
                var go = new GameObject("Minimap Camera") { hideFlags = HideFlags.HideAndDontSave };
                mapCamera = go.AddComponent<Camera>();
                mapCamera.enabled = false;
                mapCamera.orthographic = true;
                mapCamera.clearFlags = CameraClearFlags.SolidColor;
                mapCamera.backgroundColor = new Color(0.24f, 0.43f, 0.59f);
                mapCamera.allowHDR = false;
                mapCamera.allowMSAA = false;
                mapCamera.farClipPlane = CameraHeight * 2f;
                mapCamera.targetTexture = texture;
                UniversalAdditionalCameraData data = mapCamera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = false;
                data.renderShadows = false;
            }

            root = new VisualElement { name = "minimap", pickingMode = PickingMode.Ignore };
            root.AddToClassList("minimap");

            var ring = new VisualElement();
            ring.AddToClassList("minimap__ring");
            ring.RegisterCallback<PointerDownEvent>(OnMapPressed);
            root.Add(ring);

            var view = new VisualElement { pickingMode = PickingMode.Ignore };
            view.AddToClassList("minimap__view");
            image = new VisualElement { pickingMode = PickingMode.Ignore };
            image.AddToClassList("minimap__image");
            image.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(texture));
            view.Add(image);
            dotPool.Clear();
            dots = new VisualElement { pickingMode = PickingMode.Ignore };
            dots.AddToClassList("minimap__dots");
            view.Add(dots);
            var arrow = new VisualElement { pickingMode = PickingMode.Ignore };
            arrow.AddToClassList("minimap__arrow");
            view.Add(arrow);
            ring.Add(view);

            Button zoomIn = HudLayer.MakeButton("+", () => Zoom(1f / ZoomStep), "minimap__zoom", "minimap__zoom--in");
            Button zoomOut = HudLayer.MakeButton("-", () => Zoom(ZoomStep), "minimap__zoom", "minimap__zoom--out");
            root.Add(zoomIn);
            root.Add(zoomOut);

            var food = new VisualElement { pickingMode = PickingMode.Ignore };
            food.AddToClassList("minimap__food");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("minimap__food-icon");
            var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("minimap__food-glyph");
            Texture2D apple = Resources.Load<Texture2D>("HudIcons/apple");
            if (apple != null) glyph.style.backgroundImage = new StyleBackground(apple);
            icon.Add(glyph);
            HideOldFoodBar();
            count = HudLayer.MakeLabel("0", "minimap__food-count");
            count.pickingMode = PickingMode.Ignore;
            food.Add(icon);
            food.Add(count);
            root.Add(food);

            layer.Add(root);
            blockers = new[] { ring, zoomIn, zoomOut };
            foreach (VisualElement blocker in blockers) HudLayer.AddBlocker(blocker);
            shown = -1;
            return true;
        }

        private static void HideOldFoodBar()
        {
            FoodHud hud = FindAnyObjectByType<FoodHud>();
            VisualElement oldBar = hud != null ? hud.GetComponent<UIDocument>().rootVisualElement?.Q("food-hud") : null;
            if (oldBar != null) oldBar.style.display = DisplayStyle.None;
        }

        private void Zoom(float factor)
        {
            targetRadius = Mathf.Clamp(targetRadius * factor, MinRadius, MaxRadius);
        }

        private void OnMapPressed(PointerDownEvent e)
        {
            if (e.button != 0) return;
            VisualElement target = (VisualElement)e.currentTarget;
            Vector2 local = (Vector2)e.localPosition / target.resolvedStyle.width * 2f - Vector2.one;
            local.y = -local.y;
            if (local.sqrMagnitude > 1f) return;
            Quaternion yaw = Quaternion.Euler(0f, rig.transform.eulerAngles.y, 0f);
            rig.GlideTo(rig.Focus + yaw * new Vector3(local.x, 0f, local.y) * radius);
            e.StopPropagation();
        }

        private void Render()
        {
            Quaternion yaw = Quaternion.Euler(0f, rig.transform.eulerAngles.y, 0f);
            mapCamera.orthographicSize = radius;
            mapCamera.transform.SetPositionAndRotation(rig.Focus + Vector3.up * CameraHeight, Quaternion.Euler(90f, yaw.eulerAngles.y, 0f));
            mapCamera.Render();
            SyncDots(rig.Focus, yaw);
        }

        private void SyncDots(Vector3 focus, Quaternion yaw)
        {
            Quaternion inverse = Quaternion.Inverse(yaw);
            int used = 0;
            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                if (!unit.IsAlive || unit.Home == null) continue;
                Vector3 offset = inverse * (unit.transform.position - focus) / radius;
                if (offset.x * offset.x + offset.z * offset.z > 1f) continue;

                if (used == dotPool.Count)
                {
                    var added = new VisualElement { pickingMode = PickingMode.Ignore };
                    added.AddToClassList("minimap__dot");
                    var core = new VisualElement { pickingMode = PickingMode.Ignore };
                    core.AddToClassList("minimap__dot-fill");
                    added.Add(core);
                    dots.Add(added);
                    dotPool.Add(added);
                }
                VisualElement dot = dotPool[used++];
                bool mine = unit.Home == village;
                dot.style.display = DisplayStyle.Flex;
                dot.style.left = Length.Percent(50f + offset.x * 50f);
                dot.style.top = Length.Percent(50f - offset.z * 50f);
                dot[0].style.backgroundColor = unit.IsSelected ? Color.white : unit.Home.TribeColour;
                dot.EnableInClassList("minimap__dot--mine", mine);
                dot.EnableInClassList("minimap__dot--chief", unit.IsChieftain);
            }
            for (int i = used; i < dotPool.Count; i++) dotPool[i].style.display = DisplayStyle.None;
        }
    }
}
