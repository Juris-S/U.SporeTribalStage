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

        [Header("Social")]
        [Tooltip("None for non-instruments. Instruments are what a band performs with.")]
        [SerializeField] private InstrumentKind instrument = InstrumentKind.None;
        [Tooltip("Full colour icon, shown in the audience's thought balloon and on the HUD button while it can be played.")]
        [SerializeField] private Texture2D icon;
        [Tooltip("White silhouette of the icon, tinted grey on the HUD button while it cannot be played.")]
        [SerializeField] private Texture2D silhouetteIcon;
        [Tooltip("Played when the audience calls for this instrument.")]
        [SerializeField] private AudioClip callClip;
        [Tooltip("Played when the band answers with this instrument.")]
        [SerializeField] private AudioClip responseClip;

        public InstrumentKind Instrument => instrument;
        public Texture2D Icon => icon;
        public Texture2D SilhouetteIcon => silhouetteIcon;
        public AudioClip CallClip => callClip;
        public AudioClip ResponseClip => responseClip;
        public bool IsInstrument => instrument != InstrumentKind.None;

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
