using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Villagers
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class VillagerUnit : MonoBehaviour
    {
        private static readonly List<VillagerUnit> all = new List<VillagerUnit>();
        public static IReadOnlyList<VillagerUnit> All => all;

        [SerializeField] private GameObject selectionRing;
        [Tooltip("Each villager rolls its speed and turn speed within +/- this fraction, once, so a group never moves in lockstep.")]
        [SerializeField, Range(0f, 0.2f)] private float movementVariance = 0.05f;
        [Tooltip("Seconds between idle glances while standing still (min, max).")]
        [SerializeField] private Vector2 idleGlanceInterval = new Vector2(4f, 10f);
        [SerializeField] private float idleTurnSpeed = 90f;

        private NavMeshAgent agent;
        private float nextGlanceTime;
        private float glanceYaw;
        private bool glancing;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed *= 1f + Random.Range(-movementVariance, movementVariance);
            agent.angularSpeed *= 1f + Random.Range(-movementVariance, movementVariance);
            ScheduleGlance();
            SetSelected(false);
        }

        private void Update()
        {
            if (IsMoving)
            {
                glancing = false;
                ScheduleGlance();
                return;
            }

            if (glancing)
            {
                var target = Quaternion.Euler(0f, glanceYaw, 0f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, idleTurnSpeed * Time.deltaTime);
                if (Quaternion.Angle(transform.rotation, target) < 0.5f)
                {
                    glancing = false;
                    ScheduleGlance();
                }
            }
            else if (Time.time >= nextGlanceTime)
            {
                glanceYaw = Random.Range(0f, 360f);
                glancing = true;
            }
        }

        private void ScheduleGlance() => nextGlanceTime = Time.time + Random.Range(idleGlanceInterval.x, idleGlanceInterval.y);

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        public bool IsMoving => agent.pathPending || agent.remainingDistance > agent.stoppingDistance;

        public Vector3[] PathCorners => agent.hasPath ? agent.path.corners : System.Array.Empty<Vector3>();

        public void SetSelected(bool selected) => selectionRing.SetActive(selected);

        public void MoveTo(Vector3 destination) => agent.SetDestination(destination);
    }
}
