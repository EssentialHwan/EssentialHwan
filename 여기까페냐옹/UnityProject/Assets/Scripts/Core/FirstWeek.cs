namespace YeogiCafe.Core
{
    // 첫 일주일(= 체험판 7일) 연출 일정: 운에 맡기지 않고, 일주일 안에 게임의 '맛'이 하나씩 꼭 나오게 한다.
    //  1~2일차  손님 익히기(첫 단골은 방문 3번이면 보통 2~3일차에 자연히 나온다)
    //  4일차    쥐 출몰
    //  5일차    배달냥
    //  6일차    냥슐랭 심사원
    //  7일차    마감 놀아주기로 마무리(체험판은 끝 화면에서 여름 미리보기)
    // 8일차부터는 원래대로 확률 굴림. 정식판도 같은 일정(첫 주 경험이 같아야 체험판→정식판이 자연스럽다).
    public static class FirstWeek
    {
        public const int Days = 7;
        public const int MouseDay = 4, DeliveryDay = 5, InspectorDay = 6;

        public static bool Active(int day) => day >= 1 && day <= Days;

        // null이면 원래 굴림을 쓴다
        public static int? DeliveryVisits(int day) => Active(day) ? (day == DeliveryDay ? 1 : 0) : (int?)null;
        public static bool? InspectorVisit(int day) => Active(day) ? day == InspectorDay : (bool?)null;
        public static bool? MouseRaid(int day) => Active(day) ? day == MouseDay : (bool?)null;
    }
}
