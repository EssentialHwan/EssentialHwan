using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Art;
using YeogiCafe.Cafe;
using YeogiCafe.Data;

namespace YeogiCafe.Visual
{
    // 고양이 표현 전담(로직과 분리). CatBrain 상태를 읽어 스프라이트 프레임·앉은 높이·시설 이동·퇴장 걸음을 그린다.
    // 로직(관찰/만족/결제)은 전혀 건드리지 않는다 → 아트 교체 시 이 파일과 스프라이트만 바꾸면 된다.
    [RequireComponent(typeof(CatBrain))]
    public class CatView : MonoBehaviour
    {
        // 고해상도 리그 클립(CatClips) 재생. 자리에서는 성격에 따라 앉기 → 그루밍 → 식빵 → 둥글게 자기로 흘러간다.
        float groomLeft, nextGroomAt = 6f;
        float stretchLeft; Vector3 exitDoor;

        public float eatShowSeconds = 4f;
        public float loafAfterIdleSeconds = 14f;      // 한참 머물면 식빵
        public float sleepAfterIdleSeconds = 22f;     // 느긋한 고양이는 그 뒤 둥글게 말아 잔다

        // 클립 재생 상태
        string clip = "", queued;          // queued: 1회 클립(전환)이 끝나면 이어갈 루프 클립
        float clipT;
        Sprite[] clipFrames; CatClips.Def clipDef;
        enum Rest { None, Sit, Loaf, Sleep }
        Rest rest;

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
        // 시설(캣타워 등): 걸어가서 → 점프해 올라가 쉬다 → 뛰어내림. 단계는 CatBrain.FacilityPhase가 정한다.
        CatBrain.FacPhase lastFacPhase; float facT;
        const float JumpUpTime = 0.6f, JumpDownTime = 0.55f;
        bool hopping => brain != null && brain.FacilityPhase != CatBrain.FacPhase.None;
        bool ateShown;
        float soloTimer = 8f;

        // 퇴장
        bool exiting; ICatMover exitMover; float fade = 1f;

        // ── 상호작용 동작(쓰다듬기 반응·고양이끼리) ──
        // 단계 목록을 차례로 재생. meet가 있으면 그 지점(바닥)까지 걸어가서 하고, 끝나면 자리로 돌아온다.
        public struct Step { public string clip; public float seconds; public bool hasEmote; public EmoteDrawing.Kind emote; }
        readonly Queue<Step> steps = new();
        string stepClip = "sit"; float stepLeft; bool acting; bool actHasMeet; Vector3 actMeet; int actFace; float actMoveT; bool actReturning;
        const float ActMove = 0.45f;

        // 감정 아이콘
        SpriteRenderer emoteSr; float emoteT = -1f;
        const float EmoteLife = 1.6f;
        float zzzTimer;

        // 쓰다듬기 연속 횟수
        float lastPetAt = -99f; int petStreak;

        public bool IsActing => acting;
        public bool IsSeatedNow { get; private set; }
        public bool IsSleeping => rest == Rest.Sleep;
        public CatBrain Brain => brain;
        public SpriteRenderer Renderer => sr;
        public Vector3 SeatPos => transform.position;

        // 고양이끼리 어울릴 수 있는 상태: 자리에서 쉬는 중(주문 대기·먹기·시설·전환 중 제외)
        public bool CanSocialize => IsSeatedNow && !exiting && !acting && !hopping && eatTimer <= 0f && !InTransition &&
                                    brain.State == CatState.Idle && !brain.IsMoving;
        // 쓰다듬을 수 있는 상태: 자리에 있으면 주문 대기 중이어도 OK
        public bool CanTouch => IsSeatedNow && !exiting && !acting && !hopping;

        public int RegisterPet()
        {
            if (Time.time - lastPetAt > 6f) petStreak = 0;
            petStreak++; lastPetAt = Time.time;
            return petStreak;
        }

        public void DoAction(Step[] seq, Vector3? meet = null, int face = 0)
        {
            steps.Clear();
            foreach (var st in seq) steps.Enqueue(st);
            acting = true; stepLeft = 0f; actReturning = false;
            actHasMeet = meet.HasValue; actMeet = meet ?? Vector3.zero; actFace = face; actMoveT = 0f;
            rest = rest == Rest.Sleep ? Rest.Sit : rest;    // 자다가 깨면 앉은 자세로
            idleTimer = 0f;
            NextStep();
        }

