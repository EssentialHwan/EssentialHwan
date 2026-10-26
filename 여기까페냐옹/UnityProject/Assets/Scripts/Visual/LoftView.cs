using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Art;
using YeogiCafe.Core;
using YeogiCafe.Data;

namespace YeogiCafe.Visual
{
    // Lv3 '위로 넓히기': 벽 윗부분에 캣워크 선반(가로 전체) + 오른쪽 다락 데크(난간) + 사다리.
    // 고양이는 사다리 아래에서 기어올라 데크·선반의 쉼터(LoftSpot)로 걸어간다. 추리 단서는 아니고 말풍선만(높은 곳 좋아하는 고양이가 자주 간다).
    public class LoftView : MonoBehaviour
    {
        static LoftView inst;
        const float CatwalkLeft = 3.0f;                 // 선반은 통유리 위쪽만(왼쪽 벽걸이 자리는 비워 둠)
        readonly List<GameObject> parts = new();
        readonly List<LoftSpot> spots = new();
        CafeContext cafe;
        int built = -1;

        public static void Apply(int level)
        {
            if (inst == null) inst = new GameObject("[Loft]").AddComponent<LoftView>();
            inst.Set(level >= 3);
        }

        // 벽걸이 가구·창이 사다리·데크와 겹치지 않게(FixtureManager.WallBlockedAt)
        public static IEnumerable<Rect> WallRects()
        {
            if (inst == null || inst.built != 1) yield break;
            yield return new Rect(CafeLayout.LadderX - 0.45f, CafeLayout.FloorTop, 0.9f, CafeLayout.LoftDeckY - CafeLayout.FloorTop + 0.2f);
            yield return new Rect(CatwalkLeft, CafeLayout.LoftDeckY - 0.2f, CafeLayout.DeckRight - CatwalkLeft, 1.4f);   // 창틀 윗단(7.47)과는 안 겹치게
        }

        void Set(bool on)
        {
            int want = on ? 1 : 0;
            if (want == built) return;
            built = want;
            if (cafe == null) cafe = FindObjectOfType<CafeContext>();
            foreach (var s in spots) if (s != null && cafe != null) cafe.UnregisterFacility(s);
            foreach (var g in parts) if (g != null) Destroy(g);
            parts.Clear(); spots.Clear();
            if (!on) return;

            float deckY = CafeLayout.LoftDeckY;
            // 캣워크 선반: 왼쪽 끝 ~ 데크 앞(1유닛 조각을 이어 붙임)
            for (float x = CatwalkLeft; x < CafeLayout.DeckLeft - 0.01f; x += 1f)
                Part("Catwalk", RuntimeSprites.Get("loft_shelf", LoftDrawing.Shelf), new Vector3(x + 0.5f, deckY - 14f / 32f, 0f), YSort.WallProps + 6);
            // 다락 데크 + 난간
            int deckPx = Mathf.RoundToInt((CafeLayout.DeckRight - CafeLayout.DeckLeft) * 32f);
            Part("LoftDeck", RuntimeSprites.Get("loft_deck_" + deckPx, () => LoftDrawing.Deck(deckPx)),
                 new Vector3((CafeLayout.DeckLeft + CafeLayout.DeckRight) * 0.5f, deckY - 18f / 32f, 0f), YSort.WallProps + 6);
            Part("LoftRail", RuntimeSprites.Get("loft_rail_" + deckPx, () => LoftDrawing.Rail(deckPx)),
                 new Vector3((CafeLayout.DeckLeft + CafeLayout.DeckRight) * 0.5f, deckY, 0f), YSort.WallProps + 40);   // 고양이 앞(난간 사이로 보임)
            // 사다리: 바닥(징두리 아래)부터 데크까지
            int ladderPx = Mathf.RoundToInt((deckY - (CafeLayout.FloorTop - 0.6f)) * 32f);
            Part("LoftLadder", RuntimeSprites.Get("loft_ladder_" + ladderPx, () => LoftDrawing.Ladder(ladderPx)),
                 new Vector3(CafeLayout.LadderX, CafeLayout.FloorTop - 0.6f, 0f), YSort.WallProps + 7);

            foreach (var sx in CafeLayout.LoftSpotX)
            {
                var go = new GameObject("LoftSpot");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(sx, deckY, 0f);
                var sp = go.AddComponent<LoftSpot>();
                spots.Add(sp); parts.Add(go);
                if (cafe != null) cafe.RegisterFacility(sp);
            }
        }

