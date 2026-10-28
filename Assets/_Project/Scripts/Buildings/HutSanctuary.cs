using System.Collections.Generic;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Buildings
{
    [RequireComponent(typeof(MainHut))]
    public class HutSanctuary : MonoBehaviour
    {
        private const float DoorSlack = 1.5f;

        [Tooltip("Health fraction below which a villager should go in to heal.")]
        [SerializeField, Range(0f, 1f)] private float criticalFraction = 0.3f;
        [SerializeField] private float healPerSecond = 8f;
        [Tooltip("Villagers come back out once healed to this fraction.")]
        [SerializeField, Range(0f, 1f)] private float releaseFraction = 1f;

        private MainHut hut;
        private readonly List<VillagerUnit> inside = new List<VillagerUnit>();

        public bool IsSleeping => inside.Count > 0;
        public Bounds Bounds => hut.Bounds;
        public Vector3 Door => hut.transform.position;

        private float DoorRadius => hut.StandRadius + DoorSlack;

        private void Awake()
        {
            hut = GetComponent<MainHut>();
            hut.Health.Died += ReleaseAll;
        }

        public bool NeedsShelter(VillagerUnit unit) => hut.IsStanding && unit.Home == hut.Owner && unit.Health.Fraction < criticalFraction;

        public bool TryAdmit(VillagerUnit unit)
        {
            if (!IsAtDoor(unit) || !NeedsShelter(unit)) return false;
            if (!inside.Contains(unit))
            {
                inside.Add(unit);
                unit.Shelter();
            }
            return true;
        }

        public Vector3 DoorFor(VillagerUnit unit)
        {
            Vector3 toUnit = (unit.transform.position - Door).Flat();
            Vector3 dir = toUnit.sqrMagnitude > 0.01f ? toUnit.normalized : Vector3.forward;
            return NavMeshUtility.Snap(Door + dir * hut.StandRadius);
        }

        private bool IsAtDoor(VillagerUnit unit) => unit.transform.position.IsWithinFlat(Door, DoorRadius);

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
            unit.Unshelter(NavMeshUtility.Snap(point));
        }
    }
}
