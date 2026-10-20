using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;
using YeogiCafe.Data;
using YeogiCafe.Loc;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 최종 목표: 도감 고양이 모두의 취향(음식·좌석·시설) 전부 알아내기.
    // 다 채운 날 밤, 정산을 마치고 [다음 날]을 누르면 엔딩 편지가 한 번 열리고, 닫으면 자유 영업이 이어진다.
    public class EndingPanel
    {
        public static (int done, int total) Progress(SaveGame save, IReadOnlyList<CatData> cats)
        {
            int done = 0, total = 0;
            if (cats == null) return (0, 0);
            foreach (var c in cats)
            {
                if (c == null) continue;
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

        readonly GameObject go;
        public static bool IsOpen { get; private set; }

        public EndingPanel(Transform parent, SaveGame save, IReadOnlyList<CatData> cats, System.Action onClose, bool preview = false)
        {
            var scrim = UiFactory.Panel("Ending", parent, new Color(0.08f, 0.06f, 0.12f, 0.78f));
            go = scrim.gameObject;
            IsOpen = true;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scrim.SetAsLastSibling();

            var paper = UiFactory.Panel("Letter", scrim, new Color(1f, 0.96f, 0.86f, 1f));
            UiFactory.SetRect(paper, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 560));
            UiFactory.Frame(paper, UiFactory.FrameStyle.Paper, null, 3f);

            var title = UiFactory.Label("Title", paper, L.Get("ending.title"), 30, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(-40, 44));
            var sub = UiFactory.Label("Sub", paper, L.Get("ending.sub", ("day", save != null ? save.currentDay.ToString() : "?")), 18, TextAnchor.MiddleCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)sub.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -68), new Vector2(-40, 26));

            // 고양이 단체 사진: 한 줄에 5마리씩
            var list = new List<CatData>();
            if (cats != null) foreach (var c in cats) if (c != null) list.Add(c);
            bool many = list.Count > 10;                     // 13마리: 7+6 두 줄
            float cell = many ? 96f : 108f, rowH = many ? 104f : 118f, icon = many ? 72f : 84f;
            int perRow = Mathf.Min(many ? 7 : 5, Mathf.Max(1, list.Count));
            for (int i = 0; i < list.Count; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, list.Count - row * perRow);
                float x = (col - (inRow - 1) * 0.5f) * cell;
                float y = (many ? 118f : 112f) - row * rowH;
                var g = new GameObject("Cat_" + list[i].catId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                g.transform.SetParent(paper, false);
                var im = g.GetComponent<Image>(); im.sprite = CatSpriteBank.Portrait(list[i].catId); im.preserveAspect = true; im.raycastTarget = false;
                UiFactory.SetRect((RectTransform)g.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(icon, icon));
                var n = UiFactory.Label("N", g.transform, L.Get($"cat.{list[i].catId}.name"), 14, TextAnchor.UpperCenter, UiFactory.Ink);
                UiFactory.SetRect((RectTransform)n.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(104, 20));
            }

            var body = UiFactory.Label("Body", paper, L.Get("ending.body"), 18, TextAnchor.UpperCenter, UiFactory.Ink);
            body.lineSpacing = 1.15f;
            UiFactory.SetRect((RectTransform)body.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(-80, 120));

            var btn = UiFactory.Button("Continue", paper, L.Get("ending.continue"), UiFactory.Green, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(220, 48));
            btn.onClick.AddListener(() =>
            {
                if (save != null && !preview)
                {
                    save.endingSeen = true;
                    var sm = Object.FindObjectOfType<SaveManager>(); if (sm != null) sm.Save(save);
                }
                Object.Destroy(go);
                IsOpen = false;
                onClose?.Invoke();
            });
            YeogiCafe.Audio.SoundManager.Sfx("sfx_chime", 0.7f, 0f);
        }
    }
}