        void Part(string name, Sprite s, Vector3 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = order;
            parts.Add(go);
        }
    }

    // 다락·캣워크 쉼터 한 자리. 바닥 기준점 = 사다리 아래, 올라앉는 길 = 사다리로 위로 → 선반 따라 옆으로.
    public class LoftSpot : MonoBehaviour, IFacility, IFacilitySpot
    {
        static readonly FacilityTag[] tags = { FacilityTag.Height };
        string id;
        object user;
        public string FacilityId => id ??= "loft#" + GetInstanceID();
        public IReadOnlyList<FacilityTag> Tags => tags;
        public bool IsFree => user == null;
        public void Use(object cat) => user = cat;
        public void Release() => user = null;
        public bool WallMounted => false;
        public Vector3 BaseWorldPos => new Vector3(CafeLayout.LadderX, CafeLayout.FloorTop - 0.75f, 0f);
        public float PerchHeight => CafeLayout.LoftDeckY - BaseWorldPos.y;
        public float Run => transform.position.x - CafeLayout.LadderX;          // 데크/선반 위로 걸어가는 거리
        public float ClimbSeconds => 1.3f + Mathf.Abs(Run) * 0.28f;
        float ClimbPart => 1.3f / ClimbSeconds;

        // t(0~1) 진행에 따른 고양이 위치(바닥 기준점에서의 오프셋)
        public Vector3 PathOffset(float t)
        {
            t = Mathf.Clamp01(t);
            float up = ClimbPart;
            if (t < up) return new Vector3(0f, PerchHeight * (t / up), 0f);
            return new Vector3(Run * ((t - up) / (1f - up)), PerchHeight, 0f);
        }
        public bool Climbing(float t) => t < ClimbPart;
    }

    // 다락 그림(도트): 선반 조각, 데크, 난간, 사다리
    public static class LoftDrawing
    {
        static readonly Color32 Deep = PixelPalette.WoodDeep, Dark = PixelPalette.WoodDark, Mid = PixelPalette.WoodMid, Light = PixelPalette.WoodLight;

        public static PixelCanvas Shelf()
        {
            var c = new PixelCanvas(32, 14);
            c.FillRect(0, 8, 31, 13, Mid); c.HLine(0, 31, 13, Light); c.HLine(0, 31, 8, Deep);
            // 받침(삼각 브래킷)
            for (int i = 0; i < 7; i++) { c.Set(4 + i / 2, 7 - i, Dark); c.Set(5 + i / 2, 7 - i, Dark); }
            c.VLine(4, 1, 7, Deep);
            return c;
        }

        public static PixelCanvas Deck(int w)
        {
            var c = new PixelCanvas(w, 18);
            c.FillRect(0, 6, w - 1, 17, Mid); c.HLine(0, w - 1, 17, Light); c.HLine(0, w - 1, 6, Deep);
            for (int x = 10; x < w; x += 22) c.VLine(x, 7, 16, Dark);                 // 판자 이음새
            // 아래 받침 들보
            for (int x = 6; x < w; x += 40) { c.FillRect(x, 0, x + 3, 5, Dark); c.VLine(x, 0, 5, Deep); }
            return c;
        }

        public static PixelCanvas Rail(int w)
        {
            var c = new PixelCanvas(w, 18);
            c.FillRect(0, 14, w - 1, 16, Mid); c.HLine(0, w - 1, 16, Light); c.HLine(0, w - 1, 14, Deep);   // 손잡이
            for (int x = 2; x < w - 1; x += 9) { c.FillRect(x, 0, x + 1, 13, Mid); c.VLine(x + 1, 0, 13, Dark); }   // 난간살
            return c;
        }

        public static PixelCanvas Ladder(int h)
        {
            var c = new PixelCanvas(22, h);
            c.FillRect(1, 0, 3, h - 1, Mid); c.VLine(1, 0, h - 1, Light); c.VLine(3, 0, h - 1, Deep);
            c.FillRect(18, 0, 20, h - 1, Mid); c.VLine(18, 0, h - 1, Light); c.VLine(20, 0, h - 1, Deep);
            for (int y = 6; y < h; y += 11) { c.FillRect(4, y, 17, y + 1, Mid); c.HLine(4, 17, y + 1, Light); c.HLine(4, 17, y, Dark); }
            return c;
        }
    }
}
