using UnityEngine;
using UnityEngine.AI;

namespace YeogiCafe.AI
{
    // 프로덕션용 이동 구현 — NavMeshAgent 기반.
    // 씬에 NavMesh를 베이크하고, 고양이 프리팹에 NavMeshAgent + 이 컴포넌트를 붙인다.
    // CatManager가 SimpleLerpMover 대신 이 컴포넌트를 Mover로 주입하면 교체 완료.
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshCatMover : MonoBehaviour, ICatMover
    {
        NavMeshAgent agent;
        bool moving;

        void Awake() { agent = GetComponent<NavMeshAgent>(); }

        public void MoveTo(Vector3 destination)
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh) { agent.SetDestination(destination); moving = true; }
        }

        public bool HasArrived
        {
            get
            {
                if (!moving || agent == null) return true;
                if (agent.pathPending) return false;
                bool arrived = agent.remainingDistance <= agent.stoppingDistance
                               && (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f);
                if (arrived) moving = false;
                return arrived;
            }
        }

        public void Stop()
        {
            moving = false;
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
        }
    }
}
