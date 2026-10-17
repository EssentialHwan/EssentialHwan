using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;

namespace YeogiCafe.Cafe
{
    // IFacility의 씬 구현. FurnitureData(type==Facility)에서 태그를 읽는다.
    public class FacilityBehaviour : MonoBehaviour, IFacility
    {
        public FurnitureData source;

        string _id;
        public string FacilityId => _id ??= (source != null ? source.furnitureId : name) + "#" + GetInstanceID();
        public IReadOnlyList<FacilityTag> Tags => source != null ? source.facilityTags : System.Array.Empty<FacilityTag>();
        public bool IsFree => user == null;

        object user;
        public void Use(object cat) => user = cat;
        public void Release() => user = null;

        // 고양이가 올라가는 높이(유닛) — 표현용(CatView). 캣타워는 꼭대기, 쿠션·바구니는 살짝 위.
        public float PerchHeight => source == null ? 0f : source.furnitureId switch
        {
            "furn_fac_tower" => 1.15f,
            "furn_fac_cushion" => 0.15f,
            "furn_fac_toybox" => 0.2f,
            _ => 0f,
        };

        void Start() => EnsureVisual();

        public void EnsureVisual()
        {
            if (source == null || source.worldSprite == null) return;
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = source.worldSprite;
            sr.sortingOrder = source.wallMounted ? YeogiCafe.Visual.YSort.WallProps
                                                 : YeogiCafe.Visual.YSort.Order(transform.position.y);
        }
    }
}
