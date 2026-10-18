using System;
using UnityEngine;

namespace YeogiCafe.Art
{
    // 고양이 행동 클립(프레임 시퀀스). 포즈 파라미터를 시간에 따라 움직여 부드러운 모션을 만든다.
    // 이름은 CatView가 행동 상태·상호작용에 맞춰 고른다.
    public static class CatClips
    {
        public struct Def { public string name; public float fps; public bool loop; public int frames; public Func<int, int, CatRig.Pose> pose; }

        const float TAU = Mathf.PI * 2f;

        public static readonly Def[] All =
        {
            // 기본 행동
            new Def { name = "walk",     fps = 11, loop = true,  frames = 8,  pose = Walk },
            new Def { name = "stand",    fps = 6,  loop = true,  frames = 8,  pose = StandIdle },
            new Def { name = "run",      fps = 16, loop = true,  frames = 6,  pose = Run },        // 우다다(전력질주)
            new Def { name = "sit",      fps = 6,  loop = true,  frames = 12, pose = SitIdle },
            new Def { name = "loaf",     fps = 4,  loop = true,  frames = 8,  pose = LoafIdle },
            new Def { name = "sleep",    fps = 3,  loop = true,  frames = 6,  pose = SleepIdle },
            new Def { name = "groom",    fps = 8,  loop = true,  frames = 8,  pose = Groom },
            new Def { name = "stretch",  fps = 8,  loop = false, frames = 10, pose = Stretch },
            new Def { name = "order",    fps = 8,  loop = true,  frames = 8,  pose = Order },
            new Def { name = "eat",      fps = 7,  loop = true,  frames = 6,  pose = Eat },
            new Def { name = "sitdown",  fps = 10, loop = false, frames = 5,  pose = SitDown },
            new Def { name = "liedown",  fps = 8,  loop = false, frames = 5,  pose = LieDown },
            new Def { name = "curlup",   fps = 7,  loop = false, frames = 5,  pose = CurlUp },
            // 쓰다듬기 반응·고양이끼리 상호작용
            new Def { name = "rub",      fps = 7,  loop = true,  frames = 8,  pose = Rub },        // 부비부비(머리 비비기)
            new Def { name = "rollover", fps = 6,  loop = true,  frames = 8,  pose = RollOver },   // 배 보이며 뒹굴
            new Def { name = "purr",     fps = 5,  loop = true,  frames = 6,  pose = Purr },       // 눈 감고 골골
            new Def { name = "sniff",    fps = 8,  loop = true,  frames = 6,  pose = Sniff },      // 킁킁 냄새 맡기
            new Def { name = "lick",     fps = 8,  loop = true,  frames = 6,  pose = Lick },       // 서로 핥아주기
            new Def { name = "pounce",   fps = 10, loop = true,  frames = 8,  pose = Pounce },     // 엉덩이 씰룩 → 톡(놀이)
            new Def { name = "meow",     fps = 6,  loop = true,  frames = 6,  pose = Meow },       // 야옹(관심 요구)
            new Def { name = "flinch",   fps = 8,  loop = true,  frames = 6,  pose = Flinch },     // 움찔(낯가림)
            new Def { name = "hiss",     fps = 8,  loop = true,  frames = 6,  pose = HissIdle },   // 하악
            new Def { name = "swat",     fps = 12, loop = true,  frames = 6,  pose = Swat },       // 냥냥펀치
            // 시설
            new Def { name = "jumpup",   fps = 10, loop = false, frames = 6,  pose = JumpUp },     // 웅크렸다 도약 → 착지
            new Def { name = "jumpdown", fps = 11, loop = false, frames = 6,  pose = JumpDown },   // 가장자리에서 뛰어내림 → 착지
            new Def { name = "gaze",     fps = 5,  loop = true,  frames = 10, pose = Gaze },       // 창밖 올려다보기(꼬리 살랑, 가끔 채터링)
        };

        public static Def Find(string name)
        {
            foreach (var d in All) if (d.name == name) return d;
            return All[2];
        }

