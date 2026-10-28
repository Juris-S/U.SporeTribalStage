using UnityEngine;

namespace SporeTribal.Villagers
{
    public sealed class VillagerModel : MonoBehaviour
    {
        public enum Clip { Locomotion, Gather, Attack, CombatIdle, Perform, Hit, Death, Dance, AttackSlash }

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int[] StateIds =
        {
            Animator.StringToHash("Locomotion"), Animator.StringToHash("Gather"), Animator.StringToHash("Attack"),
            Animator.StringToHash("CombatIdle"), Animator.StringToHash("Perform"), Animator.StringToHash("Hit"),
            Animator.StringToHash("Death"), Animator.StringToHash("Dance0"), Animator.StringToHash("AttackSlash")
        };

        [SerializeField] private Animator animator;
        [SerializeField] private SkinnedMeshRenderer skin;
        [SerializeField] private SkinnedMeshRenderer shorts;
        [SerializeField] private SkinnedMeshRenderer hair;
        [SerializeField] private Mesh[] hairStyles;
        [SerializeField] private int danceCount = 6;
        [Tooltip("Uniform world scale of the character, 1 is the authored height.")]
        [SerializeField] private float modelScale = 1.3f;
        [Tooltip("Blend seconds into and out of the quick clips (attack, hit).")]
        [SerializeField] private float crossFade = 0.12f;
        [Tooltip("Blend seconds into and out of idle, gather, perform and dance, so a pose change never pops.")]
        [SerializeField] private float softCrossFade = 0.4f;
        [Tooltip("Seconds over which the measured ground speed eases, so the walk blend does not flicker.")]
        [SerializeField] private float speedSmoothing = 0.2f;
        [Tooltip("Length in seconds of the attack clip, used to retime it to the villager's swing.")]
        [SerializeField] private float attackLength = 0.9f;
        [Tooltip("Each villager rolls a pace multiplier in this range, so groups drift out of step instead of moving in lockstep.")]
        [SerializeField] private Vector2 paceRange = new Vector2(0.92f, 1.08f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        private MaterialPropertyBlock skinBlock;
        private MaterialPropertyBlock shortsBlock;
        private Transform rightHand;
        private Transform head;
        private float speed;
        private float speedVelocity;
        private float pace = 1f;
        private Clip current;
        private Vector3 lastPosition;
        private Transform root;

        public float ModelScale => modelScale;
        public Transform HeadAnchor { get; private set; }
        public bool IsDead { get; private set; }

        public Transform ToolSocket { get; private set; }

        public void Init(Transform root)
        {
            this.root = root;
            pace = Random.Range(paceRange.x, paceRange.y);
            animator.speed = pace;
            lastPosition = root.position;
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            skinBlock = new MaterialPropertyBlock();
            shortsBlock = new MaterialPropertyBlock();
            if (hair != null && hairStyles != null && hairStyles.Length > 0) hair.sharedMesh = hairStyles[Random.Range(0, hairStyles.Length)];
            AnimatorCullingMode culling = animator.cullingMode;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Play(StateIds[0], 0, 0f);
            animator.Update(0.001f);
            BuildAnchors();
            animator.cullingMode = culling;
            animator.Play(StateIds[0], 0, Random.value);
        }

        public void AlignToolSocket(Vector3 localPosition, Vector3 localEuler)
        {
            ToolSocket.localPosition += ToolSocket.localRotation * Vector3.Scale(localPosition, ToolSocket.localScale);
            ToolSocket.localRotation *= Quaternion.Euler(localEuler);
        }

        private void BuildAnchors()
        {
            Quaternion upright = transform.rotation;

            var middle = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            Vector3 palm = middle != null ? (rightHand.position + middle.position) * 0.5f : rightHand.position;
            ToolSocket = Anchor(rightHand, "ToolSocket", palm, upright);

            HeadAnchor = Anchor(head, "HeadAnchor", CrownPoint(), upright);
        }

        private Vector3 CrownPoint()
        {
            if (hair == null) return head.position;
            Bounds b = hair.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        private Transform Anchor(Transform bone, string name, Vector3 worldPosition, Quaternion worldRotation)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(bone, false);
            anchor.SetPositionAndRotation(worldPosition, worldRotation);
            Vector3 s = bone.lossyScale;
            anchor.localScale = new Vector3(modelScale / s.x, modelScale / s.y, modelScale / s.z);
            return anchor;
        }

        public void SetSkin(Color colour) => Paint(skin, skinBlock, colour);
        public void SetClothing(Color colour) => Paint(shorts, shortsBlock, colour);

        private static void Paint(Renderer target, MaterialPropertyBlock block, Color colour)
        {
            if (target == null) return;
            target.GetPropertyBlock(block);
            block.SetTexture(BaseMapId, Texture2D.whiteTexture);
            block.SetTexture(MainTexId, Texture2D.whiteTexture);
            block.SetColor(BaseColorId, colour);
            target.SetPropertyBlock(block);
        }

        public void Play(Clip clip, int variant = 0, float seconds = 0f)
        {
            if (IsDead) return;
            bool restart = clip == Clip.Dance || clip == Clip.Attack || clip == Clip.AttackSlash || clip == Clip.Hit;
            if (clip == current && !restart) return;
            current = clip;
            int id = clip == Clip.Dance ? Animator.StringToHash("Dance" + Mathf.Clamp(variant, 0, danceCount - 1)) : StateIds[(int)clip];
            animator.speed = (clip == Clip.Attack || clip == Clip.AttackSlash) && seconds > 0f ? attackLength / seconds : pace;
            float phase = clip == Clip.Locomotion ? Random.value : 0f;
            animator.CrossFadeInFixedTime(id, restart && clip != Clip.Dance ? crossFade : softCrossFade, 0, phase);
        }

        public void Die()
        {
            if (IsDead) return;
            Play(Clip.Death);
            IsDead = true;
            animator.speed = pace;
        }

        private void Update()
        {
            if (animator == null || root == null) return;
            Vector3 delta = root.position - lastPosition;
            delta.y = 0f;
            lastPosition = root.position;
            float measured = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
            speed = Mathf.SmoothDamp(speed, measured, ref speedVelocity, speedSmoothing);
            animator.SetFloat(SpeedId, speed);
        }
    }
}
