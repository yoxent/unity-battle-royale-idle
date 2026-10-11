using UnityEngine;
using UnityEngine.AI;

namespace BattleRoyale.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class UnitMovement : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [Min(0.1f)]
        [SerializeField] private float navMeshSnapDistance = 8f;
        [Min(0f)]
        [SerializeField] private float moveSpeed = 8f;
        [Min(0f)]
        [SerializeField] private float angularSpeed = 720f;
        [Min(0f)]
        [SerializeField] private float stoppingDistance = 0.25f;
        [SerializeField] private bool placeOnNavMeshOnStart = true;

        public NavMeshAgent Agent => agent;
        public bool IsReady => agent != null && agent.isOnNavMesh;

        private void Reset()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Awake()
        {
            CacheAgent();
            ConfigureAgent();
        }

        private void Start()
        {
            if (placeOnNavMeshOnStart)
            {
                TryPlaceOnNavMesh();
            }
        }

        public bool TryMoveTo(Vector3 requestedDestination, out Vector3 acceptedDestination)
        {
            acceptedDestination = requestedDestination;
            if (!CacheAgent() || !EnsureOnNavMesh())
            {
                return false;
            }

            if (!NavMesh.SamplePosition(
                    requestedDestination,
                    out var hit,
                    navMeshSnapDistance,
                    agent.areaMask))
            {
                return false;
            }

            acceptedDestination = hit.position;
            return agent.SetDestination(acceptedDestination);
        }

        public bool TryPlaceOnNavMesh()
        {
            if (!CacheAgent())
            {
                return false;
            }

            if (!NavMesh.SamplePosition(
                    transform.position,
                    out var hit,
                    navMeshSnapDistance,
                    agent.areaMask))
            {
                return false;
            }

            return agent.Warp(hit.position);
        }

        private bool EnsureOnNavMesh()
        {
            return agent.isOnNavMesh || TryPlaceOnNavMesh();
        }

        private bool CacheAgent()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            return agent != null;
        }

        private void ConfigureAgent()
        {
            if (agent == null)
            {
                return;
            }

            agent.speed = moveSpeed;
            agent.angularSpeed = angularSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.autoBraking = true;
            agent.updateRotation = false;
        }

        private void OnValidate()
        {
            navMeshSnapDistance = Mathf.Max(0.1f, navMeshSnapDistance);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            angularSpeed = Mathf.Max(0f, angularSpeed);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
        }
    }
}
