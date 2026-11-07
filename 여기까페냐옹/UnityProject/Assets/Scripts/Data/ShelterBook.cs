namespace YeogiCafe.Data
{
    // 냥냥 보호소 후원(후반 골드 소모처). 누적 후원액이 단계 기준을 넘으면 보상이 열린다.
    //  1~3단계: 보호소 고양이가 손님으로 찾아온다(UnlockType.Shelter) · 4단계: 사진 프레임 '보호소 하트'
    // 보호소 고양이는 '모두의 취향' 엔딩 목표에서 빠진다(후원할수록 엔딩이 멀어지지 않게).
    public static class ShelterBook
    {
        public struct Tier { public int need; public string catId; }
        public static readonly Tier[] Tiers =
        {
            new Tier { need = 500,  catId = "cat_sphynx" },
            new Tier { need = 1500, catId = "cat_kitten" },
            new Tier { need = 3000, catId = "cat_senior" },
            new Tier { need = 5000, catId = null },          // 보호소 하트 프레임
        };
        public const string RewardFrame = "heart";

        public static int TierOf(int donated)
        {
            int t = 0;
            foreach (var x in Tiers) if (donated >= x.need) t++;
            return t;
        }

        public static int NextNeed(int donated)
        {
            foreach (var x in Tiers) if (donated < x.need) return x.need;
            return -1;
        }

        public static bool IsShelterCat(string catId)
        {
            if (string.IsNullOrEmpty(catId)) return false;
            foreach (var x in Tiers) if (x.catId == catId) return true;
            return false;
        }
    }
}
