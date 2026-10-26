using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Villagers
{
    public class VillagerSpawner : MonoBehaviour
    {
        [SerializeField] private VillagerUnit prefab;
        [SerializeField] private Village village;
        [SerializeField] private int count = 6;
        [SerializeField] private float spacing = 2f;

        private void Start()
        {
            float start = -(count - 1) * spacing * 0.5f;
            for (int i = 0; i < count; i++)
                SpawnAt(transform.position + new Vector3(start + i * spacing, 0f, 0f));
        }

        public VillagerUnit SpawnAt(Vector3 position)
        {
            VillagerUnit unit = Instantiate(prefab, position, Quaternion.identity, transform);
            unit.Home = village;
            return unit;
        }
    }
}
