using UnityEngine;
using UnityEngine.EventSystems;
using YeogiCafe.Art;
using YeogiCafe.Core;
using YeogiCafe.Loc;
using YeogiCafe.Save;

namespace YeogiCafe.Visual
{
    // 냥슐랭 가이드 명판 — 문 위 벽에 붙은 빨간 에나멜 판(미슐랭 가이드 명판처럼).
    // 받은 별만큼 금별이 채워지고, 별을 새로 받으면 통통 튀며 반짝인다. 누르면 다음 별 기준을 알려준다.
    public class NyanPlaque : MonoBehaviour
    {
        public const int W = 38, H = 30;
        public static readonly Vector3 Pos = new Vector3(CafeLayout.Door.x, CafeLayout.Door.y + 2.12f, 0f);   // 문(2유닛) 바로 위

        SaveGame save;
        SpriteRenderer sr;
        int shown = -1;
        float bounce;

        public static NyanPlaque Ensure(SaveGame save)
        {
            var p = FindObjectOfType<NyanPlaque>();
            if (p == null)
            {
                var go = new GameObject("NyanPlaque");
                go.transform.position = Pos;
                p = go.AddComponent<NyanPlaque>();
                p.sr = go.AddComponent<SpriteRenderer>();
                p.sr.sortingOrder = YSort.WallProps + 2;
            }
            p.save = save;
            return p;
        }

        void Update()
        {
            if (save == null || sr == null) return;
            int n = Mathf.Clamp(save.nyanStars, 0, YeogiCafe.Food.NyanGuide.MaxStars);
            if (n != shown)
            {
                if (shown >= 0 && n > shown) bounce = 1f;       // 새 별 → 통통
                shown = n;
                sr.sprite = RuntimeSprites.Get("nyan_plaque_" + n, () => Draw(n));
            }
            if (bounce > 0f)
            {
                bounce = Mathf.Max(0f, bounce - Time.unscaledDeltaTime * 1.2f);
                float s = 1f + Mathf.Sin(bounce * Mathf.PI * 3f) * 0.12f * bounce;
                transform.localScale = new Vector3(s, s, 1f);
            }

            if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                var cam = Camera.main; if (cam == null) return;
                var w = cam.ScreenToWorldPoint(Input.mousePosition); w.z = 0f;
                if (sr.bounds.Contains(new Vector3(w.x, w.y, sr.bounds.center.z)))
                {
                    bounce = 0.6f;
                    int need = YeogiCafe.Food.NyanGuide.NextPassScore(n);
                    CustomerSpawner.Toast?.Invoke(n >= YeogiCafe.Food.NyanGuide.MaxStars
                        ? L.Get("nyan.plaque.max")
                        : L.Get("nyan.plaque.info", ("stars", YeogiCafe.Food.NyanGuide.Stars(n)), ("need", need.ToString())));
                }
            }
        }

        // ── 그림(38×30): 금테 + 빨간 에나멜 판 + 흰 고양이 얼굴 문장 + 아래 띠에 별 5개 ──
        static readonly Color32 Red = new Color32(196, 36, 48, 255), RedDark = new Color32(132, 20, 34, 255), RedLight = new Color32(226, 76, 84, 255);
        static readonly Color32 Gold = new Color32(246, 196, 72, 255), GoldDark = new Color32(170, 116, 34, 255), GoldLight = new Color32(255, 236, 150, 255);
        static readonly Color32 Cream = new Color32(255, 248, 236, 255);
        static readonly Color32 StarOff = new Color32(96, 18, 28, 255);

        static readonly string[] Star =
        {
            "..#..",
            ".###.",
            "#####",
            ".###.",
            "##.##",
        };

        static readonly string[] Emblem =      // 흰 고양이 얼굴 + 요리사 모자(위가 0행)
        {
            "....#####....",
            "...#######...",
            "...#######...",
            "....#####....",
            ".#..#####..#.",
            ".##.......##.",
            ".###########.",
            "#############",
            "##.##...##.##",
            "#############",
            "######.######",
            ".###########.",
            "..#########..",
        };

        public static PixelCanvas Draw(int stars)
        {
            var c = new PixelCanvas(W, H);
            // 금테
            c.FillRoundRect(0, 0, W - 1, H - 1, 3, GoldDark);
            c.FillRoundRect(1, 1, W - 2, H - 2, 3, Gold);
            c.HLine(4, W - 5, H - 2, GoldLight);
            // 빨간 판
            c.FillRoundRect(3, 3, W - 4, H - 4, 2, Red);
            c.HLine(5, W - 6, H - 5, RedLight);
            // 아래 별 띠(진한 빨강)
            c.FillRect(3, 3, W - 4, 11, RedDark);
            // 문장(흰 고양이 얼굴 + 요리사 모자)
            int ex = (W - Emblem[0].Length) / 2, ey = H - 6;
            for (int r = 0; r < Emblem.Length; r++)
                for (int k = 0; k < Emblem[r].Length; k++)
                    if (Emblem[r][k] == '#') c.Set(ex + k, ey - r, Cream);
            // 별 5개(받은 별은 금색, 아직은 어두운 자리)
            const int gap = 1;
            int sx = 3 + ((W - 6) - (5 * 5 + 4 * gap)) / 2;
            for (int i = 0; i < 5; i++)
            {
                bool on = i < stars;
                for (int r = 0; r < 5; r++)
                    for (int k = 0; k < 5; k++)
                        if (Star[r][k] == '#') c.Set(sx + i * (5 + gap) + k, 9 - r, on ? (r == 1 && k == 2 ? GoldLight : Gold) : StarOff);
            }
            // 나사
            c.Set(2, 2, GoldLight); c.Set(W - 3, 2, GoldLight); c.Set(2, H - 3, GoldLight); c.Set(W - 3, H - 3, GoldLight);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }
    }
}
