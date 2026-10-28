using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Art;
using YeogiCafe.Core;
using YeogiCafe.Data;

namespace YeogiCafe.Visual
{
    // Lv3 '위로 넓히기': 벽 윗부분에 캣워크 선반(가로 전체) + 오른쪽 다락 데크(난간) + 벽 계단 선반(캣스텝).
    // 고양이는 벽에 지그재그로 붙은 작은 선반을 폴짝폴짝 뛰어 올라 데크·선반의 쉼터(LoftSpot)로 걸어간다.
    //  (10/28 형: 사다리를 걷는 자세로 수직으로 오르는 게 웃겨서 → 점프 + 벽 선반으로 교체) 추리 단서는 아니고 말풍선만(높은 곳 좋아하는 고양이가 자주 간다).
    public class LoftView : MonoBehaviour
    {
        static LoftView inst;
        public const float CatwalkLeft = 3.0f;                 // 선반은 통유리 위쪽만(왼쪽 벽걸이 자리는 비워 둠)
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
            yield return new Rect(LoftSpot.StepsLeft - 0.1f, CafeLayout.FloorTop, LoftSpot.StepsRight - LoftSpot.StepsLeft + 0.2f, CafeLayout.LoftDeckY - CafeLayout.FloorTop + 0.2f);   // 캣스텝
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
            // 캣스텝: 벽에 지그재그로 붙은 작은 선반(맨 아래 바닥 점과 맨 위 데크 점은 제외) — 윗면이 고양이 발 높이
            for (int i = 1; i < LoftSpot.Steps.Length - 1; i++)
                Part("CatStep", RuntimeSprites.Get("loft_step", LoftDrawing.Step),
                     new Vector3(LoftSpot.Steps[i].x, LoftSpot.Steps[i].y - 14f / 32f, 0f), YSort.WallProps + 7);

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

    // 다락·캣워크 쉼터 한 자리. 바닥 기준점 = 캣스텝 아래, 올라앉는 길 = 캣스텝을 폴짝폴짝 → 데크/선반 따라 옆으로.
    public class LoftSpot : MonoBehaviour, IFacility, IFacilitySpot
    {
        static readonly FacilityTag[] tags = { FacilityTag.Height };
        // 캣스텝 발 디딤 점(월드): [0] 바닥 출발점 · [1..4] 벽 선반(좌우 지그재그) · [5] 데크 끝. 한 칸 오를 때마다 대각선 점프.
        // 10/28 형: 맨 오른쪽 벽으로(창 옆이 아니라 데크 오른쪽 끝 아래) → 올라가서 데크를 따라 왼쪽으로 걸어간다
        public static readonly Vector2[] Steps =
        {
            new Vector2(15.45f, CafeLayout.FloorTop - 0.75f),
            new Vector2(16.05f, 4.55f), new Vector2(16.65f, 5.4f), new Vector2(16.05f, 6.25f), new Vector2(16.65f, 7.0f),
            new Vector2(16.25f, CafeLayout.LoftDeckY),
        };
        public const float StepsLeft = 15.6f, StepsRight = 17.1f;     // 선반 그림 폭 포함(벽걸이 가구 겹침 판정)
        const float HopTime = 0.46f;                                  // 한 칸: 웅크림 → 도약 → 착지
        const float Crouch = 0.24f, Land = 0.86f;                     // 한 칸 안에서 도약 시작·착지 시점(0~1)

        string id;
        object user;
        public string FacilityId => id ??= "loft#" + GetInstanceID();
        public IReadOnlyList<FacilityTag> Tags => tags;
        public bool IsFree => user == null;
        public void Use(object cat) => user = cat;
        public void Release() => user = null;
        public bool WallMounted => false;
        public Vector3 BaseWorldPos => new Vector3(Steps[0].x, Steps[0].y, 0f);
        public float PerchHeight => CafeLayout.LoftDeckY - BaseWorldPos.y;
        static int Hops => Steps.Length - 1;
        Vector2 Top => Steps[Steps.Length - 1];
        public float Run => transform.position.x - Top.x;                    // 데크 끝에서 데크/선반 위로 걸어가는 거리
        public float ClimbSeconds => Hops * HopTime + Mathf.Abs(Run) * 0.28f + 0.05f;
        float ClimbPart => Hops * HopTime / ClimbSeconds;

        void Hop(float t, out int i, out float u)
        {
            float h = Mathf.Clamp01(t / ClimbPart) * Hops;
            i = Mathf.Min(Mathf.FloorToInt(h), Hops - 1); u = h - i;
        }

        // t(0~1) 진행에 따른 고양이 위치(바닥 기준점에서의 오프셋)
        public Vector3 PathOffset(float t)
        {
            t = Mathf.Clamp01(t);
            var b0 = Steps[0];
            if (t < ClimbPart)
            {
                Hop(t, out int i, out float u);
                Vector2 a = Steps[i] - b0, b = Steps[i + 1] - b0;
                float k = u < Crouch ? 0f : u > Land ? 1f : (u - Crouch) / (Land - Crouch);
                float e = k * k * (3f - 2f * k);
                float arc = (u > Crouch && u < Land) ? Mathf.Sin(k * Mathf.PI) * 0.32f : 0f;
                return new Vector3(Mathf.Lerp(a.x, b.x, e), Mathf.Lerp(a.y, b.y, e) + arc, 0f);
            }
            var top = Top - b0;
            return new Vector3(top.x + Run * ((t - ClimbPart) / (1f - ClimbPart)), top.y, 0f);
        }
        public bool Climbing(float t) => t < ClimbPart;
        // 점프 자세: 0 = 웅크림(디딤·착지) · 2 = 공중(뻗음) — CatClips 'jumpup' 프레임 번호
        public int HopFrame(float t) { Hop(t, out _, out float u); return (u < Crouch || u > Land) ? 0 : 2; }
        // 지금 칸에서 뛰는 가로 방향(+1 오른쪽 / -1 왼쪽) — 올라갈 때 기준
        public float HopDir(float t) { Hop(t, out int i, out _); return Mathf.Sign(Steps[i + 1].x - Steps[i].x); }
        public int HopIndex(float t) { Hop(t, out int i, out float u); return i * 2 + (u < Crouch ? 0 : 1); }   // 도약 순간 소리용
    }

    // 다락 그림(도트): 선반 조각, 데크, 난간, 캣스텝
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

        // 벽에 붙은 작은 발판(캣스텝): 윗면 = 맨 윗줄, 아래에 삼각 받침 하나
        public static PixelCanvas Step()
        {
            var c = new PixelCanvas(26, 14);
            c.FillRect(0, 8, 25, 13, Mid); c.HLine(0, 25, 13, Light); c.HLine(0, 25, 8, Deep); c.VLine(0, 8, 13, Deep); c.VLine(25, 8, 13, Deep);
            for (int i = 0; i < 7; i++) { c.Set(10 + i / 2, 7 - i, Dark); c.Set(11 + i / 2, 7 - i, Dark); }
            c.VLine(10, 1, 7, Deep);
            return c;
        }
    }
}
