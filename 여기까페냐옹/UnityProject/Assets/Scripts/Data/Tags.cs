// 《여기까페냐옹》 S1 코어 스켈레톤 — 공통 태그/열거형
// 출처: Unity개발사양서 §0.1. 이 파일은 하드코딩 분기를 막기 위한 태그 기반 매칭의 기반.
namespace YeogiCafe.Data
{
    public enum PersonalityTag { Relaxed, Curious, Social, Lonely, Active, Introvert, Glutton, Playful, Fickle }
    public enum SeatTag        { Normal, Window, Corner, TwoSeat, Sofa, BarTable, Quiet, OutsideView }
    public enum FacilityTag    { Play, Height, Active, Soft, Relax, Quiet, Social, Natural }
    public enum FoodTag        { Fish, Sweet, Fruit, Meal, Warm }
    public enum AtmosphereAxis { Quiet, Lively, Warm, Natural, Luxury }

    public enum PrefAxis  { Food, Seat, Facility }              // 관찰 3축 (P2: 분위기 제외)
    public enum ClueType  { SeatUse, FacilityUse, FoodEat, SocialAction, IdleGaze }
    public enum ObsState  { Unknown, Suspected, Confirmable, Recorded } // P4 4단계

    public enum FurnitureType { Seat, Facility, Decoration }
    public enum DayPhase      { Prep, Morning, Lunch, Evening, Settlement }

    public enum CatState
    {
        Enter, FindSeat, Sit, DecideOrder, Order, WaitFood, Eat,
        FacilityAction, Idle, Pay, Leave,
        WaitOutside, LeaveDisappointed   // 만석 성격분기 (부록 J-B)
    }

    public static class ClueTypeExtensions
    {
        // ClueType → 관찰 축 매핑 (하드코딩 if 대신 여기 한 곳에서)
        public static PrefAxis ToAxis(this ClueType t)
        {
            switch (t)
            {
                case ClueType.FoodEat:      return PrefAxis.Food;
                case ClueType.SeatUse:      return PrefAxis.Seat;
                case ClueType.IdleGaze:     return PrefAxis.Seat;   // 창밖 응시=창가석 신호
                case ClueType.FacilityUse:  return PrefAxis.Facility;
                default:                    return PrefAxis.Facility;
            }
        }
    }
}
