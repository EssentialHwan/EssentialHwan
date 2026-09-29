# 픽셀 아트 (절차적 생성)

《여기까페냐옹》의 픽셀 리소스를 **코드로 생성**한다. 모든 스프라이트가 단일 팔레트·단일 붓을 쓰므로 **톤앤매너가 강제로 통일**된다("한 게임의 리소스").

## 생성 방법
Unity 상단 메뉴 **여기까페냐옹 ▸ 픽셀 아트 생성** 클릭 → `Assets/Art/Generated/`에 PNG 44개 생성.
- `cats/` — 고양이 8종 정지 (32×32, 팔레트 스왑 + 고유 무늬)
- `cats_sheet/` — 고양이 8종 **애니메이션 스프라이트시트** (32×32 × 7프레임: idle0/1·walk0/1·eat0/1·sleep, Multiple 슬라이스·발밑 피벗)
- `furniture/` — 가구 18종 (32×32, 좌석6·시설6·장식6)
- `icons/` — UI 아이콘 8종 (16×16: 별·체크·물음표·골드·시계·돋보기·자물쇠·하트)
- `bg/` — 배경 심리스 타일 2종 (floor·wall, 32×32)

임포트 설정 자동 적용: Sprite / **Point 필터**(크리스프 도트) / 무압축 / PPU 통일. 시트는 Multiple 모드로 프레임 슬라이스까지 자동.

## 애니메이션 사용
`cats_sheet/{id}_sheet.png`의 슬라이스된 스프라이트로 Unity Animation 클립 구성:
- **idle**: idle0↔idle1 (깜빡임), **walk**: walk0↔walk1 (몸 들썩임·꼬리 흔들기), **eat**: eat0↔eat1 (반눈+그릇), **sleep**: 단일(웅크림+Zzz)
- 프레임 이름이 슬라이스에 지정되어 있어 Animator에서 바로 참조 가능.

## 통일성 규칙 (코드로 보장)
| 요소 | 강제 방식 |
|---|---|
| 색 | `PixelPalette` 상수만 사용. 즉석 색 금지 |
| 아웃라인 | `AutoOutline(PixelPalette.Outline)` — 전 스프라이트 동일 짙은 갈색 |
| 접지 그림자 | `GroundShadow()` — 반투명 타원 통일 |
| 셰이딩 | 털·나무·파스텔 전부 light/mid/shade 3톤 세트 |
| 실루엣 | `PixelCanvas` 공용 프리미티브(Disc/FillRect/Mirror) |

## 파일
- `Scripts/Art/PixelPalette.cs` — 단일 팔레트(색의 유일한 출처)
- `Scripts/Art/PixelCanvas.cs` — 공용 드로잉 버퍼(붓)
- `Scripts/Art/SpriteDrawing.cs` — 고양이 8종 + 아이콘 8종
- `Scripts/Art/FurnitureDrawing.cs` — 가구 18종
- `Editor/PixelArtGenerator.cs` — PNG 일괄 생성기(메뉴)

## 검증
`Assets/Tests/EditMode/PixelArtTests.cs` — 통일성 자동 검증(공용 아웃라인 포함·고양이 털색 상호 구별·전 스프라이트 비어있지 않음·규격). dotnet 헤드리스 통과.

## 품질 등급 / 확장
현재는 **일관된 스타일의 준수한 절차적 도트**(플레이테스트·M1 즉시 사용 가능). 정식 출시용 고퀄 손맛이 필요하면:
1. 이 생성물을 베이스로 Aseprite에서 리터치, 또는
2. 애니메이션 프레임(walk/eat/sleep) 추가 — `SpriteDrawing`에 프레임 함수 추가로 확장.
팔레트·규격은 이 시스템을 그대로 기준으로 삼으면 손맛 아트도 톤이 어긋나지 않는다.
