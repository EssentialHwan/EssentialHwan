using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Art
{
    // 창밖 거리의 행인·탈것 도트(10/29). 문자 템플릿 → PixelCanvas. 미리보기: Tools/street/sprites.py (같은 문자열)
    //  . 투명  o 외곽선  F 털  L 밝은 털  e 눈  p 코  B 옷·차체  b 차체 그늘  w 유리  k 바퀴  s 은색  r 빨강  y 노랑(라이트)  u/U 우산
    public static class StreetArt
    {
        static readonly Dictionary<string, string[]> T = new()
        {
            ["walk4_0"] = new[] {
                "...........o..o.",
                "..........oFooFo",
                "o.........oFFFFo",
                "oFo.......oFeFeo",
                ".oFo......oLFpLo",
                "..oFooooooFFLLo.",
                "..oFFFFFFFFFFo..",
                "..oFFFFFFFFLLo..",
                "...oFFFFFFFLLo..",
                "...oFo.oFo.oFo..",
                "...oo..oo..oo..." },
            ["walk4_1"] = new[] {
                "...........o..o.",
                "..........oFooFo",
                "o.........oFFFFo",
                "oFo.......oFeFeo",
                ".oFo......oLFpLo",
                "..oFooooooFFLLo.",
                "..oFFFFFFFFFFo..",
                "..oFFFFFFFFLLo..",
                "...oFFFFFFFLLo..",
                "..oFo..oFo..oFo.",
                "..oo...oo...oo.." },
            ["umb_0"] = new[] {
                "......o......",
                "...ooUuuoo...",
                ".ooUUuuuuuoo.",
                "oUUUuuuuuuuuo",
                "ooooooooooooo",
                "......o......",
                "....oo.o.o...",
                "....oFoFFo...",
                "....oFeFeo...",
                "....oLFpLo...",
                "....ooBBBoo..",
                "....oBBBBBo..",
                "....oBBBBo...",
                "....oBBBBo...",
                "....oLo.oLo..",
                "....oo..oo...",
                "............." },
            ["umb_1"] = new[] {
                "......o......",
                "...ooUuuoo...",
                ".ooUUuuuuuoo.",
                "oUUUuuuuuuuuo",
                "ooooooooooooo",
                "......o......",
                "....oo.o.o...",
                "....oFoFFo...",
                "....oFeFeo...",
                "....oLFpLo...",
                "....ooBBBoo..",
                "....oBBBBBo..",
                "....oBBBBo...",
                "....oBBBBo...",
                "...oLo..oLo..",
                "...oo....oo..",
                "............." },
            ["bag_0"] = new[] {
                "....o..o....",
                "...oFooFo...",
                "...oFFFFo...",
                "...oFeFeo...",
                "...oLFpLo...",
                "....oBBo....",
                "...oBBBBo...",
                "..oFBBBBoo..",
                "..o.oBBBorro",
                "....oBBBorro",
                "....oLooLorr",
                "....oo..oo.." },
            ["bag_1"] = new[] {
                "....o..o....",
                "...oFooFo...",
                "...oFFFFo...",
                "...oFeFeo...",
                "...oLFpLo...",
                "....oBBo....",
                "...oBBBBo...",
                "..oFBBBBoo..",
                "..o.oBBBorro",
                "....oBBBorro",
                "...oLo.oLorr",
                "...oo...oo.." },
            ["car_0"] = new[] {
                "........oooooooooo..........",
                ".......oBwwwwoowwwo.........",
                "......oBwwo.owowwwwo........",
                ".....oBBwoFooFowwwwwo.......",
                "....oBBBwoeFeFowwwwwwoooo...",
                "..ooBBBBBoLpLooBBBBBBBBBBoo.",
                ".oBBBBBBBBBBBBBBBBBBBBBBBByo",
                ".oBBBBBBBBbBBBBBBBBbBBBBBByo",
                ".obbbbbbbbbbbbbbbbbbbbbbbbbo",
                ".ooookkkooooooooooookkkooooo",
                "....kkskk..........kkskk....",
                "....kksk...........kksk.....",
                ".....kk.............kk......" },
            ["car_1"] = new[] {
                "........oooooooooo..........",
                ".......oBwwwwoowwwo.........",
                "......oBwwo.owowwwwo........",
                ".....oBBwoFooFowwwwwo.......",
                "....oBBBwoeFeFowwwwwwoooo...",
                "..ooBBBBBoLpLooBBBBBBBBBBoo.",
                ".oBBBBBBBBBBBBBBBBBBBBBBBByo",
                ".oBBBBBBBBbBBBBBBBBbBBBBBByo",
                ".obbbbbbbbbbbbbbbbbbbbbbbbbo",
                ".ooookkkooooooooooookkkooooo",
                "....kskkk..........kskkk....",
                "....kkks...........kkks.....",
                ".....kk.............kk......" },
            ["bike_0"] = new[] {
                "..........o..o....",
                ".........oFooFo...",
                ".........oFFFFo...",
                ".........oFeFeo...",
                ".........oLFpLo...",
                "........ooBBBo....",
                ".......oBBBBBoss..",
                "......oBBBBoo..s..",
                ".....oBBBoLo...s..",
                "....sssssoo.sss...",
                "..kkk.s.s...s.kkk.",
                ".k...k.s.s.s.k...k",
                ".k.s.k..sss..k.s.k",
                ".k...k.......k...k",
                "..kkk.........kkk." },
            ["bike_1"] = new[] {
                "..........o..o....",
                ".........oFooFo...",
                ".........oFFFFo...",
                ".........oFeFeo...",
                ".........oLFpLo...",
                "........ooBBBo....",
                ".......oBBBBBoss..",
                "......oBBBBoo..s..",
                ".....oBBBoLo...s..",
                "....sssssoo.sss...",
                "..kkk.s.s...s.kkk.",
                ".k...k.s.s.s.k...k",
                ".ks..k..sss..ks..k",
                ".k...k.......k...k",
                "..kkk.........kkk." },
            ["moto_0"] = new[] {
                "............oooo.......",
                "...........orrrro......",
                "..oooooo..orrrrrro.....",
                "..oyyyyo..oFeFeFo......",
                "..oyyyyo..oLFpLo.......",
                "..oyyyyo...oBBo........",
                "..oooooo..oBBBBo..ss...",
                "...ooooo.oBBBoo..s.....",
                "..orrrrrrrrrrrrrrrrro..",
                ".orrrrrrrrrrrrrrrrrryo.",
                "..oooooooooooooooooo...",
                "..kkk............kkk...",
                ".k.s.k..........k.s.k..",
                ".k...k..........k...k..",
                "..kkk............kkk..." },
            ["moto_1"] = new[] {
                "............oooo.......",
                "...........orrrro......",
                "..oooooo..orrrrrro.....",
                "..oyyyyo..oFeFeFo......",
                "..oyyyyo..oLFpLo.......",
                "..oyyyyo...oBBo........",
                "..oooooo..oBBBBo..ss...",
                "...ooooo.oBBBoo..s.....",
                "..orrrrrrrrrrrrrrrrro..",
                ".orrrrrrrrrrrrrrrrrryo.",
                "..oooooooooooooooooo...",
                "..kkk............kkk...",
                ".ks..k..........ks..k..",
                ".k...k..........k...k..",
                "..kkk............kkk..." },
            ["bus_0"] = new[] {
                "..oooooooooooooooooooooooooooooooooooooooooooo...",
                ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBo..",
                ".oBwwwwwwoBwwwwwwoBwwwwwwoBwwwwwwoBwwwwwwwwwwBo..",
                ".oBwoFoFwoBwwwwwwoBwoFoFwoBwwoFoFoBwwwwwwwwwwBo..",
                ".oBwFeFewoBwwwwwwoBwFeFewoBwwFeFeoBwwwwwwwwwwBo..",
                ".oBwLpLwwoBwwwwwwoBwLpLwwoBwwLpLwoBwwwwwwwwwwBo..",
                ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBo.",
                ".oyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyBo.",
                ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBByo",
                ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBByo",
                ".obbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbo",
                ".ooooookkkkooooooooooooooooooooooooookkkkoooooo..",
                ".....kkssskk......................kkssskk........",
                ".....kksskkk......................kksskkk........",
                "......kkkk.........................kkkk.........." },
            ["bus_1"] = new[] {
                "..oooooooooooooooooooooooooooooooooooooooooooo...",
                ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBo..",
                ".oBwwwwwwoBwwwwwwoBwwwwwwoBwwwwwwoBwwwwwwwwwwBo..",
                ".oBwoFoFwoBwwwwwwoBwoFoFwoBwwoFoFoBwwwwwwwwwwBo..",
                ".oBwFeFewoBwwwwwwoBwFeFewoBwwFeFeoBwwwwwwwwwwBo..",
                ".oBwLpLwwoBwwwwwwoBwLpLwwoBwwLpLwoBwwwwwwwwwwBo..",
                ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBo.",
                ".oyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyBo.",
                ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBByo",
                ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBByo",
                ".obbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbo",
                ".ooooookkkkooooooooooooooooooooooooookkkkoooooo..",
                ".....kkskskk......................kkskskk........",
                ".....kkkskkk......................kkkskkk........",
                "......kkkk.........................kkkk.........." },
            ["tiny_0"] = new[] {
                "....o.o",
                "o..oFFo",
                "oFFFFeF",
                ".oFFFFo",
                ".o.o.o.",
                "......." },
            ["tiny_1"] = new[] {
                "....o.o",
                "o..oFFo",
                "oFFFFeF",
                ".oFFFFo",
                "o.o.o..",
                "......." },
            ["bird_0"] = new[] {
                "o...o",
                ".o.o.",
                "..o.." },
            ["bird_1"] = new[] {
                ".....",
                "oo.oo",
                "..o.." },
        };

        static Color32 C(int r, int g, int b) => new Color32((byte)r, (byte)g, (byte)b, 255);
        // 털: 치즈·회색·검정·흰·크림·고등어(진회갈)
        static readonly Color32[] FurD = { C(240, 156, 70), C(150, 152, 166), C(66, 60, 72), C(244, 240, 234), C(236, 206, 160), C(128, 112, 96) };
        static readonly Color32[] FurL = { C(252, 214, 170), C(226, 228, 236), C(130, 120, 130), C(255, 255, 255), C(252, 238, 214), C(214, 200, 180) };
        // 옷·차체: 빨강·파랑·노랑·초록·흰·분홍
        static readonly Color32[] BodyD = { C(230, 92, 92), C(86, 150, 214), C(250, 200, 80), C(120, 190, 130), C(238, 238, 240), C(240, 160, 190) };
        static readonly Color32[] BodyS = { C(176, 60, 70), C(60, 106, 170), C(200, 150, 50), C(80, 140, 96), C(170, 176, 190), C(196, 116, 150) };
        public const int FurCount = 6, BodyCount = 6;

        public static bool Has(string key) => T.ContainsKey(key);

        // dim: 비·밤에 살짝 어둡게(창밖 풍경 톤과 맞춤). lights: 밤 전조등·창문 노랗게 밝게
        public static Sprite Get(string tpl, int fur, int body, int dimPct = 100, bool lights = false)
        {
            string key = $"street_{tpl}_{fur}_{body}_{dimPct}_{(lights ? 1 : 0)}";
            return RuntimeSprites.Get(key, () => Draw(T[tpl], fur, body, dimPct / 100f, lights));
        }

        static PixelCanvas Draw(string[] rows, int fur, int body, float dim, bool lights)
        {
            int h = rows.Length, w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            var c = new PixelCanvas(w, h);
            for (int ry = 0; ry < h; ry++)
            {
                string r = rows[ry]; int y = h - 1 - ry;
                for (int x = 0; x < r.Length; x++)
                {
                    char ch = r[x]; if (ch == '.') continue;
                    Color32 col = ch switch
                    {
                        'o' => C(54, 40, 44), 'e' => C(30, 24, 30), 'p' => C(240, 150, 160), 'k' => C(40, 38, 46), 's' => C(190, 196, 206),
                        'r' => C(222, 70, 70), 'u' => C(120, 170, 230), 'U' => C(86, 128, 196),
                        'w' => lights ? C(255, 214, 140) : C(178, 214, 236),
                        'y' => lights ? C(255, 248, 200) : C(255, 226, 120),
                        'F' => FurD[fur % FurCount], 'L' => FurL[fur % FurCount],
                        'B' => BodyD[body % BodyCount], 'b' => BodyS[body % BodyCount],
                        _ => C(255, 0, 255)
                    };
                    bool glow = lights && (ch == 'y' || ch == 'w');
                    if (!glow && dim < 0.999f) col = new Color32((byte)(col.r * dim), (byte)(col.g * dim), (byte)(col.b * dim), 255);
                    c.Set(x, y, col);
                }
            }
            return c;
        }
    }
}
