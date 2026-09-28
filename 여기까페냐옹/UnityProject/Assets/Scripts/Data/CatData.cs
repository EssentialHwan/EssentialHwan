using UnityEngine;

namespace YeogiCafe.Data
{
    // 사양서 §1.1 — Truth(숨김). 취향 4축 필드는 세이브에 절대 저장하지 않는다.
    [CreateAssetMenu(menuName = "YeogiCafe/CatData")]
    public class CatData : ScriptableObject
    {
        [Header("식별")]
        public string catId;                 // "cat_cheese"
        public string displayNameKey;        // 로컬라이즈 키
        public Sprite portrait;
        public string species;

        [Header("성격 (부록 J-C: [0]=첫방문 즉시공개, [1]=관찰공개)")]
        public PersonalityTag[] personalityTags = new PersonalityTag[2];

        [Header("── Truth: 숨겨진 취향 (세이브 금지) ──")]
        public string foodFavoriteMenuId;
        public FoodTag[] foodPreferredTags;      // 정식 판정(P10). MVP 미사용
        public SeatTag seatPreference;
        public FacilityTag facilityPreference;
        public AtmosphereAxis atmospherePreference; // 꾸미기용 (P2)

        [Header("행동/진행")]
        public float spawnWeight = 1f;
        public UnlockCondition unlockCondition = new UnlockCondition();
        [Header("관계(정식) — 잠재 짝 제한")]
        public string[] relationshipHintCatIds;   // 예: 회색냥↔젖소냥
        [TextArea] public string devNote;

        public bool HasPersonality(PersonalityTag t)
        {
            if (personalityTags == null) return false;
            foreach (var p in personalityTags) if (p == t) return true;
            return false;
        }
    }
}
