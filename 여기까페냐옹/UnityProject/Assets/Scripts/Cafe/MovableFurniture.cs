using System;
using UnityEngine;
using YeogiCafe.Core;

namespace YeogiCafe.Cafe
{
    // 배치 모드에서 끌어서 옮길 수 있는 가구. 위치 범위(바닥/벽)와 저장 콜백을 가진다.
    public class MovableFurniture : MonoBehaviour
    {
        public enum Area { Floor, Wall, FloorFlat, Ceiling }   // FloorFlat: 러그처럼 바닥에 깔리는 것(겹쳐도 됨) · Ceiling: 천장 조명(가로로만)
        public Area area;
        public bool lockY;                             // 가로로만 옮긴다(통유리 창 — 높이는 창 자리로 고정)
        public float lockedY;
        public float halfWidth;                        // 넓은 벽걸이(통유리)는 벽 밖으로 삐져나가지 않게
        public Action<Vector3> onMoved;                // 저장(FurnitureManager가 연결)

        public SpriteRenderer rendererOverride;        // 그림이 자식에 있는 것(통유리: 창틀)
        public SpriteRenderer Renderer => rendererOverride != null ? rendererOverride : GetComponent<SpriteRenderer>();

        public Vector3 Clamp(Vector3 p)
        {
            p.z = 0f;
            p.x = Mathf.Round(p.x * 10f) / 10f; p.y = Mathf.Round(p.y * 10f) / 10f;   // 0.1유닛 격자
            if (area == Area.Ceiling)
            {
                p.x = Mathf.Clamp(p.x, 0.9f, CafeLayout.WallMaxX(CafeLayout.ViewLevel) + 0.4f);
                p.y = lockedY;
                return p;
            }
            if (area == Area.Wall && halfWidth > 0.6f)
            {
                p.x = Mathf.Clamp(p.x, 0.5f + halfWidth, CafeLayout.WallMaxX(CafeLayout.ViewLevel) + 0.8f - halfWidth);
                if (lockY) p.y = lockedY;
                return p;
            }
            if (area == Area.Wall)
            {
                p.x = Mathf.Clamp(p.x, 0.9f, CafeLayout.WallMaxX(CafeLayout.ViewLevel));
                p.y = Mathf.Clamp(p.y, CafeLayout.FloorTop + 1.2f, CafeLayout.WallMaxY(CafeLayout.ViewLevel));
                if (lockY) p.y = lockedY;
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
