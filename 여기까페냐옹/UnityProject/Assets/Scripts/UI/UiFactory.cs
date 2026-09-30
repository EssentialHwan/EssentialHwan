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
        public static readonly Color Pink      = new(230f / 255f, 160f / 255f, 172f / 255f, 1f);
        public static readonly Color Green     = new(150f / 255f, 184f / 255f, 128f / 255f, 1f);
        public static readonly Color PanelBg   = new(245f / 255f, 232f / 255f, 212f / 255f, 0.98f);
        public static readonly Color Scrim     = new(74f / 255f, 54f / 255f, 47f / 255f, 0.55f);

        public static Font DefaultFont =>
            Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "AppleGothic", "NanumGothic", "Arial" }, 24);

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
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
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
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(bg.r * 1.08f, bg.g * 1.08f, bg.b * 1.08f, bg.a);
            colors.pressedColor = new Color(bg.r * 0.9f, bg.g * 0.9f, bg.b * 0.9f, bg.a);
            colors.disabledColor = new Color(bg.r, bg.g, bg.b, 0.4f);
            btn.colors = colors;

            labelText = Label("Label", go.transform, label, 22, TextAnchor.MiddleCenter, fg);
            var lrt = (RectTransform)labelText.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            return btn;
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
