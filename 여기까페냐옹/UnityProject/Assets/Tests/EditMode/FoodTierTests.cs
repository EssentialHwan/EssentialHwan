using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;

namespace YeogiCafe.Tests
{
    // P10 / 정식 — 음식 2계층(최애 + 계열). 정식 모드에서 계열 선호가 무관보다 우선.
    public class FoodTierTests
    {
        CatData cat;
        [SetUp]
        public void Setup()
        {
            cat = ScriptableObject.CreateInstance<CatData>();
            cat.catId = "c";
            cat.foodFavoriteMenuId = "menu_none_here"; // 최애는 목록에 없음
            cat.foodPreferredTags = new[] { FoodTag.Fish };
            cat.personalityTags = new[] { PersonalityTag.Relaxed, PersonalityTag.Relaxed };
        }

        static MenuData M(string id, params FoodTag[] tags)
        {
            var m = ScriptableObject.CreateInstance<MenuData>();
            m.menuId = id; m.foodTags = tags; m.price = 50;
            return m;
        }

        [Test]
        public void FullMode_PrefersTagMatch_OverUnrelated()
        {
            var menus = new List<MenuData> {
                M("m_fish", FoodTag.Fish),     // 계열 일치
                M("m_sweet", FoodTag.Sweet),   // 무관
            };
            // 정식 모드(mvpMode:false): Fish 태그 +40 → 다회 반복해도 m_fish 우세
            int fishCount = 0;
            for (int i = 0; i < 30; i++)
            {
                var o = OrderDecider.Choose(cat, menus, mvpMode: false);
                if (o.primary.menuId == "m_fish") fishCount++;
            }
            Assert.Greater(fishCount, 20, "정식 모드에서 계열(Fish) 선호가 무관보다 자주 선택");
        }

        [Test]
        public void MvpMode_IgnoresTag_FavoriteOnly()
        {
            var menus = new List<MenuData> {
                M("m_fish", FoodTag.Fish),
                M("m_sweet", FoodTag.Sweet),
            };
            // MVP 모드: 최애가 목록에 없으니 태그 가산 없음 → 랜덤(둘 다 가능). 크래시/예외 없이 선택되면 통과.
            var o = OrderDecider.Choose(cat, menus, mvpMode: true);
            Assert.IsNotNull(o.primary);
            Assert.IsTrue(o.wasForcedSecond, "최애 미제공 → 차선 강제 플래그");
        }
    }
}
