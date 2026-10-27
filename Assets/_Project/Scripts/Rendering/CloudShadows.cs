using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SporeTribal.Rendering
{
    [RequireComponent(typeof(Light))]
    public class CloudShadows : MonoBehaviour
    {
        [SerializeField] private Texture2D cloudTexture;
        [Tooltip("World size in metres covered by one tile of the cloud texture.")]
        [SerializeField] private Vector2 tileSize = new Vector2(90f, 90f);
        [Tooltip("Drift speed in m/s at normal wind strength.")]
        [SerializeField] private float speed = 2.5f;
        [Tooltip("Used when there is no WindController in the scene.")]
        [SerializeField] private Vector2 fallbackDirection = new Vector2(0.6f, 0.8f);

        private UniversalAdditionalLightData lightData;
        private Vector2 offset;

        private void OnEnable()
        {
            var sun = GetComponent<Light>();
            lightData = GetComponent<UniversalAdditionalLightData>();
            if (lightData == null) lightData = gameObject.AddComponent<UniversalAdditionalLightData>();

            sun.cookie = cloudTexture;
            lightData.lightCookieSize = tileSize;
        }

        private void OnDisable()
        {
            GetComponent<Light>().cookie = null;
        }

        private void Update()
        {
            var wind = WindController.Current;
            Vector2 velocity = wind != null
                ? wind.Direction * speed * Mathf.Max(wind.Strength, 0.3f)
                : fallbackDirection.normalized * speed;

            var world = new Vector3(velocity.x, 0f, velocity.y);
            var lightVelocity = new Vector2(Vector3.Dot(world, transform.right), Vector3.Dot(world, transform.up));

            offset += lightVelocity * Time.deltaTime;
            offset.x %= tileSize.x;
            offset.y %= tileSize.y;
            lightData.lightCookieOffset = offset;
        }
    }
}
