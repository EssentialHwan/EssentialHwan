using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;
using YeogiCafe.Data;
using YeogiCafe.Loc;

namespace YeogiCafe.UI
{
    // 냥냥 보호소 후원 창(정산 화면에서 연다). 누적 후원액으로 단계가 오르고, 단계마다 보호소 고양이가 손님으로 찾아온다.
    public class ShelterPanel
    {
        readonly GameUiController owner;
        readonly Transform parent;
        GameObject go;
        public bool IsOpen => go != null;

        public ShelterPanel(GameUiController owner, Transform parent) { this.owner = owner; this.parent = parent; }

        public void Open() { Render(); }
        public void Close() { if (go != null) Object.Destroy(go); go = null; }

        void Render(int newTier = -1)
        {
            Close();
            var s = owner.Save; if (s == null) return;
            var scrim = UiFactory.Panel("ShelterScrim", parent, new Color(0.08f, 0.07f, 0.12f, 0.6f));
            go = scrim.gameObject;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scrim.SetAsLastSibling();

            var paper = UiFactory.Panel("Paper", scrim, new Color(1f, 0.97f, 0.9f, 1f));
            UiFactory.SetRect(paper, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 600));
            UiFactory.Frame(paper, UiFactory.FrameStyle.Paper, null, 3f);

            var title = UiFactory.Label("Title", paper, L.Get("shelter.title"), 28, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(-40, 38));
            var intro = UiFactory.Label("Intro", paper, L.Get("shelter.intro"), 16, TextAnchor.UpperCenter, UiFactory.WoodDark);
            intro.lineSpacing = 1.1f;
            UiFactory.SetRect((RectTransform)intro.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -62), new Vector2(-90, 48));

