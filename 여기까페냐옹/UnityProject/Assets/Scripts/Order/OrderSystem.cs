using System;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.Order
{
    // 사양서 §15.3 — 주문/조리/서빙.
    // 흐름: 고양이 주문(Place) → 플레이어가 주방에서 해당 기구를 누름(StartAtTool) → cookTime 후 완성(Ready)
    //       → 완성품을 누르면 서빙(Serve) → CatBrain WaitFood→Eat. 기다리다 인내심이 다하면 취소(불만 퇴장).
    public enum TicketState { Queued, Cooking, Ready, Served, Cancelled }

    public class OrderTicket
    {
        public string catId;
        public MenuData menu;
        public TicketState state = TicketState.Queued;
        public float cookProgress;    // 0..cookTime
        public float patience = 45f;  // 인내심(초) — 주문 대기 중에만 줄어든다
        public float maxPatience = 45f;
        public string toolId;         // 조리 중인 기구
        // 모호 주문(단골): 메뉴 이름 대신 힌트로 말한다. 플레이어가 메뉴를 골라야(Resolve) 조리 가능.
        public MenuData requested;    // 고양이가 실제로 원한 메뉴
        public bool vague;            // 힌트 주문이었나
        public bool unresolved;       // 아직 플레이어가 메뉴를 정하지 않음
        public string hintKey;        // order.hint.{menuId}.{n}
        public bool IsCorrectDish => menu == requested;
        // 별점: 완성 순간 업그레이드 단계 확률로 굴림 → 다 되고 오래 두면 서빙 때 하나 깎임(신선도)
        public int stars;
        public bool stale;            // 신선도 감점 받음
        public float placedAt, readyAt, servedAt;   // Time.time
        public float ServeWait => servedAt - placedAt;

        public float CookRatio => menu == null || menu.cookTime <= 0 ? 1f : Mathf.Clamp01(cookProgress / menu.cookTime);
        public float PatienceRatio => maxPatience <= 0 ? 0f : Mathf.Clamp01(patience / maxPatience);
    }

    public class OrderSystem : MonoBehaviour
    {
        public float defaultPatience = 45f;
        public static System.Func<string, int> MenuLevel = _ => 0;     // 메뉴 업그레이드 단계(GameFlowController가 세이브로 연결)

        readonly List<OrderTicket> tickets = new();
        public IReadOnlyList<OrderTicket> Tickets => tickets;

        public event Action<OrderTicket> OnTicketPlaced;
        public event Action<OrderTicket> OnTicketReady;
        public event Action<OrderTicket> OnTicketServed;
        public event Action<OrderTicket> OnTicketCancelled;

        public OrderTicket Place(string catId, MenuData menu, bool vague = false, string hintKey = null)
        {
            var t = new OrderTicket { catId = catId, menu = menu, requested = menu, patience = defaultPatience, maxPatience = defaultPatience,
                                      vague = vague, unresolved = vague, hintKey = hintKey, placedAt = Time.time };
            if (vague) { t.patience += 15f; t.maxPatience += 15f; }    // 해석할 시간을 조금 더
            tickets.Add(t);
            OnTicketPlaced?.Invoke(t);
            return t;
        }

        // (구) 티켓 직접 조리 — 호환 유지
        public void StartCooking(OrderTicket t)
        {
            if (t.state == TicketState.Queued) { t.state = TicketState.Cooking; t.toolId = CookingStations.ToolFor(t.menu?.menuId); }
        }

        // 기구별 상태 조회
        public OrderTicket CookingOrReadyAt(string toolId)
        {
            foreach (var t in tickets)
                if (t.toolId == toolId && (t.state == TicketState.Cooking || t.state == TicketState.Ready)) return t;
            return null;
        }

        // 모호 주문 해석: 플레이어가 고른 메뉴로 확정(틀려도 그대로 조리·서빙됨)
        public void Resolve(OrderTicket t, MenuData chosen)
        {
            if (t == null || chosen == null || !t.unresolved) return;
            t.menu = chosen; t.unresolved = false;
        }

        public OrderTicket OldestQueuedFor(string toolId)
        {
            foreach (var t in tickets)
                if (t.state == TicketState.Queued && !t.unresolved && CookingStations.ToolFor(t.menu?.menuId) == toolId) return t;
            return null;
        }

        // 기구에서 고른 메뉴로 조리 시작: 그 메뉴를 기다리는 가장 오래된 주문 → 없으면 아직 해석 안 된 힌트 주문(고른 메뉴로 확정).
        // 기다리는 주문이 없으면 null(만들지 않음).
        public OrderTicket StartMenuAt(string toolId, MenuData menu)
        {
            if (menu == null || CookingOrReadyAt(toolId) != null) return null;
            OrderTicket pick = null;
            foreach (var t in tickets)
                if (t.state == TicketState.Queued && !t.unresolved && t.menu == menu) { pick = t; break; }
            if (pick == null)
                foreach (var t in tickets)
                    if (t.state == TicketState.Queued && t.unresolved) { Resolve(t, menu); pick = t; break; }
            if (pick == null) return null;
            pick.state = TicketState.Cooking; pick.toolId = toolId; pick.cookProgress = 0f;
            return pick;
        }

        public int WaitingFor(MenuData menu)
        {
            int n = 0;
            foreach (var t in tickets) if (t.state == TicketState.Queued && !t.unresolved && t.menu == menu) n++;
            return n;
        }
        public int WaitingVague()
        {
            int n = 0;
            foreach (var t in tickets) if (t.state == TicketState.Queued && t.unresolved) n++;
            return n;
        }

        // 기구를 누름: 완성품이 있으면 서빙, 비어 있으면 가장 오래 기다린 해당 메뉴 주문을 조리 시작.
        public enum ToolResult { Nothing, Started, Served, Busy }
        public ToolResult PressTool(string toolId, out OrderTicket ticket)
        {
            ticket = CookingOrReadyAt(toolId);
            if (ticket != null)
            {
                if (ticket.state == TicketState.Ready) { Serve(ticket); return ToolResult.Served; }
                return ToolResult.Busy;
            }
            ticket = OldestQueuedFor(toolId);
            if (ticket == null) return ToolResult.Nothing;
            ticket.state = TicketState.Cooking;
            ticket.toolId = toolId;
            ticket.cookProgress = 0f;
            return ToolResult.Started;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = tickets.Count - 1; i >= 0; i--)
            {
                var t = tickets[i];
                switch (t.state)
                {
                    case TicketState.Queued:
                        t.patience -= dt;
                        if (t.patience <= 0) { t.state = TicketState.Cancelled; tickets.RemoveAt(i); OnTicketCancelled?.Invoke(t); }
                        break;
                    case TicketState.Cooking:
                        t.cookProgress += dt;
                        if (t.menu == null || t.cookProgress >= t.menu.cookTime)
                        {
                            t.state = TicketState.Ready; t.readyAt = Time.time;
                            t.stars = YeogiCafe.Food.RecipeBook.RollStars(t.menu != null ? MenuLevel(t.menu.menuId) : 0);
                            OnTicketReady?.Invoke(t);
                        }
                        break;
                }
            }
        }

        public void Serve(OrderTicket t)
        {
            if (t.state != TicketState.Ready) return;
            t.state = TicketState.Served;
            t.servedAt = Time.time;
            if (t.stars > 1 && t.servedAt - t.readyAt > YeogiCafe.Food.RecipeBook.FreshSeconds) { t.stars--; t.stale = true; }
            tickets.Remove(t);
            OnTicketServed?.Invoke(t);
        }

        // 고양이가 떠나면(영업 종료 등) 그 고양이 주문은 조용히 정리
        public void DropForCat(string catId)
        {
            for (int i = tickets.Count - 1; i >= 0; i--)
                if (tickets[i].catId == catId) { tickets[i].state = TicketState.Cancelled; tickets.RemoveAt(i); }
        }

        public void ClearAll() => tickets.Clear();
    }
}
