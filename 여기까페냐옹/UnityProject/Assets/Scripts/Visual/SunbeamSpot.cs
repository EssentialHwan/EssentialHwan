using UnityEngine;
using YeogiCafe.Art;
using YeogiCafe.Cafe;
using YeogiCafe.Core;
using YeogiCafe.Data;

namespace YeogiCafe.Visual
{
    // 햇볕 자리: 맑은 날 통유리로 들어온 햇살이 바닥에 창 모양으로 떨어진다.
    // 아침엔 왼쪽, 점심엔 가운데, 저녁엔 오른쪽으로 길게 — 시간 따라 자리가 옮겨 간다.
    // 따뜻한 걸 좋아하는 고양이가 찾아가 녹아내리는 '시설'(Warm 태그, 추리 단서 아님). 흐린 날·비 오는 날엔 없다.
    public class SunbeamSpot : MonoBehaviour
    {
        FacilityBehaviour fac; CafeContext cafe; DayManager day;
        SpriteRenderer sr; bool registered; float alpha; int shownPhase = -1;

        void Start()
        {
            cafe = FindObjectOfType<CafeContext>();
            day = FindObjectOfType<DayManager>();
            var go = new GameObject("Sunbeam");
            go.transform.SetParent(transform, false);
            var data = ScriptableObject.CreateInstance<FurnitureData>();
            data.furnitureId = "sunbeam"; data.type = FurnitureType.Facility;
            data.facilityTags = new[] { FacilityTag.Warm };
            fac = go.AddComponent<FacilityBehaviour>(); fac.source = data;
            sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = YSort.FlatOnFloor + 3;
            sr.color = new Color(1, 1, 1, 0);
        }

        int Phase()
        {
            if (day == null) return 1;
            return day.Phase == DayPhase.Morning || day.Phase == DayPhase.Prep ? 0 : day.Phase == DayPhase.Lunch ? 1 : 2;
        }

        void Update()
        {
            if (fac == null || cafe == null) return;
            bool sunny = Weather.Current == WeatherKind.Sunny && day != null && day.Phase != DayPhase.Settlement;
            int ph = Phase();
            // 자리 이동은 아무도 안 쓰고 있을 때만
            if (ph != shownPhase && fac.IsFree)
            {
                shownPhase = ph;
                var g = Geo(ph);
                fac.transform.position = new Vector3(g.spotX, g.spotY, 0f);
                sr.sprite = BeamSprite(ph);
            }
            float want = sunny ? (ph == 2 ? 0.75f : 1f) : 0f;
            alpha = Mathf.MoveTowards(alpha, want, Time.deltaTime * 0.5f);
            sr.color = new Color(1f, 1f, 1f, alpha);
            if (sunny && !registered) { cafe.RegisterFacility(fac); registered = true; }
            else if (!sunny && registered && fac.IsFree) { cafe.UnregisterFacility(fac); registered = false; }
        }

        // ── 바닥 햇살 = 통유리 창(8×2.2유닛, 4칸 + 위쪽 채광창 띠)을 바닥에 투영한 모양 ──
        // 벽쪽 가장자리(near)가 유리 아랫단, 화면 아래쪽(far)이 유리 윗단에 대응한다.
        // 해가 낮은 아침·저녁은 길고 비스듬하게, 해가 높은 점심은 짧고 곧게 벽 가까이.
        struct BeamGeo { public float nearY, depth, shift, skew, widen, spotX, spotY; }

        static BeamGeo Geo(int ph)
        {
            var g = new BeamGeo();
            switch (ph)
            {
                case 0:  g.nearY = 3.85f; g.depth = 2.3f; g.shift = -1.1f; g.skew = -1.6f; break;   // 아침: 왼쪽으로 길게
                case 1:  g.nearY = 4.05f; g.depth = 1.25f; g.shift = 0f;   g.skew = 0f;    break;   // 점심: 창 바로 앞에 짧게
                default: g.nearY = 3.85f; g.depth = 2.5f; g.shift = 1.2f;  g.skew = 1.9f;  break;   // 저녁: 오른쪽으로 더 길게
            }
            g.widen = 0.08f;                                     // 화면 앞쪽(먼 쪽)일수록 살짝 넓게 — 원근감
            // 고양이가 찾아가는 자리: 햇살 한가운데(돌아다니는 범위 안으로)
            g.spotX = CafeLayout.GlassCenterX + g.shift + g.skew * 0.5f;
            g.spotY = Mathf.Clamp(g.nearY - g.depth * 0.5f, CafeLayout.RoamMinY + 0.3f, CafeLayout.RoamMaxY - 0.1f);
            return g;
        }

