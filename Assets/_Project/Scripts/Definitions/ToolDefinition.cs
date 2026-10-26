using UnityEngine;

namespace SporeTribal.Definitions
{
    [CreateAssetMenu(menuName = "Tribal/Tool", fileName = "Tool")]
    public class ToolDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Tool";
        [SerializeField] private int cost = 15;
        [SerializeField] private GameObject heldModel;
        [Tooltip("Placement relative to the villager's Hand anchor (world-sized units, the anchor cancels the body scale).")]
        [SerializeField] private Vector3 heldPosition;
        [SerializeField] private Vector3 heldEuler;
        [Tooltip("Degrees the tool leans forward (top toward the villager's front), applied after the Euler rotation.")]
        [SerializeField] private float heldForwardTilt = 15f;
        [SerializeField] private float heldScale = 1.4f;

        [Header("Combat")]
        [Tooltip("Damage per hit. Zero means the tool is not a weapon and the villager fights unarmed.")]
        [SerializeField] private int damage;
        [Tooltip("Damage per hit against buildings. Zero means the villager razes with bare hands.")]
        [SerializeField] private int razeDamage;
        [Tooltip("Seconds between hits.")]
        [SerializeField] private float attackCooldown = 2f;

        public string DisplayName => displayName;
        public int Cost => cost;
        public int Damage => damage;
        public int RazeDamage => razeDamage;
        public float AttackCooldown => attackCooldown;

        public GameObject SpawnHeld(Transform hand)
        {
            GameObject held = Instantiate(heldModel, hand);
            held.name = displayName;
            held.transform.SetLocalPositionAndRotation(heldPosition, Quaternion.Euler(heldForwardTilt, 0f, 0f) * Quaternion.Euler(heldEuler));
            held.transform.localScale = Vector3.one * heldScale;
            return held;
        }
    }
}