        public static PixelCanvas Frame(string catId, Def d, int i) => CatRig.Render(catId, d.pose(i, d.frames));

        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
        static float Ph(int i, int n) => i / (float)n;

        // ── 걷기: 왼뒤→왼앞→오른뒤→오른앞(실제 보행 순서), 몸 오르내림, 머리·꼬리 흔들림 ──
        static CatRig.Pose Walk(int i, int n)
        {
            var p = CatRig.Stand();
            float ph = Ph(i, n);
            void Step(ref CatRig.Leg l, float off, float stride, float lift)
            {
                // 발을 든 동안(sin>0) 앞으로(+x), 딛는 동안 뒤로 밀림 → 몸이 앞으로 나아가는 걸음
                // (이전엔 부호가 반대라 공중에서 발이 뒤로 가는 '문워크'였음)
                float a = TAU * (ph + off);
                l.px -= Mathf.Cos(a) * stride;
                l.py += Mathf.Max(0f, Mathf.Sin(a)) * lift;
                if (l.knee) { l.kx -= Mathf.Cos(a) * stride * 0.6f; l.ky += Mathf.Max(0f, Mathf.Sin(a)) * lift * 0.8f; }
            }
            Step(ref p.bn, 0.00f, 3.5f, 2.5f); Step(ref p.fn, 0.25f, 4f, 2.5f);
            Step(ref p.bf, 0.50f, 3.5f, 2.5f); Step(ref p.ff, 0.75f, 4f, 2.5f);
            float bob = Mathf.Cos(TAU * ph * 2f) * 0.7f;
            p.bodyY += bob; p.chestY += bob; p.hipY += bob;
            p.headY += bob * 0.6f + Mathf.Sin(TAU * ph) * 0.4f;
            p.tailAng = 2.2f + Mathf.Sin(TAU * ph) * 0.12f; p.tailCurl = -0.09f + Mathf.Sin(TAU * ph + 1f) * 0.03f;
            return p;
        }

        // ── 우다다: 뻗기(앞발 앞·뒷발 뒤) ↔ 모으기(네 발이 몸 아래, 등이 둥글게)를 오가는 갤럽 ──
        static CatRig.Pose RunStretch()
        {
            var p = CatRig.Stand();
            p.bodyX = 31; p.bodyY = 15; p.bodyRx = 14f; p.bodyRy = 6.2f; p.bodyAng = 0.02f;
            p.chestX = 40; p.chestY = 15.5f; p.chestR = 7f; p.hipX = 21; p.hipY = 15f; p.hipR = 7.2f;
            p.headX = 48; p.headY = 22; p.headTilt = 0.1f; p.earFold = 0.35f; p.face = CatRig.Face.Wide;
            p.fn = new CatRig.Leg { jx = 42, jy = 12, px = 52, py = 7, r = 2.6f, show = true };
            p.ff = new CatRig.Leg { jx = 39, jy = 12, px = 49, py = 9, r = 2.6f, show = true, far = true };
            p.bn = new CatRig.Leg { jx = 21, jy = 12, px = 10, py = 8, r = 2.7f, show = true };
            p.bf = new CatRig.Leg { jx = 24, jy = 12, px = 13, py = 10, r = 2.7f, show = true, far = true };
            p.tailX = 16; p.tailY = 17; p.tailAng = 2.85f; p.tailCurl = 0.02f; p.tailLen = 13;
            p.shadowRx = 13;
            return p;
        }

