using UnityEngine;

namespace YeogiCafe.AI
{
    // 이동 추상화 — FSM 로직과 이동 구현(NavMesh/직선보간)을 분리.
    // CatBrain은 목적지를 요청하고 도착 여부만 확인한다.
    public interface ICatMover
    {
        void MoveTo(Vector3 destination);
        bool HasArrived { get; }
        void Stop();
    }

    // NavMesh 없이도 동작하는 기본 구현(직선 이동). 프로토/테스트용.
    // 실제 빌드는 NavMeshCatMover(별도)로 교체 권장.
    public class SimpleLerpMover : MonoBehaviour, ICatMover
    {
        public float speed = 2.5f;
        Vector3 target;
        bool moving;

        public void MoveTo(Vector3 destination) { target = destination; moving = true; }
        public bool HasArrived => !moving;
        public void Stop() { moving = false; }

        void Update()
        {
            if (!moving) return;
            var pos = transform.position;
            var dir = new Vector3(target.x - pos.x, target.y - pos.y, target.z - pos.z);
            float dist = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
            if (dist < 0.05f) { moving = false; return; }
            float step = speed * Time.deltaTime;
            float t = Mathf.Min(1f, step / dist);
            transform.position = new Vector3(pos.x + dir.x * t, pos.y + dir.y * t, pos.z + dir.z * t);
        }
    }
}
