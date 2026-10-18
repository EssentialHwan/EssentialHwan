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
            var card = ObservationCardBuilder.Build(data, cs, null);
            var row = UiFactory.Panel("Card_" + data.catId, cardsContent, UiFactory.Cream);
            ((RectTransform)row.transform).sizeDelta = new Vector2(0, 44);

            string catName = L.Get($"cat.{data.catId}.name");
            string axisSummary =
                $"{L.Get("ui.axis.food")}{ObservationCardBuilder.StateIcon(card.food.state)} " +
                $"{L.Get("ui.axis.seat")}{ObservationCardBuilder.StateIcon(card.seat.state)} " +
                $"{L.Get("ui.axis.facility")}{ObservationCardBuilder.StateIcon(card.facility.state)}";

            var label = UiFactory.Label("Name", row.transform, $"{catName}    {axisSummary}", 20,
                                        TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)label.transform, new Vector2(0, 0), new Vector2(1, 1),
                              new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(-320, 0));

            // 축별 [기록] 버튼 — Confirmable(★)에서만 활성
            BuildRecordButton(row.transform, data.catId, PrefAxis.Food, card.food.canRecord, -300);
            BuildRecordButton(row.transform, data.catId, PrefAxis.Seat, card.seat.canRecord, -200);
            BuildRecordButton(row.transform, data.catId, PrefAxis.Facility, card.facility.canRecord, -100);
        }

        void BuildRecordButton(Transform parent, string catId, PrefAxis axis, bool canRecord, float xOffset)
        {
            string tag = axis == PrefAxis.Food ? L.Get("ui.axis.food")
                       : axis == PrefAxis.Seat ? L.Get("ui.axis.seat")
                       : L.Get("ui.axis.facility");

            // 이미 기록한 축은 버튼 대신 빨간 체크 + 축 이름
            var save = owner.Save;
            var cs = save != null ? save.GetOrCreateCat(catId) : null;
            if (cs != null && cs.observation.Get(axis).state == ObsState.Recorded)
            {
                var go = new GameObject($"Checked_{axis}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                var img = go.GetComponent<Image>(); img.sprite = checkSprite; img.raycastTarget = false;
                UiFactory.SetRect((RectTransform)go.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                                  new Vector2(1, 0.5f), new Vector2(xOffset - 56, 0), new Vector2(34, 34));
                var t = UiFactory.Label($"CheckedLbl_{axis}", parent, tag, 15, TextAnchor.MiddleLeft, UiFactory.PinkDeep);
                UiFactory.SetRect((RectTransform)t.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                                  new Vector2(1, 0.5f), new Vector2(xOffset, 0), new Vector2(56, 34));
                return;
            }

            var btn = UiFactory.Button($"Rec_{axis}", parent,
                                       L.Get("ui.settle.record") + " " + tag,
                                       canRecord ? UiFactory.Pink : UiFactory.Wood, UiFactory.Cream, out var lbl);
            lbl.fontSize = 16;
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(xOffset, 0), new Vector2(92, 36));
            btn.interactable = canRecord;
            var capturedAxis = axis;
            btn.onClick.AddListener(() =>
            {
                if (busy) return;
                owner.StartCoroutine(RecordWithPaw(catId, capturedAxis, (RectTransform)btn.transform));
            });
        }

        IEnumerator RecordWithPaw(string catId, PrefAxis axis, RectTransform btnRt)
        {
            busy = true;
            bool ok = false; int bonus = 0;
            yield return PawTap(btnRt.position, () =>
            {
                var obs = owner.Obs;
                ok = obs != null && obs.TryRecord(catId, axis, out bonus);
                if (ok) owner.StartCoroutine(PopCheck(btnRt));
            });
            if (ok)
            {
                if (bonus > 0 && owner.Economy != null) owner.Economy.AddGold(bonus);
                owner.RefreshHud();
                yield return new WaitForSecondsRealtime(0.2f);
                busy = false;
                Show();   // 카드 갱신(버튼 → 체크)
            }
            busy = false;
        }
    }
}
