using System.Collections;
using System.Collections.Generic;
using SporeTribal.Economy;
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
        [SerializeField] private float gatherSeconds = 1.5f;
        [SerializeField] private int carryCapacity = 3;
        [SerializeField] private float arriveDistance = 2.2f;
        [SerializeField] private float depositDistance = 3f;
        [SerializeField] private float searchRadius = 25f;

        private NavMeshAgent agent;
        private float nextGlanceTime;
        private float glanceYaw;
        private bool glancing;
        private string gatherStage;
        private Coroutine gatherRoutine;
        private FoodNode spotNode;

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

        public bool IsSelected { get; private set; }
        public bool IsGathering => gatherStage != null;
        public float SearchRadius => searchRadius;

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            selectionRing.SetActive(selected);
        }

        public string Intent => gatherStage != null ? $"Gather: {gatherStage}" : IsMoving ? "Move" : "Idle";

        public void MoveTo(Vector3 destination)
        {
            agent.SetDestination(destination);
        }

        public void Gather(FoodNode node)
        {
            if (gatherRoutine != null) StopCoroutine(gatherRoutine);
            ReleaseSpot();
            gatherRoutine = StartCoroutine(GatherRoutine(node));
        }

        private IEnumerator GatherRoutine(FoodNode node)
        {
            Village home = FindAnyObjectByType<Village>();
            while (node != null)
            {
                if (!node.Available)
                {
                    FoodNode next = FoodNode.Nearest(transform.position, searchRadius);
                    if (next != null) node = next;
                    else
                    {
                        gatherStage = "waiting for regrowth";
                        yield return new WaitUntil(() => node.Available);
                    }
                }

                gatherStage = "to food";
                spotNode = node;
                yield return WalkTo(node.ClaimSpot(this, transform.position, arriveDistance));

                int carried = 0;
                while (carried < carryCapacity && node.Available)
                {
                    gatherStage = $"picking {carried + 1}/{carryCapacity}";
                    yield return new WaitForSeconds(gatherSeconds);
                    carried += node.Take(1);
                }
                ReleaseSpot();
                if (carried == 0) continue;

                gatherStage = $"home with {carried}";
                yield return WalkTo(StandPoint(home.transform.position, depositDistance));

                home.Deposit(carried);
            }
            ReleaseSpot();
            gatherStage = null;
        }

        private void ReleaseSpot()
        {
            if (spotNode != null) spotNode.ReleaseSpot(this);
            spotNode = null;
        }

        private Vector3 StandPoint(Vector3 point, float standDistance)
        {
            Vector3 offset = transform.position - point;
            offset.y = 0f;
            return offset.magnitude > standDistance ? point + offset.normalized * standDistance : transform.position;
        }

        private IEnumerator WalkTo(Vector3 stop)
        {
            if (NavMesh.SamplePosition(stop, out NavMeshHit hit, 2f, NavMesh.AllAreas)) stop = hit.position;
            agent.SetDestination(stop);
            yield return null;
            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) yield return null;
        }
    }
}
