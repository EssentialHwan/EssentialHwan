using YeogiCafe.Save;

namespace YeogiCafe.Core
{
    // 체험판(스팀 넥스트 페스트용) 설정. 데모 빌드 메뉴가 YEOGI_DEMO 기호를 붙여 빌드한다.
    // 체험판: 봄 1~7일차까지 → 7일차 정산 뒤 [다음 날]에서 체험판 끝 화면(위시리스트 안내).
    // 저장 폴더는 정식판과 같다(제품명 동일) → 같은 PC면 체험판 카페가 정식판으로 그대로 이어진다.
    public static class Demo
    {
#if YEOGI_DEMO
        public static readonly bool IsDemo = true;
#else
        public static readonly bool IsDemo = false;
#endif
        public const int LastDay = 7;

        // 스팀 앱 ID가 나오면 넣는다 → 끝 화면의 [위시리스트에 추가] 버튼이 상점 페이지를 연다(0이면 버튼 숨김)
        public const int SteamAppId = 0;
        public static string StoreUrl => SteamAppId > 0 ? $"https://store.steampowered.com/app/{SteamAppId}/" : "";

        public static bool Finished(SaveGame s) => IsDemo && s != null && s.demoFinished;
        public static bool IsLastDay(SaveGame s) => IsDemo && s != null && s.currentDay >= LastDay;
    }
}
