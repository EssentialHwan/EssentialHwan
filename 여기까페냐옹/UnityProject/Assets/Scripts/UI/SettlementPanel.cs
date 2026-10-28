using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Art;
using UnityEngine.UI;
using YeogiCafe.Data;
using YeogiCafe.Loc;
using YeogiCafe.Observation;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 하루 정산 패널 — 매출/재료비/순익 + 고양이별 관찰 카드([기록] 버튼) + [다음 날].
    // Truth 접근 없이 ObservationCardBuilder(=ObservationRecord 기반)만 렌더한다.
    public class SettlementPanel
    {
        readonly GameUiController owner;
        readonly GameObject rootGo;
        Text revenueText, cardsInfo, levelText, expandInfo;
        Button expandBtn, shelterBtn; Text expandLbl;
        RectTransform cardsContent;
        WheelList cardsList;

        // 밤의 관찰 일지 연출: 플레이어 고양이 앞발이 [기록]을 체크하고, 하루를 마칠 때 손도장을 찍는다.
        RectTransform paw; Image pawImg; Sprite pawBack, pawPress, pawPads, checkSprite, stampSprite;
        RectTransform stampBox; Image stampImg; Text stampLabel;
        bool busy;
        public SettlementPanel(GameUiController owner, Transform parent)
        {
            this.owner = owner;

            // 배경 스크림(정산 중 게임 화면 가림)
            var scrim = UiFactory.Panel("SettleScrim", parent, new Color(0.13f, 0.14f, 0.27f, 0.82f));   // 밤
            rootGo = scrim.gameObject;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = UiFactory.Panel("SettlePanel", scrim.transform, UiFactory.PanelBg);
            UiFactory.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              Vector2.zero, new Vector2(840, 640));
            UiFactory.Frame(panel, UiFactory.FrameStyle.Window);

            var title = UiFactory.Label("Title", panel.transform, L.Get("ui.settle.title"), 26,
                                        TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(0, 40));     // 창 테두리(≈24px) 아래로

            revenueText = UiFactory.Label("Revenue", panel.transform, "", 21, TextAnchor.UpperCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)revenueText.transform, new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -68), new Vector2(-300, 60));      // 오른쪽 위 도장 칸과 안 겹치게

            // 오늘 온 고양이가 많으면(13마리) 휠로 내려 본다
            cardsList = WheelList.Create(panel.transform, "Cards", 4f, out cardsContent);
            UiFactory.SetRect(cardsList.view, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                              new Vector2(0, -6), new Vector2(-48, -294));    // 아래 결과 문구(cardsInfo) 줄과 안 겹치게 끝을 올림

            cardsInfo = UiFactory.Label("CardsInfo", panel.transform, "", 20, TextAnchor.LowerCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)cardsInfo.transform, new Vector2(0, 0), new Vector2(1, 0),
                              new Vector2(0.5f, 0), new Vector2(0, 104), new Vector2(-72, 30));    // 테두리에 붙지 않게 가운데 정렬·안쪽 여백
            cardsInfo.raycastTarget = false;                                                         // 글자가 버튼 클릭을 가로채지 않게
            cardsInfo.resizeTextForBestFit = true; cardsInfo.resizeTextMinSize = 13; cardsInfo.resizeTextMaxSize = 20;   // 긴 결과 문구는 줄여서 한 칸 안에
            cardsInfo.verticalOverflow = VerticalWrapMode.Truncate;

            var nextBtn = UiFactory.Button("Next", panel.transform, L.Get("ui.settle.next"),
                                           UiFactory.Green, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)nextBtn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                              new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(220, 56));
            nextBtn.onClick.AddListener(() => { if (!busy) owner.StartCoroutine(FinishDay()); });
            UiFactory.SetRect((RectTransform)nextBtn.transform, new Vector2(1, 0), new Vector2(1, 0),
                              new Vector2(1, 0), new Vector2(-24, 20), new Vector2(200, 52));

            // 카페 넓히기(성장) — 관찰 성과 또는 가구 수로 열리는 같은 방 확장(P3 OR 경로)
            expandBtn = UiFactory.Button("Expand", panel.transform, L.Get("ui.settle.expand"),
                                         UiFactory.Wood, UiFactory.Cream, out expandLbl);
            expandLbl.resizeTextForBestFit = true; expandLbl.resizeTextMinSize = 14; expandLbl.resizeTextMaxSize = 22;
            UiFactory.SetRect((RectTransform)expandBtn.transform, new Vector2(0, 0), new Vector2(0, 0),
                              new Vector2(0, 0), new Vector2(24, 20), new Vector2(180, 52));
            expandBtn.onClick.AddListener(() =>
            {
                owner.TryExpandCafe(out var msg);
                expandInfo.text = msg;
                RenderLevel();
            });
            // 냥냥 보호소 후원(카페 Lv2부터) — 오늘 번 골드를 다른 고양이를 위해
            shelterBtn = UiFactory.Button("Shelter", panel.transform, L.Get("shelter.btn"), UiFactory.PinkDeep, UiFactory.Cream, out var shLbl);
            shLbl.fontSize = 19; shLbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.SetRect((RectTransform)shelterBtn.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(364, 20), new Vector2(190, 52));
            shelterBtn.onClick.AddListener(() => { if (!busy) owner.OpenShelter(); });
            levelText = UiFactory.Label("Level", panel.transform, "", 20, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)levelText.transform, new Vector2(0, 0), new Vector2(0, 0),
                              new Vector2(0, 0), new Vector2(214, 20), new Vector2(140, 52));
            expandInfo = UiFactory.Label("ExpandInfo", panel.transform, "", 16, TextAnchor.MiddleLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)expandInfo.transform, new Vector2(0, 0), new Vector2(1, 0),
                              new Vector2(0, 0), new Vector2(24, 76), new Vector2(-48, 24));

            // 오늘의 도장 칸(창 오른쪽 위 — 아래쪽 추리 줄을 가리지 않게)
            var box = UiFactory.Panel("StampBox", panel.transform, new Color(1f, 0.98f, 0.94f, 1f));
            stampBox = (RectTransform)box.transform;
            UiFactory.Frame(stampBox, UiFactory.FrameStyle.Paper);
            UiFactory.SetRect(stampBox, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-26, -26), new Vector2(104, 104));
            var boxTitle = UiFactory.Label("StampTitle", box.transform, L.Get("ui.settle.stamp"), 15, TextAnchor.UpperCenter, UiFactory.WoodDark);
            UiFactory.FitOneLine(boxTitle, 92, 11);                  // 도장 칸 104px 안에 한 줄(영어 'Today's Stamp')
            UiFactory.SetRect((RectTransform)boxTitle.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(0, 20));
            var si = new GameObject("Stamp", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            si.transform.SetParent(box.transform, false);
            stampImg = si.GetComponent<Image>(); stampImg.raycastTarget = false; stampImg.preserveAspect = true;
            UiFactory.SetRect((RectTransform)si.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -3), new Vector2(64, 64));
            stampLabel = UiFactory.Label("StampDay", box.transform, "", 14, TextAnchor.LowerCenter, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)stampLabel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(0, 18));

            bool art = PawArt.Has;     // 새 앞발 원화(주방과 같은 손)
            pawBack = art ? PawArt.Get(PawArt.Idle, false) : RuntimeSprites.Get("paw_back", () => PawDrawing.Back());
            pawPress = art ? PawArt.Get(PawArt.Press, false) : RuntimeSprites.Get("paw_back_press", () => PawDrawing.Back(true));
            pawPads = art ? PawArt.Get(PawArt.Groom0, false) : RuntimeSprites.Get("paw_pads", PawDrawing.Pads);
            checkSprite = RuntimeSprites.Get("ui_check", PawDrawing.Check);
            stampSprite = RuntimeSprites.Get("ui_pawstamp", PawDrawing.Stamp);

            // 앞발(화면 아래에서 올라옴). 피벗 = 발가락 끝 → 목표 지점에 발끝이 닿는다.
            var pg = new GameObject("PlayerPaw", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pg.transform.SetParent(scrim.transform, false);
            paw = (RectTransform)pg.transform; pawImg = pg.GetComponent<Image>(); pawImg.raycastTarget = false;
            pawImg.sprite = pawBack;
            paw.anchorMin = paw.anchorMax = new Vector2(0.5f, 0f);
            paw.pivot = new Vector2(0.5f, PawArt.Has ? PawArt.TipY(PawArt.Idle) : 0.94f);
            paw.sizeDelta = PawArt.Has ? PawArt.CellSize * 1.0f : new Vector2(PawDrawing.W, PawDrawing.H) * 2.6f;
            pg.SetActive(false);

            rootGo.SetActive(false);
        }

        // ── 이야기 카드(단골·졸업 편지) — 커피토크/스피릿페어러 참고 ──
        GameObject storyGo;

        // ── 냥슐랭 심사 결과 카드(그날 심사원이 왔으면, 이야기 다음에 한 번) ──
        void TryOpenReport()
        {
            var s = owner.Save; if (s == null || storyGo != null) return;
            var r = s.lastReport; int today = owner.Day != null ? owner.Day.CurrentDay : -1;
            if (r == null || r.day != today || r.seen) return;
            var scrim = UiFactory.Panel("ReportScrim", rootGo.transform, new Color(0.08f, 0.07f, 0.12f, 0.55f));
            storyGo = scrim.gameObject;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var paper = UiFactory.Panel("Report", scrim.transform, new Color(1f, 0.98f, 0.92f, 1f));
            UiFactory.SetRect(paper, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 420));
            UiFactory.Frame(paper, UiFactory.FrameStyle.Paper, null, 3f);
            var t = UiFactory.Label("T", paper, L.Get("nyan.report.title"), 24, TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)t.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(-60, 34));
            bool pass = r.starsAfter > r.starsBefore;
            string body =
                L.Get("nyan.report.dish", ("n", r.dish.ToString())) + "\n" +
                L.Get("nyan.report.speed", ("n", r.speed.ToString())) + "\n" +
                L.Get("nyan.report.furn", ("n", r.furniture.ToString())) + "\n" +
                L.Get("nyan.report.clean", ("n", r.clean.ToString())) + "\n\n" +
                L.Get("nyan.report.total", ("n", r.total.ToString()), ("need", YeogiCafe.Food.NyanGuide.NextPassScore(r.starsBefore).ToString()));
            var b = UiFactory.Label("B", paper, body, 19, TextAnchor.UpperCenter, UiFactory.WoodDark);
            b.lineSpacing = 1.15f;
            UiFactory.SetRect((RectTransform)b.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -72), new Vector2(-80, 200));
            var res = UiFactory.Label("R", paper, (pass ? L.Get("nyan.report.pass") : L.Get("nyan.report.fail")) + "\n" + YeogiCafe.Food.NyanGuide.Stars(r.starsAfter),
                                      22, TextAnchor.MiddleCenter, pass ? UiFactory.PinkDeep : UiFactory.Ink);
            UiFactory.SetRect((RectTransform)res.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(-60, 64));
            var next = UiFactory.Button("Next", paper, L.Get("story.next"), UiFactory.Green, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)next.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(160, 46));
            next.onClick.AddListener(() =>
            {
                r.seen = true;
                var sm = Object.FindObjectOfType<YeogiCafe.Save.SaveManager>(); if (sm != null) sm.Save(s);
                Object.Destroy(storyGo); storyGo = null;
            });
            if (pass) YeogiCafe.Audio.SoundManager.Sfx("sfx_chime", 0.6f, 0.02f);
        }

        void OpenStory(StoryDTO st)
        {
            storyGo = StoryCard.Open(rootGo.transform, st, stampSprite, () =>
            {
                if (owner.Save != null && owner.Save.storyQueue.Count > 0)
                {
                    owner.Save.storyQueue.RemoveAt(0);
                    // 본 이야기는 바로 저장 — 정산 중에 게임을 끄면 다음에 같은 이야기가 또 뜨던 문제
                    var sm = Object.FindObjectOfType<YeogiCafe.Save.SaveManager>();
                    if (sm != null) sm.Save(owner.Save);
                }
                Object.Destroy(storyGo); storyGo = null;
                if (owner.Save != null && owner.Save.storyQueue.Count > 0) OpenStory(owner.Save.storyQueue[0]);
                else TryOpenReport();
            });
        }

        // ── 앞발 연출 ──
        IEnumerator MovePaw(Vector3 from, Vector3 to, float sec)
        {
            for (float t = 0; t < sec; t += Time.unscaledDeltaTime)
            {
                float k = t / sec; k = k * k * (3f - 2f * k);
                paw.position = Vector3.Lerp(from, to, k);
                yield return null;
            }
            paw.position = to;
        }

        Vector3 PawHome(Vector3 target)
        {
            // 목표 바로 아래, 화면 밖
            return new Vector3(target.x + 40f, -paw.rect.height * paw.lossyScale.y * 0.2f, 0f);
        }

        IEnumerator PawTap(Vector3 target, System.Action onPress)
        {
            paw.gameObject.SetActive(true); paw.SetAsLastSibling();
            pawImg.sprite = pawBack;
            var home = PawHome(target);
            yield return MovePaw(home, target + new Vector3(0, 24f, 0), 0.28f);
            yield return MovePaw(paw.position, target, 0.07f);
            pawImg.sprite = pawPress;
            YeogiCafe.Audio.SoundManager.Sfx("sfx_knead", 0.5f);
            onPress?.Invoke();
            yield return new WaitForSecondsRealtime(0.14f);
            pawImg.sprite = pawBack;
            yield return MovePaw(paw.position, home, 0.25f);
            paw.gameObject.SetActive(false);
        }

        // 체크 표시가 '쓱' 그려지듯 나타남
        IEnumerator PopCheck(RectTransform at)
        {
            var go = new GameObject("CheckPop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(rootGo.transform, false);
            var rt = (RectTransform)go.transform; var img = go.GetComponent<Image>();
            img.sprite = checkSprite; img.raycastTarget = false; img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal; img.fillAmount = 0f;
            rt.sizeDelta = new Vector2(48, 48); rt.position = at.position;
            for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime) { img.fillAmount = t / 0.22f; yield return null; }
            img.fillAmount = 1f;
            yield return new WaitForSecondsRealtime(0.35f);
            Object.Destroy(go);
        }

        // 하루 마무리: 손도장 쾅 → 다음 날
        IEnumerator FinishDay()
        {
            busy = true;
            var target = stampImg.rectTransform.position;
            paw.gameObject.SetActive(true); paw.SetAsLastSibling();
            var home = PawHome(target);
            pawImg.sprite = pawPads;                                       // 젤리를 보여주며 올라옴
            yield return MovePaw(home, target + new Vector3(0, 60f, 0), 0.35f);
            yield return new WaitForSecondsRealtime(0.18f);
            pawImg.sprite = pawBack;
            yield return MovePaw(paw.position, target + new Vector3(0, 30f, 0), 0.08f);   // 쾅
            pawImg.sprite = pawPress;
            YeogiCafe.Audio.SoundManager.Sfx("sfx_stamp", 0.7f, 0.02f);
            var save = owner.Save;
            int n = save != null ? save.pawStamps + 1 : 1;
            stampImg.sprite = stampSprite; stampImg.color = Color.white;
            stampImg.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(-14f, 14f));
            stampLabel.text = L.Get("ui.settle.stampday", ("n", (owner.Day != null ? owner.Day.CurrentDay : n).ToString()));
            for (float t = 0; t < 0.16f; t += Time.unscaledDeltaTime)
            {
                float s = Mathf.Lerp(1.35f, 1f, t / 0.16f);
                stampImg.rectTransform.localScale = Vector3.one * s;
                yield return null;
            }
            stampImg.rectTransform.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(0.18f);
            pawImg.sprite = pawBack;
            yield return MovePaw(paw.position, home, 0.3f);
            paw.gameObject.SetActive(false);
            if (save != null) save.pawStamps = n;
            yield return new WaitForSecondsRealtime(0.6f);
            busy = false;
            owner.StartNextDay();
        }

        public void Show()
        {
            rootGo.SetActive(true);
            if (!busy && stampImg != null) { stampImg.sprite = null; stampImg.color = new Color(1, 1, 1, 0); stampLabel.text = ""; }
            if (expandInfo != null) expandInfo.text = "";
            RenderEconomy();
            cardsContent.anchoredPosition = Vector2.zero;       // 새 밤 일지는 맨 위부터
            RenderCards();
            RenderLevel();
            if (storyGo == null && owner.Save != null && owner.Save.storyQueue.Count > 0) OpenStory(owner.Save.storyQueue[0]);
            else TryOpenReport();
        }

        void RenderLevel()
        {
            if (shelterBtn != null) shelterBtn.gameObject.SetActive(owner.CafeLevel >= 2);
            // 최대 레벨이면 [카페 넓히기] 대신 '최대 레벨' 표시 + 눌리지 않게(회색)
            bool max = owner.CafeAtMax;
            if (expandBtn != null)
            {
                expandBtn.interactable = !max;
                expandBtn.GetComponent<Image>().color = max ? UiFactory.CreamDim : UiFactory.Wood;
                expandLbl.text = L.Get(max ? "cafe.block.max" : "ui.settle.expand");
                expandLbl.color = max ? UiFactory.WoodDark : UiFactory.Cream;
            }
            levelText.text = L.Get("ui.settle.cafelevel", ("n", owner.CafeLevel.ToString()));
        }
        public void Hide() => rootGo.SetActive(false);
        public bool IsOpen => rootGo != null && rootGo.activeSelf;

        void RenderEconomy()
        {
            var eco = owner.Economy;
            if (eco == null) { revenueText.text = ""; return; }
            revenueText.text =
                $"{L.Get("ui.settle.revenue")}: {eco.TodayRevenue}G   " +
                $"{L.Get("ui.settle.cost")}: {eco.TodayCost}G   " +
                $"{L.Get("ui.settle.net")}: {eco.TodayNet}G";
            int lost = owner.LostToMice;
            if (lost > 0) revenueText.text += "\n<color=#C45C6E>" + L.Get("ui.settle.mice", ("g", lost.ToString())) + "</color>";
        }

        void ClearCards() => cardsList.Clear();

        void RenderCards()
        {
            ClearCards();
            var save = owner.Save;
            if (save == null) { cardsInfo.text = ""; return; }

            int shown = 0;
            foreach (var cs in save.cats)
            {
                if (cs.lastVisitDay < 0) continue;
                // 오늘 온 고양이만(일지는 '오늘 밤' 기록). 전체는 도감에서.
                int today = owner.Day != null ? owner.Day.CurrentDay : cs.lastVisitDay;
                if (cs.lastVisitDay != today) continue;
                var data = owner.LookupCat(cs.catId);
                if (data == null) continue;
                BuildCatRow(data, cs);
                shown++;
            }
            cardsInfo.text = shown == 0 ? L.Get("ui.settle.needmore") : "";
            cardsList.Fit(toTop: false);
        }

        void BuildCatRow(CatData data, CatSaveData cs)
        {
            var row = UiFactory.Panel("Card_" + data.catId, cardsContent, UiFactory.Cream);
            UiFactory.Frame(row, UiFactory.FrameStyle.Card, UiFactory.Cream);
            ((RectTransform)row.transform).sizeDelta = new Vector2(0, 40);

            string catName = L.Get($"cat.{data.catId}.name");
            var label = UiFactory.Label("Name", row.transform, catName, 20, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.FitOneLine(label, 104, 13);                    // 'タキシードニャン' 같은 긴 이름이 체크 표시를 덮지 않게
            UiFactory.SetRect((RectTransform)label.transform, new Vector2(0, 0), new Vector2(0, 1),
                              new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(110, 0));

            // 축마다 칸 하나: 기록했으면 체크+알아낸 이름, 아니면 상태 + [추리] 버튼
            BuildAxisCell(row.transform, data, cs, PrefAxis.Food, 0);
            BuildAxisCell(row.transform, data, cs, PrefAxis.Seat, 1);
            BuildAxisCell(row.transform, data, cs, PrefAxis.Facility, 2);

            // [확인] — 고른 답을 한꺼번에 채점(오브라 딘식). 도전 가능한 칸을 모두 골라야 활성.
            var eligible = EligibleAxes(data.catId);
            if (eligible.Count == 0) return;
            bool allPicked = true;
            foreach (var a in eligible) if (string.IsNullOrEmpty(cs.observation.Get(a).pendingGuess)) allPicked = false;
            var cbtn = UiFactory.Button("Confirm", row.transform, L.Get("ui.guess.confirm"),
                                        allPicked ? UiFactory.PinkDeep : UiFactory.Wood, UiFactory.Cream, out var clbl);
            clbl.fontSize = 15;
            UiFactory.SetRect((RectTransform)cbtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(-6, 0), new Vector2(70, 34));
            cbtn.interactable = allPicked;
            cbtn.onClick.AddListener(() => { if (!busy) owner.StartCoroutine(ConfirmWithPaw(data, (RectTransform)cbtn.transform)); });
        }

        List<PrefAxis> EligibleAxes(string catId)
        {
            var list = new List<PrefAxis>();
            var obs = owner.Obs; if (obs == null) return list;
            int day = owner.Day != null ? owner.Day.CurrentDay : 0;
            foreach (var a in new[] { PrefAxis.Food, PrefAxis.Seat, PrefAxis.Facility })
                if (obs.CanGuess(catId, a, day) == ObservationManager.GuessResult.Correct) list.Add(a);
            return list;
        }

        bool IsCorrect(CatData cat, PrefAxis axis, string id)
        {
            foreach (var o in BuildOptions(cat, axis)) if (o.id == id) return o.correct;
            return false;
        }

        IEnumerator ConfirmWithPaw(CatData cat, RectTransform btnRt)
        {
            busy = true;
            var cs = owner.Save.GetOrCreateCat(cat.catId);
            var items = new List<ObservationManager.BatchItem>();
            foreach (var a in EligibleAxes(cat.catId))
            {
                string id = cs.observation.Get(a).pendingGuess;
                items.Add(new ObservationManager.BatchItem { axis = a, guessId = id, correct = IsCorrect(cat, a, id) });
            }
            bool all = false; int n = 0, bonus = 0;
            int day = owner.Day != null ? owner.Day.CurrentDay : 0;
            yield return PawTap(btnRt.position, () =>
            {
                all = owner.Obs.SubmitBatch(cat.catId, items, day, out n, out bonus);
                if (all) owner.StartCoroutine(PopCheck(btnRt));
            });
            string catName = L.Get($"cat.{cat.catId}.name");
            if (all)
            {
                if (bonus > 0 && owner.Economy != null) owner.Economy.AddGold(bonus);
                owner.RefreshHud();
                cardsInfo.text = L.Get("ui.guess.allok", ("name", catName), ("g", bonus.ToString()));
                YeogiCafe.Core.GameEvents.Raise(YeogiCafe.Core.GameEvents.GuessCorrect, cat.catId);
            }
            else cardsInfo.text = L.Get("ui.guess.partial", ("name", catName), ("n", n.ToString()), ("t", items.Count.ToString()));
            yield return new WaitForSecondsRealtime(0.3f);
            busy = false;
            string keep = cardsInfo.text;
            Show();
            cardsInfo.text = keep;
        }

        static string AxisLabel(PrefAxis axis) =>
            axis == PrefAxis.Food ? L.Get("ui.axis.food") : axis == PrefAxis.Seat ? L.Get("ui.axis.seat") : L.Get("ui.axis.facility");

        void BuildAxisCell(Transform parent, CatData data, CatSaveData cs, PrefAxis axis, int index)
        {
            const float cellW = 190f, startX = 120f;
            var rec = cs.observation.Get(axis);
            var cell = new GameObject($"Cell_{axis}", typeof(RectTransform));
            cell.transform.SetParent(parent, false);
            var crt = (RectTransform)cell.transform;
            UiFactory.SetRect(crt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                              new Vector2(startX + index * cellW, 0), new Vector2(cellW - 8, 0));

            if (rec.state == ObsState.Recorded)
            {
                var go = new GameObject("Check", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(cell.transform, false);
                var img = go.GetComponent<Image>(); img.sprite = checkSprite; img.raycastTarget = false;
                UiFactory.SetRect((RectTransform)go.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                  new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(30, 30));
                // 기록된 칸은 값만(체크 + 'Fish Cake') — 칸 위치가 곧 축이고, 축 이름까지 붙이면 긴 언어에서 11px로 줄었다
                var t = UiFactory.Label("Val", cell.transform, ObservationCardBuilder.ValueName(axis, rec.recordedValue),
                                        15, TextAnchor.MiddleLeft, UiFactory.PinkDeep);
                UiFactory.FitOneLine(t, cellW - 8 - 36, 11);
                UiFactory.SetRect((RectTransform)t.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                                  new Vector2(34, 0), new Vector2(-34, 0));
                return;
            }

            bool picked = !string.IsNullOrEmpty(rec.pendingGuess);
            string stateText = picked
                ? $"{AxisLabel(axis)} {ObservationCardBuilder.ValueName(axis, rec.pendingGuess)}?"
                : $"{AxisLabel(axis)} {ObservationCardBuilder.AxisText(rec)}";
            var stateLbl = UiFactory.Label("State", cell.transform, stateText, 15, TextAnchor.MiddleLeft, picked ? UiFactory.Ink : UiFactory.WoodDark);
            UiFactory.FitOneLine(stateLbl, cellW - 8 - 66 - 4, 11);                 // 버튼과 겹치지 않게 한 줄로
            UiFactory.SetRect((RectTransform)stateLbl.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                              new Vector2(0, 0), new Vector2(-66, 0));

            var obs = owner.Obs;
            int day = owner.Day != null ? owner.Day.CurrentDay : 0;
            var can = obs != null ? obs.CanGuess(data.catId, axis, day) : ObservationManager.GuessResult.NotReady;
            bool ok = can == ObservationManager.GuessResult.Correct;
            var btn = UiFactory.Button($"Guess_{axis}", cell.transform, L.Get(picked ? "ui.guess.change" : "ui.guess.btn"),
                                       ok ? UiFactory.Pink : UiFactory.Wood, UiFactory.Cream, out var lbl);
            lbl.fontSize = 14;
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(60, 34));
            btn.interactable = ok;
            btn.onClick.AddListener(() => { if (!busy) OpenGuess(data, axis, (RectTransform)btn.transform); });
        }

        // ── 추리 선택지 팝업 ──
        GameObject guessGo;
        Text guessMsg;

        struct Option { public string id, name; public Sprite icon; public bool correct; }

        List<Option> BuildOptions(CatData cat, PrefAxis axis)
        {
            var list = new List<Option>();
            var seen = new HashSet<string>();
            var cafe = owner.Cafe;
            if (cafe == null) return list;
            if (axis == PrefAxis.Food)
            {
                foreach (var m in cafe.GetAvailableMenus())
                {
                    if (m == null || m.seasonal || !seen.Add(m.menuId)) continue;      // 한정 메뉴는 추리 보기에서 제외(정답이 될 수 없음)
                    list.Add(new Option { id = m.menuId, name = L.Get("menu." + m.menuId), icon = KitchenView.DishSprite(m.menuId), correct = CatTruth.IsFood(cat, m) });
                }
            }
            else if (axis == PrefAxis.Seat)
            {
                foreach (var s in cafe.GetSeats())
                {
                    var f = (s as YeogiCafe.Cafe.SeatBehaviour) != null ? ((YeogiCafe.Cafe.SeatBehaviour)s).source : null;
                    if (f == null || !seen.Add(f.furnitureId)) continue;
                    list.Add(new Option { id = f.furnitureId, name = L.Get($"furn.{f.furnitureId}.name"), icon = f.worldSprite, correct = CatTruth.IsSeat(cat, f) });
                }
            }
            else
            {
                foreach (var x in cafe.GetFacilities())
                {
                    var f = (x as YeogiCafe.Cafe.FacilityBehaviour) != null ? ((YeogiCafe.Cafe.FacilityBehaviour)x).source : null;
                    if (f == null || !seen.Add(f.furnitureId)) continue;
                    if (IsUniversal(f)) continue;                  // 과일 박스(누구나 좋아함)는 추리 보기에서 제외
                    list.Add(new Option { id = f.furnitureId, name = L.Get($"furn.{f.furnitureId}.name"), icon = f.worldSprite, correct = CatTruth.IsFacility(cat, f) });
                }
            }
            return list;
        }

        static bool IsUniversal(YeogiCafe.Data.FurnitureData f)
        {
            if (f.facilityTags == null) return false;
            foreach (var t in f.facilityTags) if (t == YeogiCafe.Data.FacilityTag.Box) return true;   // 온열방석은 '푹신한 곳'으로 추리 보기에 나온다
            return false;
        }

        public void DebugGuess(CatData cat, PrefAxis axis) => OpenGuess(cat, axis, null);
        public void DebugCloseGuess() => CloseGuess();
        public void DebugStory(StoryDTO st) { if (storyGo != null) Object.Destroy(storyGo); OpenStory(st); }
        public void DebugReport()
        {
            var s = owner.Save; if (s == null) return;
            if (storyGo != null) Object.Destroy(storyGo); storyGo = null;
            var r = s.lastReport; r.day = owner.Day != null ? owner.Day.CurrentDay : 1; r.seen = false;
            r.dish = 30; r.speed = 20; r.furniture = 18; r.clean = 12; r.total = 80; r.starsBefore = 0; r.starsAfter = 1;
            TryOpenReport();
        }
        public void DebugCloseStory() { if (storyGo != null) Object.Destroy(storyGo); storyGo = null; }

        void OpenGuess(CatData cat, PrefAxis axis, RectTransform from)
        {
            CloseGuess();
            var cs = owner.Save != null ? owner.Save.GetOrCreateCat(cat.catId) : null;
            var rec = cs != null ? cs.observation.Get(axis) : null;
            var options = BuildOptions(cat, axis);

            var scrim = UiFactory.Panel("GuessScrim", rootGo.transform, new Color(0.1f, 0.08f, 0.12f, 0.45f));
            guessGo = scrim.gameObject;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var box = UiFactory.Panel("GuessBox", scrim.transform, UiFactory.PanelBg);
            int rows = Mathf.Max(1, (options.Count + 3) / 4);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              new Vector2(0, 10), new Vector2(640, 192 + rows * 112));
            UiFactory.Frame(box, UiFactory.FrameStyle.Window);

            string catName = L.Get($"cat.{cat.catId}.name");
            var title = UiFactory.Label("Title", box.transform, L.Get("ui.guess.title", ("name", catName), ("axis", AxisLabel(axis))),
                                        22, TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(-40, 36));   // 위 테두리와 겹치지 않게(10/28)

            guessMsg = UiFactory.Label("Msg", box.transform, options.Count == 0 ? L.Get("ui.guess.none") : "", 17, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)guessMsg.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(-40, 28));

            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                bool wrongBefore = rec != null && rec.wrongGuesses.Contains(opt.id);
                int r = i / 4, c = i % 4;
                var b = UiFactory.Button("Opt_" + opt.id, box.transform, "", wrongBefore ? UiFactory.Wood : UiFactory.Cream, UiFactory.Ink, out var bl);
                var brt = (RectTransform)b.transform;
                UiFactory.SetRect(brt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                                  new Vector2(-222 + c * 148, -72 - r * 112), new Vector2(136, 104));
                bl.text = opt.name; bl.fontSize = 15; bl.alignment = TextAnchor.LowerCenter;
                { var blr = (RectTransform)bl.transform; blr.anchorMin = Vector2.zero; blr.anchorMax = Vector2.one; blr.offsetMin = new Vector2(4, 13); blr.offsetMax = new Vector2(-4, 0); }   // 이름이 버튼 아래 테두리에 겹치지 않게(10/28)
                var ic = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                ic.transform.SetParent(b.transform, false);
                var icImg = ic.GetComponent<Image>(); icImg.sprite = opt.icon; icImg.preserveAspect = true; icImg.raycastTarget = false;
                if (wrongBefore) icImg.color = new Color(1, 1, 1, 0.4f);
                UiFactory.SetRect((RectTransform)ic.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(64, 64));
                if (wrongBefore)
                {
                    var x = UiFactory.Label("X", b.transform, "×", 44, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
                    UiFactory.SetRect((RectTransform)x.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0, 8), Vector2.zero);
                    b.interactable = false;
                }
                var captured = opt;
                b.onClick.AddListener(() => { if (!busy) owner.StartCoroutine(PickWithPaw(cat, axis, captured, brt)); });
            }

            var close = UiFactory.Button("Close", box.transform, L.Get("ui.guess.cancel"), UiFactory.Wood, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(140, 40));
            close.onClick.AddListener(() => { if (!busy) CloseGuess(); });
            paw.SetAsLastSibling();
        }

        void CloseGuess()
        {
            if (guessGo != null) Object.Destroy(guessGo);
            guessGo = null;
        }

        // 답을 '골라 두기'만 한다(채점은 [확인]에서 한꺼번에)
        IEnumerator PickWithPaw(CatData cat, PrefAxis axis, Option opt, RectTransform btnRt)
        {
            busy = true;
            yield return PawTap(btnRt.position, () =>
            {
                if (owner.Save != null) owner.Save.GetOrCreateCat(cat.catId).observation.Get(axis).pendingGuess = opt.id;
            });
            CloseGuess();
            busy = false;
            Show();
        }
    }
}
