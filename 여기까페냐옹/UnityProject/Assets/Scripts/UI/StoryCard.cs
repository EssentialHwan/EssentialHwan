using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;
using YeogiCafe.Loc;
using YeogiCafe.Progression;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 이야기 한 장(정산 화면 · 도감 '이야기' 탭 공용).
    //  단골 편지·졸업 편지 = 크림색 편지지 + 초상 · 기억 = 빛바랜 사진 같은 세피아 종이 · 노트 = 줄 노트
    public static class StoryCard
    {
        public static GameObject Open(Transform parent, StoryDTO st, Sprite stamp, System.Action onNext, string nextLabel = null)
        {
            var scrim = UiFactory.Panel("StoryScrim", parent, new Color(0.08f, 0.07f, 0.12f, 0.55f));
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bool grad = st.kind == StoryBook.KindGrad, memory = st.kind == StoryBook.KindMemory, note = st.kind == StoryBook.KindNote;

            Color paperCol = grad ? new Color(1f, 0.96f, 0.86f, 1f)
                           : memory ? new Color(0.93f, 0.86f, 0.74f, 1f)
                           : note ? new Color(0.99f, 0.97f, 0.9f, 1f)
                           : new Color(1f, 0.99f, 0.95f, 1f);
            Vector2 size = memory || note ? new Vector2(680, 440) : new Vector2(620, 400);
            var paper = UiFactory.Panel("Letter", scrim.transform, paperCol);
            UiFactory.SetRect(paper, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            UiFactory.Frame(paper, UiFactory.FrameStyle.Paper, memory ? new Color(0.86f, 0.76f, 0.6f, 1f) : (Color?)null, 3f);

            string name = st.catId != null ? L.Get($"cat.{st.catId}.name") : "";
            string titleText = note ? L.Get("story.title.note", ("n", st.note.ToString()), ("t", StoryBook.NoteCount.ToString()))
                                    : L.Get(StoryBook.TitleKey(st), ("name", name));
            Color titleCol = grad ? UiFactory.PinkDeep : memory ? new Color(0.45f, 0.32f, 0.22f, 1f) : UiFactory.Ink;
            var title = UiFactory.Label("Title", paper.transform, titleText, 24, TextAnchor.MiddleCenter, titleCol);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(-40, 36));
            UiFactory.FitOneLine(title, size.x - 40f, 14);

            float bodyTop = -55f;
            if (memory || note)
            {
                // 부제: 기억 = '뉴로냐 이전', 노트 = 어디서 찾았는지
                // 기억 부제는 고양이별로 바꿀 수 있다(꼬물냥은 뉴로냐 이후에 태어나 '사람이 있던 시절'이 없다)
                string sub = memory ? (Opt($"ep.{st.catId}.memory.sub") ?? L.Get("story.memory.sub")) : L.Get($"note.{st.note}.where");
                var sl = UiFactory.Label("Sub", paper.transform, sub, 15, TextAnchor.MiddleCenter, UiFactory.WoodDark);
                sl.fontStyle = FontStyle.Italic;
                UiFactory.SetRect((RectTransform)sl.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(-60, 24));
                UiFactory.FitOneLine(sl, size.x - 60f, 10);
                bodyTop = -82f;
            }

            float left = 230f, right = -70f;                      // 편지: 초상 오른쪽(예전 배치 그대로)
            if (note)
            {
                left = 44f; right = -36f;
                // 노트 종이: 왼쪽 빨간 여백선(가로줄은 글줄과 안 맞아서 뺐다)
                var mg = UiFactory.Panel("Margin", paper.transform, new Color(0.85f, 0.45f, 0.45f, 0.45f));
                mg.GetComponent<Image>().raycastTarget = false;
                UiFactory.SetRect(mg, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(34, -12), new Vector2(2, -96));
            }
            else
            {
                var pg = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pg.transform.SetParent(paper.transform, false);
                var pimg = pg.GetComponent<Image>(); pimg.sprite = CatSpriteBank.Portrait(st.catId); pimg.preserveAspect = true; pimg.raycastTarget = false;
                if (memory) pimg.color = new Color(0.92f, 0.8f, 0.62f, 1f);          // 빛바랜 사진
                Vector2 psz = memory ? new Vector2(150, 130) : new Vector2(170, 140);
                UiFactory.SetRect((RectTransform)pg.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 10), psz);
                if (memory) { left = 186f; right = -32f; }
            }

            var sb = new System.Text.StringBuilder();
            foreach (var k in StoryBook.LineKeys(st))
            {
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append(L.Get(k, ("name", name)));
            }
            // 노트 뒤에 붙는 '나'의 한 줄(마지막 장: 뒷문 고양이가 누구였는지)
            string after = note ? Opt($"note.{st.note}.after") : null;
            if (after != null) sb.Append("\n\n<i><color=#9A5B44>").Append(after).Append("</color></i>");
            var body = UiFactory.Label("Body", paper.transform, sb.ToString(), note ? 18 : 19, TextAnchor.MiddleLeft, UiFactory.Ink);
            body.resizeTextForBestFit = true; body.resizeTextMinSize = 12; body.resizeTextMaxSize = note ? 18 : 19;
            if (note) body.alignment = TextAnchor.UpperLeft;
            // 본문 영역: 제목 아래 ~ 버튼 위
            var brt = (RectTransform)body.transform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(0.5f, 0.5f);
            brt.offsetMin = new Vector2(left, 75f); brt.offsetMax = new Vector2(right, bodyTop);

            if (grad && stamp != null)
            {
                // 졸업 도장(금빛 손도장)
                var sg = new GameObject("GradStamp", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                sg.transform.SetParent(paper.transform, false);
                var simg = sg.GetComponent<Image>(); simg.sprite = stamp; simg.color = new Color(0.95f, 0.75f, 0.3f, 1f); simg.raycastTarget = false;
                UiFactory.SetRect((RectTransform)sg.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 70), new Vector2(72, 72));
                sg.transform.localEulerAngles = new Vector3(0, 0, -12f);
            }

            var next = UiFactory.Button("Next", paper.transform, nextLabel ?? L.Get("story.next"), UiFactory.Green, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)next.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(160, 46));
            var go = scrim.gameObject;
            next.onClick.AddListener(() => onNext?.Invoke());
            if (memory || note) YeogiCafe.Audio.SoundManager.Sfx("sfx_chime", 0.45f, 0.02f);
            return go;
        }

        // 없는 키면 null(L.Get은 없는 키를 그대로 돌려준다)
        static string Opt(string key) => L.Has(key) ? L.Get(key) : null;

        // 도감 목록용 작은 노트 아이콘(16×18)
        public static PixelCanvas NoteIcon()
        {
            var c = new PixelCanvas(16, 18);
            Color32 cover = new Color32(150, 96, 70, 255), edge = new Color32(96, 60, 44, 255), page = new Color32(250, 244, 226, 255), line = new Color32(150, 180, 210, 255);
            c.FillRect(1, 0, 15, 17, edge);
            c.FillRect(2, 1, 14, 16, cover);
            c.FillRect(5, 3, 13, 14, page);
            for (int y = 5; y <= 12; y += 3) c.HLine(6, 12, y, line);
            for (int y = 2; y <= 15; y += 3) { c.Set(1, y, page); c.Set(2, y, edge); }   // 스프링
            return c;
        }
    }
}
