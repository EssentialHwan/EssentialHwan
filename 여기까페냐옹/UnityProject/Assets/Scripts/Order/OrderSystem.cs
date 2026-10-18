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

        public float CookRatio => menu == null || menu.cookTime <= 0 ? 1f : Mathf.Clamp01(cookProgress / menu.cookTime);
        public float PatienceRatio => maxPatience <= 0 ? 0f : Mathf.Clamp01(patience / maxPatience);
    }

    public class OrderSystem : MonoBehaviour
    {
        public float defaultPatience = 45f;

        readonly List<OrderTicket> tickets = new();
        public IReadOnlyList<OrderTicket> Tickets => tickets;

        public event Action<OrderTicket> OnTicketPlaced;
        public event Action<OrderTicket> OnTicketReady;
        public event Action<OrderTicket> OnTicketServed;
        public event Action<OrderTicket> OnTicketCancelled;

        public OrderTicket Place(string catId, MenuData menu)
        {
            var t = new OrderTicket { catId = catId, menu = menu, patience = defaultPatience, maxPatience = defaultPatience };
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

        public OrderTicket OldestQueuedFor(string toolId)
        {
            foreach (var t in tickets)
                if (t.state == TicketState.Queued && CookingStations.ToolFor(t.menu?.menuId) == toolId) return t;
            return null;
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
                        if (t.menu == null || t.cookProgress >= t.menu.cookTime) { t.state = TicketState.Ready; OnTicketReady?.Invoke(t); }
                        break;
                }
            }
        }

        public void Serve(OrderTicket t)
        {
            if (t.state != TicketState.Ready) return;
            t.state = TicketState.Served;
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
