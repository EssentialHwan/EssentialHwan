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

        // ── 카페 레벨별 화면(확장 = 실제로 보이는 공간이 넓어짐) ──
        //  Lv1: 확대된 아늑한 가게(문~오른쪽 벽 앞, 가로 ≈ 13.5유닛) · Lv2: 오른쪽 공간이 이어져 드러남(가로 ≈ 15유닛)
        //  Lv3: 위로 넓히기(다락) — 3단계 작업 전까지는 Lv2 화면을 쓴다.
        // 카운터는 없앴다(주문은 자리에서 받는다) → 오른쪽 끝까지 손님 공간.
        public static int ViewLevel = 2;                  // 화면에 반영된 레벨(GameUiController가 확장 연출과 함께 바꾼다)
        public static Vector3 ViewPos(int lv) => lv <= 1 ? new Vector3(7.0f, 3.95f, -10f) : lv == 2 ? new Vector3(7.75f, 4.0f, -10f) : new Vector3(8.6f, 5.0f, -10f);
        public static float ViewSize(int lv) => lv <= 1 ? 3.8f : lv == 2 ? 4.2f : 4.9f;   // Lv3은 위로 넓어져 고양이가 조금 작아짐(약 86%)
        public static Vector3 CameraPos => ViewPos(ViewLevel);
        public static float CameraSize => ViewSize(ViewLevel);
        // 고양이가 다니고 가구를 놓을 수 있는 오른쪽 끝(화면 오른쪽 가장자리에서 여유를 둠)
        public static float FloorMaxX(int lv) => lv <= 1 ? 12.6f : lv == 2 ? 14.3f : 15.6f;
        public static float WallMaxX(int lv) => lv <= 1 ? 13.2f : lv == 2 ? 14.6f : 15.9f;
        // Lv3 다락: 벽 위쪽에 캣워크 선반(가로 전체) + 오른쪽 다락 데크, 사다리로 오른다
        public const float LoftDeckY = 7.7f, LadderX = 12.4f, DeckLeft = 12.8f, DeckRight = 16.9f;   // 선반은 통유리 창틀 바로 위(상단 메뉴 바에 안 가리게)
        public static readonly float[] LoftSpotX = { 13.7f, 15.5f, 9.4f, 5.3f };   // 데크 2자리 + 캣워크(창 위 선반) 2자리
        public static float WallMaxY(int lv) => lv <= 1 ? 6.9f : CeilingY - 1.1f;      // Lv1은 화면 위쪽이 좁다

        public static readonly Vector3 Door = new Vector3(1.2f, FloorTop, 0f);
        public static readonly Vector3 Entrance = new Vector3(1.2f, 3.9f, 0f);  // 고양이 등장/퇴장 지점(문 앞)
        public static readonly float[] WindowX = { 5.0f, 9.6f };
        public const float WindowBottom = 5.6f;
        // 통유리 창(가게 앞 쇼윈도): 유리 영역(바깥 풍경)의 아래·가운데·크기
        public const float GlassCenterX = 7.3f, GlassBottom = 5.12f, GlassWidth = 8f, GlassHeight = 70f / 32f;
        public static float GlassLeft => GlassCenterX - GlassWidth * 0.5f;
        public static float GlassRight => GlassCenterX + GlassWidth * 0.5f;
        public static readonly Vector3 MenuBoard = new Vector3(13.6f, 6.0f, 0f);
        public static readonly float[] LampX = { 3.2f, 7.3f, 11.6f };
        public const float CeilingY = 8.6f;
        public static readonly Vector3 BigPlant = new Vector3(0.45f, 2.5f, 0f);

        // 고양이가 자유롭게 돌아다니는 바닥 영역(카운터 앞은 제외)
        public const float RoamMinX = 1.6f, RoamMinY = 0.45f, RoamMaxY = 3.55f;
        public static float RoamMaxX => FloorMaxX(ViewLevel);
        public static Vector3 RandomFloorPoint() =>
            new Vector3(Random.Range(RoamMinX, RoamMaxX), Random.Range(RoamMinY, RoamMaxY), 0f);
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
            // 벽(시계·액자·창문·커튼) — 통유리 양옆
            new SlotSpec(2.45f, 6.5f, W), new SlotSpec(12.3f, 6.5f, W),
            // Lv2 — 앞줄
            new SlotSpec(3.3f, 0.6f, F, lv: 2), new SlotSpec(5.5f, 0.6f, F, lv: 2), new SlotSpec(7.7f, 0.6f, F, lv: 2),
            new SlotSpec(9.9f, 0.6f, F, lv: 2), new SlotSpec(12.1f, 0.6f, F, lv: 2),
            new SlotSpec(12.3f, 7.5f, W, lv: 2), new SlotSpec(2.45f, 7.5f, W, lv: 2),   // 윗줄 벽은 Lv1 화면 밖
            // Lv2 — 오른쪽에 새로 드러난 공간(옛 카운터 자리)
            new SlotSpec(13.4f, 3.2f, F, lv: 2), new SlotSpec(13.6f, 1.9f, F, lv: 2), new SlotSpec(14.1f, 0.6f, F, lv: 2),
            // Lv3 — 가장자리
            new SlotSpec(1.3f, 0.6f, F, lv: 3),
        };
    }
}
