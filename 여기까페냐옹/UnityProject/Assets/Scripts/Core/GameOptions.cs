using UnityEngine;

namespace YeogiCafe.Core
{
    // 놀이 방식 설정(11/02). 설정 창에서 켜고 끄며 PlayerPrefs에 남는다. 새로 들어오는 손님부터 적용.
    //  관찰 모드: 주문 없이 — 손님이 와서 좋아하는 자리에 앉고 시설·장난감에서 놀다 간다(결제·음식 단서 없음).
    //            주방이 필요 없는 모드라 배달냥·냥슐랭 심사원도 오지 않는다. 첫 주(튜토리얼)엔 켤 수 없다.
    //  배달냥: 끄면 배달 주문 손님이 오지 않는다.
    public static class GameOptions
    {
        const string KObserve = "opt.observe", KDelivery = "opt.delivery";
        public static bool ObserveMode
        {
            get => PlayerPrefs.GetInt(KObserve, 0) == 1;
            set { PlayerPrefs.SetInt(KObserve, value ? 1 : 0); PlayerPrefs.Save(); }
        }
        public static bool DeliveryOn
        {
            get => PlayerPrefs.GetInt(KDelivery, 1) == 1;
            set { PlayerPrefs.SetInt(KDelivery, value ? 1 : 0); PlayerPrefs.Save(); }
        }
        // 오늘 실제로 관찰 모드인가(첫 주엔 설정과 상관없이 꺼짐 — 튜토리얼이 주문을 전제로 한다)
        public static bool ObserveActive(int day) => ObserveMode && !FirstWeek.Active(day);
    }
}
