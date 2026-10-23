using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;
using YeogiCafe.Core;
using YeogiCafe.Data;
using YeogiCafe.Loc;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 체험판 끝 화면: 봄 첫 일주일 동안 만난 고양이·알아낸 취향을 보여 주고, 위시리스트로 안내한다.
    // 닫기 버튼은 없다(시작 화면으로 / 종료). 시간은 멈춘다.
    public class DemoEndPanel
    {
        public static bool IsOpen { get; private set; }
        readonly GameObject go;

        public DemoEndPanel(Transform parent, SaveGame save, IReadOnlyList<CatData> cats, bool preview = false)
        {
            IsOpen = true;
            Time.timeScale = 0f;
            var scrim = UiFactory.Panel("DemoEnd", parent, new Color(0.08f, 0.06f, 0.12f, 0.8f));
            go = scrim.gameObject;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scrim.SetAsLastSibling();

            var paper = UiFactory.Panel("Letter", scrim, new Color(1f, 0.96f, 0.86f, 1f));
            UiFactory.SetRect(paper, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 540));
            UiFactory.Frame(paper, UiFactory.FrameStyle.Paper, null, 3f);

            var title = UiFactory.Label("Title", paper, L.Get("demo.end.title"), 30, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(-40, 44));

            // 만난 고양이(한 번이라도 온 손님)
            var met = new List<CatData>();
            if (cats != null && save != null)
                foreach (var c in cats)
                    if (c != null && !ShelterBook.IsShelterCat(c.catId) && save.GetOrCreateCat(c.catId).visitCount > 0) met.Add(c);
            int prefs = EndingPanel.Progress(save, cats).done;
            var sub = UiFactory.Label("Sub", paper, L.Get("demo.end.sub", ("cats", met.Count.ToString()), ("prefs", prefs.ToString())), 18, TextAnchor.MiddleCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)sub.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(-60, 48));

            // 만난 고양이 초상(한 줄 최대 7마리)
            int perRow = Mathf.Clamp(met.Count, 1, 7);
            for (int i = 0; i < met.Count && i < 14; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, met.Count - row * perRow);
                float x = (col - (inRow - 1) * 0.5f) * 96f, y = met.Count > perRow ? 78f - row * 100f : 40f;
                var g = new GameObject("Cat_" + met[i].catId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                g.transform.SetParent(paper, false);
                var im = g.GetComponent<Image>(); im.sprite = CatSpriteBank.Portrait(met[i].catId); im.preserveAspect = true; im.raycastTarget = false;
                UiFactory.SetRect((RectTransform)g.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(104, 104));
            }

            var body = UiFactory.Label("Body", paper, L.Get("demo.end.body"), 17, TextAnchor.UpperCenter, UiFactory.Ink);
            body.lineSpacing = 1.15f;
            UiFactory.SetRect((RectTransform)body.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 86), new Vector2(-80, 96));

            // 버튼: [위시리스트에 추가](앱 ID가 있을 때) · [시작 화면으로] · [종료]
            var buttons = new List<(string label, Color bg, Color fg, System.Action act)>();
            if (!string.IsNullOrEmpty(Demo.StoreUrl))
                buttons.Add((L.Get("demo.end.wishlist"), UiFactory.Green, UiFactory.Ink, () => Application.OpenURL(Demo.StoreUrl)));
            buttons.Add((L.Get("demo.end.title_btn"), UiFactory.Cream, UiFactory.Ink, () =>
            {
                if (preview) { Close(); return; }
                IsOpen = false; Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }));
            buttons.Add((L.Get("ui.title.quit"), UiFactory.Wood, UiFactory.Cream, () =>
            {
                if (preview) { Close(); return; }
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }));
            float w = buttons.Count == 3 ? 220f : 240f, gap = 14f;
            float total = buttons.Count * w + (buttons.Count - 1) * gap;
            for (int i = 0; i < buttons.Count; i++)
            {
                var b = buttons[i];
                var btn = UiFactory.Button("Btn" + i, paper, b.label, b.bg, b.fg, out var lbl);
                UiFactory.FitOneLine(lbl, w - 20f, 12);
                UiFactory.SetRect((RectTransform)btn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                                  new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), 24), new Vector2(w, 48));
                var act = b.act;
                btn.onClick.AddListener(() => act());
            }
            YeogiCafe.Audio.SoundManager.Sfx("sfx_chime", 0.7f, 0f);
        }

        void Close()
        {
            if (go != null) Object.Destroy(go);
            IsOpen = false; Time.timeScale = 1f;
        }
    }
}