        static CatRig.Pose RunGather()
        {
            var p = CatRig.Stand();
            p.bodyX = 30; p.bodyY = 17; p.bodyRx = 11.5f; p.bodyRy = 7.6f; p.bodyAng = 0.06f;
            p.chestX = 37; p.chestY = 16; p.chestR = 7.2f; p.hipX = 23; p.hipY = 18f; p.hipR = 7.6f;
            p.headX = 45; p.headY = 22; p.headTilt = 0.15f; p.earFold = 0.35f; p.face = CatRig.Face.Wide;
            p.fn = new CatRig.Leg { jx = 37, jy = 12, px = 33, py = CatRig.Ground, r = 2.6f, show = true };
            p.ff = new CatRig.Leg { jx = 35, jy = 12, px = 30, py = CatRig.Ground + 1, r = 2.6f, show = true, far = true };
            p.bn = new CatRig.Leg { jx = 24, jy = 14, px = 32, py = CatRig.Ground + 1, r = 2.7f, show = true };
            p.bf = new CatRig.Leg { jx = 26, jy = 14, px = 35, py = CatRig.Ground + 2, r = 2.7f, show = true, far = true };
            p.tailX = 16; p.tailY = 19; p.tailAng = 2.7f; p.tailCurl = -0.04f; p.tailLen = 16;
            p.shadowRx = 11;
            return p;
        }

        static CatRig.Pose Run(int i, int n)
        {
            // 0~2: 뻗었다가 모으기, 3~5: 모았다가 뻗기 (몸이 공중에 뜨는 순간 포함)
            float t = i < 3 ? i / 2f : 1f - (i - 3) / 2f;
            var p = CatRig.Lerp(RunStretch(), RunGather(), Ease(t));
            float lift = (i == 0 || i == 5) ? 2f : 0f;                  // 뻗었을 때 살짝 뜸
            p.bodyY += lift; p.chestY += lift; p.hipY += lift; p.headY += lift;
            return p;
        }

        static CatRig.Pose StandIdle(int i, int n)
        {
            var p = CatRig.Stand();
            float ph = Ph(i, n);
            p.tailAng = 2.3f + Mathf.Sin(TAU * ph) * 0.18f;
            p.headTilt = Mathf.Sin(TAU * ph) * 0.06f;
            p.eyeOpen = i == n - 2 ? 0 : 1;
            return p;
        }

        // ── 앉아서: 꼬리 끝 살랑, 숨쉬기, 깜빡임, 귀 쫑긋, 고개 갸웃 ──
        static CatRig.Pose SitIdle(int i, int n)
        {
            var p = CatRig.Sit();
            float ph = Ph(i, n);
            float br = Mathf.Sin(TAU * ph) * 0.45f;
            p.bodyRy += br; p.chestR += br * 0.4f; p.headY += br * 0.5f;
            p.tailCurl = 0.06f + Mathf.Sin(TAU * ph) * 0.025f;
            p.eyeOpen = (i == 7) ? 0 : 1;
            p.earFold = (i == 3) ? 0.15f : 0f;
            p.headTilt = Mathf.Sin(TAU * ph * 0.5f) * 0.09f;
            return p;
        }

        static CatRig.Pose LoafIdle(int i, int n)
        {
            var p = CatRig.Loaf();
            float ph = Ph(i, n);
            float br = Mathf.Sin(TAU * ph) * 0.5f;
            p.bodyRy += br; p.headY += br * 0.4f;
            p.eyeOpen = (i >= 4 && i <= 5) ? 0 : 1;
            p.tailCurl = 0.035f + Mathf.Sin(TAU * ph) * 0.012f;
            return p;
        }

        static CatRig.Pose SleepIdle(int i, int n)
        {
            var p = CatRig.Curl();
            float br = Mathf.Sin(TAU * i / n) * 0.6f;
            p.bodyRy += br; p.bodyRx += br * 0.3f; p.hipR += br * 0.3f; p.headY += br * 0.3f;
            p.earFold = i == 2 ? 0.55f : 0.35f;
            return p;
        }

        // ── 그루밍: 앞발을 입가로 들어(머리 앞에 그림) 핥기 ──
        static CatRig.Pose Groom(int i, int n)
        {
            var p = CatRig.Sit();
            float lick = 0.5f + 0.5f * Mathf.Sin(TAU * Ph(i, n) * 2f);
            p.headTilt = 0.22f + lick * 0.08f; p.headX -= 0.5f; p.headY -= 1f;
            float mx = p.headX + p.headR * 0.42f, my = p.headY - p.headR * 0.75f;
            p.fn = new CatRig.Leg { jx = 37, jy = 15, px = mx + 1f + lick, py = my - 3f + lick * 1.5f, r = 2.5f, show = true, over = true };
            p.eyeOpen = 0;
            p.tongue = lick > 0.5f;
            p.tailCurl = 0.065f + Mathf.Sin(TAU * Ph(i, n)) * 0.02f;
            return p;
        }

