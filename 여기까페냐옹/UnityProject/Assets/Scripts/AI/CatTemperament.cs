using YeogiCafe.Data;

namespace YeogiCafe.AI
{
    // 고양이 기질(Truth) → 쓰다듬기 반응·고양이끼리 상호작용 결정. 순수 함수(테스트 용이).
    // 기질은 첫 번째 성격 태그(첫 방문에 공개되는 것)에서 나오지만, 실제 반응은 단골 단계·관계에 따라 달라져
    // 플레이어가 '이 애는 처음엔 경계하다가 친해지면 애교'처럼 관찰로 알아가게 한다.
    public enum TouchStyle { Affectionate, Playful, Curious, Fickle, Shy, Grumpy }

    // 관찰 결과로 저장되는 반응 종류(세이브 인덱스 고정 — 순서 바꾸지 말 것)
    public enum TouchResult { Love = 0, Play = 1, Sniff = 2, Flinch = 3, Hiss = 4 }

    public enum SocialKind { None, Rub, Groom, Sniff, Play, Hiss, Fight, Ambush }

    public static class CatTemperament
    {
        public const int ResultCount = 5;

        // 대장냥이: 밥 먹고 쉬는 다른 고양이 자리를 가끔 뺏는다(첫 성격이 내향 = 까망냥, 동네 대장)
        public static bool IsBoss(CatData d) =>
            d != null && d.personalityTags != null && d.personalityTags.Length > 0 && d.personalityTags[0] == PersonalityTag.Introvert;
        public const float BossStealChance = 0.5f;

        // 캣초딩: 장난 기질(첫 성격 활발·장난) — 우다다가 잦고, 쉬는 고양이를 몰래 덮친다
        public static bool IsKid(CatData d) => Style(d) == TouchStyle.Playful;
        public static float ZoomiesChance(CatData d) => IsKid(d) ? 0.35f : 0.08f;

        public static TouchStyle Style(CatData d)
        {
            if (d == null || d.personalityTags == null || d.personalityTags.Length == 0) return TouchStyle.Curious;
            switch (d.personalityTags[0])
            {
                case PersonalityTag.Relaxed:
                case PersonalityTag.Social:
                case PersonalityTag.Glutton:   return TouchStyle.Affectionate;
                case PersonalityTag.Active:
                case PersonalityTag.Playful:   return TouchStyle.Playful;
                case PersonalityTag.Fickle:    return TouchStyle.Fickle;
                case PersonalityTag.Lonely:    return TouchStyle.Shy;
                case PersonalityTag.Introvert: return TouchStyle.Grumpy;
                default:                       return TouchStyle.Curious;
            }
        }

        // petsRecent: 최근 몇 초 사이 연속으로 만진 횟수(이번 포함, 1부터). rnd: 0~1.
        public static TouchResult React(TouchStyle s, int petsRecent, int regularStage, float rnd)
        {
            switch (s)
            {
                case TouchStyle.Affectionate:
                    return TouchResult.Love;
                case TouchStyle.Playful:
                    return petsRecent >= 3 ? TouchResult.Love : TouchResult.Play;
                case TouchStyle.Curious:
                    return petsRecent <= 1 ? TouchResult.Sniff : TouchResult.Love;
                case TouchStyle.Fickle:
                    // 변덕: 처음 한두 번은 좋아하다가 계속 만지면 냥펀치(가끔은 그냥 좋아함)
                    if (petsRecent <= 1) return rnd < 0.75f ? TouchResult.Love : TouchResult.Hiss;
                    return rnd < 0.65f ? TouchResult.Hiss : TouchResult.Love;
                case TouchStyle.Shy:
                    // 낯가림: 단골(2단계↑)이 되면 마음을 연다
                    if (regularStage >= 2) return TouchResult.Love;
                    return petsRecent >= 3 && rnd < 0.3f ? TouchResult.Sniff : TouchResult.Flinch;
                case TouchStyle.Grumpy:
                    // 까칠: 기본은 하악. 단골 3단계면 냄새 정도는 맡게 해 준다
                    return regularStage >= 3 ? TouchResult.Sniff : TouchResult.Hiss;
            }
            return TouchResult.Sniff;
        }

        // 만족도 기분 변화(+1 좋음 / -1 싫음). 한 방문 누적은 SessionMoodClamp로 제한.
        public static int MoodDelta(TouchResult r) => r switch
        {
            TouchResult.Love => 1,
            TouchResult.Play => 1,
            TouchResult.Flinch => -1,
            TouchResult.Hiss => -1,
            _ => 0
        };
        public const int SessionMoodClamp = 2;

        // 관찰 기록(반응별 본 횟수) → 도감에 보여줄 '플레이어가 알아낸' 쓰다듬기 성향 키. 아직 없으면 null.
        public static string ObservedKey(int[] seen)
        {
            if (seen == null || seen.Length < ResultCount) return null;
            int love = seen[(int)TouchResult.Love], play = seen[(int)TouchResult.Play], sniff = seen[(int)TouchResult.Sniff];
            int flinch = seen[(int)TouchResult.Flinch], hiss = seen[(int)TouchResult.Hiss];
            int total = love + play + sniff + flinch + hiss;
            if (total == 0) return null;
            if (hiss > 0 && love > 0) return "touch.fickle";
            if (hiss > 0) return "touch.hates";
            if (flinch > 0 && love > 0) return "touch.warmed";
            if (flinch > 0) return "touch.shy";
            if (play > 0) return "touch.playful";
            if (love > 0) return "touch.loves";
            return "touch.curious";
        }

        // 두 고양이가 가까이 쉬고 있을 때 벌어질 일. relStage: 0 모름 / 1 지인 / 2 친구.
        public static SocialKind Pair(TouchStyle a, TouchStyle b, int relStage, float rnd)
        {
            bool grumpy = a == TouchStyle.Grumpy || b == TouchStyle.Grumpy;
            bool fickle = a == TouchStyle.Fickle || b == TouchStyle.Fickle;
            bool playful = a == TouchStyle.Playful || b == TouchStyle.Playful;

            // 캣초딩은 친구든 아니든 일단 덮치고 본다
            if (playful && rnd < 0.45f) return SocialKind.Ambush;
            if (relStage >= 2) return rnd < 0.5f ? SocialKind.Groom : (playful && rnd < 0.75f ? SocialKind.Play : SocialKind.Rub);
            if (grumpy)
            {
                // 까칠이 + 지지 않는 성격(변덕·장난)이면 싸움, 아니면 하악으로 끝
                bool feisty = a == TouchStyle.Fickle || b == TouchStyle.Fickle || playful || (a == TouchStyle.Grumpy && b == TouchStyle.Grumpy);
                if (relStage >= 1) return rnd < 0.5f ? SocialKind.Sniff : SocialKind.Hiss;
                return feisty && rnd < 0.6f ? SocialKind.Fight : SocialKind.Hiss;
            }
            if (fickle && relStage == 0 && rnd < 0.3f) return SocialKind.Hiss;
            if (relStage == 0) return SocialKind.Sniff;            // 처음 보는 사이는 우선 킁킁
            if (playful && rnd < 0.5f) return SocialKind.Play;
            if (a == TouchStyle.Shy || b == TouchStyle.Shy) return SocialKind.Sniff;
            return rnd < 0.6f ? SocialKind.Rub : SocialKind.Sniff;
        }
    }
}
