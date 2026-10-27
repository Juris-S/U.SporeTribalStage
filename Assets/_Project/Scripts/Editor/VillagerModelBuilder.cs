using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SporeTribal.EditorTools
{
    public static class VillagerModelBuilder
    {
        private const string Ithappy = "Assets/ithappy/Creative_Characters_FREE/";
        private const string Kevin = "Assets/Kevin Iglesias/Human Animations/Animations/Male/";
        private const string OutDir = "Assets/_Project/Prefabs/Characters/";
        private const string ControllerPath = OutDir + "VillagerAnimator.controller";
        private const string PrefabPath = OutDir + "VillagerModel.prefab";
        private const string MaterialPath = OutDir + "Character.mat";

        private static readonly string[] HiddenParts =
        {
            "Accessories", "Full_body", "Glasses", "Gloves", "Hat", "Mustache", "Outerwear", "Shoes", "T_Shirt"
        };

        private static readonly string[] DanceClips =
        {
            "Social/Dance/Steps/HumanM@Dance01", "Social/Dance/Steps/HumanM@Dance02", "Social/Dance/Steps/HumanM@Dance03",
            "Social/Dance/Steps/HumanM@Dance04", "Social/Dance/Steps/HumanM@Dance05", "Social/Dance/Steps/HumanM@Dance06"
        };

        [MenuItem("Tribal/Build Villager Model")]
        public static void Build()
        {
            Directory.CreateDirectory(OutDir);
            AssetDatabase.Refresh();
            EnsureLooping(Ithappy + "Animations/Other_Animations/Walk_Forward.anim");
            AnimatorController controller = BuildController();
            BuildPrefab(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Villager model built at " + PrefabPath);
        }

        private static AnimationClip Clip(string kevinPath)
        {
            string path = Kevin + kevinPath + ".fbx";
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is AnimationClip clip && !clip.name.StartsWith("__preview")) return clip;
            throw new FileNotFoundException(path);
        }

        private static void EnsureLooping(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.loopTime) return;
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static AnimatorController BuildController()
        {
            if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip("Idles/HumanM@Idle01"), 0f);
            tree.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>(Ithappy + "Animations/Other_Animations/Walk_Forward.anim"), 2f);
            tree.AddChild(Clip("Movement/Run/HumanM@Run01_Forward"), 5f);
            machine.defaultState = locomotion;

            AddState(machine, "Gather", Clip("Work/Gathering/HumanM@Gathering01"));
            ReturnTo(AddState(machine, "Attack", Clip("Combat/1H/HumanM@Attack1H01_R")), locomotion);
            AddState(machine, "CombatIdle", Clip("Combat/1H/HumanM@CombatIdle1H01"));
            AddState(machine, "Perform", Clip("Idles/HumanM@Idle01"));
            ReturnTo(AddState(machine, "Hit", Clip("Combat/HumanM@CombatDamage01")), locomotion);
            AddState(machine, "Death", Clip("Combat/HumanM@Death01"));
            for (int i = 0; i < DanceClips.Length; i++) AddState(machine, "Dance" + i, Clip(DanceClips[i]));
            return controller;
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, AnimationClip clip)
        {
            AnimatorState state = machine.AddState(name);
            state.motion = clip;
            state.writeDefaultValues = true;
            return state;
        }

        private static void ReturnTo(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 0.9f;
            transition.duration = 0.25f;
        }

        private static void BuildPrefab(AnimatorController controller)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Ithappy + "Prefabs/Base_Mesh.prefab");
            Material material = BuildMaterial();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "VillagerModel";

            var animator = instance.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            SkinnedMeshRenderer skin = Part(instance, "Body", "Body/Body_010");
            Part(instance, "Faces", "Faces/Male_emotion_usual_001");
            SkinnedMeshRenderer hair = Part(instance, "Hairstyle", "Hairstyle/Hairstyle_Male_001");
            SkinnedMeshRenderer shorts = Part(instance, "Pants", "Shorts/Shorts_003");
            foreach (string hidden in HiddenParts) Find(instance, hidden).gameObject.SetActive(false);

            var model = instance.AddComponent<SporeTribal.Villagers.VillagerModel>();
            var so = new SerializedObject(model);
            so.FindProperty("animator").objectReferenceValue = animator;
            so.FindProperty("skin").objectReferenceValue = skin;
            so.FindProperty("shorts").objectReferenceValue = shorts;
            so.FindProperty("hair").objectReferenceValue = hair;
            SerializedProperty styles = so.FindProperty("hairStyles");
            styles.arraySize = 2;
            styles.GetArrayElementAtIndex(0).objectReferenceValue = MeshOf("Hairstyle/Hairstyle_Male_001");
            styles.GetArrayElementAtIndex(1).objectReferenceValue = MeshOf("Hairstyle/Hairstyle_Male_005");
            so.FindProperty("attackLength").floatValue = Clip("Combat/1H/HumanM@Attack1H01_R").length;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
        }

        private static Material BuildMaterial()
        {
            var original = AssetDatabase.LoadAssetAtPath<Material>(Ithappy + "Materials/Color.mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Project/BentLit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", original.GetTexture("_BaseMap") != null ? original.GetTexture("_BaseMap") : original.GetTexture("_MainTex"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh MeshOf(string part)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ithappy + "Prefabs/" + part + ".prefab");
            return prefab.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
        }

        private static SkinnedMeshRenderer Find(GameObject instance, string name)
        {
            foreach (SkinnedMeshRenderer r in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (r.name == name) return r;
            throw new System.InvalidOperationException("No renderer named " + name);
        }

        private static SkinnedMeshRenderer Part(GameObject instance, string slot, string part)
        {
            SkinnedMeshRenderer target = Find(instance, slot);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ithappy + "Prefabs/" + part + ".prefab");
            SkinnedMeshRenderer from = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            target.sharedMesh = from.sharedMesh;
            var materials = new Material[from.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++) materials[i] = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            target.sharedMaterials = materials;
            target.gameObject.SetActive(true);
            return target;
        }
    }
}
