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
            UiFactory.SetRect(paper, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 620));
            UiFactory.Frame(paper, UiFactory.FrameStyle.Paper, null, 3f);

            var title = UiFactory.Label("Title", paper, L.Get("demo.end.title"), 30, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(-40, 44));

            // 만난 고양이(한 번이라도 온 손님) / 아직 못 만난 고양이(여름 미리보기 그림자)
            var met = new List<CatData>(); CatData unmet = null;
            if (cats != null && save != null)
                foreach (var c in cats)
                {
                    if (c == null || ShelterBook.IsShelterCat(c.catId)) continue;
                    if (save.GetOrCreateCat(c.catId).visitCount > 0) met.Add(c);
                    else if (unmet == null) unmet = c;
                }
            int prefs = EndingPanel.Progress(save, cats).done;
            var sub = UiFactory.Label("Sub", paper, L.Get("demo.end.sub", ("cats", met.Count.ToString()), ("prefs", prefs.ToString())), 18, TextAnchor.MiddleCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)sub.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(-60, 48));

            // 만난 고양이 초상: 한 줄(최대 9마리)
            int shown = Mathf.Min(met.Count, 9);
            for (int i = 0; i < shown; i++)
                Img(paper, "Cat_" + met[i].catId, CatSpriteBank.Portrait(met[i].catId), new Vector2((i - (shown - 1) * 0.5f) * 76f, 152f), new Vector2(80, 80), Color.white);

            // 여름 미리보기: 여름 한낮 창밖 + 아직 못 만난 손님의 그림자 — "다음 계절엔 누가 올까?"
            var outside = RuntimeSprites.Get("glass_out_0_1_1", () => GlassWindowDrawing.Outside(0, 1, 1));
            var frame = RuntimeSprites.Get("glass_frame", GlassWindowDrawing.Frame);
            const float K = 1.45f;
            float gw = GlassWindowDrawing.GW * K, gh = GlassWindowDrawing.GH * K;
            Img(paper, "SummerOutside", outside, new Vector2(0, 30), new Vector2(gw, gh), Color.white);
            Img(paper, "SummerFrame", frame, new Vector2(0, 30 - 1.5f * K),   // 창틀 안 유리 중심이 창틀 중심보다 1.5px 위
                new Vector2(GlassWindowDrawing.FW * K, GlassWindowDrawing.FH * K), Color.white);
            if (unmet != null)
                Img(paper, "Silhouette", CatSpriteBank.Portrait(unmet.catId), new Vector2(gw * 0.36f, -14), new Vector2(104, 104), new Color(0.16f, 0.12f, 0.14f, 1f));
            var tease = UiFactory.Label("Tease", paper, L.Get("demo.end.tease"), 18, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)tease.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -78), new Vector2(680, 28));

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

        static void Img(RectTransform parent, string name, Sprite sp, Vector2 pos, Vector2 size, Color tint)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            g.transform.SetParent(parent, false);
            var im = g.GetComponent<Image>(); im.sprite = sp; im.preserveAspect = true; im.raycastTarget = false; im.color = tint;
            UiFactory.SetRect((RectTransform)g.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        }

        void Close()
        {
            if (go != null) Object.Destroy(go);
            IsOpen = false; Time.timeScale = 1f;
        }
    }
}
