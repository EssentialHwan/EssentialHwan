using System;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.Order
{
    // 사양서 §15.3 — 주문/조리/서빙. [S2] S1에서는 CatBrain이 Order→Eat 직행(조리 생략).
    // 이 스켈레톤은 S2에서 CatBrain의 Order→WaitFood→Eat 경로와 연결.
    public enum TicketState { Queued, Cooking, Ready, Served, Cancelled }

    public class OrderTicket
    {
        public string catId;
        public MenuData menu;
        public TicketState state = TicketState.Queued;
        public float cookProgress;    // 0..cookTime
        public float patience = 30f;  // 인내심(초)
    }

    // [S2] 주문 큐 + 원탭 조리 + 클릭 배정 서빙(부록 J-G).
    public class OrderSystem : MonoBehaviour
    {
        readonly List<OrderTicket> tickets = new();
        public IReadOnlyList<OrderTicket> Tickets => tickets;

        public event Action<OrderTicket> OnTicketReady;
        public event Action<OrderTicket> OnTicketServed;
        public event Action<OrderTicket> OnTicketCancelled;

        public OrderTicket Place(string catId, MenuData menu)
        {
            var t = new OrderTicket { catId = catId, menu = menu };
            tickets.Add(t);
            return t;
        }

        // 플레이어가 티켓 클릭 → 조리 시작(원탭)
        public void StartCooking(OrderTicket t)
        {
            if (t.state == TicketState.Queued) t.state = TicketState.Cooking;
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
                        if (t.patience <= 0) { t.state = TicketState.Cancelled; OnTicketCancelled?.Invoke(t); tickets.RemoveAt(i); }
                        break;
                    case TicketState.Cooking:
                        t.cookProgress += dt;
                        if (t.menu != null && t.cookProgress >= t.menu.cookTime) { t.state = TicketState.Ready; OnTicketReady?.Invoke(t); }
                        break;
                }
            }
        }

        // 플레이어가 완성 음식 클릭 → 서빙(자동 이동 후 완료)
        public void Serve(OrderTicket t)
        {
            if (t.state != TicketState.Ready) return;
            t.state = TicketState.Served;
            OnTicketServed?.Invoke(t);
            tickets.Remove(t);
        }
    }
}
