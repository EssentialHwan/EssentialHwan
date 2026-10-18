using System;
using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Art
{
    // 고양이 행동 클립(프레임 시퀀스). 포즈 파라미터를 시간에 따라 움직여 부드러운 모션을 만든다.
    // 이름은 CatView가 행동 상태에 맞춰 고른다.
    public static class CatClips
    {
        public struct Def { public string name; public float fps; public bool loop; public int frames; public Func<int, int, CatRig.Pose> pose; }

        const float TAU = Mathf.PI * 2f;

        public static readonly Def[] All =
        {
            new Def { name = "walk",     fps = 11, loop = true,  frames = 8, pose = Walk },
            new Def { name = "sit",      fps = 6,  loop = true,  frames = 12, pose = SitIdle },
            new Def { name = "loaf",     fps = 4,  loop = true,  frames = 8, pose = LoafIdle },
            new Def { name = "sleep",    fps = 3,  loop = true,  frames = 6, pose = SleepIdle },
            new Def { name = "groom",    fps = 8,  loop = true,  frames = 8, pose = Groom },
            new Def { name = "stretch",  fps = 8,  loop = false, frames = 10, pose = Stretch },
            new Def { name = "order",    fps = 8,  loop = true,  frames = 8, pose = Order },
            new Def { name = "eat",      fps = 7,  loop = true,  frames = 6, pose = Eat },
            new Def { name = "sitdown",  fps = 10, loop = false, frames = 5, pose = SitDown },
            new Def { name = "liedown",  fps = 8,  loop = false, frames = 5, pose = LieDown },
            new Def { name = "curlup",   fps = 7,  loop = false, frames = 5, pose = CurlUp },
            new Def { name = "stand",    fps = 6,  loop = true,  frames = 8, pose = StandIdle },
        };

        public static Def Find(string name)
        {
            foreach (var d in All) if (d.name == name) return d;
            return All[1];
        }

        public static PixelCanvas Frame(string catId, Def d, int i) => CatRig.Render(catId, d.pose(i, d.frames));

        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        // ── 걷기: 왼뒤→왼앞→오른뒤→오른앞 순서(실제 고양이 보행), 몸 오르내림, 머리·꼬리 흔들림 ──
        static CatRig.Pose Walk(int i, int n)
        {
            var p = CatRig.Stand();
            float ph = i / (float)n;
            void Step(ref CatRig.Leg l, float off, float stride, float lift)
            {
                float a = TAU * (ph + off);
                l.px += Mathf.Cos(a) * stride;
                l.py += Mathf.Max(0f, Mathf.Sin(a)) * lift;
                if (l.knee) { l.kx += Mathf.Cos(a) * stride * 0.6f; l.ky += Mathf.Max(0f, Mathf.Sin(a)) * lift * 0.8f; }
            }
            Step(ref p.bn, 0.00f, 4f, 3f); Step(ref p.fn, 0.25f, 4.5f, 3f);
            Step(ref p.bf, 0.50f, 4f, 3f); Step(ref p.ff, 0.75f, 4.5f, 3f);
            float bob = Mathf.Cos(TAU * ph * 2f) * 0.8f;
            p.bodyY += bob; p.chestY += bob; p.hipY += bob;
            p.headY += bob * 0.6f + Mathf.Sin(TAU * ph) * 0.4f;
            p.tailAng = 2.25f + Mathf.Sin(TAU * ph) * 0.12f; p.tailCurl = -0.09f + Mathf.Sin(TAU * ph + 1f) * 0.03f;
            return p;
        }

        // ── 서서 둘러보기 ──
        static CatRig.Pose StandIdle(int i, int n)
        {
            var p = CatRig.Stand();
            float ph = i / (float)n;
            p.tailAng = 2.4f + Mathf.Sin(TAU * ph) * 0.18f;
            p.headTilt = Mathf.Sin(TAU * ph) * 0.06f;
            p.eyeOpen = i == n - 2 ? 0 : 1;
            return p;
        }

        // ── 앉아서: 꼬리 끝 살랑, 숨쉬기, 가끔 깜빡임·귀 쫑긋 ──
        static CatRig.Pose SitIdle(int i, int n)
        {
            var p = CatRig.Sit();
            float ph = i / (float)n;
            float br = Mathf.Sin(TAU * ph) * 0.45f;
            p.bodyRy += br; p.chestR += br * 0.4f; p.headY += br * 0.5f;
            p.tailCurl = 0.06f + Mathf.Sin(TAU * ph) * 0.025f;
            p.eyeOpen = (i == 7) ? 0 : 1;
            p.earFold = (i == 3) ? 0.18f : 0f;
            p.headTilt = Mathf.Sin(TAU * ph * 0.5f) * 0.05f;
            return p;
        }

        // ── 식빵: 아주 느린 숨쉬기 + 졸린 눈 ──
        static CatRig.Pose LoafIdle(int i, int n)
        {
            var p = CatRig.Loaf();
            float ph = i / (float)n;
            float br = Mathf.Sin(TAU * ph) * 0.5f;
            p.bodyRy += br; p.headY += br * 0.4f;
            p.eyeOpen = (i >= 4 && i <= 5) ? 0 : 1;
            p.tailCurl = 0.035f + Mathf.Sin(TAU * ph) * 0.012f;
            return p;
        }

        // ── 둥글게 말아 자기: 숨쉬기(몸이 부풀었다 꺼짐), 귀 가끔 씰룩 ──
        static CatRig.Pose SleepIdle(int i, int n)
        {
            var p = CatRig.Curl();
            float br = Mathf.Sin(TAU * i / n) * 0.6f;
            p.bodyRy += br; p.bodyRx += br * 0.3f; p.hipR += br * 0.3f;
            p.earFold = i == 2 ? 0.45f : 0.25f;
            return p;
        }

        // ── 그루밍: 앞발을 들어 핥고 → 얼굴 쓸기 ──
        static CatRig.Pose Groom(int i, int n)
        {
            var p = CatRig.Sit();
            float ph = i / (float)n;
            float lick = 0.5f + 0.5f * Mathf.Sin(TAU * ph * 2f);
            p.fn = new CatRig.Leg { jx = 38, jy = 17, px = 44 + lick * 1.5f, py = 22 + lick * 2f, r = 2.4f, show = true };
            p.headTilt = 0.35f + lick * 0.1f;
            p.headX -= 1; p.headY -= 1.5f;
            p.eyeOpen = 0;
            p.tongue = lick > 0.5f;
            p.tailCurl = 0.065f + Mathf.Sin(TAU * ph) * 0.02f;
            return p;
        }

        // ── 기지개: 앉음 → 앞으로 쭉(엉덩이 들고) → 유지 → 섬 ──
        static CatRig.Pose Stretch(int i, int n)
        {
            var st = CatRig.Stand();
            var bow = CatRig.Stand();
            bow.chestX = 44; bow.chestY = 11; bow.chestR = 7.5f;
            bow.bodyX = 31; bow.bodyY = 15; bow.bodyAng = -0.35f;
            bow.hipX = 20; bow.hipY = 21;
            bow.headX = 53; bow.headY = 15; bow.headTilt = -0.25f; bow.eyeOpen = 0;
            bow.fn = new CatRig.Leg { jx = 46, jy = 9, px = 56, py = CatRig.Ground, r = 2.5f, show = true };
            bow.ff = new CatRig.Leg { jx = 43, jy = 9, px = 52, py = CatRig.Ground, r = 2.5f, show = true, far = true };
            bow.tailAng = 1.9f; bow.tailCurl = -0.05f;
            float t = i < n * 0.4f ? Ease(i / (n * 0.4f)) : i < n * 0.75f ? 1f : 1f - Ease((i - n * 0.75f) / (n * 0.25f));
            var p = CatRig.Lerp(st, bow, t);
            p.tongue = t > 0.9f && i % 2 == 0;      // 하품하듯
            return p;
        }

        // ── 주문: 앉아서 앞발을 들어 까딱까딱 + 꼬리 세움 ──
        static CatRig.Pose Order(int i, int n)
        {
            var p = CatRig.Sit();
            float w = Mathf.Sin(TAU * i / n);
            p.fn = new CatRig.Leg { jx = 39, jy = 18, px = 46 + w * 1.5f, py = 27 + Mathf.Abs(w) * 2f, r = 2.4f, show = true };
            p.headTilt = -0.08f; p.headY += 0.5f;
            p.tailFront = false; p.tailX = 17; p.tailY = 10; p.tailAng = 2.0f; p.tailCurl = -0.1f + w * 0.03f; p.tailLen = 20;
            return p;
        }

        // ── 먹기: 웅크리고 고개 숙여 그릇에 ──
        static CatRig.Pose Eat(int i, int n)
        {
            var p = CatRig.Loaf();
            float b = Mathf.Abs(Mathf.Sin(TAU * i / n));
            p.headX = 47; p.headY = 13 + b * 1.5f; p.headTilt = 0.55f;
            p.bowl = true; p.eyeOpen = 0; p.tongue = b < 0.3f;
            p.tailCurl = 0.035f + Mathf.Sin(TAU * i / n) * 0.01f;
            return p;
        }

        // ── 전환(한 번 재생): 서기→앉기, 앉기→식빵, 식빵→둥글게 ──
        static CatRig.Pose SitDown(int i, int n) => CatRig.Lerp(CatRig.Stand(), CatRig.Sit(), Ease(i / (float)(n - 1)));
        static CatRig.Pose LieDown(int i, int n) => CatRig.Lerp(CatRig.Sit(), CatRig.Loaf(), Ease(i / (float)(n - 1)));
        static CatRig.Pose CurlUp(int i, int n) => CatRig.Lerp(CatRig.Loaf(), CatRig.Curl(), Ease(i / (float)(n - 1)));
    }
}
