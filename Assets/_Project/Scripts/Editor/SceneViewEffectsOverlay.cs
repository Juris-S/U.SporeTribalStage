using SporeTribal.Rendering;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace SporeTribal.EditorTools
{
    [Overlay(typeof(SceneView), "Tribal View")]
    public class SceneViewEffectsOverlay : Overlay
    {
        private const string FogField = "fogInSceneView";
        private const string BendField = "bendInSceneView";

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            root.Add(CreateToggle<DistanceFog>("Distance fog", FogField));
            root.Add(CreateToggle<WorldBendController>("World bend", BendField));
            return root;
        }

        private static Toggle CreateToggle<T>(string label, string fieldName) where T : MonoBehaviour
        {
            var toggle = new Toggle(label);
            toggle.SetValueWithoutNotify(Read<T>(fieldName));
            toggle.RegisterValueChangedCallback(evt => Write<T>(fieldName, evt.newValue));
            toggle.schedule.Execute(() => toggle.SetValueWithoutNotify(Read<T>(fieldName))).Every(500);
            return toggle;
        }

        private static bool Read<T>(string fieldName) where T : MonoBehaviour
        {
            T target = Object.FindAnyObjectByType<T>();
            if (target == null) return false;

            return new SerializedObject(target).FindProperty(fieldName).boolValue;
        }

        private static void Write<T>(string fieldName, bool value) where T : MonoBehaviour
        {
            T target = Object.FindAnyObjectByType<T>();
            if (target == null) return;

            var so = new SerializedObject(target);
            so.FindProperty(fieldName).boolValue = value;
            so.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
    }
}