        // ── 기지개: 섬 → 앞으로 쭉(엉덩이 들고) → 유지 → 섬 ──
        static CatRig.Pose Bow()
        {
            var b = CatRig.Stand();
            b.chestX = 42; b.chestY = 10; b.chestR = 7f;
            b.bodyX = 31; b.bodyY = 13.5f; b.bodyAng = -0.32f;
            b.hipX = 21; b.hipY = 18.5f;
            b.headX = 50; b.headY = 14; b.headTilt = -0.15f; b.eyeOpen = 0;
            b.fn = new CatRig.Leg { jx = 44, jy = 8, px = 56, py = CatRig.Ground, r = 2.6f, show = true };
            b.ff = new CatRig.Leg { jx = 41, jy = 8, px = 52, py = CatRig.Ground, r = 2.6f, show = true, far = true };
            b.tailAng = 1.85f; b.tailCurl = -0.05f;
            return b;
        }

        static CatRig.Pose Stretch(int i, int n)
        {
            float t = i < n * 0.4f ? Ease(i / (n * 0.4f)) : i < n * 0.75f ? 1f : 1f - Ease((i - n * 0.75f) / (n * 0.25f));
            var p = CatRig.Lerp(CatRig.Stand(), Bow(), t);
            if (t > 0.9f) { p.mouthOpen = i % 2 == 0; p.eyeOpen = 0; }      // 하품
            return p;
        }

        // ── 주문: 앉아서 앞발 까딱 + 꼬리 세움 ──
        static CatRig.Pose Order(int i, int n)
        {
            var p = CatRig.Sit();
            float w = Mathf.Sin(TAU * i / n);
            p.fn = new CatRig.Leg { jx = 38, jy = 16, px = 46 + w * 1.5f, py = 22 + Mathf.Abs(w) * 2f, r = 2.5f, show = true, over = true };
            p.headTilt = -0.08f; p.headY += 0.5f;
            p.mouthOpen = i == 2 || i == 3;
            p.tailFront = false; p.tailX = 18; p.tailY = 10; p.tailAng = 2.0f; p.tailCurl = -0.1f + w * 0.03f; p.tailLen = 20;
            return p;
        }

        // ── 먹기: 웅크리고 고개 숙여 그릇에 ──
        static CatRig.Pose Eat(int i, int n)
        {
            var p = CatRig.Loaf();
            float b = Mathf.Abs(Mathf.Sin(TAU * i / n));
            p.headX = 45; p.headY = 15 + b * 1.2f; p.headTilt = 0.3f;
            p.bowl = true; p.face = CatRig.Face.Happy; p.tongue = b < 0.3f;
            p.tailCurl = 0.035f + Mathf.Sin(TAU * i / n) * 0.01f;
            return p;
        }

        static CatRig.Pose SitDown(int i, int n) => CatRig.Lerp(CatRig.Stand(), CatRig.Sit(), Ease(i / (float)(n - 1)));
        static CatRig.Pose LieDown(int i, int n) => CatRig.Lerp(CatRig.Sit(), CatRig.Loaf(), Ease(i / (float)(n - 1)));
        static CatRig.Pose CurlUp(int i, int n) => CatRig.Lerp(CatRig.Loaf(), CatRig.Curl(), Ease(i / (float)(n - 1)));

        // ═════════ 점프 ═════════
        static CatRig.Pose Crouch()
        {
            var p = CatRig.Lerp(CatRig.Stand(), CatRig.Loaf(), 0.55f);
            p.hipY += 2f; p.hipX += 1f; p.bodyAng = -0.12f;
            p.headX = 46; p.headY = 21; p.headTilt = 0.05f; p.face = CatRig.Face.Wide;
            p.tailFront = false; p.tailX = 17; p.tailY = 14; p.tailAng = 2.55f; p.tailCurl = -0.03f; p.tailLen = 14;
            return p;
        }

