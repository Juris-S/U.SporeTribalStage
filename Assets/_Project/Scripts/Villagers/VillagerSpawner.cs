using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Villagers
{
    public class VillagerSpawner : MonoBehaviour
    {
        [SerializeField] private VillagerUnit prefab;
        [SerializeField] private Village village;
        [Tooltip("Every tribe starts with one chieftain plus this many regular villagers.")]
        [SerializeField, Min(0)] private int regulars = 2;
        [SerializeField] private float spacing = 2f;

        private void Start()
        {
            int total = regulars + 1;
            float start = -(total - 1) * spacing * 0.5f;
            for (int i = 0; i < total; i++)
            {
                VillagerUnit unit = SpawnAt(transform.position + new Vector3(start + i * spacing, 0f, 0f));
                if (i == 0) unit.MakeChieftain();
            }
        }

        public VillagerUnit SpawnAt(Vector3 position)
        {
            VillagerUnit unit = Instantiate(prefab, position, Quaternion.identity, transform);
            unit.Home = village;
            return unit;
        }
    }
}
