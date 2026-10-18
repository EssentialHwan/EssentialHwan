using UnityEngine;
using YeogiCafe.Cafe;

namespace YeogiCafe.Core
{
    // 카페 공간 배치표(단일 소스). 정면 2D, 1유닛 = 32px, 바닥은 y < FloorTop, 벽은 그 위.
    // SceneBuilder(공간 아트 배치)와 S1Bootstrap(좌석·슬롯 생성)이 같은 좌표를 써야 가구가 벽/창과 맞는다.
    public static class CafeLayout
    {
        public const float FloorTop = 4.2f;               // 바닥/벽 경계(징두리 하단)
        public const float RoomLeft = -5f, RoomRight = 21f;   // 주방 시점(카메라 뒤로 뺌)에서도 벽·바닥이 끊기지 않게 넉넉히

        public static readonly Vector3 CameraPos = new Vector3(7.5f, 4.0f, -10f);
        public const float CameraSize = 4.2f;             // 세로 8.4유닛 → 16:9에서 가로 ≈ 15유닛

        public static readonly Vector3 Door = new Vector3(1.2f, FloorTop, 0f);
        public static readonly Vector3 Entrance = new Vector3(1.2f, 3.9f, 0f);  // 고양이 등장/퇴장 지점(문 앞)
        public static readonly float[] WindowX = { 5.0f, 9.6f };
        public const float WindowBottom = 5.6f;
        public static readonly Vector3 Counter = new Vector3(13.4f, 3.3f, 0f);
        public static readonly Vector3 MenuBoard = new Vector3(13.6f, 6.0f, 0f);
        public static readonly float[] LampX = { 3.2f, 7.3f, 11.6f };
        public const float CeilingY = 8.6f;
        public static readonly Vector3 BigPlant = new Vector3(0.45f, 2.5f, 0f);

        // 고양이가 자유롭게 돌아다니는 바닥 영역(카운터 앞은 제외)
        public const float RoamMinX = 1.6f, RoamMaxX = 12.6f, RoamMinY = 0.45f, RoamMaxY = 3.55f;
        public static Vector3 RandomFloorPoint()
        {
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3(Random.Range(RoamMinX, RoamMaxX), Random.Range(RoamMinY, RoamMaxY), 0f);
                if (p.x > 11.6f && p.y > 2.5f) continue;          // 카운터
                return p;
            }
            return new Vector3(7f, 1.2f, 0f);
        }
        public static Vector3 ClampFloor(Vector3 p) =>
            new Vector3(Mathf.Clamp(p.x, RoamMinX, RoamMaxX), Mathf.Clamp(p.y, RoamMinY, RoamMaxY), 0f);

        // 시작 좌석 3개(가운데 줄). 창가 자리는 비워 둬서 '창가석 사볼까?'를 유도한다.
        public static readonly Vector2[] StarterSeats = { new Vector2(4.4f, 1.9f), new Vector2(6.6f, 1.9f), new Vector2(8.8f, 1.9f) };

        public struct SlotSpec
        {
            public Vector2 pos; public FurnitureManager.SlotKind kind; public bool nearWindow; public int minLevel;
            public SlotSpec(float x, float y, FurnitureManager.SlotKind k, bool win = false, int lv = 1)
            { pos = new Vector2(x, y); kind = k; nearWindow = win; minLevel = lv; }
        }

        const FurnitureManager.SlotKind F = FurnitureManager.SlotKind.Floor;
        const FurnitureManager.SlotKind W = FurnitureManager.SlotKind.Wall;

        // 상점 가구 배치 슬롯. 카페 성장(같은 방 확장)으로 앞줄(Lv2)·가장자리(Lv3)가 열린다.
        public static readonly SlotSpec[] Slots =
        {
            // Lv1 — 뒷줄(창 아래 2칸은 창가석 우선) + 가운데 줄 양끝
            new SlotSpec(5.0f, 3.2f, F, win: true), new SlotSpec(9.6f, 3.2f, F, win: true),
            new SlotSpec(3.2f, 3.2f, F), new SlotSpec(7.3f, 3.2f, F), new SlotSpec(11.4f, 3.2f, F),
            new SlotSpec(2.2f, 1.9f, F), new SlotSpec(11.0f, 1.9f, F),
            // 벽(시계·액자·창문·커튼)
            new SlotSpec(3.0f, 6.6f, W), new SlotSpec(7.3f, 6.6f, W), new SlotSpec(11.9f, 6.6f, W),
            // Lv2 — 앞줄
            new SlotSpec(3.3f, 0.6f, F, lv: 2), new SlotSpec(5.5f, 0.6f, F, lv: 2), new SlotSpec(7.7f, 0.6f, F, lv: 2),
            new SlotSpec(9.9f, 0.6f, F, lv: 2), new SlotSpec(12.1f, 0.6f, F, lv: 2),
            new SlotSpec(1.2f, 7.0f, W, lv: 2),
            // Lv3 — 가장자리
            new SlotSpec(13.1f, 1.9f, F, lv: 3), new SlotSpec(1.3f, 0.6f, F, lv: 3),
        };
    }
}
