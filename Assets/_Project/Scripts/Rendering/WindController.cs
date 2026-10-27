using UnityEngine;

namespace SporeTribal.Rendering
{
    [DefaultExecutionOrder(-50)]
    public class WindController : MonoBehaviour
    {
        private static readonly int DirId = Shader.PropertyToID("_GlobalWindDir");
        private static readonly int StrengthId = Shader.PropertyToID("_GlobalWindStrength");
        private static readonly int PhaseId = Shader.PropertyToID("_GlobalWindPhase");

        public static WindController Current { get; private set; }

        [Header("Direction")]
        [Tooltip("Starting heading in degrees (0 = towards +z).")]
        [SerializeField] private float startHeading = 60f;
        [Tooltip("Seconds between heading changes (random in range).")]
        [SerializeField] private Vector2 headingChangeInterval = new Vector2(10f, 25f);
        [Tooltip("How far a heading change can swing, in degrees either way.")]
        [SerializeField] private float maxHeadingSwing = 70f;
        [SerializeField] private float headingSmoothTime = 4f;

        [Header("Strength")]
        [SerializeField] private float breezeStrength = 1f;
        [Tooltip("Extra strength added by gusts on top of the breeze.")]
        [SerializeField] private float gustStrength = 0.6f;
        [Tooltip("Chance per heading change that the wind settles into a calm instead.")]
        [SerializeField, Range(0f, 1f)] private float calmChance = 0.25f;
        [SerializeField] private float calmStrength = 0.12f;
        [SerializeField] private float strengthSmoothTime = 2.5f;

        [Header("Gust waves")]
        [Tooltip("How fast gust waves roll across the map at full strength.")]
        [SerializeField] private float gustWaveSpeed = 1.6f;

        private float heading;
        private float targetHeading;
        private float headingVelocity;
        private float baseStrength;
        private float targetStrength;
        private float strengthVelocity;
        private float nextChange;
        private float phase;

        public Vector2 Direction { get; private set; }
        public float Strength { get; private set; }

        private void OnEnable()
        {
            Current = this;
            heading = targetHeading = startHeading;
            baseStrength = targetStrength = breezeStrength;
            nextChange = Time.time + Random.Range(headingChangeInterval.x, headingChangeInterval.y);
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            Shader.SetGlobalFloat(StrengthId, 0f);
        }

        private void Update()
        {
            if (Time.time >= nextChange) PickNextWeather();

            heading = Mathf.SmoothDampAngle(heading, targetHeading, ref headingVelocity, headingSmoothTime);
            baseStrength = Mathf.SmoothDamp(baseStrength, targetStrength, ref strengthVelocity, strengthSmoothTime);

            float gust = Mathf.PerlinNoise(Time.time * 0.25f, 3.7f);
            gust = Mathf.Clamp01((gust - 0.35f) * 1.6f);
            Strength = baseStrength * (1f + gust * gustStrength / Mathf.Max(breezeStrength, 0.01f));

            float rad = heading * Mathf.Deg2Rad;
            Direction = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            phase += Time.deltaTime * gustWaveSpeed * Mathf.Max(Strength, 0.2f);

            Shader.SetGlobalVector(DirId, new Vector4(Direction.x, Direction.y, 0f, 0f));
            Shader.SetGlobalFloat(StrengthId, Strength);
            Shader.SetGlobalFloat(PhaseId, phase);
        }

        private void PickNextWeather()
        {
            nextChange = Time.time + Random.Range(headingChangeInterval.x, headingChangeInterval.y);
            targetHeading = heading + Random.Range(-maxHeadingSwing, maxHeadingSwing);
            targetStrength = Random.value < calmChance ? calmStrength : breezeStrength * Random.Range(0.8f, 1.2f);
        }
    }
}
