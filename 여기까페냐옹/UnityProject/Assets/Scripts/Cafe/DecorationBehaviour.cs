using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.Cafe
{
    // 장식 가구(시계·액자·러그·조명·커튼·화병)의 씬 표현. 관찰 채널은 아니고 분위기 기여만(AtmosphereSystem).
    public class DecorationBehaviour : MonoBehaviour
    {
        public FurnitureData source;

        void Start() => EnsureVisual();

        public void EnsureVisual()
        {
            if (source == null || source.worldSprite == null) return;
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = source.worldSprite;
            if (source.wallMounted) sr.sortingOrder = YeogiCafe.Visual.YSort.WallProps;
            else if (source.furnitureId == "furn_deco_rug") sr.sortingOrder = YeogiCafe.Visual.YSort.FlatOnFloor;
            else sr.sortingOrder = YeogiCafe.Visual.YSort.Order(transform.position.y);
        }
    }
}
