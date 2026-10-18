using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Cafe;
using YeogiCafe.Data;

namespace YeogiCafe.Visual
{
    // 고양이 표현 전담(로직과 분리). CatBrain 상태를 읽어 스프라이트 프레임·앉은 높이·시설 이동·퇴장 걸음을 그린다.
    // 로직(관찰/만족/결제)은 전혀 건드리지 않는다 → 아트 교체 시 이 파일과 스프라이트만 바꾸면 된다.
    [RequireComponent(typeof(CatBrain))]
    public class CatView : MonoBehaviour
    {
        // animFrames 인덱스 규약(PixelArtGenerator 시트 순서와 동일)
        const int Idle0 = 0, Idle1 = 1, Walk0 = 2, Eat0 = 4, Sleep = 6, Groom = 7, Stretch = 8, OrderPaw = 9;

        // 대기 중 가끔 그루밍(개체마다 박자가 달라 보이게 랜덤)
        float groomLeft, nextGroomAt = 6f;
        // 자리에서 일어날 때 기지개 후 문으로
        float stretchLeft; Vector3 exitDoor;

        public float frameTime = 0.32f;
        public float eatShowSeconds = 4f;
        public float sleepAfterIdleSeconds = 10f;

        public CatData Data { get; private set; }
        public bool IsExiting => exiting;
        public Transform BubbleAnchor => visual;     // 말풍선이 따라갈 지점

        CatBrain brain;
        Transform visual;
        SpriteRenderer sr;
        Vector3 lastPos;
        CatState lastState;
        float animT, eatTimer, idleTimer;

        // 시설 방문(표현만): 자리 → 시설 → 자리
        Vector3 hopWorld; float hopT; bool hopping;
        const float HopMove = 0.5f, HopStay = 2.6f;

        // 퇴장
        bool exiting; ICatMover exitMover; float fade = 1f;

        void Awake()
        {
            brain = GetComponent<CatBrain>();
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            sr = visual.gameObject.AddComponent<SpriteRenderer>();
        }

        public void Init(CatData data)
        {
            Data = data;
            sr.sprite = Frame(Idle0);
            brain.OnFacilityUse += HandleFacilityUse;
            lastPos = transform.position;
        }

        void OnDestroy()
        {
            if (brain != null) brain.OnFacilityUse -= HandleFacilityUse;
        }

        Sprite Frame(int i)
        {
            if (Data == null) return null;
            var f = Data.animFrames;
            if (f != null && i < f.Length && f[i] != null) return f[i];
            return Data.worldSprite;
        }

        void HandleFacilityUse(IFacility f)
        {
            if (!(f is Component c)) return;
            var fb = c.GetComponent<FacilityBehaviour>();
            hopWorld = c.transform.position + Vector3.up * (fb != null ? fb.PerchHeight : 0f);
            // 벽걸이 시설(창문)은 그 아래 바닥으로 다가가 올려다본다
            if (fb != null && fb.source != null && fb.source.wallMounted)
                hopWorld = new Vector3(c.transform.position.x, brain.CurrentSeatPos.y, 0f);
            hopT = 0f; hopping = true; idleTimer = 0f;
        }

        // 퇴장 걸음: 로직은 이미 끝났고(정산 완료), 문까지 걸어가서 사라진다.
        public void BeginExit(Vector3 door)
        {
            exiting = true; hopping = false;
            brain.enabled = false;
            exitMover = GetComponent<ICatMover>();
            exitDoor = door;
            stretchLeft = 0.8f;                     // 기지개 한 번 켜고 출발
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            Vector3 delta = pos - lastPos; lastPos = pos;
            bool walking = delta.sqrMagnitude > 1e-7f;
            if (Mathf.Abs(delta.x) > 1e-4f) sr.flipX = delta.x < 0f;

            var st = brain.State;
            if (!exiting && st == CatState.Idle && lastState != CatState.Idle && brain.ServedMenu != null)
                eatTimer = eatShowSeconds;            // 주문 직후 몇 초간 먹는 모습
            lastState = st;

            bool seated = !exiting && !walking && brain.CurrentSeat != null && IsSeatedState(st);
            float sitH = seated ? SitHeight() : 0f;

            // ── 위치(표현 오프셋) ──
            Vector3 local = new Vector3(0f, sitH, 0f);
            float sortY = pos.y;
            if (hopping && seated)
            {
                hopT += dt;
                Vector3 toHop = hopWorld - pos;
                float k;
                if (hopT < HopMove) k = Smooth(hopT / HopMove);
                else if (hopT < HopMove + HopStay) k = 1f;
                else if (hopT < HopMove * 2 + HopStay) k = 1f - Smooth((hopT - HopMove - HopStay) / HopMove);
                else { k = 0f; hopping = false; }
                local = Vector3.Lerp(local, toHop, k);
                if (k > 0.5f) sortY = hopWorld.y - 0.01f;   // 시설 위에서는 시설 기준 정렬
                if (Mathf.Abs(toHop.x) > 0.01f && k > 0f && k < 1f)
                    sr.flipX = (hopT < HopMove + HopStay) ? toHop.x < 0f : toHop.x > 0f;   // 갈 때/돌아올 때 진행 방향
            }
            else if (!seated) hopping = false;
            visual.localPosition = local;

            // ── 프레임 ──
            animT += dt;
            int phase = (int)(animT / frameTime) % 2;
            int idx;
            bool moving = walking || (hopping && (hopT < HopMove || hopT > HopMove + HopStay));
            if (exiting && stretchLeft > 0f)
            {
                stretchLeft -= dt; idx = Stretch;
                if (stretchLeft <= 0f && exitMover != null) exitMover.MoveTo(exitDoor);
            }
            else if (moving) { idx = Walk0 + phase; idleTimer = 0f; }
            else if (seated && st == CatState.WaitFood)
                idx = (animT % 1.4f) < 1.0f ? OrderPaw : Idle0;              // 앞발 흔들며 주문 대기
            else if (eatTimer > 0f) { eatTimer -= dt; idx = Eat0 + phase; idleTimer = 0f; }
            else
            {
                idleTimer += dt;
                bool sleepy = seated && !hopping && Data != null &&
                              Data.HasPersonality(PersonalityTag.Relaxed) && idleTimer > sleepAfterIdleSeconds;
                if (!sleepy && seated && groomLeft <= 0f && idleTimer > nextGroomAt)
                {
                    groomLeft = 1.6f; nextGroomAt = idleTimer + Random.Range(5f, 11f);
                }
                if (groomLeft > 0f) { groomLeft -= dt; idx = phase == 0 ? Groom : Idle1; }
                else idx = sleepy ? Sleep : ((animT % 3.2f) < 0.18f ? Idle1 : Idle0);   // 가끔 눈 깜빡임
            }
            sr.sprite = Frame(idx);

            sr.sortingOrder = YSort.Order(sortY) + YSort.CatAboveSeat;

            // ── 퇴장 처리 ──
            if (exiting)
            {
                if (stretchLeft <= 0f && (exitMover == null || exitMover.HasArrived))
                {
                    fade -= dt * 3f;
                    sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fade));
                    if (fade <= 0f) Destroy(gameObject);
                }
            }
        }

        float SitHeight()
        {
            if (brain.CurrentSeat is SeatBehaviour sb) return sb.SitHeight;
            return 0.2f;
        }

        static bool IsSeatedState(CatState s) =>
            s == CatState.Sit || s == CatState.DecideOrder || s == CatState.WaitFood ||
            s == CatState.Eat || s == CatState.Idle || s == CatState.FacilityAction;

        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    }
}
