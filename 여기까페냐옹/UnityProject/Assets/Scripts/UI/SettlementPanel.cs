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
        Button expandBtn;
        RectTransform cardsContent;

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

            var title = UiFactory.Label("Title", panel.transform, L.Get("ui.settle.title"), 26,
                                        TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(0, 40));

            revenueText = UiFactory.Label("Revenue", panel.transform, "", 22, TextAnchor.UpperLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)revenueText.transform, new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(-40, 80));

            var cardsGo = new GameObject("Cards", typeof(RectTransform), typeof(VerticalLayoutGroup));
            cardsGo.transform.SetParent(panel.transform, false);
            cardsContent = (RectTransform)cardsGo.transform;
            UiFactory.SetRect(cardsContent, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                              new Vector2(0, -14), new Vector2(-40, -230));
            var vlg = cardsGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6; vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            cardsInfo = UiFactory.Label("CardsInfo", panel.transform, "", 20, TextAnchor.LowerLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)cardsInfo.transform, new Vector2(0, 0), new Vector2(1, 0),
                              new Vector2(0.5f, 0), new Vector2(0, 104), new Vector2(-40, 30));

            var nextBtn = UiFactory.Button("Next", panel.transform, L.Get("ui.settle.next"),
                                           UiFactory.Green, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)nextBtn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                              new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(220, 56));
            nextBtn.onClick.AddListener(() => { if (!busy) owner.StartCoroutine(FinishDay()); });
            UiFactory.SetRect((RectTransform)nextBtn.transform, new Vector2(1, 0), new Vector2(1, 0),
                              new Vector2(1, 0), new Vector2(-24, 20), new Vector2(200, 52));

            // 카페 넓히기(성장) — 관찰 성과 또는 가구 수로 열리는 같은 방 확장(P3 OR 경로)
            expandBtn = UiFactory.Button("Expand", panel.transform, L.Get("ui.settle.expand"),
                                         UiFactory.Wood, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)expandBtn.transform, new Vector2(0, 0), new Vector2(0, 0),
                              new Vector2(0, 0), new Vector2(24, 20), new Vector2(180, 52));
            expandBtn.onClick.AddListener(() =>
            {
                owner.TryExpandCafe(out var msg);
                expandInfo.text = msg;
                RenderLevel();
            });
            levelText = UiFactory.Label("Level", panel.transform, "", 20, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)levelText.transform, new Vector2(0, 0), new Vector2(0, 0),
                              new Vector2(0, 0), new Vector2(214, 20), new Vector2(140, 52));
            expandInfo = UiFactory.Label("ExpandInfo", panel.transform, "", 16, TextAnchor.MiddleLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)expandInfo.transform, new Vector2(0, 0), new Vector2(1, 0),
                              new Vector2(0, 0), new Vector2(24, 76), new Vector2(-48, 24));

            // 오늘의 도장 칸(오른쪽 아래, 다음 날 버튼 위)
            var box = UiFactory.Panel("StampBox", panel.transform, new Color(1f, 0.98f, 0.94f, 1f));
            stampBox = (RectTransform)box.transform;
            UiFactory.SetRect(stampBox, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 84), new Vector2(120, 120));
            var boxTitle = UiFactory.Label("StampTitle", box.transform, L.Get("ui.settle.stamp"), 15, TextAnchor.UpperCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)boxTitle.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(0, 20));
            var si = new GameObject("Stamp", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            si.transform.SetParent(box.transform, false);
            stampImg = si.GetComponent<Image>(); stampImg.raycastTarget = false; stampImg.preserveAspect = true;
            UiFactory.SetRect((RectTransform)si.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(80, 80));
            stampLabel = UiFactory.Label("StampDay", box.transform, "", 14, TextAnchor.LowerCenter, UiFactory.PinkDeep);
            UiFactory.SetRect((RectTransform)stampLabel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(0, 18));

            pawBack = RuntimeSprites.Get("paw_back", () => PawDrawing.Back());
            pawPress = RuntimeSprites.Get("paw_back_press", () => PawDrawing.Back(true));
            pawPads = RuntimeSprites.Get("paw_pads", PawDrawing.Pads);
            checkSprite = RuntimeSprites.Get("ui_check", PawDrawing.Check);
            stampSprite = RuntimeSprites.Get("ui_pawstamp", PawDrawing.Stamp);

            // 앞발(화면 아래에서 올라옴). 피벗 = 발가락 끝 → 목표 지점에 발끝이 닿는다.
            var pg = new GameObject("PlayerPaw", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pg.transform.SetParent(scrim.transform, false);
            paw = (RectTransform)pg.transform; pawImg = pg.GetComponent<Image>(); pawImg.raycastTarget = false;
            pawImg.sprite = pawBack;
            paw.anchorMin = paw.anchorMax = new Vector2(0.5f, 0f);
            paw.pivot = new Vector2(0.5f, 0.94f);
            paw.sizeDelta = new Vector2(PawDrawing.W, PawDrawing.H) * 2.6f;
            pg.SetActive(false);

            rootGo.SetActive(false);
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
            RenderCards();
            RenderLevel();
        }

        void RenderLevel()
        {
            levelText.text = L.Get("ui.settle.cafelevel", ("n", owner.CafeLevel.ToString()));
        }
        public void Hide() => rootGo.SetActive(false);

        void RenderEconomy()
        {
            var eco = owner.Economy;
            if (eco == null) { revenueText.text = ""; return; }
            revenueText.text =
                $"{L.Get("ui.settle.revenue")}: {eco.TodayRevenue}G   " +
                $"{L.Get("ui.settle.cost")}: {eco.TodayCost}G   " +
                $"{L.Get("ui.settle.net")}: {eco.TodayNet}G";
        }

        void ClearCards()
        {
            for (int i = cardsContent.childCount - 1; i >= 0; i--)
                Object.Destroy(cardsContent.GetChild(i).gameObject);
        }

        void RenderCards()
        {
            ClearCards();
            var save = owner.Save;
            if (save == null) { cardsInfo.text = ""; return; }

            int shown = 0;
            foreach (var cs in save.cats)
            {
                if (cs.lastVisitDay < 0) continue;
                var data = owner.LookupCat(cs.catId);
                if (data == null) continue;
                BuildCatRow(data, cs);
                shown++;
            }
            cardsInfo.text = shown == 0 ? L.Get("ui.settle.needmore") : "";
        }

        void BuildCatRow(CatData data, CatSaveData cs)
        {
            var row = UiFactory.Panel("Card_" + data.catId, cardsContent, UiFactory.Cream);
            ((RectTransform)row.transform).sizeDelta = new Vector2(0, 44);

            string catName = L.Get($"cat.{data.catId}.name");
            var label = UiFactory.Label("Name", row.transform, catName, 20, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)label.transform, new Vector2(0, 0), new Vector2(0, 1),
                              new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(110, 0));

            // 축마다 칸 하나: 기록했으면 체크+알아낸 이름, 아니면 상태 + [추리] 버튼
            BuildAxisCell(row.transform, data, cs, PrefAxis.Food, 0);
            BuildAxisCell(row.transform, data, cs, PrefAxis.Seat, 1);
            BuildAxisCell(row.transform, data, cs, PrefAxis.Facility, 2);
        }

        static string AxisLabel(PrefAxis axis) =>
            axis == PrefAxis.Food ? L.Get("ui.axis.food") : axis == PrefAxis.Seat ? L.Get("ui.axis.seat") : L.Get("ui.axis.facility");

        void BuildAxisCell(Transform parent, CatData data, CatSaveData cs, PrefAxis axis, int index)
        {
            const float cellW = 220f, startX = 130f;
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
                var t = UiFactory.Label("Val", cell.transform, $"{AxisLabel(axis)} {ObservationCardBuilder.ValueName(axis, rec.recordedValue)}",
                                        15, TextAnchor.MiddleLeft, UiFactory.PinkDeep);
                UiFactory.SetRect((RectTransform)t.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                                  new Vector2(34, 0), new Vector2(-34, 0));
                return;
            }

            var stateLbl = UiFactory.Label("State", cell.transform, $"{AxisLabel(axis)} {ObservationCardBuilder.AxisText(rec)}",
                                           15, TextAnchor.MiddleLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)stateLbl.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                              new Vector2(0, 0), new Vector2(-84, 0));

            var obs = owner.Obs;
            int day = owner.Day != null ? owner.Day.CurrentDay : 0;
            var can = obs != null ? obs.CanGuess(data.catId, axis, day) : ObservationManager.GuessResult.NotReady;
            bool ok = can == ObservationManager.GuessResult.Correct;
            var btn = UiFactory.Button($"Guess_{axis}", cell.transform, L.Get("ui.guess.btn"),
                                       ok ? UiFactory.Pink : UiFactory.Wood, UiFactory.Cream, out var lbl);
            lbl.fontSize = 15;
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(76, 34));
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
                    if (m == null || !seen.Add(m.menuId)) continue;
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
                    list.Add(new Option { id = f.furnitureId, name = L.Get($"furn.{f.furnitureId}.name"), icon = f.worldSprite, correct = CatTruth.IsFacility(cat, f) });
                }
            }
            return list;
        }

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
                              new Vector2(0, 10), new Vector2(640, 180 + rows * 112));

            string catName = L.Get($"cat.{cat.catId}.name");
            var title = UiFactory.Label("Title", box.transform, L.Get("ui.guess.title", ("name", catName), ("axis", AxisLabel(axis))),
                                        22, TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(0, 36));

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
                                  new Vector2(-222 + c * 148, -60 - r * 112), new Vector2(136, 104));
                bl.text = opt.name; bl.fontSize = 15; bl.alignment = TextAnchor.LowerCenter;
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
                b.onClick.AddListener(() => { if (!busy) owner.StartCoroutine(GuessWithPaw(cat, axis, captured, brt)); });
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

        IEnumerator GuessWithPaw(CatData cat, PrefAxis axis, Option opt, RectTransform btnRt)
        {
            busy = true;
            var obs = owner.Obs;
            int day = owner.Day != null ? owner.Day.CurrentDay : 0;
            var result = ObservationManager.GuessResult.NotReady; int bonus = 0;
            bool early = false;
            if (owner.Save != null) early = owner.Save.GetOrCreateCat(cat.catId).observation.Get(axis).state == ObsState.Suspected;
            yield return PawTap(btnRt.position, () =>
            {
                if (obs != null) result = obs.TryGuess(cat.catId, axis, opt.id, opt.correct, day, out bonus);
                if (result == ObservationManager.GuessResult.Correct) owner.StartCoroutine(PopCheck(btnRt));
            });
            string catName = L.Get($"cat.{cat.catId}.name");
            if (result == ObservationManager.GuessResult.Correct)
            {
                if (bonus > 0 && owner.Economy != null) owner.Economy.AddGold(bonus);
                owner.RefreshHud();
                guessMsg.text = L.Get("ui.guess.correct", ("name", catName), ("item", opt.name))
                              + (early ? "\n" + L.Get("ui.guess.early", ("g", bonus.ToString())) : "");
            }
            else if (result == ObservationManager.GuessResult.Wrong) guessMsg.text = L.Get("ui.guess.wrong");
            else guessMsg.text = L.Get("ui.guess.tried");
            yield return new WaitForSecondsRealtime(1.4f);
            CloseGuess();
            busy = false;
            Show();
        }
    }
}