            // 진행도
            int donated = s.shelterDonated, tier = ShelterBook.TierOf(donated), next = ShelterBook.NextNeed(donated);
            int prevNeed = tier > 0 ? ShelterBook.Tiers[tier - 1].need : 0;
            string prog = next < 0 ? L.Get("shelter.done", ("total", donated.ToString("N0")))
                                   : L.Get("shelter.progress", ("total", donated.ToString("N0")), ("left", (next - donated).ToString("N0")));
            var pl = UiFactory.Label("Prog", paper, prog, 18, TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)pl.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -116), new Vector2(-60, 26));
            var barBg = UiFactory.Panel("BarBg", paper, UiFactory.CreamDim);
            UiFactory.SetRect(barBg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -146), new Vector2(560, 14));
            var fill = UiFactory.Panel("Fill", barBg, UiFactory.PinkDeep);
            float k = next < 0 ? 1f : Mathf.Clamp01((donated - prevNeed) / (float)(next - prevNeed));
            UiFactory.Anchor(fill, Vector2.zero, new Vector2(k, 1), Vector2.zero, Vector2.zero);

            // 단계 목록
            for (int i = 0; i < ShelterBook.Tiers.Length; i++)
            {
                var t = ShelterBook.Tiers[i];
                bool got = tier > i, isNext = tier == i;
                var row = UiFactory.Panel("Tier", paper, got ? new Color(0.93f, 0.97f, 0.88f, 1f) : UiFactory.Cream);
                UiFactory.Frame(row, UiFactory.FrameStyle.Card, got ? new Color(0.93f, 0.97f, 0.88f, 1f) : UiFactory.Cream);
                UiFactory.SetRect(row, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -172 - i * 70), new Vector2(680, 62));

                var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                icon.transform.SetParent(row, false);
                var img = icon.GetComponent<Image>(); img.raycastTarget = false; img.preserveAspect = true;
                if (t.catId != null) { img.sprite = CatSpriteBank.Portrait(t.catId); if (!got) img.color = new Color(0.15f, 0.12f, 0.12f, 0.85f); }   // 아직이면 실루엣
                else img.color = new Color(0, 0, 0, 0);
                UiFactory.SetRect((RectTransform)icon.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(56, 56));
                if (t.catId == null)
                {
                    var h = UiFactory.Label("Heart", icon.transform, "♥", 30, TextAnchor.MiddleCenter, got ? UiFactory.PinkDeep : UiFactory.WoodDark);
                    UiFactory.Anchor((RectTransform)h.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }

                string reward = t.catId != null
                    ? (got || isNext ? L.Get("shelter.reward.cat", ("name", L.Get($"cat.{t.catId}.name"))) : L.Get("shelter.reward.secret"))
                    : L.Get("shelter.reward.frame");
                var lt = UiFactory.Label("T", row, L.Get("shelter.tier", ("n", (i + 1).ToString()), ("g", t.need.ToString("N0"))) + "   " + reward,
                                         17, TextAnchor.MiddleLeft, UiFactory.Ink);
                lt.horizontalOverflow = HorizontalWrapMode.Overflow;
                UiFactory.SetRect((RectTransform)lt.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(76, 0), new Vector2(-190, 0));
                UiFactory.FitOneLine(lt, 680 - 76 - 120, 12);
                var st = UiFactory.Label("S", row, L.Get(got ? "shelter.got" : isNext ? "shelter.next" : "shelter.locked"), 16, TextAnchor.MiddleRight,
                                         got ? new Color(0.36f, 0.6f, 0.3f, 1f) : UiFactory.WoodDark);
                UiFactory.SetRect((RectTransform)st.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(110, 0));
                st.horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            // 후원 버튼
            int gold = owner.Economy != null ? owner.Economy.Gold : 0;
            int left = next < 0 ? 0 : next - donated;
            Btn(paper, L.Get("shelter.give", ("g", "100")), next >= 0 && gold >= 100, new Vector2(-270, 30), () => Donate(100));
            Btn(paper, L.Get("shelter.give", ("g", "500")), next >= 0 && gold >= 500, new Vector2(-90, 30), () => Donate(500));
            Btn(paper, L.Get("shelter.giveall", ("g", left.ToString("N0"))), next >= 0 && gold >= left && left > 0, new Vector2(110, 30), () => Donate(left), 210);
            var close = UiFactory.Button("Close", paper, L.Get("photo.close"), UiFactory.Green, UiFactory.Ink, out var cl);
            cl.fontSize = 18;
            UiFactory.SetRect((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(300, 30), new Vector2(140, 44));
            close.onClick.AddListener(Close);

            if (newTier > 0) News(scrim, newTier);
        }

        void Btn(RectTransform parent, string text, bool on, Vector2 pos, System.Action act, float w = 160)
        {
            var b = UiFactory.Button("Give", parent, text, on ? UiFactory.PinkDeep : UiFactory.CreamDim, on ? UiFactory.Cream : UiFactory.WoodDark, out var lbl);
            lbl.fontSize = 17; lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.SetRect((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), pos, new Vector2(w, 44));
            b.interactable = on;
            b.onClick.AddListener(() => act());
        }

        void Donate(int amount)
        {
            var s = owner.Save; var eco = owner.Economy;
            if (s == null || eco == null || amount <= 0) return;
            int before = ShelterBook.TierOf(s.shelterDonated);
            if (!eco.TrySpend(amount)) { owner.ShowToast(L.Get("ui.shop.insufficient")); return; }
            s.shelterDonated += amount;
            int after = ShelterBook.TierOf(s.shelterDonated);
            if (after >= 4 && !s.photoFrames.Contains(ShelterBook.RewardFrame)) s.photoFrames.Add(ShelterBook.RewardFrame);
            owner.RefreshHud();
            YeogiCafe.Audio.SoundManager.Sfx(after > before ? "sfx_chime" : "sfx_coin", 0.6f);
            YeogiCafe.Core.GameEvents.Raise(YeogiCafe.Core.GameEvents.Donate);
            Render(after > before ? after : -1);
        }

        // 단계가 올랐을 때 보호소 소식 한 장
        void News(Transform scrim, int tier)
        {
            var t = ShelterBook.Tiers[tier - 1];
            var card = UiFactory.Panel("News", scrim, new Color(1f, 0.99f, 0.95f, 1f));
            UiFactory.SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 300));
            UiFactory.Frame(card, UiFactory.FrameStyle.Paper, null, 3f);
            var title = UiFactory.Label("T", card, L.Get("shelter.news.title"), 24, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(-40, 34));
            if (t.catId != null)
            {
                var pg = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pg.transform.SetParent(card, false);
                var pimg = pg.GetComponent<Image>(); pimg.sprite = CatSpriteBank.Portrait(t.catId); pimg.preserveAspect = true; pimg.raycastTarget = false;
                UiFactory.SetRect((RectTransform)pg.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 4), new Vector2(140, 120));
            }
            string body = t.catId != null ? L.Get($"shelter.news.{t.catId}", ("name", L.Get($"cat.{t.catId}.name"))) : L.Get("shelter.news.frame");
            var b = UiFactory.Label("B", card, body, 18, TextAnchor.MiddleLeft, UiFactory.Ink);
            b.lineSpacing = 1.15f;
            UiFactory.SetRect((RectTransform)b.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                              new Vector2(t.catId != null ? 80 : 0, 8), new Vector2(t.catId != null ? -220 : -60, -130));
            var ok = UiFactory.Button("Ok", card, L.Get("story.next"), UiFactory.Green, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(150, 44));
            ok.onClick.AddListener(() => Object.Destroy(card.gameObject));
        }
    }
}
