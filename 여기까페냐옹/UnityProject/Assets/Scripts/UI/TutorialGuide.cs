using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Loc;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 1일차 안내 카드: 게임의 한 바퀴(손님 관찰 → 주문·서빙 → 마감 놀아주기 → 밤 추리 기록)를 순서대로 짚어 준다.
    // 단계는 세이브(tutorialStep)에 남아, 중간에 껐다 켜도 이어서 보인다. 2일차부터 시작한 기존 세이브는 건너뛴다.
    public class TutorialGuide
    {
        public const int Welcome = 0, Order = 1, Served = 2, Closing = 3, Settle = 4, Done = 5;

        readonly GameUiController owner;
        readonly RectTransform card;
        readonly Text text;
        float autoHide = -1f;

        public TutorialGuide(GameUiController owner, Transform parent)
        {
            this.owner = owner;
            card = UiFactory.Panel("TutorialGuide", parent, new Color(1f, 0.97f, 0.88f, 0.98f));
            // 왼쪽 아래 구석: 위쪽 알림(리뷰·주문)과 가운데 패널을 가리지 않게
            UiFactory.SetRect(card, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 16), new Vector2(470, 96));
            UiFactory.Frame(card, UiFactory.FrameStyle.Paper);
            var tag = UiFactory.Label("Tag", card, L.Get("tut.tag"), 14, TextAnchor.UpperLeft, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)tag.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(200, 20));
            text = UiFactory.Label("Text", card, "", 17, TextAnchor.MiddleLeft, UiFactory.Ink);
            text.lineSpacing = 1.1f;
            UiFactory.SetRect((RectTransform)text.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-14, -8), new Vector2(-72, -30));
            var close = UiFactory.Button("Close", card, "×", UiFactory.Pink, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(36, 36));
            close.onClick.AddListener(() => card.gameObject.SetActive(false));
            card.gameObject.SetActive(false);
        }

        SaveGame Save => owner.Save;
        public int Step => Save != null ? Save.tutorialStep : Done;

        // 이어하기: 2일차 이후 세이브는 안내를 건너뛴다
        public void Begin()
        {
            var s = Save; if (s == null) return;
            if (s.tutorialStep < Done && s.currentDay > 1) s.tutorialStep = Done;
            if (s.tutorialStep == Welcome || s.tutorialStep == Order) Show(s.tutorialStep == Welcome ? "tut.welcome" : "tut.order");
        }

        public void DebugReplay() { if (Save != null) Go(Welcome, "tut.welcome"); }

        public void OnOrderPlaced() { if (Step == Welcome) Go(Order, "tut.order"); }
        public void OnServed() { if (Step <= Order) { Go(Served, "tut.served"); autoHide = 14f; } }
        public void OnClosing() { if (Step < Closing) Go(Closing, "tut.closing"); }
        public void OnSettlement() { if (Step < Settle) Go(Settle, "tut.settle"); }
        public void OnNextDay()
        {
            if (Step >= Done) return;
            Save.tutorialStep = Done;
            card.gameObject.SetActive(false);
        }

        void Go(int step, string key)
        {
            if (Save == null) return;
            Save.tutorialStep = step;
            Show(key);
        }

        void Show(string key)
        {
            text.text = L.Get(key);
            card.gameObject.SetActive(true);
            card.SetAsLastSibling();
            autoHide = -1f;
        }

        public void Tick(float dt)
        {
            if (autoHide < 0f || !card.gameObject.activeSelf) return;
            autoHide -= dt;
            if (autoHide <= 0f) card.gameObject.SetActive(false);
        }

        // 다른 패널이 위로 올라왔을 때 안내가 가려지지 않게
        public void BringToFront()
        {
            if (EndingPanel.IsOpen) { if (card.gameObject.activeSelf) card.SetAsFirstSibling(); return; }
            if (card.gameObject.activeSelf) card.SetAsLastSibling();
        }
    }
}
