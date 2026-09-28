using NUnit.Framework;
using YeogiCafe.Order;

namespace YeogiCafe.Tests
{
    // 사양서 §15.3 / T22 관련 — 주문 티켓 상태 전이(Update 타이머 제외한 순수 전이).
    public class OrderSystemTests
    {
        [Test]
        public void Ticket_Lifecycle_QueuedToServed()
        {
            var t = new OrderTicket { catId = "cat_cheese" };
            Assert.AreEqual(TicketState.Queued, t.state);

            // 조리 시작
            t.state = TicketState.Cooking;
            Assert.AreEqual(TicketState.Cooking, t.state);

            // 완성 → 서빙 준비
            t.state = TicketState.Ready;
            // 서빙
            t.state = TicketState.Served;
            Assert.AreEqual(TicketState.Served, t.state);
        }

        [Test]
        public void Patience_Depletion_LeadsToCancellable()
        {
            var t = new OrderTicket { patience = 1f };
            // 인내심이 0 이하가 되면 취소 대상(매니저 Update가 Cancelled로 전이)
            t.patience -= 1.5f;
            Assert.Less(t.patience, 0f, "인내심 소진 시 취소 조건 성립(→불만 퇴장)");
        }
    }
}
