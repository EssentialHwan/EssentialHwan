#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using YeogiCafe.Art;

namespace YeogiCafe.EditorTools
{
    // 메뉴 "여기까페냐옹/픽셀 아트 생성"으로 고양이·가구·아이콘 PNG를 일괄 생성.
    // 전부 PixelPalette + PixelCanvas 기반 → 통일된 톤앤매너(한 게임의 리소스).
    // 생성물: Assets/Art/Generated/{cats,furniture,icons}/*.png (Point 필터·무압축 = 크리스프 도트).
    public static class PixelArtGenerator
    {
        const string Root = "Assets/Art/Generated";

        static readonly string[] CatIds = {
            "cat_cheese","cat_calico","cat_cow","cat_black",
            "cat_tuxedo","cat_mackerel","cat_siamese","cat_gray"
        };

        [MenuItem("여기까페냐옹/픽셀 아트 생성")]
        public static void GenerateAll()
        {
            EnsureDir($"{Root}/cats");
            EnsureDir($"{Root}/furniture");
            EnsureDir($"{Root}/icons");

            EnsureDir($"{Root}/cats_sheet");
            EnsureDir($"{Root}/bg");

            // 고양이 8종 — 단일 프레임(정지) + 애니메이션 스프라이트시트(7프레임)
            foreach (var id in CatIds)
            {
                Save(SpriteDrawing.Cat(id), $"{Root}/cats/{id}.png", pixelsPerUnit: 32);
                SaveSheet(SpriteDrawing.CatSheet(id), $"{Root}/cats_sheet/{id}_sheet.png", 32);
            }

            // 배경 타일(심리스)
            Save(SpriteDrawing.FloorTile(), $"{Root}/bg/floor.png", 32);
            Save(SpriteDrawing.WallTile(),  $"{Root}/bg/wall.png", 32);

            // 가구 18종
            SaveFurn("furn_seat_normal", FurnitureDrawing.SeatNormal());
            SaveFurn("furn_seat_window", FurnitureDrawing.SeatWindow());
            SaveFurn("furn_seat_corner", FurnitureDrawing.SeatCorner());
            SaveFurn("furn_seat_two",    FurnitureDrawing.SeatTwo());
            SaveFurn("furn_seat_sofa",   FurnitureDrawing.SeatSofa());
            SaveFurn("furn_seat_bar",    FurnitureDrawing.SeatBar());
            SaveFurn("furn_fac_tower",   FurnitureDrawing.FacTower());
            SaveFurn("furn_fac_cushion", FurnitureDrawing.FacCushion());
            SaveFurn("furn_fac_toybox",  FurnitureDrawing.FacToybox());
            SaveFurn("furn_fac_scratch", FurnitureDrawing.FacScratcher());
            SaveFurn("furn_fac_plant",   FurnitureDrawing.FacPlant());
            SaveFurn("furn_fac_window",  FurnitureDrawing.FacWindow());
            SaveFurn("furn_deco_clock",  FurnitureDrawing.DecoClock());
            SaveFurn("furn_deco_frame",  FurnitureDrawing.DecoFrame());
            SaveFurn("furn_deco_rug",    FurnitureDrawing.DecoRug());
            SaveFurn("furn_deco_lamp",   FurnitureDrawing.DecoLamp());
            SaveFurn("furn_deco_curtain",FurnitureDrawing.DecoCurtain());
            SaveFurn("furn_deco_vase",   FurnitureDrawing.DecoVase());

            // 아이콘 8종
            SaveIcon("icon_star",      SpriteDrawing.IconStar());
            SaveIcon("icon_check",     SpriteDrawing.IconCheck());
            SaveIcon("icon_question",  SpriteDrawing.IconQuestion());
            SaveIcon("icon_gold",      SpriteDrawing.IconGold());
            SaveIcon("icon_clock",     SpriteDrawing.IconClock());
            SaveIcon("icon_magnifier", SpriteDrawing.IconMagnifier());
            SaveIcon("icon_lock",      SpriteDrawing.IconLock());
            SaveIcon("icon_heart",     SpriteDrawing.IconHeart());

            AssetDatabase.Refresh();
            Debug.Log("[PixelArtGenerator] 픽셀 아트 생성 완료: 고양이 8(+시트 8) · 가구 18 · 아이콘 8 · 배경 2 = 44개");
        }

        static void SaveFurn(string id, PixelCanvas c) => Save(c, $"{Root}/furniture/{id}.png", 32);
        static void SaveIcon(string id, PixelCanvas c) => Save(c, $"{Root}/icons/{id}.png", 16);

        // 스프라이트시트: Multiple 모드로 32×32 그리드 슬라이스
        static void SaveSheet(PixelCanvas canvas, string assetPath, int cell)
        {
            var tex = new Texture2D(canvas.W, canvas.H, TextureFormat.RGBA32, false);
            tex.SetPixels32(canvas.Pixels);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(assetPath), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath);

            var ti = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (ti == null) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.spritePixelsPerUnit = cell;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;

            // 프레임 슬라이스 메타 지정
            int frames = canvas.W / cell;
            var metas = new SpriteMetaData[frames];
            string[] names = { "idle0","idle1","walk0","walk1","eat0","eat1","sleep" };
            for (int i = 0; i < frames; i++)
            {
                metas[i] = new SpriteMetaData
                {
                    name = (i < names.Length ? names[i] : "frame" + i),
                    rect = new Rect(i * cell, 0, cell, cell),
                    pivot = new Vector2(0.5f, 0.1f),         // 발밑 피벗(접지)
                    alignment = (int)SpriteAlignment.Custom
                };
            }
#pragma warning disable CS0618
            ti.spritesheet = metas;
#pragma warning restore CS0618
            ti.SaveAndReimport();
        }

        static void Save(PixelCanvas canvas, string assetPath, int pixelsPerUnit)
        {
            var tex = new Texture2D(canvas.W, canvas.H, TextureFormat.RGBA32, false);
            tex.SetPixels32(canvas.Pixels);
            tex.Apply();

            string full = Path.GetFullPath(assetPath);
            File.WriteAllBytes(full, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath);

            // 픽셀아트 임포트 설정: Sprite, Point 필터, 압축 없음, PPU 통일
            var ti = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point;              // 크리스프 도트
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.spritePixelsPerUnit = pixelsPerUnit;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
        }

        static void EnsureDir(string dir)
        {
            if (AssetDatabase.IsValidFolder(dir)) return;
            var parent = Path.GetDirectoryName(dir).Replace("\\", "/");
            var leaf = Path.GetFileName(dir);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
