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
                float x = ph == 0 ? 4.6f : ph == 1 ? 7.3f : 10.0f;
                fac.transform.position = new Vector3(x, 2.75f, 0f);
                sr.sprite = RuntimeSprites.Get("sunbeam_" + ph, () => Beam(ph));
            }
            float want = sunny ? (ph == 2 ? 0.75f : 1f) : 0f;
            alpha = Mathf.MoveTowards(alpha, want, Time.deltaTime * 0.5f);
            sr.color = new Color(1f, 1f, 1f, alpha);
            if (sunny && !registered) { cafe.RegisterFacility(fac); registered = true; }
            else if (!sunny && registered && fac.IsFree) { cafe.UnregisterFacility(fac); registered = false; }
        }

        // 바닥에 비친 창 모양 햇살(4칸 창살, 시간대별 기울기·길이). 피벗은 아래 가운데.
        static PixelCanvas Beam(int phase)
        {
            int W = phase == 2 ? 128 : 104, H = 34;
            var c = new PixelCanvas(W, H);
            float skew = phase == 0 ? 0.55f : phase == 1 ? 0f : -0.75f;
            var col = phase == 2 ? new Color32(255, 196, 120, 80) : new Color32(255, 236, 160, 72);
            var edge = phase == 2 ? new Color32(255, 214, 150, 110) : new Color32(255, 246, 200, 100);
            int paneW = (W - 28) / 4;
            for (int y = 2; y < H; y++)
            {
                float off = (y - H / 2f) * skew;
                for (int p = 0; p < 4; p++)
                {
                    int x0 = Mathf.RoundToInt(10 + p * (paneW + 3) + off), x1 = x0 + paneW;
                    if (y == 2 || y == H - 1 || y == 17) continue;            // 창살(가로)
                    for (int x = x0; x <= x1; x++) c.Set(x, y, x == x0 || x == x1 || y == 3 || y == H - 2 ? edge : col);
                }
            }
            return c;
        }
    }
}