        static readonly System.Collections.Generic.Dictionary<int, Sprite> beams = new();

        static Sprite BeamSprite(int ph)
        {
            if (beams.TryGetValue(ph, out var s) && s != null) return s;
            var g = Geo(ph);
            const float PPU = 32f;
            float gl = CafeLayout.GlassLeft, gw = CafeLayout.GlassWidth;
            // 네 귀퉁이로 월드 영역 구하기
            float xMin = Mathf.Min(gl + g.shift, gl + g.shift + g.skew - gw * g.widen * 0.5f);
            float xMax = Mathf.Max(gl + gw + g.shift, gl + gw + g.shift + g.skew + gw * g.widen * 0.5f);
            float yMin = g.nearY - g.depth, yMax = g.nearY;
            int W = Mathf.CeilToInt((xMax - xMin) * PPU) + 2, H = Mathf.CeilToInt((yMax - yMin) * PPU) + 2;

            var col = ph == 2 ? new Color32(255, 196, 120, 66) : new Color32(255, 236, 160, 56);
            var edge = ph == 2 ? new Color32(255, 214, 150, 92) : new Color32(255, 246, 200, 80);
            const int GW = GlassWindowDrawing.GW, GH = GlassWindowDrawing.GH;
            int transom = GH - 13;                                   // 창틀 그림과 같은 위치의 가로 띠
            float bar = 1.6f / GW;                                   // 문살 반폭(유리 비율)

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "sunbeam_" + ph };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float wx = xMin + (x + 0.5f) / PPU, wy = yMin + (y + 0.5f) / PPU;
                    float f = (g.nearY - wy) / g.depth;                         // 0 = 벽쪽(유리 아랫단), 1 = 앞쪽(유리 윗단)
                    if (f < 0f || f > 1f) continue;
                    float cx = gl + gw * 0.5f + g.shift + g.skew * f;
                    float half = gw * 0.5f * (1f + g.widen * f);
                    float u = (wx - (cx - half)) / (half * 2f);                 // 유리 가로 비율 0~1
                    if (u < 0f || u > 1f) continue;
                    bool mullion = false;
                    for (int i = 1; i < 4; i++) if (Mathf.Abs(u - i / 4f) < bar) mullion = true;
                    float gyPx = f * GH;                                        // 유리 세로 위치(px)
                    if (Mathf.Abs(gyPx - transom) < 1.5f) mullion = true;
                    if (mullion) continue;
                    // 칸 가장자리 한 줄은 조금 밝게(빛 경계)
                    float du = 1.2f / (half * 2f * PPU), dv = 1.2f / (g.depth * PPU);
                    bool rim = u < du || u > 1f - du || f < dv || f > 1f - dv;
                    for (int i = 1; i < 4; i++) if (Mathf.Abs(u - i / 4f) < bar + du) rim = true;
                    if (Mathf.Abs(gyPx - transom) < 1.5f + dv * GH) rim = true;
                    px[y * W + x] = rim ? edge : col;
                }
            tex.SetPixels32(px); tex.Apply();
            var pivot = new Vector2((g.spotX - xMin) * PPU / W, (g.spotY - yMin) * PPU / H);   // 시설 자리 = 스프라이트 기준점
            s = Sprite.Create(tex, new Rect(0, 0, W, H), pivot, PPU, 0, SpriteMeshType.FullRect);
            s.name = "sunbeam_" + ph;
            beams[ph] = s;
            return s;
        }
    }
}