        // 위로 뛰어오르는 공중 자세: 몸이 비스듬히 쭉, 앞발은 위·앞으로 뻗고 뒷발은 뒤로
        static CatRig.Pose LeapUp()
        {
            var p = CatRig.Stand();
            p.bodyX = 31; p.bodyY = 19; p.bodyRx = 13.5f; p.bodyRy = 6.5f; p.bodyAng = 0.42f;
            p.chestX = 39; p.chestY = 24; p.chestR = 7f;
            p.hipX = 22; p.hipY = 14; p.hipR = 7.5f;
            p.headX = 45; p.headY = 34; p.headTilt = -0.2f; p.face = CatRig.Face.Wide; p.earFold = 0.1f;
            p.fn = new CatRig.Leg { jx = 41, jy = 22, px = 49, py = 31, r = 2.6f, show = true };
            p.ff = new CatRig.Leg { jx = 38, jy = 21, px = 46, py = 30, r = 2.6f, show = true, far = true };
            p.bn = new CatRig.Leg { jx = 22, jy = 12, px = 14, py = 6, r = 2.7f, show = true };
            p.bf = new CatRig.Leg { jx = 25, jy = 12, px = 17, py = 5, r = 2.7f, show = true, far = true };
            p.tailX = 16; p.tailY = 13; p.tailAng = 3.6f; p.tailCurl = 0.03f; p.tailLen = 17;
            p.shadowRx = 9;
            return p;
        }

        // 뛰어내리는 공중 자세: 머리가 아래로, 앞발을 바닥 쪽으로 뻗음
        static CatRig.Pose LeapDown()
        {
            var p = CatRig.Stand();
            p.bodyX = 31; p.bodyY = 16; p.bodyRx = 13.5f; p.bodyRy = 6.5f; p.bodyAng = -0.4f;
            p.chestX = 39; p.chestY = 12; p.chestR = 7f;
            p.hipX = 22; p.hipY = 21; p.hipR = 7.5f;
            p.headX = 47; p.headY = 18; p.headTilt = 0.25f; p.face = CatRig.Face.Wide;
            p.fn = new CatRig.Leg { jx = 41, jy = 10, px = 47, py = CatRig.Ground, r = 2.6f, show = true };
            p.ff = new CatRig.Leg { jx = 38, jy = 10, px = 44, py = CatRig.Ground + 1, r = 2.6f, show = true, far = true };
            p.bn = new CatRig.Leg { jx = 21, jy = 20, px = 13, py = 25, r = 2.7f, show = true };
            p.bf = new CatRig.Leg { jx = 24, jy = 20, px = 16, py = 26, r = 2.7f, show = true, far = true };
            p.tailX = 16; p.tailY = 22; p.tailAng = 2.6f; p.tailCurl = -0.06f; p.tailLen = 17;
            p.shadowRx = 9;
            return p;
        }

        static CatRig.Pose JumpUp(int i, int n)
        {
            switch (i)
            {
                case 0: return CatRig.Lerp(CatRig.Stand(), Crouch(), 0.6f);
                case 1: { var c = Crouch(); c.hipY += 1f; c.tailAng += 0.1f; return c; }   // 엉덩이 씰룩(조준)
                case 2: return CatRig.Lerp(Crouch(), LeapUp(), 0.6f);
                case 3: return LeapUp();
                case 4: return CatRig.Lerp(LeapUp(), Crouch(), 0.7f);
                default: return CatRig.Lerp(Crouch(), CatRig.Loaf(), 0.5f);
            }
        }

