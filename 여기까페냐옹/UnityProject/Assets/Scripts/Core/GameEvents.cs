using System;

namespace YeogiCafe.Core
{
    // 가벼운 게임 이벤트 버스 — 의뢰(퀘스트)·통계가 여러 시스템의 '일어난 일'을 듣는다.
    // 발신자는 서로를 몰라도 된다(쓰다듬기 연출, 주방, 선물, 추리 등).
    public static class GameEvents
    {
        public const string PetLove = "pet_love";          // 쓰다듬어 애교 반응
        public const string Pet = "pet";                   // 아무 쓰다듬기
        public const string Zoomies = "zoomies";           // 우다다 목격
        public const string Rub = "social_rub";            // 부비부비
        public const string Groom = "social_groom";        // 서로 핥아주기
        public const string Fight = "social_fight";        // 싸움
        public const string Ambush = "social_ambush";      // 캣초딩 덮치기
        public const string Steal = "seat_steal";          // 대장냥이 자리 뺏기
        public const string Serve = "serve";               // 서빙
        public const string MindRead = "mind_read";        // 모호 주문을 맞힘
        public const string WrongDish = "wrong_dish";      // 모호 주문을 틀림
        public const string Gift = "gift";                 // 선물 줍기
        public const string GuessCorrect = "guess_ok";     // 추리 정답
        public const string Graduate = "graduate";         // 졸업
        public const string MouseCaught = "mouse_caught";  // 쥐를 잡음
        public const string MouseEscaped = "mouse_escaped"; // 쥐 소동(놓침)
        public const string ClosingPlay = "closing_play";  // 마감 놀아주기 완료
        public const string Suckle = "suckle";             // 쭙쭙이 목격

        public static event Action<string, string> OnEvent;   // (eventId, catId)
        public static void Raise(string id, string catId = null) => OnEvent?.Invoke(id, catId);
    }
}