        void NextStep()
        {
            if (steps.Count == 0) { stepLeft = 0f; return; }
            var st = steps.Dequeue();
            stepLeft = st.seconds;
            stepClip = st.clip;
            clip = ""; Play(st.clip);
            if (st.hasEmote) ShowEmote(st.emote);
        }

        public void ShowEmote(EmoteDrawing.Kind k)
        {
            if (emoteSr == null)
            {
                var go = new GameObject("Emote");
                go.transform.SetParent(visual, false);
                emoteSr = go.AddComponent<SpriteRenderer>();
            }
            emoteSr.sprite = RuntimeSprites.Get("emote_" + k, () => EmoteDrawing.Draw(k));
            emoteSr.gameObject.SetActive(true);
            emoteT = 0f;
        }

        void TickEmote(float dt)
        {
            if (emoteSr == null || emoteT < 0f) return;
            emoteT += dt;
            float k = emoteT / EmoteLife;
            if (k >= 1f) { emoteSr.gameObject.SetActive(false); emoteT = -1f; return; }
            float pop = emoteT < 0.15f ? Mathf.Lerp(0.4f, 1.15f, emoteT / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((emoteT - 0.15f) / 0.1f));
            emoteSr.transform.localScale = Vector3.one * pop;
            float head = rest == Rest.Sleep || clip == "loaf" || clip == "purr" || clip == "rollover" || clip == "flinch" ? 0.75f : 1.15f;
            emoteSr.transform.localPosition = new Vector3(sr.flipX ? -0.25f : 0.25f, head + k * 0.25f, 0f);
            emoteSr.color = new Color(1f, 1f, 1f, k > 0.75f ? (1f - k) / 0.25f : 1f);
            emoteSr.sortingOrder = sr.sortingOrder + 2;
        }

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
            Play("sit");
            brain.OnFacilityUse += HandleFacilityUse;
            lastPos = transform.position;
        }

        void OnDestroy()
        {
            if (brain != null) brain.OnFacilityUse -= HandleFacilityUse;
        }

        // 루프 클립 전환. 같은 클립이면 이어서 재생(끊김 방지).
        void Play(string name)
        {
            if (name == clip && queued == null) return;
            clip = name; queued = null; clipT = 0f;
            clipDef = CatClips.Find(name);
            clipFrames = Data != null ? CatSpriteBank.Clip(Data.catId, name) : null;
        }

        // 1회 전환 클립(앉기·눕기 등) 재생 후 next 루프로.
        void PlayThen(string once, string next)
        {
            Play(once); queued = next;
        }

        void Tick(float dt)
        {
            if (clipFrames == null || clipFrames.Length == 0) return;
            clipT += dt;
            int i = (int)(clipT * clipDef.fps);
            if (!clipDef.loop && i >= clipFrames.Length)
            {
                if (queued != null) { var n = queued; queued = null; clip = ""; Play(n); i = 0; }
                else i = clipFrames.Length - 1;
            }
            sr.sprite = clipFrames[clipDef.loop ? i % clipFrames.Length : Mathf.Min(i, clipFrames.Length - 1)];
        }

        bool InTransition => queued != null;

        void HandleFacilityUse(IFacility f) { idleTimer = 0f; }

