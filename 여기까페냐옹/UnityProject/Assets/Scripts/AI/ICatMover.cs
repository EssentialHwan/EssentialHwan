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

    // NavMesh 없이도 동작하는 기본 구현(직선 이동).
    // 걷기 v2(10/28): 순간 최고속도·순간 정지 대신 0.28초에 걸쳐 출발하고, 도착 직전 부드럽게 감속(도착 조향).
    // 걷기 그림은 CatView가 '걸은 거리'로 넘기므로 속도가 바뀌어도 발이 미끄러지지 않는다.
    public class SimpleLerpMover : MonoBehaviour, ICatMover
    {
        public float speed = 1.3f;          // 기본 걸음(걷기 v2 보폭 3.2px/프레임 × 약 13fps)
        public float accelTime = 0.28f;     // 멈춤 → 최고속도
        Vector3 target;
        bool moving;
        float v;

        public float CurrentSpeed => moving ? v : 0f;
        public bool IsMoving => moving;
        public Vector3 Target => target;

        public void MoveTo(Vector3 destination) { target = destination; moving = true; }   // 걷던 중이면 속도 유지(경유지 사이에서 안 멈춤)
        public bool HasArrived => !moving;
        public void Stop() { moving = false; v = 0f; }

        void Update()
        {
            if (!moving) { v = 0f; return; }
            var pos = transform.position;
            var dir = new Vector3(target.x - pos.x, target.y - pos.y, target.z - pos.z);
            float dist = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
            if (dist < 0.04f) { moving = false; v = 0f; return; }
            float accel = speed / Mathf.Max(0.05f, accelTime);
            float decel = accel * 1.3f;
            float want = Mathf.Min(speed, Mathf.Max(Mathf.Sqrt(2f * decel * dist), 0.3f));   // 남은 거리로 멈출 수 있는 속도까지만
            v = Mathf.MoveTowards(v, want, (want > v ? accel : decel * 2f) * Time.deltaTime);
            float step = v * Time.deltaTime;
            float t = Mathf.Min(1f, step / dist);
            transform.position = new Vector3(pos.x + dir.x * t, pos.y + dir.y * t, pos.z + dir.z * t);
        }
    }
}
