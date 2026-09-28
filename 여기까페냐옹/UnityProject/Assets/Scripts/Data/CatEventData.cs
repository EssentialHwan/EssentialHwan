using UnityEngine;

namespace YeogiCafe.Data
{
    // 사양서 §30 / 문서 01 — 단골 에피소드·이벤트. 트리거 충족 시 재생, 1회성.
    public enum EventTrigger { RegularStage, VisitCount, RelationStage }

    [CreateAssetMenu(menuName = "YeogiCafe/CatEventData")]
    public class CatEventData : ScriptableObject
    {
        public string eventId;              // "ep_cheese_1" / "rel_gray_cow"
        public string catId;                // 대상 고양이(관계 이벤트는 짝의 한쪽)
        public string catIdB;               // 관계 이벤트 상대(RelationStage 전용)
        public EventTrigger trigger = EventTrigger.RegularStage;
        public int triggerValue = 3;        // 단골 3단계 / 관계 2단계(친구) 도달 시
        public string[] scriptLineKeys;     // 로컬라이즈 키 배열 (문서 01/07)

        [Header("보상")]
        public int rewardGold;
        public bool rewardCatalogStamp = true;
        public string rewardFurnitureId;    // 있으면 가구 레시피 해금
    }
}
