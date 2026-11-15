using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.Art
{
    // 선물 아이콘 16×16 (모양 템플릿 × 색)
    public static class GiftDrawing
    {
        static readonly Color32[][] Pal =
        {
            new Color32[]{ new(120, 82, 58, 255), new(160, 112, 80, 255) },    // 0 커피콩
            new Color32[]{ new(236, 232, 224, 255), new(255, 252, 246, 255) }, // 1 흰색(뼈·뚜껑)
            new Color32[]{ new(222, 84, 96, 255), new(246, 140, 150, 255) },   // 2 빨강
            new Color32[]{ new(150, 160, 172, 255), new(196, 204, 214, 255) }, // 3 은색
            new Color32[]{ new(110, 168, 96, 255), new(156, 206, 132, 255) },  // 4 초록
            new Color32[]{ new(208, 140, 60, 255), new(240, 186, 100, 255) },  // 5 캐러멜
            new Color32[]{ new(196, 128, 76, 255), new(232, 170, 110, 255) },  // 6 구운 꼬리
            new Color32[]{ new(132, 176, 96, 255), new(240, 232, 216, 255) },  // 7 잎/깃털
            new Color32[]{ new(150, 146, 150, 255), new(196, 192, 196, 255) }, // 8 회색
            new Color32[]{ new(84, 78, 92, 255), new(240, 228, 210, 255) },    // 9 털 두 가닥
            new Color32[]{ new(236, 214, 226, 255), new(255, 244, 250, 255) }, // 10 솜(분홍빛)
            new Color32[]{ new(210, 170, 70, 255), new(240, 210, 120, 255) },  // 11 병뚜껑
            new Color32[]{ new(150, 112, 80, 255), new(214, 186, 150, 255) },  // 12 참새 깃털
            new Color32[]{ new(170, 124, 82, 255), new(210, 168, 120, 255) },  // 13 나무
            new Color32[]{ new(96, 150, 220, 255), new(160, 200, 240, 255) },  // 14 파랑 공
            new Color32[]{ new(140, 210, 220, 200), new(220, 248, 255, 230) }, // 15 유리구슬
        };

        public static PixelCanvas Draw(string giftId)
        {
            var c = new PixelCanvas(16, 16);
            if (!GiftCatalog.TryGet(giftId, out var g)) return c;
            var d = Pal[Mathf.Clamp(g.color, 0, Pal.Length - 1)][0];
            var l = Pal[Mathf.Clamp(g.color, 0, Pal.Length - 1)][1];
            switch (g.shape)
            {
                case GiftCatalog.Shape.Bone:
                    c.FillRect(4, 7, 11, 8, l); c.Disc(3, 6, 1, l); c.Disc(3, 9, 1, l); c.Disc(12, 6, 1, l); c.Disc(12, 9, 1, l);
                    c.HLine(4, 11, 7, d); break;
                case GiftCatalog.Shape.Leaf:
                    for (int y = 3; y <= 12; y++) { int w = (int)(Mathf.Sin((y - 3) / 9f * Mathf.PI) * 4f); c.HLine(8 - w, 8 + w, y, y % 2 == 0 ? d : l); }
                    c.VLine(8, 2, 12, d); c.Set(8, 1, d); break;
                case GiftCatalog.Shape.Feather:
                    for (int i = 0; i < 9; i++) { c.HLine(4 + i / 2, 8 + i / 2, 3 + i, l); }
                    for (int i = 0; i < 11; i++) c.Set(4 + i / 2 + 2, 2 + i, d); break;
                case GiftCatalog.Shape.Ball:
                    c.Disc(8, 8, 5, d); c.Disc(7, 9, 3, l); c.Set(6, 10, PixelPalette.Highlight); break;
                case GiftCatalog.Shape.Stone:
                    c.FillRoundRect(3, 4, 12, 10, 3, d); c.FillRoundRect(4, 7, 10, 9, 2, l); break;
                case GiftCatalog.Shape.Disc:
                    c.Disc(8, 8, 5, d); c.Disc(8, 8, 3, l); c.Set(6, 10, PixelPalette.Highlight); break;
                case GiftCatalog.Shape.Fluff:
                    c.Disc(6, 7, 3, l); c.Disc(10, 7, 3, l); c.Disc(8, 9, 3, l); c.Set(5, 9, d); c.Set(11, 6, d); c.Set(8, 6, d); break;
                case GiftCatalog.Shape.Bean:
                    c.FillRoundRect(4, 4, 11, 11, 3, d); c.VLine(8, 5, 10, l); break;
                case GiftCatalog.Shape.Ribbon:
                    c.FillRect(3, 6, 6, 10, d); c.FillRect(10, 6, 13, 10, d); c.FillRect(7, 7, 9, 9, l); c.Set(5, 4, d); c.Set(11, 4, d); break;
                case GiftCatalog.Shape.Berry:
                    c.Disc(8, 7, 4, d); c.HLine(5, 11, 11, PixelPalette.GreenMid); c.HLine(6, 10, 12, PixelPalette.GreenDark); c.Set(7, 6, l); c.Set(9, 8, l); break;
                case GiftCatalog.Shape.Tail:
                    c.FillRect(3, 7, 9, 9, d); for (int i = 0; i < 4; i++) c.VLine(10 + i, 7 - i, 9 + i, l); c.HLine(3, 8, 8, l); break;
                case GiftCatalog.Shape.Candy:
                    c.FillRoundRect(5, 5, 11, 11, 1, d); c.FillRect(6, 9, 10, 10, l); break;
                case GiftCatalog.Shape.Stick:
                    for (int i = 0; i < 10; i++) { c.Set(3 + i, 4 + i, d); c.Set(4 + i, 4 + i, l); } break;
            }
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }
    }
}
