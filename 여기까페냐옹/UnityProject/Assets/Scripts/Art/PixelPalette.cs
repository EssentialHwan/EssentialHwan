using UnityEngine;

namespace YeogiCafe.Art
{
    // 《여기까페냐옹》 통일 팔레트 — 모든 픽셀 리소스가 오직 이 색만 사용한다.
    // 톤: 파스텔 따뜻톤 코지 카페. 소프트 셰이딩(라이트/미드/셰이드 3톤 세트).
    // 이 파일이 게임 전체 색의 단일 소스. 새 색을 즉석에서 만들지 말 것.
    public static class PixelPalette
    {
        // ── 공용 ──
        public static readonly Color32 Transparent = new(0, 0, 0, 0);
        public static readonly Color32 Outline     = new(74, 54, 47, 255);   // 짙은 갈색(검정 대신, 부드럽게)
        public static readonly Color32 OutlineSoft = new(120, 92, 78, 255);  // 내부 경계용 연한 아웃라인
        public static readonly Color32 Shadow      = new(74, 54, 47, 60);    // 접지 그림자(반투명)
        public static readonly Color32 White       = new(250, 246, 238, 255);// 순백 대신 크림
        public static readonly Color32 Highlight   = new(255, 252, 245, 255);

        // ── 카페 환경(따뜻한 나무·크림) ──
        public static readonly Color32 WoodLight = new(214, 178, 138, 255);
        public static readonly Color32 WoodMid   = new(184, 142, 100, 255);
        public static readonly Color32 WoodDark  = new(150, 110, 74, 255);
        public static readonly Color32 CreamLight= new(245, 232, 212, 255);
        public static readonly Color32 CreamMid  = new(230, 210, 182, 255);

        // ── 파스텔 악센트 (분위기·가구·UI) ──
        public static readonly Color32 PinkLight = new(247, 200, 208, 255);
        public static readonly Color32 PinkMid   = new(230, 160, 172, 255);
        public static readonly Color32 GreenLight= new(190, 214, 168, 255);
        public static readonly Color32 GreenMid  = new(150, 184, 128, 255);
        public static readonly Color32 BlueLight = new(178, 208, 226, 255);
        public static readonly Color32 BlueMid   = new(132, 176, 202, 255);
        public static readonly Color32 YellowLight=new(248, 224, 156, 255);
        public static readonly Color32 YellowMid = new(232, 198, 112, 255);
        public static readonly Color32 LilacLight= new(210, 196, 230, 255);
        public static readonly Color32 LilacMid  = new(176, 156, 208, 255);

        // ── 고양이 털색 (8종, 각 라이트/미드/셰이드 3톤 — 소프트 셰이딩 통일) ──
        public struct FurSet { public Color32 light, mid, shade; }

        public static FurSet Fur(string catId) => catId switch
        {
            "cat_cheese"   => Set(245, 200, 120,  224, 168, 84,  190, 132, 60),   // 치즈(오렌지 태비)
            "cat_calico"   => Set(250, 240, 228,  236, 210, 180, 200, 160, 120),  // 삼색(베이스 크림)
            "cat_cow"      => Set(248, 246, 242,  220, 216, 210, 120, 116, 112),  // 젖소(흰+검 얼룩 베이스)
            "cat_black"    => Set(96, 90, 104,     72, 66, 82,   50, 46, 60),      // 까망(차콜, 순검정 아님)
            "cat_tuxedo"   => Set(240, 238, 234,   90, 86, 96,   60, 56, 68),      // 턱시도(흑백)
            "cat_mackerel" => Set(150, 168, 184,   118, 138, 156, 88, 106, 124),  // 고등어(블루그레이 태비)
            "cat_siamese"  => Set(240, 226, 200,   206, 182, 150, 120, 100, 82),  // 샴(크림+포인트)
            "cat_gray"     => Set(196, 198, 204,   162, 164, 172, 122, 124, 134), // 회색
            _              => Set(220, 200, 180,   190, 168, 146, 150, 128, 108),
        };

        // 고양이 포인트색(귀 안쪽·코) — 통일된 연분홍
        public static readonly Color32 CatNose = new(214, 130, 138, 255);
        public static readonly Color32 CatEye  = new(96, 150, 120, 255);   // 부드러운 초록 눈(통일)

        static FurSet Set(byte lr, byte lg, byte lb, byte mr, byte mg, byte mb, byte sr, byte sg, byte sb)
            => new() { light = new(lr, lg, lb, 255), mid = new(mr, mg, mb, 255), shade = new(sr, sg, sb, 255) };
    }
}
