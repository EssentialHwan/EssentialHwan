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
                Save(SpriteDrawing.Cat(id), $"{Root}/cats/{id}.png", 32, GroundPivot(32));
                SaveSheet(SpriteDrawing.CatSheet(id), $"{Root}/cats_sheet/{id}_sheet.png", 32);
            }

            // 배경 타일(심리스) + 공간 소품 — 피벗: 타일=좌하단, 벽 소품=하단중앙, 천장 조명=상단중앙
            Save(RoomDrawing.FloorTile(),    $"{Root}/bg/floor.png", 32, new Vector2(0f, 0f));
            Save(RoomDrawing.WallTile(),     $"{Root}/bg/wall.png", 32, new Vector2(0f, 0f));
            Save(RoomDrawing.WainscotTile(), $"{Root}/bg/wainscot.png", 32, new Vector2(0f, 0f));
            Save(RoomDrawing.WindowBig(),    $"{Root}/bg/window_big.png", 32, new Vector2(0.5f, 0f));
            Save(RoomDrawing.Door(),         $"{Root}/bg/door.png", 32, new Vector2(0.5f, 0f));
            Save(RoomDrawing.MenuBoard(),    $"{Root}/bg/menu_board.png", 32, new Vector2(0.5f, 0f));
            Save(RoomDrawing.PendantLamp(),  $"{Root}/bg/pendant_lamp.png", 32, new Vector2(0.5f, 1f));
            Save(RoomDrawing.Counter(),      $"{Root}/bg/counter.png", 32, GroundPivot(56));
            Save(RoomDrawing.BigPlant(),     $"{Root}/bg/big_plant.png", 32, GroundPivot(48));

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
            Debug.Log("[PixelArtGenerator] 픽셀 아트 생성 완료: 고양이 8(+시트 8) · 가구 18 · 아이콘 8 · 공간 9");
        }

        // 가구: 벽걸이는 중앙 피벗(벽 슬롯 중심), 바닥 가구는 접지선(y=4px) 피벗 → 위치 = 바닥 위 발 딛는 점.
        static readonly System.Collections.Generic.HashSet<string> WallMounted = new()
            { "furn_fac_window", "furn_deco_clock", "furn_deco_frame", "furn_deco_curtain" };
        static void SaveFurn(string id, PixelCanvas c) =>
            Save(c, $"{Root}/furniture/{id}.png", 32,
                 WallMounted.Contains(id) || id == "furn_deco_rug" ? new Vector2(0.5f, 0.5f) : GroundPivot(c.H));

        // 접지선 4px 기준 피벗(캔버스 높이별)
        static Vector2 GroundPivot(int canvasHeight) => new Vector2(0.5f, 4f / canvasHeight);
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
                    pivot = new Vector2(0.5f, 4f / cell),    // 발밑 피벗(접지선 4px, 단일 스프라이트와 동일)
                    alignment = (int)SpriteAlignment.Custom
                };
            }
#pragma warning disable CS0618
            ti.spritesheet = metas;
#pragma warning restore CS0618
            ti.SaveAndReimport();
        }

        static void Save(PixelCanvas canvas, string assetPath, int pixelsPerUnit)
            => Save(canvas, assetPath, pixelsPerUnit, new Vector2(0.5f, 0.5f));

        static void Save(PixelCanvas canvas, string assetPath, int pixelsPerUnit, Vector2 pivot)
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
                var st = new TextureImporterSettings();
                ti.ReadTextureSettings(st);
                st.spriteAlignment = (int)SpriteAlignment.Custom;
                st.spritePivot = pivot;
                st.spriteMeshType = SpriteMeshType.FullRect;     // 도트 가장자리 깎임 방지
                ti.SetTextureSettings(st);
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
