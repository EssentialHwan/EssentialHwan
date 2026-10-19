using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Art;
using YeogiCafe.Core;
using YeogiCafe.Data;
using YeogiCafe.Loc;

namespace YeogiCafe.Visual
{
    // 쥐 소동: 가끔 벽 아래 쥐구멍에서 생쥐가 튀어나와 홀을 쏘다닌다.
    //  · 몇 초 안에 클릭(앞발 탁!)으로 잡으면 → 살살 밖에 놓아주고 끝. 손님들은 박수.
    //  · 놓치면 → 고양이들이 사냥 본능 폭발! 주문·식사를 내팽개치고 쥐를 쫓아 뛰어다닌다.
    //    이미 밥을 받은 고양이는 그대로 계산을 잊고 떠난다(그날 음식값 손실, 정산에 표시).
    public class MouseRaid : MonoBehaviour
    {
        public static MouseRaid Instance { get; private set; }
        public static System.Action<string> Toast;            // UI 주입

        public const float CatchWindow = 5f, ChaseSeconds = 6.5f;
        const float Speed = 2.6f, ChaseSpeed = 3.6f, CatchRadius = 0.55f;

        enum St { None, Loose, Chase, Caught, Leaving }
        St st;
        CatManager cats; DayManager day;
        Transform mouse, hole; SpriteRenderer msr, hsr;
        Vector3 target, holePos; float t, pauseT, animT;
        int rolledDay = -1; float triggerAt = -1f;
        readonly List<CatBrain> chasers = new();

        public bool Active => st != St.None;

        void Awake() { Instance = this; }

        void Resolve()
        {
            if (cats == null) cats = FindObjectOfType<CatManager>();
            if (day == null) day = FindObjectOfType<DayManager>();
        }

        // 하루에 한 번 굴림: 2일차부터 45% 확률로 점심~저녁 사이 어느 순간 출몰
        void Schedule()
        {
            if (day.CurrentDay == rolledDay) return;
            rolledDay = day.CurrentDay;
            triggerAt = -1f;
            if (day.CurrentDay < 2 || Random.value > 0.45f || day.cfg == null) return;
            float m = day.cfg.phaseSeconds.x, total = m + day.cfg.phaseSeconds.y + day.cfg.phaseSeconds.z;
            triggerAt = Random.Range(m * 0.9f, total * 0.88f);
        }

        void Update()
        {
            Resolve();
            if (day == null || cats == null) return;
            Schedule();
            float dt = Time.deltaTime;
            if (st == St.None)
            {
                if (triggerAt > 0f && day.DayTimer >= triggerAt && day.Phase != DayPhase.Settlement && !CatInteractionDirector.Suspended
                    && cats.Active.Count >= 2) { triggerAt = -1f; Spawn(); }
                FadeHole(dt);
                return;
            }
            if (day.Phase == DayPhase.Settlement) { Cleanup(); return; }
            t += dt; animT += dt;
            switch (st)
            {
                case St.Loose:
                    Scurry(dt, Speed);
                    if (t >= CatchWindow) StartChase();
                    break;
                case St.Chase:
                    Scurry(dt, ChaseSpeed);
                    if (t >= ChaseSeconds) { st = St.Leaving; target = holePos + Vector3.down * 0.2f; }
                    break;
                case St.Leaving:
                    MoveToward(target, ChaseSpeed, dt);
                    if ((mouse.position - target).sqrMagnitude < 0.01f) Cleanup();
                    break;
                case St.Caught:
                    msr.color = new Color(1, 1, 1, Mathf.Clamp01(1.2f - t));
                    mouse.position += Vector3.up * dt * 0.4f;
                    if (t >= 1.2f) Cleanup();
                    break;
            }
            if (mouse != null)
            {
                if (st != St.Caught) msr.sprite = Frame(((int)(animT * 12f)) % 2);
                msr.sortingOrder = YSort.Order(mouse.position.y) + YSort.CatAboveSeat + 2;
            }
        }

        static Sprite Frame(int i) => RuntimeSprites.Get("mouse_" + i, () => MouseDrawing.Draw(i));

        public void Spawn()
        {
            Resolve();
            if (st != St.None) return;
            float x = Random.Range(2.6f, 12.2f);
            holePos = new Vector3(x, CafeLayout.FloorTop - 0.02f, 0f);
            if (hole == null)
            {
                var h = new GameObject("MouseHole"); h.transform.SetParent(transform, false);
                hole = h.transform; hsr = h.AddComponent<SpriteRenderer>();
                hsr.sprite = RuntimeSprites.Get("mouse_hole", MouseDrawing.Hole);
                hsr.sortingOrder = YSort.WallProps + 6;
            }
            hole.position = holePos; hsr.color = Color.white;
            if (mouse == null)
            {
                var g = new GameObject("Mouse"); g.transform.SetParent(transform, false);
                mouse = g.transform; msr = g.AddComponent<SpriteRenderer>();
                mouse.localScale = Vector3.one * 0.9f;
            }
            mouse.gameObject.SetActive(true); msr.color = Color.white;
            mouse.position = holePos + Vector3.down * 0.15f;
            target = CafeLayout.RandomFloorPoint();
            st = St.Loose; t = 0f; pauseT = 0f;
            Toast?.Invoke(L.Get("mouse.appear"));
            YeogiCafe.Audio.SoundManager.Sfx("sfx_squeak_2", 0.5f);
            foreach (var b in cats.Active)
            {
                if (b == null) continue;
                var v = b.GetComponent<CatView>();
                if (v != null) v.ShowEmote(EmoteDrawing.Kind.Exclaim);
            }
        }

