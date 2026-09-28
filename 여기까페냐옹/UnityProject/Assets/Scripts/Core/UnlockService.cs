using System.Collections.Generic;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Core
{
    // 사양서 §32 — 해금 판정 서비스. 전체 콘텐츠 중 현재 세이브 상태로 해금된 것만 필터.
    public static class UnlockService
    {
        public static List<CatData> UnlockedCats(IEnumerable<CatData> all, SaveGame save)
        {
            var list = new List<CatData>();
            foreach (var c in all)
                if (c != null && (c.unlockCondition == null || c.unlockCondition.IsMet(save)))
                    list.Add(c);
            return list;
        }

        public static List<MenuData> UnlockedMenus(IEnumerable<MenuData> all, SaveGame save)
        {
            var list = new List<MenuData>();
            foreach (var m in all)
                if (m != null && (m.unlockCondition == null || m.unlockCondition.IsMet(save)))
                    list.Add(m);
            return list;
        }

        // 새로 해금된 고양이 id (직전 대비 신규) — "새 손님이 찾아왔어요!" 알림용
        public static List<string> NewlyUnlockedCatIds(IEnumerable<CatData> all, SaveGame save, HashSet<string> known)
        {
            var newly = new List<string>();
            foreach (var c in all)
            {
                if (c == null) continue;
                bool met = c.unlockCondition == null || c.unlockCondition.IsMet(save);
                if (met && !known.Contains(c.catId)) { newly.Add(c.catId); known.Add(c.catId); }
            }
            return newly;
        }
    }
}
