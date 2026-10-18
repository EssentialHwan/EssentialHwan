using UnityEngine;
using UnityEngine.UI;

namespace YeogiCafe.UI
{
    // 코드로 UGUI 위젯을 만드는 공용 팩토리.
    // TMP 의존을 피하려고 기본 UnityEngine.UI.Text / Button / Image만 사용한다(브리핑 권장).
    // 통일 팔레트(파스텔 따뜻톤)를 UI에도 적용해 절차적/AI 아트와 톤을 맞춘다.
    public static class UiFactory
    {
        // ── 팔레트(PixelPalette hex와 동일 계열) ──
        public static readonly Color Cream    = new(245f / 255f, 232f / 255f, 212f / 255f, 1f);
        public static readonly Color CreamDim  = new(230f / 255f, 210f / 255f, 182f / 255f, 0.96f);
        public static readonly Color Wood      = new(184f / 255f, 142f / 255f, 100f / 255f, 1f);
        public static readonly Color WoodDark  = new(150f / 255f, 110f / 255f, 74f / 255f, 1f);
        public static readonly Color Ink       = new(74f / 255f, 54f / 255f, 47f / 255f, 1f);
        public static readonly Color PinkDeep  = new(196f / 255f, 92f / 255f, 110f / 255f, 1f);
        public static readonly Color Pink      = new(230f / 255f, 160f / 255f, 172f / 255f, 1f);
        public static readonly Color YellowLight = new(248f / 255f, 224f / 255f, 156f / 255f, 1f);
        public static readonly Color Green     = new(150f / 255f, 184f / 255f, 128f / 255f, 1f);
        public static readonly Color PanelBg   = new(245f / 255f, 232f / 255f, 212f / 255f, 0.98f);
        public static readonly Color Scrim     = new(74f / 255f, 54f / 255f, 47f / 255f, 0.55f);