        // 퇴장 걸음: 로직은 이미 끝났고(정산 완료), 문까지 걸어가서 사라진다.
        public void BeginExit(Vector3 door)
        {
            exiting = true;
            brain.enabled = false;
            exitMover = GetComponent<ICatMover>();
            exitDoor = door;
            stretchLeft = 1.3f;                     // 기지개 한 번 켜고 출발(stretch 클립 길이)
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            Vector3 delta = pos - lastPos; lastPos = pos;
            bool walking = delta.sqrMagnitude > 1e-7f;
            if (Mathf.Abs(delta.x) > 1e-4f) sr.flipX = delta.x < 0f;
            else if (!walking && brain.AtSeat && brain.CurrentSeat is Component seatC && delta.sqrMagnitude < 1e-9f && clip == "walk")
                sr.flipX = seatC.transform.position.x > CafeCenterX;    // 자리에 앉으면 카페 가운데를 바라본다

            var st = brain.State;
            if (!exiting && !ateShown && st == CatState.Idle && brain.ServedMenu != null)
            { eatTimer = eatShowSeconds; ateShown = true; }   // 서빙 직후 한 번, 몇 초간 먹는 모습
            lastState = st;

            // '쉬는 중' = 자리든 바닥이든 시설 위든 멈춰 있는 상태
            bool seated = !exiting && !walking && IsSeatedState(st);
            IsSeatedNow = seated && !hopping;
            // 걷기 시작하거나 퇴장하면 동작 취소. 주문 결정처럼 잠깐 다른 상태를 거치는 건 무시.
            if (acting && (exiting || walking || hopping)) { acting = false; steps.Clear(); }
            if (acting) seated = true;
            float sitH = seated && brain.AtSeat ? SitHeight() : 0f;   // 의자 위에서만 앉은 높이

            // ── 위치(표현 오프셋) ──
            Vector3 local = new Vector3(0f, sitH, 0f);
            float sortY = pos.y;
            // 시설: 점프로 올라가고(포물선) 위에서 쉬다가 뛰어내림
            var fp = brain.FacilityPhase;
            if (fp != lastFacPhase) { lastFacPhase = fp; facT = 0f; OnFacPhase(fp); }
            facT += dt;
            float perch = brain.FacilityPerch;
            if (!exiting && perch > 0.05f && (fp == CatBrain.FacPhase.Perched || fp == CatBrain.FacPhase.Dismount))
            {
                float y;
                if (fp == CatBrain.FacPhase.Perched)
                {
                    float t = Mathf.Clamp01((facT - 0.2f) / (JumpUpTime - 0.2f));      // 0.2초 웅크림 후 도약
                    y = Mathf.Lerp(0f, perch, Smooth(t)) + Mathf.Sin(t * Mathf.PI) * 0.35f;
                }
                else
                {
                    float t = Mathf.Clamp01((facT - 0.1f) / (JumpDownTime - 0.15f));
                    y = Mathf.Lerp(perch, 0f, t * t) + Mathf.Sin(t * Mathf.PI) * 0.2f;   // 떨어질수록 빨라짐
                }
                local = new Vector3(0f, y, 0f);
            }

            // 상호작용: 만날 지점으로 걸어가 동작 → 돌아오기
            bool actWalking = false;
            if (acting)
            {
                if (actHasMeet)
                {
                    Vector3 toMeet = actMeet - pos;
                    float k;
                    if (!actReturning) { actMoveT = Mathf.Min(actMoveT + dt, ActMove); k = Smooth(actMoveT / ActMove); actWalking = actMoveT < ActMove; }
                    else { actMoveT = Mathf.Max(actMoveT - dt, 0f); k = Smooth(actMoveT / ActMove); actWalking = actMoveT > 0f; }
                    local = Vector3.Lerp(local, toMeet, k);
                    if (k > 0.5f) sortY = actMeet.y - 0.01f;
                    if (actWalking && Mathf.Abs(toMeet.x) > 0.01f) sr.flipX = actReturning ? toMeet.x > 0f : toMeet.x < 0f;
                    else if (!actReturning && actFace != 0) sr.flipX = actFace < 0;
                }
                else if (actFace != 0) sr.flipX = actFace < 0;

                if (!actWalking)
                {
                    if (!actReturning)
                    {
                        stepLeft -= dt;
                        if (stepLeft <= 0f)
                        {
                            if (steps.Count > 0) NextStep();
                            else if (actHasMeet) actReturning = true;
                            else acting = false;
                        }
                    }
                    else if (actMoveT <= 0f)
                    {
                        acting = false;
                        if (brain.CurrentSeat is Component sc) sr.flipX = sc.transform.position.x > CafeCenterX;
                    }
                }
            }
            visual.localPosition = local;

            // ── 클립 선택 ──
            bool moving = walking;
            if (exiting && stretchLeft > 0f)
            {
                if (clip != "stretch") Play("stretch");
                stretchLeft -= dt;
                if (stretchLeft <= 0f && exitMover != null) exitMover.MoveTo(exitDoor);
            }
            else if (acting) Play(actWalking ? "walk" : stepClip);
            else if (moving) { Play("walk"); idleTimer = 0f; rest = Rest.None; }
            else if (fp == CatBrain.FacPhase.Dismount && perch > 0.05f) Play("jumpdown");
            else if (fp == CatBrain.FacPhase.Perched && brain.FacilityWall) Play("gaze");                      // 창밖 구경
            else if (fp == CatBrain.FacPhase.Perched && facT < JumpUpTime + 0.1f && perch > 0.05f) { }           // 점프 클립 재생 중
            else if (fp == CatBrain.FacPhase.Perched && IsPlayFacility()) Play("pounce");                      // 장난감 놀이
            else if (!seated) { Play(exiting ? "walk" : "stand"); rest = Rest.None; }
            else if (st == CatState.WaitFood) { Play("order"); idleTimer = 0f; rest = Rest.Sit; }
            else if (eatTimer > 0f) { eatTimer -= dt; Play("eat"); idleTimer = 0f; rest = Rest.Loaf; }
            else if (!InTransition)
            {
                idleTimer += dt;
                bool relaxed = Data != null && Data.HasPersonality(PersonalityTag.Relaxed);
                float sleepAt = relaxed ? sleepAfterIdleSeconds * 0.6f : float.MaxValue;
                float loafAt = relaxed ? loafAfterIdleSeconds * 0.6f : loafAfterIdleSeconds;
                if (rest == Rest.None) { PlayThen("sitdown", "sit"); rest = Rest.Sit; }
                else if (rest == Rest.Sit && idleTimer > loafAt) { PlayThen("liedown", "loaf"); rest = Rest.Loaf; }
                else if (rest == Rest.Loaf && idleTimer > sleepAt) { PlayThen("curlup", "sleep"); rest = Rest.Sleep; }
                else if (rest == Rest.Sit)
                {
                    if (groomLeft <= 0f && idleTimer > nextGroomAt) { groomLeft = 2.0f; nextGroomAt = idleTimer + Random.Range(5f, 11f); }
                    if (groomLeft > 0f) { groomLeft -= dt; Play("groom"); }
                    else Play("sit");
                    TrySoloPlay(dt);
                }
                else if (rest == Rest.Loaf && clip != "loaf") Play("loaf");
            }
            Tick(dt);

            sr.sortingOrder = YSort.Order(sortY) + YSort.CatAboveSeat;
            if (clip == "sleep" && !acting) { zzzTimer -= dt; if (zzzTimer <= 0f) { zzzTimer = 3.5f; ShowEmote(EmoteDrawing.Kind.Zzz); } }
            TickEmote(dt);

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

        const float CafeCenterX = 7.5f;

        void OnFacPhase(CatBrain.FacPhase fp)
        {
            if (fp == CatBrain.FacPhase.Perched)
            {
                idleTimer = 0f;
                if (brain.FacilityPerch > 0.05f) { PlayThen("jumpup", "loaf"); rest = Rest.Loaf; }   // 올라가서 식빵 → (느긋하면) 잠
                if (brain.CurrentFacility is Component c) sr.flipX = c.transform.position.x > CafeCenterX;
            }
            else if (fp == CatBrain.FacPhase.Dismount) { clip = ""; queued = null; rest = Rest.None; }
        }

        bool IsPlayFacility()
        {
            var f = brain.CurrentFacility; if (f == null) return false;
            foreach (var t in f.Tags) if (t == FacilityTag.Play) return true;
            return false;
        }

        // 혼자 놀기: 바닥에서 쉬다가 가끔 허공 덮치기·뒹굴기(장난꾸러기일수록 자주), 아무나 가끔 기지개
        void TrySoloPlay(float dt)
        {
            if (acting || !IsSeatedNow || brain.AtSeat || brain.State != CatState.Idle) return;
            soloTimer -= dt;
            if (soloTimer > 0f) return;
            bool playful = Data != null && (Data.HasPersonality(PersonalityTag.Playful) || Data.HasPersonality(PersonalityTag.Active));
            soloTimer = Random.Range(7f, 14f) * (playful ? 0.6f : 1.4f);
            float r = Random.value;
            if (playful && r < 0.5f) DoAction(new[] { new Step { clip = "pounce", seconds = 1.6f, hasEmote = true, emote = EmoteDrawing.Kind.Exclaim } });
            else if (playful && r < 0.8f) DoAction(new[] { new Step { clip = "rollover", seconds = 2.2f, hasEmote = true, emote = EmoteDrawing.Kind.Note } });
            else if (r < 0.5f) DoAction(new[] { new Step { clip = "stretch", seconds = 1.25f } });
        }

        float SitHeight()
        {
            if (brain.VisualSeat is SeatBehaviour sb) return sb.SitHeight;
            return 0.2f;
        }

        static bool IsSeatedState(CatState s) =>
            s == CatState.Sit || s == CatState.DecideOrder || s == CatState.Order || s == CatState.WaitFood ||
            s == CatState.Eat || s == CatState.Idle || s == CatState.FacilityAction;

        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    }
}
