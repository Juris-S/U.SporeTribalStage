using System.Collections.Generic;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Buildings
{
    [RequireComponent(typeof(MainHut))]
    public class HutSanctuary : MonoBehaviour
    {
        [Tooltip("Health fraction below which a villager should go in to heal.")]
        [SerializeField, Range(0f, 1f)] private float criticalFraction = 0.3f;
        [SerializeField] private float healPerSecond = 8f;
        [Tooltip("Villagers come back out once healed to this fraction.")]
        [SerializeField, Range(0f, 1f)] private float releaseFraction = 1f;

        private MainHut hut;
        private readonly List<VillagerUnit> inside = new List<VillagerUnit>();

        public bool IsSleeping => inside.Count > 0;
        public Vector3 Door => hut.transform.position;
        public float DoorRadius => hut.StandRadius + 1.5f;

        private void Awake()
        {
            hut = GetComponent<MainHut>();
            hut.Health.Died += ReleaseAll;
        }

        public bool NeedsShelter(VillagerUnit unit) => hut.IsStanding && unit.Home == hut.Owner && unit.Health.Fraction < criticalFraction;

        public bool AtDoor(VillagerUnit unit)
        {
            Vector3 to = unit.transform.position - Door;
            to.y = 0f;
            return to.sqrMagnitude <= DoorRadius * DoorRadius;
        }

        public void Admit(VillagerUnit unit)
        {
            if (inside.Contains(unit)) return;
            inside.Add(unit);
            unit.Shelter();
        }

        private void Update()
        {
            for (int i = inside.Count - 1; i >= 0; i--)
            {
                VillagerUnit unit = inside[i];
                if (unit == null) { inside.RemoveAt(i); continue; }
                unit.Health.Heal(healPerSecond * Time.deltaTime);
                if (unit.Health.Fraction >= releaseFraction) Release(i);
            }
        }

        private void ReleaseAll()
        {
            for (int i = inside.Count - 1; i >= 0; i--) Release(i);
        }

        private void Release(int index)
        {
            VillagerUnit unit = inside[index];
            inside.RemoveAt(index);
            if (unit == null) return;
            Vector2 dir = Random.insideUnitCircle.normalized;
            Vector3 point = Door + new Vector3(dir.x, 0f, dir.y) * hut.StandRadius;
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas)) point = hit.position;
            unit.Unshelter(point);
        }
    }
}
