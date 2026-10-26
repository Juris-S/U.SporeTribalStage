using UnityEngine;

namespace SporeTribal.Villagers
{
    public class VillagerSpawner : MonoBehaviour
    {
        [SerializeField] private VillagerUnit prefab;
        [SerializeField] private int count = 6;
        [SerializeField] private float spacing = 2f;

        private void Start()
        {
            float start = -(count - 1) * spacing * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Vector3 position = transform.position + new Vector3(start + i * spacing, 0f, 0f);
                Instantiate(prefab, position, Quaternion.identity, transform);
            }
        }
    }
}