        static CatRig.Pose JumpDown(int i, int n)
        {
            switch (i)
            {
                case 0: { var p = CatRig.Lerp(CatRig.Loaf(), CatRig.Stand(), 0.7f); p.headTilt = 0.3f; p.face = CatRig.Face.Wide; p.tailFront = false; p.tailX = 17; p.tailY = 16; p.tailAng = 2.4f; p.tailCurl = -0.06f; p.tailLen = 17; return p; }  // 아래를 내려다봄
                case 1: return CatRig.Lerp(CatRig.Stand(), LeapDown(), 0.5f);
                case 2: return LeapDown();
                case 3: return LeapDown();
                case 4: return CatRig.Lerp(LeapDown(), Crouch(), 0.75f);   // 착지 충격
                default: return CatRig.Lerp(Crouch(), CatRig.Stand(), 0.6f);
            }
        }

        // 창밖 보기: 앉아서 고개를 들고, 꼬리 끝 살랑, 가끔 '깍깍'(채터링)
        static CatRig.Pose Gaze(int i, int n)
        {
            var p = CatRig.Sit();
            float ph = Ph(i, n);
            p.headTilt = -0.28f + Mathf.Sin(TAU * ph) * 0.05f; p.headY += 1f;
            p.face = CatRig.Face.Wide;
            p.mouthOpen = i == 6 || i == 8;
            p.tailFront = false; p.tailX = 18; p.tailY = 6; p.tailAng = -0.4f + Mathf.Sin(TAU * ph * 2f) * 0.25f; p.tailCurl = 0.02f; p.tailLen = 20;
            return p;
        }

        // ═════════ 상호작용 ═════════

        // 부비부비: 선 채로 머리를 앞으로 내밀어 위아래로 비빔, 웃는 눈, 꼬리 꼿꼿(반가움)
        static CatRig.Pose Rub(int i, int n)
        {
            var p = CatRig.Stand();
            float w = Mathf.Sin(TAU * Ph(i, n));
            p.headX = 48 + w * 0.8f; p.headY = 24 + w * 1.6f; p.headTilt = 0.25f - w * 0.18f;
            p.chestX += 1; p.chestY -= 1;
            p.face = CatRig.Face.Happy; p.earFold = 0.12f;
            p.tailAng = 1.65f; p.tailCurl = -0.06f + w * 0.03f;        // 꼬리 세우고 끝만 갈고리
            p.bodyAng = -0.04f;
            return p;
        }

        // 배 보이며 뒹굴: 몸을 좌우로 굴리며 앞발로 허공 꾹꾹
        static CatRig.Pose RollOver(int i, int n)
        {
            var p = CatRig.BellyUp();
            float w = Mathf.Sin(TAU * Ph(i, n));
            p.bodyAng = 0.05f + w * 0.06f; p.headTilt = -0.55f + w * 0.12f; p.headY += w * 0.6f;
            p.fn.px += w * 1.5f; p.fn.py += Mathf.Abs(w) * 1.5f;
            p.ff.px -= w * 1.5f; p.ff.py += (1 - Mathf.Abs(w)) * 1.5f;
            p.tailAng = 3.0f + w * 0.15f;
            return p;
        }

        // 골골: 식빵 자세로 눈 감고 행복, 몸이 미세하게 떨림
        static CatRig.Pose Purr(int i, int n)
        {
            var p = CatRig.Loaf();
            p.face = CatRig.Face.Happy;
            float v = (i % 2 == 0) ? 0.25f : -0.25f;
            p.bodyRy += v; p.headY += v; p.headTilt = 0.1f;
            p.tailCurl = 0.035f + Mathf.Sin(TAU * Ph(i, n)) * 0.015f;
            return p;
        }

        // 킁킁: 고개를 앞으로 길게 빼고 짧게 끄덕
        static CatRig.Pose Sniff(int i, int n)
        {
            var p = CatRig.Stand();
            float w = (i % 3 == 0) ? 1f : 0f;
            p.headX = 49 + w * 0.8f; p.headY = 22 - w * 0.6f; p.headTilt = 0.2f;
            p.chestX += 1.5f; p.chestY -= 1.5f; p.bodyAng = -0.08f;
            p.face = CatRig.Face.Wide;
            p.tailAng = 2.4f; p.tailCurl = -0.04f;
            return p;
        }

