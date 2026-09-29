using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Art;

namespace YeogiCafe.Tests
{
    // 픽셀 리소스 통일성 검증(순수 로직). "전부 한 게임의 리소스"임을 코드로 보장.
    public class PixelArtTests
    {
        static readonly string[] CatIds = {
            "cat_cheese","cat_calico","cat_cow","cat_black",
            "cat_tuxedo","cat_mackerel","cat_siamese","cat_gray"
        };

        static int CountOpaque(PixelCanvas c)
        {
            int n = 0;
            foreach (var p in c.Pixels) if (p.a == 255) n++;
            return n;
        }

        [Test]
        public void AllCats_AreNonEmpty_AndCorrectSize()
        {
            foreach (var id in CatIds)
            {
                var c = SpriteDrawing.Cat(id);
                Assert.AreEqual(32, c.W); Assert.AreEqual(32, c.H);
                Assert.Greater(CountOpaque(c), 120, $"{id}: 스프라이트가 충분히 채워져야");
            }
        }

        [Test]
        public void AllCats_ShareOutlineColor()
        {
            // 통일 규칙: 모든 고양이 아웃라인은 동일한 PixelPalette.Outline
            var outline = PixelPalette.Outline;
            foreach (var id in CatIds)
            {
                var c = SpriteDrawing.Cat(id);
                bool hasOutline = false;
                foreach (var p in c.Pixels)
                    if (p.r == outline.r && p.g == outline.g && p.b == outline.b && p.a == 255) { hasOutline = true; break; }
                Assert.IsTrue(hasOutline, $"{id}: 공용 아웃라인 색을 포함해야(톤 통일)");
            }
        }

        [Test]
        public void EachCat_HasDistinctFur()
        {
            // 팔레트 스왑이 실제로 다른 색을 만들어 8마리가 구별되는지
            var seen = new HashSet<string>();
            foreach (var id in CatIds)
            {
                var f = PixelPalette.Fur(id);
                string key = $"{f.mid.r},{f.mid.g},{f.mid.b}";
                Assert.IsFalse(seen.Contains(key), $"{id}: 털색이 다른 고양이와 겹치면 안 됨");
                seen.Add(key);
            }
        }

        [Test]
        public void Furniture_AllNonEmpty_32()
        {
            var pieces = new List<PixelCanvas> {
                FurnitureDrawing.SeatNormal(), FurnitureDrawing.SeatWindow(), FurnitureDrawing.SeatCorner(),
                FurnitureDrawing.SeatTwo(), FurnitureDrawing.SeatSofa(), FurnitureDrawing.SeatBar(),
                FurnitureDrawing.FacTower(), FurnitureDrawing.FacCushion(), FurnitureDrawing.FacToybox(),
                FurnitureDrawing.FacScratcher(), FurnitureDrawing.FacPlant(), FurnitureDrawing.FacWindow(),
                FurnitureDrawing.DecoClock(), FurnitureDrawing.DecoFrame(), FurnitureDrawing.DecoRug(),
                FurnitureDrawing.DecoLamp(), FurnitureDrawing.DecoCurtain(), FurnitureDrawing.DecoVase(),
            };
            Assert.AreEqual(18, pieces.Count, "가구 18종");
            foreach (var c in pieces)
            {
                Assert.AreEqual(32, c.W); Assert.AreEqual(32, c.H);
                Assert.Greater(CountOpaque(c), 40, "가구 스프라이트가 비어있으면 안 됨");
            }
        }

        [Test]
        public void Icons_AllNonEmpty_16()
        {
            var icons = new List<PixelCanvas> {
                SpriteDrawing.IconStar(), SpriteDrawing.IconCheck(), SpriteDrawing.IconQuestion(),
                SpriteDrawing.IconGold(), SpriteDrawing.IconClock(), SpriteDrawing.IconMagnifier(),
                SpriteDrawing.IconLock(), SpriteDrawing.IconHeart(),
            };
            Assert.AreEqual(8, icons.Count);
            foreach (var c in icons)
            {
                Assert.AreEqual(16, c.W); Assert.AreEqual(16, c.H);
                Assert.Greater(CountOpaque(c), 8, "아이콘이 비어있으면 안 됨");
            }
        }

        [Test]
        public void Canvas_AutoOutline_WrapsOpaque()
        {
            var c = new PixelCanvas(8, 8);
            c.FillRect(3, 3, 4, 4, PixelPalette.PinkMid);
            c.AutoOutline(PixelPalette.Outline);
            // 채운 블록 바로 위 픽셀은 아웃라인이어야
            var above = c.Get(3, 5);
            Assert.AreEqual(PixelPalette.Outline.r, above.r);
            Assert.AreEqual((byte)255, above.a);
        }

        [Test]
        public void Canvas_GroundShadow_IsSemiTransparent()
        {
            var c = new PixelCanvas(16, 8);
            c.GroundShadow(8, 4, 6);
            var s = c.Get(8, 4);
            Assert.Greater(s.a, 0); Assert.Less(s.a, 255, "접지 그림자는 반투명");
        }
    }
}
