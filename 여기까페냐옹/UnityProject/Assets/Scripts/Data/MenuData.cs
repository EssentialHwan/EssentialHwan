using UnityEngine;

namespace YeogiCafe.Data
{
    // 사양서 §1.2 — 메뉴. MVP는 foodFavorite(정확 일치)만 판정, 계열(foodTags)은 정식.
    [CreateAssetMenu(menuName = "YeogiCafe/MenuData")]
    public class MenuData : ScriptableObject
    {
        public string menuId;              // "menu_fishcake"
        public string displayNameKey;
        public int price;
        public int cost;
        public float cookTime = 4f;
        public FoodTag[] foodTags;
        public int satiety = 50;
        public int satisfaction = 5;
        public UnlockCondition unlockCondition = new UnlockCondition();

        public bool HasTag(FoodTag t)
        {
            if (foodTags == null) return false;
            foreach (var f in foodTags) if (f == t) return true;
            return false;
        }
    }
}
