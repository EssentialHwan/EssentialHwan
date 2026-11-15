using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;
using YeogiCafe.Data;
using YeogiCafe.Loc;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 엔딩 두 단계 (2026-11-12 확정)
    //  · 일반 엔딩 '모두의 단골집': 보호소 고양이를 뺀 13마리의 취향(음식·좌석·시설)을 전부 알아낸 밤.
    //  · 진엔딩 '다른 고양이들까지': 보호소 3마리까지 16마리 전부를 알아낸 밤. 일반 엔딩을 본 뒤에만 열린다.
    //    → 순서는 언제나 일반 → 진엔딩. 보호소를 먼저 끝냈어도 진엔딩이 먼저 뜨지 않는다.
    //  · 같은 밤에 둘 다 채우면: 일반 엔딩 → [계속 영업하기] → 전환 장면("그날 밤, 보호소에서 편지가…") → 진엔딩.
    //    다음 날로 미루지 않는다(엔딩을 본 플레이어가 그대로 게임을 꺼 버리면 더 많이 한 사람이 보상을 놓친다).
    //  · 정산을 마치고 [다음 날]을 누르면 열리고, 닫으면 자유 영업이 이어진다.
    public class EndingPanel
    {
        // 일반 엔딩 목표: 보호소 고양이 제외
        public static (int done, int total) Progress(SaveGame save, IReadOnlyList<CatData> cats) => Count(save, cats, includeShelter: false);

        // 진엔딩 목표: 보호소 고양이 포함 전부
        public static (int done, int total) TrueProgress(SaveGame save, IReadOnlyList<CatData> cats) => Count(save, cats, includeShelter: true);

        static (int done, int total) Count(SaveGame save, IReadOnlyList<CatData> cats, bool includeShelter)
        {
            int done = 0, total = 0;
            if (cats == null) return (0, 0);
            foreach (var c in cats)
            {
                if (c == null || (!includeShelter && ShelterBook.IsShelterCat(c.catId))) continue;
                total += 3;
                if (save != null) done += Mathf.Min(3, save.GetOrCreateCat(c.catId).observation.RecordedAxisCount());
            }
            return (done, total);
        }

        public static bool Ready(SaveGame save, IReadOnlyList<CatData> cats)
        {
            if (save == null || save.endingSeen) return false;
            var p = Progress(save, cats);
            return p.total > 0 && p.done >= p.total;
        }

        public static bool TrueReady(SaveGame save, IReadOnlyList<CatData> cats)
        {
            if (save == null || !save.endingSeen || save.trueEndingSeen) return false;
            var p = TrueProgress(save, cats);
            return p.total > 0 && p.done >= p.total;
        }

        readonly GameObject go;
        public static bool IsOpen { get; private set; }
        public void DebugClose() { if (go != null) Object.Destroy(go); IsOpen = false; }

        // trueEnding: 진엔딩 편지 · chained: 같은 밤 일반 엔딩 바로 뒤라 전환 장면을 먼저 보여 준다
        public EndingPanel(Transform parent, SaveGame save, IReadOnlyList<CatData> cats, System.Action onClose,
                           bool preview = false, bool trueEnding = false, bool chained = false)
        {
            var scrim = UiFactory.Panel(trueEnding ? "TrueEnding" : "Ending", parent, new Color(0.08f, 0.06f, 0.12f, 0.78f));
            go = scrim.gameObject;
            IsOpen = true;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scrim.SetAsLastSibling();

            var paper = UiFactory.Panel("Letter", scrim, new Color(1f, 0.96f, 0.86f, 1f));
            UiFactory.SetRect(paper, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 560));
            UiFactory.Frame(paper, UiFactory.FrameStyle.Paper, null, 3f);

            string k = trueEnding ? "ending.true." : "ending.";
            var title = UiFactory.Label("Title", paper, L.Get(k + "title"), 30, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(-40, 44));
            string subText = trueEnding
                ? L.Get("ending.true.sub", ("n", CountCats(cats, includeShelter: true).ToString()))
                : L.Get("ending.sub", ("day", save != null ? save.currentDay.ToString() : "?"));
            var sub = UiFactory.Label("Sub", paper, subText, 18, TextAnchor.MiddleCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)sub.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -68), new Vector2(-40, 26));

            // 사진: 일반 엔딩 = 단골 13마리 단체 사진, 진엔딩 = 보호소 고양이들
            var list = new List<CatData>();
            if (cats != null)
                foreach (var c in cats)
                    if (c != null && ShelterBook.IsShelterCat(c.catId) == trueEnding) list.Add(c);
            bool many = list.Count > 10;                     // 13마리: 7+6 두 줄
            bool few = list.Count <= 3;                      // 보호소 3마리: 크게 한 줄
            float cell = many ? 96f : few ? 150f : 108f, rowH = many ? 104f : 118f, icon = many ? 72f : few ? 112f : 84f;
            int perRow = Mathf.Min(many ? 7 : 5, Mathf.Max(1, list.Count));
            for (int i = 0; i < list.Count; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, list.Count - row * perRow);
                float x = (col - (inRow - 1) * 0.5f) * cell;
                float y = (many ? 118f : few ? 96f : 112f) - row * rowH;
                var g = new GameObject("Cat_" + list[i].catId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                g.transform.SetParent(paper, false);
                var im = g.GetComponent<Image>(); im.sprite = CatSpriteBank.Portrait(list[i].catId); im.preserveAspect = true; im.raycastTarget = false;
                UiFactory.SetRect((RectTransform)g.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(icon, icon));
                var n = UiFactory.Label("N", g.transform, L.Get($"cat.{list[i].catId}.name"), 14, TextAnchor.UpperCenter, UiFactory.Ink);
                UiFactory.SetRect((RectTransform)n.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(104, 20));
            }

            var body = UiFactory.Label("Body", paper, L.Get(k + "body"), 18, TextAnchor.UpperCenter, UiFactory.Ink);
            body.lineSpacing = 1.15f;
            UiFactory.SetRect((RectTransform)body.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(-80, 120));

            var btn = UiFactory.Button("Continue", paper, L.Get("ending.continue"), UiFactory.Green, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(260, 48));      // 'Keep the cafe open'(영어)
            btn.onClick.AddListener(() =>
            {
                if (save != null && !preview)
                {
                    if (trueEnding) save.trueEndingSeen = true; else save.endingSeen = true;
                    var sm = Object.FindObjectOfType<SaveManager>(); if (sm != null) sm.Save(save);
                }
                Object.Destroy(go);
                IsOpen = false;
                onClose?.Invoke();
            });

            if (!chained)
            {
                YeogiCafe.Audio.SoundManager.Sfx("sfx_finale", 0.7f, 0f);
                return;
            }

            // 전환 장면: 편지를 숨겨 두고 한 줄만 먼저 보여 준다. 화면을 누르면 편지가 열린다.
            paper.gameObject.SetActive(false);
            var lead = UiFactory.Button("Lead", scrim, "", new Color(0f, 0f, 0f, 0.35f), UiFactory.Ink, out _);
            UiFactory.Anchor((RectTransform)lead.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var leadText = UiFactory.Label("LeadText", lead.transform, L.Get("ending.true.lead"), 24, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.86f, 1f));
            UiFactory.SetRect((RectTransform)leadText.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 12), new Vector2(-120, 80));
            var hint = UiFactory.Label("Hint", lead.transform, "▶", 18, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.86f, 0.7f));
            UiFactory.SetRect((RectTransform)hint.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -48), new Vector2(40, 30));
            lead.onClick.AddListener(() =>
            {
                Object.Destroy(lead.gameObject);
                paper.gameObject.SetActive(true);
                YeogiCafe.Audio.SoundManager.Sfx("sfx_finale", 0.7f, 0f);
            });
        }

        static int CountCats(IReadOnlyList<CatData> cats, bool includeShelter)
        {
            int n = 0;
            if (cats != null) foreach (var c in cats) if (c != null && (includeShelter || !ShelterBook.IsShelterCat(c.catId))) n++;
            return n;
        }
    }
}
