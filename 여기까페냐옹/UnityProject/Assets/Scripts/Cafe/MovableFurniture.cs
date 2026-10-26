using System;
using UnityEngine;
using YeogiCafe.Core;

namespace YeogiCafe.Cafe
{
    // 배치 모드에서 끌어서 옮길 수 있는 가구. 위치 범위(바닥/벽)와 저장 콜백을 가진다.
    public class MovableFurniture : MonoBehaviour
    {
        public enum Area { Floor, Wall, FloorFlat }   // FloorFlat: 러그처럼 바닥에 깔리는 것(겹쳐도 됨)
        public Area area;
        public Action<Vector3> onMoved;                // 저장(FurnitureManager가 연결)

        public SpriteRenderer Renderer => GetComponent<SpriteRenderer>();

        public Vector3 Clamp(Vector3 p)
        {
            p.z = 0f;
            p.x = Mathf.Round(p.x * 10f) / 10f; p.y = Mathf.Round(p.y * 10f) / 10f;   // 0.1유닛 격자
            if (area == Area.Wall)
            {
                p.x = Mathf.Clamp(p.x, 0.9f, CafeLayout.WallMaxX(CafeLayout.ViewLevel));
                p.y = Mathf.Clamp(p.y, CafeLayout.FloorTop + 1.2f, CafeLayout.WallMaxY(CafeLayout.ViewLevel));
            }
            else
            {
                p.x = Mathf.Clamp(p.x, 0.9f, CafeLayout.FloorMaxX(CafeLayout.ViewLevel) + 0.4f);   // 레벨별 화면 안
                p.y = Mathf.Clamp(p.y, 0.3f, CafeLayout.FloorTop - 0.55f);
            }
            return p;
        }

        public void RefreshVisual()
        {
            var sb = GetComponent<SeatBehaviour>(); if (sb != null) sb.EnsureVisual();
            var fb = GetComponent<FacilityBehaviour>(); if (fb != null) fb.EnsureVisual();
            var db = GetComponent<DecorationBehaviour>(); if (db != null) db.EnsureVisual();
        }
    }
}
