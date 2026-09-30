using System.Collections.Generic;
using UnityEngine;
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
        Text revenueText, cardsInfo;
        RectTransform cardsContent;

        public SettlementPanel(GameUiController owner, Transform parent)
        {
            this.owner = owner;

            // 배경 스크림(정산 중 게임 화면 가림)
            var scrim = UiFactory.Panel("SettleScrim", parent, UiFactory.Scrim);
            rootGo = scrim.gameObject;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = UiFactory.Panel("SettlePanel", scrim.transform, UiFactory.PanelBg);
            UiFactory.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              Vector2.zero, new Vector2(760, 560));

            UiFactory.Label("Title", panel.transform, "📊 " + L.Get("ui.settle.title"), 30,
                            TextAnchor.UpperCenter, UiFactory.Ink);

            revenueText = UiFactory.Label("Revenue", panel.transform, "", 22, TextAnchor.UpperLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)revenueText.transform, new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(-40, 80));

            var cardsGo = new GameObject("Cards", typeof(RectTransform), typeof(VerticalLayoutGroup));
            cardsGo.transform.SetParent(panel.transform, false);
            cardsContent = (RectTransform)cardsGo.transform;
            UiFactory.SetRect(cardsContent, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                              new Vector2(0, -30), new Vector2(-40, -220));
            var vlg = cardsGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8; vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            cardsInfo = UiFactory.Label("CardsInfo", panel.transform, "", 20, TextAnchor.LowerLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)cardsInfo.transform, new Vector2(0, 0), new Vector2(1, 0),
                              new Vector2(0.5f, 0), new Vector2(0, 78), new Vector2(-40, 36));

            var nextBtn = UiFactory.Button("Next", panel.transform, L.Get("ui.settle.next"),
                                           UiFactory.Green, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)nextBtn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                              new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(220, 56));
            nextBtn.onClick.AddListener(() => owner.StartNextDay());

            rootGo.SetActive(false);
        }

        public void Show()
        {
            rootGo.SetActive(true);
            RenderEconomy();
            RenderCards();
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
            ((RectTransform)row.transform).sizeDelta = new Vector2(0, 64);

            string catName = L.Get($"cat.{data.catId}.name");
            string axisSummary =
                $"{L.Get("ui.axis.food")}{ObservationCardBuilder.StateIcon(card.food.state)} " +
                $"{L.Get("ui.axis.seat")}{ObservationCardBuilder.StateIcon(card.seat.state)} " +
                $"{L.Get("ui.axis.facility")}{ObservationCardBuilder.StateIcon(card.facility.state)}";

            var label = UiFactory.Label("Name", row.transform, $"{catName}    {axisSummary}", 22,
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
            var btn = UiFactory.Button($"Rec_{axis}", parent,
                                       L.Get("ui.settle.record") + " " + tag,
                                       canRecord ? UiFactory.Pink : UiFactory.Wood, UiFactory.Cream, out var lbl);
            lbl.fontSize = 16;
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(xOffset, 0), new Vector2(92, 48));
            btn.interactable = canRecord;
            var capturedAxis = axis;
            btn.onClick.AddListener(() =>
            {
                var obs = owner.Obs;
                if (obs != null && obs.TryRecord(catId, capturedAxis, out int bonus))
                {
                    if (bonus > 0 && owner.Economy != null) owner.Economy.AddGold(bonus);
                    btn.interactable = false;
                    owner.RefreshHud();
                    Show();   // 카드 갱신(상태 ✓ 반영)
                }
            });
        }
    }
}
