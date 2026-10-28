namespace YeogiCafe.Visual
{
    // 정면 2D 카페의 그리기 순서 규칙(한 곳에서만 정의).
    // 바닥 위 오브젝트는 화면 아래(=앞)에 있을수록 위에 그린다. 벽·바닥 타일은 항상 뒤.
    public static class YSort
    {
        public const int FloorTiles = -3000;
        public const int WallTiles  = -2500;
        public const int WallProps  = -2000;   // 창·문·칠판·벽걸이 가구
        public const int FlatOnFloor = -1500;  // 러그처럼 바닥에 깔리는 것
        public const int CatAboveSeat = 5;     // 같은 y의 좌석보다 고양이를 위에

        public static int Order(float worldY) => -(int)(worldY * 100f);
    }
}
