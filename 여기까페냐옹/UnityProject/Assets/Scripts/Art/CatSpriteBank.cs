using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Art
{
    // 고해상도 고양이 리그(CatRig) → 클립별 Sprite 프레임. 고양이마다 아틀라스 텍스처 1장(클립=행, 프레임=열).
    // 처음 요청될 때 한 번 그린다(고양이 1마리 ≈ 100프레임).
    // 도트 시트: Resources/CatSheets/{catId}.bytes (CatSheet). 정식 아트 교체: Resources/ArtOverride/cat_rig/{catId}.png 에 같은 격자(64x48, 행 순서 = CatClips.All)로 넣으면 그것을 쓴다.
    public static class CatSpriteBank
    {
        public const int FW = CatRig.W, FH = CatRig.H;
        public const float PPU = 32f;          // 가구와 같은 픽셀 크기(32px = 1유닛). 예전 40 → 고양이 25% 크게(리디자인 표정이 보이게)
        public const float Scale = 40f / PPU;  // 예전 크기 대비 배율 — 머리 위 말풍선·감정 아이콘·클릭 범위가 따라 커진다

        static readonly Dictionary<string, Dictionary<string, Sprite[]>> bank = new();

        public static Sprite[] Clip(string catId, string clip)
        {
            var cat = Load(catId);
            if (cat.TryGetValue(clip, out var f)) return f;
            return cat["sit"];
        }

        public static Sprite Portrait(string catId) => Clip(catId, "sit")[0];

        static Dictionary<string, Sprite[]> Load(string catId)
        {
            if (bank.TryGetValue(catId, out var have) && have["sit"][0] != null) return have;

            var defs = CatClips.All;
            bool sheet = CatSheet.Has(catId);                 // 도트 시트가 있으면 그것으로(없으면 리그)
            int Count(int r) => sheet ? CatSheet.FrameCount(catId, defs[r].name) : defs[r].frames;
            int cols = 0; for (int r = 0; r < defs.Length; r++) cols = Mathf.Max(cols, Count(r));
            int rows = defs.Length;

            var tex = Resources.Load<Texture2D>("ArtOverride/cat_rig/" + catId);
            if (tex == null)
            {
                tex = new Texture2D(cols * FW, rows * FH, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "catrig_" + catId };
                var clear = new Color32[tex.width * tex.height];
                tex.SetPixels32(clear);
                for (int r = 0; r < rows; r++)
                    for (int i = 0; i < Count(r); i++)
                    {
                        var px = sheet ? CatSheet.Frame(catId, defs[r].name, i, FW, FH, CatRig.Ground) : CatClips.Frame(catId, defs[r], i).Pixels;
                        tex.SetPixels32(i * FW, (rows - 1 - r) * FH, FW, FH, px);
                    }
                tex.Apply(false, true);     // 읽기 불필요 → GPU 전용
            }
            else tex.filterMode = FilterMode.Point;

            var dict = new Dictionary<string, Sprite[]>();
            var pivot = new Vector2(0.5f, CatRig.Ground / (float)FH);
            for (int r = 0; r < rows; r++)
            {
                var arr = new Sprite[Count(r)];
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = LitSprites.Create(tex, new Rect(i * FW, (rows - 1 - r) * FH, FW, FH), pivot, PPU);
                    arr[i].name = $"{catId}_{defs[r].name}{i}";
                }
                dict[defs[r].name] = arr;
            }
            bank[catId] = dict;
            return dict;
        }
    }
}
