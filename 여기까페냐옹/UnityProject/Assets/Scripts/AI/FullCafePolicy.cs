using YeogiCafe.Data;

namespace YeogiCafe.AI
{
    // 부록 J-B: 만석 시 성격별 분기. CatBrain과 테스트가 공유하는 순수 로직.
    public static class FullCafePolicy
    {
        // 느긋함/내향적 → 대기, 그 외(활발/변덕/호기심 등) → 귀가
        public static CatState DecideWhenFull(CatData cat)
        {
            if (cat.HasPersonality(PersonalityTag.Relaxed) ||
                cat.HasPersonality(PersonalityTag.Introvert))
                return CatState.WaitOutside;
            return CatState.LeaveDisappointed;
        }
    }
}