        float squeakT = 1f;

        void Scurry(float dt, float speed)
        {
            if (pauseT > 0f) { pauseT -= dt; return; }       // 잠깐 멈춰 킁킁(잡을 기회)
            MoveToward(target, speed, dt);
            // 달리는 동안 '사사삭' 발소리, 가끔 '찍'
            YeogiCafe.Audio.SoundManager.Sfx("sfx_scurry", 0.35f, 0.1f, "mouse_run", 0.45f);
            squeakT -= dt;
            if (squeakT <= 0f) { squeakT = Random.Range(1.2f, 2.6f); YeogiCafe.Audio.SoundManager.Sfx(Random.value < 0.6f ? "sfx_squeak" : "sfx_squeak_2", 0.4f, 0.12f); }
            if ((mouse.position - target).sqrMagnitude < 0.02f)
            {
                target = CafeLayout.RandomFloorPoint();
                if (st == St.Loose && Random.value < 0.45f) { pauseT = Random.Range(0.35f, 0.7f); YeogiCafe.Audio.SoundManager.Sfx("sfx_squeak", 0.4f, 0.12f); }
            }
        }

        void MoveToward(Vector3 to, float speed, float dt)
        {
            var p = mouse.position;
            var d = to - p; float len = d.magnitude;
            if (len > 0.001f) mouse.position = p + d * Mathf.Min(1f, speed * dt / len);
            if (Mathf.Abs(d.x) > 0.02f) msr.flipX = d.x < 0f;
        }

        void StartChase()
        {
            st = St.Chase; t = 0f;
            chasers.Clear();
            int skipped = 0;
            foreach (var b in cats.Active)
            {
                if (b == null) continue;
                // 느긋한 고양이 일부는 귀찮아서 안 쫓는다
                if (b.data != null && b.data.HasPersonality(PersonalityTag.Relaxed) && Random.value < 0.5f) continue;
                if (b.StartChase(() => mouse != null ? mouse.position : b.transform.position, ChaseSeconds + Random.Range(-0.8f, 0.8f)))
                {
                    chasers.Add(b);
                    if (b.ChaseSkippedPay) skipped++;
                }
            }
            Toast?.Invoke(skipped > 0 ? L.Get("mouse.chase.pay", ("n", skipped.ToString())) : L.Get("mouse.chase"));
            YeogiCafe.Audio.SoundManager.Sfx("sfx_squeak_2", 0.5f);
            GameEvents.Raise(GameEvents.MouseEscaped);
        }

        // 클릭 판정(CatInteractionDirector가 고양이 쓰다듬기보다 먼저 물어본다)
        public static bool TryCatch(Vector3 world)
        {
            var r = Instance;
            if (r == null || r.mouse == null || (r.st != St.Loose && r.st != St.Chase)) return false;
            var d = r.mouse.position + Vector3.up * 0.18f - world; d.z = 0f;
            if (d.magnitude > CatchRadius) return false;
            r.Caught();
            return true;
        }

        void Caught()
        {
            bool wasChase = st == St.Chase;
            st = St.Caught; t = 0f;
            msr.sprite = RuntimeSprites.Get("mouse_2", () => MouseDrawing.Draw(2));
            YeogiCafe.Audio.SoundManager.Sfx("sfx_catch", 0.7f, 0.03f);
            foreach (var b in chasers) if (b != null) b.EndChase();
            chasers.Clear();
            Toast?.Invoke(L.Get(wasChase ? "mouse.caught.late" : "mouse.caught"));
            GameEvents.Raise(GameEvents.MouseCaught);
            foreach (var b in cats.Active)
            {
                if (b == null) continue;
                var v = b.GetComponent<CatView>();
                if (v != null) v.ShowEmote(EmoteDrawing.Kind.Sparkle);
            }
        }

        void Cleanup()
        {
            foreach (var b in chasers) if (b != null) b.EndChase();
            chasers.Clear();
            st = St.None;
            if (mouse != null) mouse.gameObject.SetActive(false);
        }

        void FadeHole(float dt)
        {
            if (hsr == null || hsr.color.a <= 0f) return;
            var c = hsr.color; c.a = Mathf.Max(0f, c.a - dt * 0.25f); hsr.color = c;
        }
    }
}
