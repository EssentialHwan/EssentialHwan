using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.Food
{
    // 배달냥: 배달 하이바·조끼를 입은 아비시니안(사바나) 고양이.
    // 자리에 앉지 않고 카운터 앞에 서서 메뉴 여러 개를 한꺼번에 주문 → 전부 나오면(또는 기다리다 지치면) 바로 떠난다.
    // 도감·단서·리뷰·관계와 무관한 '추가 매출' 손님. 각 메뉴를 판매가보다 싸게(PayRate) 사 간다.
    public static class DeliveryCat
    {
        public const string Id = "cat_delivery";
        public const float PayRate = 0.7f;                                  // 판매가의 70%
        public static readonly Vector3 WaitPoint = new Vector3(11.0f, 2.25f, 0f);   // 카운터 왼쪽 앞

        public static bool IsDelivery(string catId) => catId == Id;

        // 주문 개수: 기본 2개, 카페 Lv2부터 +1, 가끔 +1 (최대 4)
        public static int RollCount(int cafeLevel)
            => Mathf.Clamp(2 + (cafeLevel >= 2 ? 1 : 0) + (Random.value < 0.35f ? 1 : 0), 2, 4);

        // 여러 개를 한 번에 만들어야 하니 인내심은 개수만큼 넉넉히
        public static float Patience(int count) => 70f + 20f * count;

        // 하루 방문 계획: 2일차부터 하루 0~2번(점심·저녁 사이 아무 때나)
        public static int RollVisits(int day, int cafeLevel)
        {
            if (day < 2) return 0;
            int n = Random.value < 0.75f ? 1 : 0;
            if (cafeLevel >= 2 && Random.value < 0.5f) n++;
            return n;
        }

        static CatData data;
        public static CatData Data
        {
            get
            {
                if (data != null) return data;
                data = ScriptableObject.CreateInstance<CatData>();
                data.catId = Id; data.displayNameKey = "cat.cat_delivery.name"; data.name = Id;
                data.personalityTags = new[] { PersonalityTag.Active, PersonalityTag.Social };
                data.foodFavoriteMenuId = ""; data.seatPreference = SeatTag.Normal;
                data.facilityPreference = FacilityTag.Active; data.atmospherePreference = AtmosphereAxis.Lively;
                data.unlockCondition = new UnlockCondition { type = UnlockType.Always };
                return data;
            }
        }
    }
}