        // 핥아주기(상대 머리를): 앉아서 고개를 앞으로 숙여 혀
        static CatRig.Pose Lick(int i, int n)
        {
            var p = CatRig.Sit();
            float w = Mathf.Sin(TAU * Ph(i, n));
            p.headX = 41 + w * 0.6f; p.headY = 29 + w * 1.2f; p.headTilt = 0.32f;
            p.chestX += 1;
            p.eyeOpen = 0; p.tongue = w > -0.2f;
            return p;
        }

        // 놀이 덮치기: 엉덩이 씰룩씰룩 낮게 웅크렸다가 앞발 톡
        static CatRig.Pose Pounce(int i, int n)
        {
            var p = CatRig.Loaf();
            p.face = CatRig.Face.Wide; p.headY = 18; p.headX = 45;
            if (i < n - 2)
            {
                float w = Mathf.Sin(TAU * i / 3f);
                p.hipY = 13 + Mathf.Abs(w) * 1.5f; p.hipX = 19 + w * 1.2f; p.bodyAng = -0.08f;
                p.tailFront = false; p.tailX = 14; p.tailY = 12; p.tailAng = 2.6f + w * 0.15f; p.tailCurl = -0.05f; p.tailLen = 17;
            }
            else
            {
                p.fn = new CatRig.Leg { jx = 44, jy = 9, px = 55, py = 9, r = 2.5f, show = true, over = true };
                p.headX = 47;
                p.tailFront = false; p.tailX = 14; p.tailY = 12; p.tailAng = 2.4f; p.tailCurl = -0.05f; p.tailLen = 17;
            }
            return p;
        }

        // 야옹: 앉아서 고개 들고 입 벌림
        static CatRig.Pose Meow(int i, int n)
        {
            var p = CatRig.Sit();
            bool open = i >= 1 && i <= 3;
            p.headTilt = open ? -0.18f : -0.05f; p.headY += open ? 0.8f : 0f;
            p.mouthOpen = open; p.face = open ? CatRig.Face.Sleepy : CatRig.Face.Normal; if (open) p.eyeOpen = 0;
            p.tailCurl = 0.06f + Mathf.Sin(TAU * Ph(i, n)) * 0.03f;
            return p;
        }

        // 움찔: 몸을 낮추고 귀를 뒤로, 동그래진 눈. 살짝 떨림.
        static CatRig.Pose Flinch(int i, int n)
        {
            var p = CatRig.Loaf();
            p.headX = 41; p.headY = 17; p.headTilt = 0.1f;
            p.bodyX -= 1; p.chestX -= 1.5f;
            p.earFold = 0.85f; p.face = CatRig.Face.Wide;
            float v = (i % 2 == 0) ? 0.3f : -0.3f;
            p.bodyX += v; p.headX += v;
            p.tailCurl = 0.05f;
            return p;
        }

        // 하악: 부푼 몸이 앞뒤로 들썩
        static CatRig.Pose HissIdle(int i, int n)
        {
            var p = CatRig.Hiss();
            float w = Mathf.Sin(TAU * Ph(i, n));
            p.bodyY += w * 0.5f; p.hipY += w * 0.5f; p.headX += w * 0.6f;
            p.mouthOpen = i % 3 != 2;
            p.tailCurl = -0.02f + w * 0.02f;
            return p;
        }

        // 냥냥펀치: 하악 자세에서 앞발을 빠르게 휘두름
        static CatRig.Pose Swat(int i, int n)
        {
            var p = CatRig.Hiss();
            p.bodyY -= 1; p.hipY -= 1;
            float a = TAU * Ph(i, n);
            p.fn = new CatRig.Leg { jx = 41, jy = 14, px = 57 + Mathf.Cos(a) * 4f, py = 12 + Mathf.Sin(a) * 4f, r = 2.6f, show = true, over = true };
            p.mouthOpen = true;
            return p;
        }
    }
}