        // 게임 내장 폰트(갈무리11, SIL OFL — 상업 이용 가능, Assets/Resources/Fonts/LICENSE_Galmuri.txt).
        // 이전엔 OS 폰트(맑은 고딕)에 의존 → 한글 폰트 없는 PC·Steam Deck에서 글자가 안 보일 위험.
        static Font cachedFont;
        public static Font DefaultFont
        {
            get
            {
                if (cachedFont != null) return cachedFont;
                cachedFont = Resources.Load<Font>("Fonts/Galmuri11");
                if (cachedFont == null)
                    cachedFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "AppleGothic", "NanumGothic", "Arial" }, 24);
                return cachedFont;
            }
        }

        static Sprite whiteSprite;
        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null) return whiteSprite;
                var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Color.white); tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                return whiteSprite;
            }
        }

        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        public static Text Label(string name, Transform parent, string text, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = DefaultFont;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            // 기본은 줄바꿈(Wrap): 긴 문장이 칸 밖으로 새어 나가지 않게. 한 줄 고정이 필요한 곳만 Overflow로 바꾼다.
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        // 버튼 + 내부 라벨. onClick 배선은 호출부에서.
        public static Button Button(string name, Transform parent, string label, Color bg, Color fg, out Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            img.sprite = ButtonSprite(false); img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 0.5f;   // 도트 2배
            var btn = go.GetComponent<Button>();
            // 틴트는 Image.color(bg)에 곱해진다 → 흰색 기준 배율로
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            btn.colors = colors;
            var st = btn.spriteState; st.pressedSprite = ButtonSprite(true); btn.spriteState = st;
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() => YeogiCafe.Audio.SoundManager.Sfx("sfx_click", 0.5f, 0.05f));

            labelText = Label("Label", go.transform, label, 22, TextAnchor.MiddleCenter, fg);
            var lrt = (RectTransform)labelText.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            return btn;
        }

        // ── 테두리(9-slice) ──
        public enum FrameStyle { Window, Paper, Card, Bubble, Plank }

        public static Sprite ButtonSprite(bool pressed) => YeogiCafe.Art.RuntimeSprites.GetSliced(pressed ? "ui_btn_p" : "ui_btn",
            () => YeogiCafe.Art.UiFrameDrawing.Button(pressed), YeogiCafe.Art.UiFrameDrawing.ButtonBorder);

        public static Sprite FrameSprite(FrameStyle s)
        {
            switch (s)
            {
                case FrameStyle.Paper:  return YeogiCafe.Art.RuntimeSprites.GetSliced("ui_paper", YeogiCafe.Art.UiFrameDrawing.Paper, YeogiCafe.Art.UiFrameDrawing.PaperBorder);
                case FrameStyle.Card:   return YeogiCafe.Art.RuntimeSprites.GetSliced("ui_card", YeogiCafe.Art.UiFrameDrawing.Card, YeogiCafe.Art.UiFrameDrawing.CardBorder);
                case FrameStyle.Bubble: return YeogiCafe.Art.RuntimeSprites.GetSliced("ui_bubble", YeogiCafe.Art.UiFrameDrawing.Bubble, YeogiCafe.Art.UiFrameDrawing.BubbleBorder);
                case FrameStyle.Plank:  return YeogiCafe.Art.RuntimeSprites.GetSliced("ui_plank", YeogiCafe.Art.UiFrameDrawing.Plank, YeogiCafe.Art.UiFrameDrawing.PlankBorder);
                default:                return YeogiCafe.Art.RuntimeSprites.GetSliced("ui_window", YeogiCafe.Art.UiFrameDrawing.Window, YeogiCafe.Art.UiFrameDrawing.WindowBorder);
            }
        }

        // 이미 만든 패널에 테두리 입히기. tint: Card처럼 흰 바탕 스타일은 색으로 물들임(그 외는 흰색 권장).
        public static void Frame(RectTransform panel, FrameStyle style, Color? tint = null, float pixelScale = 2f)
        {
            var img = panel.GetComponent<Image>();
            if (img == null) return;
            float a = img.color.a;
            img.sprite = FrameSprite(style); img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / pixelScale;
            var c = tint ?? Color.white; c.a = tint.HasValue ? tint.Value.a : Mathf.Max(a, 0.98f);
            img.color = c;
        }

        // 테두리 창 + 위쪽 가운데 원목 이름표
        public static Text TitleTab(RectTransform panel, string text, float width = 220f)
        {
            var tab = Panel("TitleTab", panel, Wood);
            Frame(tab, FrameStyle.Plank, new Color(0.82f, 0.62f, 0.44f, 1f));
            SetRect(tab, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(width, 38));
            var t = Label("T", tab, text, 20, TextAnchor.MiddleCenter, Cream);
            Anchor((RectTransform)t.transform, Vector2.zero, Vector2.one, new Vector2(8, 2), new Vector2(-8, 0));
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0.29f, 0.21f, 0.18f, 0.8f); sh.effectDistance = new Vector2(1, -2);
            return t;
        }

        // 가로 슬라이더(0~1): 원목 홈 + 분홍 채움 + 동그란 손잡이
        public static Slider Slider(string name, Transform parent, float value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var bg = Panel("Bg", go.transform, new Color(0.72f, 0.56f, 0.39f, 1f));
            Frame(bg, FrameStyle.Card, new Color(0.86f, 0.76f, 0.64f, 1f), 1f);
            Anchor(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillArea = new GameObject("FillArea", typeof(RectTransform)); fillArea.transform.SetParent(go.transform, false);
            Anchor((RectTransform)fillArea.transform, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            var fill = Panel("Fill", fillArea.transform, Pink);
            Anchor(fill, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            var handleArea = new GameObject("HandleArea", typeof(RectTransform)); handleArea.transform.SetParent(go.transform, false);
            Anchor((RectTransform)handleArea.transform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            var handle = Panel("Handle", handleArea.transform, Cream);
            Frame(handle, FrameStyle.Bubble, Cream, 1f);
            handle.sizeDelta = new Vector2(26, 26);
            var s = go.GetComponent<Slider>();
            s.fillRect = fill; s.handleRect = handle; s.targetGraphic = handle.GetComponent<Image>();
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = 0f; s.maxValue = 1f; s.value = value;
            return s;
        }

        // 앵커/오프셋 지정 헬퍼(픽셀 좌표).
        public static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
        }

        // 화면 상단·좌측 등 고정 크기 배치.
        public static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                   Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        }
    }
}
