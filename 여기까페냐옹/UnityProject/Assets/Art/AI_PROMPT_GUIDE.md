# 《여기까페냐옹》 AI 아트 제작 가이드 (ChatGPT / 이미지 생성 AI용)

이 문서는 **이미지 생성 AI(ChatGPT DALL·E, Midjourney, Stable Diffusion 등)** 로 게임의 정식 아트 리소스를 만들 때 넣는 **정밀 프롬프트 + 규격서**다.

- 절차적 생성기(`Editor/PixelArtGenerator.cs`, `Scripts/Art/*`)가 만드는 도트는 **플레이스홀더**다. 스타일·규격의 기준일 뿐, 최종 아트가 아니다.
- 아래 팔레트 hex는 **`Scripts/Art/PixelPalette.cs`에서 그대로 추출**한 값이다. AI에게 이 hex를 강제해 **절차적 생성물과 AI 생성물의 톤이 100% 일치**하게 한다.
- 규격(픽셀 크기·PPU·피벗)은 절차적 생성물과 동일해야 씬/코드 수정 없이 교체된다.

> ⚠️ **핵심 원칙**: 새 색을 즉석에서 만들지 말 것. 아래 팔레트 밖의 색이 나오면 리터치로 팔레트에 스냅. 이것이 "한 게임의 리소스"를 보장하는 유일한 규칙이다.

---

## 0. 스타일 앵커 (모든 프롬프트에 공통으로 붙일 문장)

프롬프트 영어 앵커(권장, AI가 가장 잘 알아듣는 형태):

```
cozy pastel cat-cafe pixel art, warm soft-shaded 32x32 sprite,
3-tone soft shading (light / mid / shade), unified dark-brown outline #4A362F (never pure black),
crisp point-filter dots, transparent background, front-facing, grounded soft elliptical shadow,
limited palette only (use ONLY the listed hex colors), no anti-aliasing, no gradient banding,
gentle rounded silhouette, calm heartwarming tone.
```

한국어 앵커(ChatGPT 대화용):

```
파스텔 따뜻톤 코지 고양이 카페 도트(픽셀) 아트. 32x32 스프라이트, 소프트 셰이딩 3톤(라이트/미드/셰이드),
통일 아웃라인은 짙은 갈색 #4A362F(순검정 금지), 크리스프 도트(안티에일리어싱 없음), 배경 투명,
정면 뷰, 발밑에 반투명 타원 접지 그림자, 아래 지정된 hex 색만 사용, 둥글고 안정적인 실루엣, 잔잔하고 따뜻한 분위기.
```

---

## 1. 통일 팔레트 (PixelPalette.cs 원본 hex)

**이 색들만 사용.** AI에게 "use ONLY these hex colors"로 강제하고, 결과가 벗어나면 Aseprite의 팔레트 매핑으로 스냅.

### 공용
| 이름 | HEX | RGB | 용도 |
|---|---|---|---|
| Outline | `#4A362F` | 74,54,47 | 전 스프라이트 공용 아웃라인(짙은 갈색, 검정 대용) |
| OutlineSoft | `#785C4E` | 120,92,78 | 내부 경계용 연한 아웃라인 |
| White(크림) | `#FAF6EE` | 250,246,238 | 순백 대신 크림 화이트 |
| Highlight | `#FFFCF5` | 255,252,245 | 하이라이트 |
| Shadow | `#4A362F` @ alpha 60 | 74,54,47,60 | 반투명 접지 그림자 |

### 카페 환경 (따뜻한 나무·크림)
| 이름 | HEX | RGB |
|---|---|---|
| WoodLight | `#D6B28A` | 214,178,138 |
| WoodMid | `#B88E64` | 184,142,100 |
| WoodDark | `#966E4A` | 150,110,74 |
| CreamLight | `#F5E8D4` | 245,232,212 |
| CreamMid | `#E6D2B6` | 230,210,182 |

### 파스텔 악센트 (분위기·가구·UI) — 각 라이트/미드 2톤
| 이름 | Light | Mid |
|---|---|---|
| Pink | `#F7C8D0` | `#E6A0AC` |
| Green | `#BED6A8` | `#96B880` |
| Blue | `#B2D0E2` | `#84B0CA` |
| Yellow | `#F8E09C` | `#E8C670` |
| Lilac | `#D2C4E6` | `#B09CD0` |

### 고양이 포인트색 (통일)
| 이름 | HEX | RGB | 용도 |
|---|---|---|---|
| CatNose | `#D6828A` | 214,130,138 | 코·귀 안쪽 연분홍 |
| CatEye | `#609678` | 96,150,120 | 부드러운 초록 눈(8종 공통) |

---

## 2. 고양이 8종 (32×32, PPU 32)

**규격**: 32×32px, 투명 배경, 정면 앉은 포즈, 발밑 피벗(0.5, 0.1), PPU=32, 아웃라인 `#4A362F` 통일, 눈 `#609678`·코 `#D6828A` 통일.

각 고양이는 **털색 3톤(light/mid/shade)** 이 고정돼 있다(PixelPalette.Fur). 이 3색을 프롬프트에 넣어 8마리가 서로 구별되되 톤은 통일되게 한다.

| catId | 한글명 | 종류/특징 | Fur Light | Fur Mid | Fur Shade |
|---|---|---|---|---|---|
| cat_cheese | 치즈냥 | 오렌지 태비(줄무늬) | `#F5C878` | `#E0A854` | `#BE843C` |
| cat_calico | 삼색냥 | 크림 베이스 + 오렌지·검정 얼룩 | `#FAF0E4` | `#ECD2B4` | `#C8A078` |
| cat_cow | 젖소냥 | 흰 베이스 + 검정 얼룩 | `#F8F6F2` | `#DCD8D2` | `#787470` |
| cat_black | 까망냥 | 차콜(순검정 아님) | `#605A68` | `#484252` | `#322E3C` |
| cat_tuxedo | 턱시도냥 | 흑백(가슴·얼굴 흰 V) | `#F0EEEA` | `#5A5660` | `#3C3844` |
| cat_mackerel | 고등어냥 | 블루그레이 태비 | `#96A8B8` | `#768A9C` | `#586A7C` |
| cat_siamese | 샴냥 | 크림 + 진한 포인트(귀·얼굴) | `#F0E2C8` | `#CEB696` | `#786452` |
| cat_gray | 회색냥 | 단색 그레이 | `#C4C6CC` | `#A2A4AC` | `#7A7C86` |

### 고양이 개별 프롬프트 템플릿

`{앵커}` + 아래 문장을 붙여 사용:

```
A single cute {한글명 영문설명} cat sitting front-facing, 32x32 pixel sprite.
Fur uses exactly three tones: light {LightHex}, mid {MidHex}, shade {ShadeHex}.
Eyes soft green #609678, nose/inner-ears pink #D6828A, unified outline #4A362F.
{개체 무늬 설명}. Rounded stable silhouette, soft elliptical ground shadow, transparent background,
no anti-aliasing, crisp dots. Use ONLY these hex colors.
```

예시 — 치즈냥:
```
A single cute cheese-orange tabby cat sitting front-facing, 32x32 pixel sprite.
Fur uses exactly three tones: light #F5C878, mid #E0A854, shade #BE843C.
Eyes soft green #609678, nose/inner-ears pink #D6828A, unified outline #4A362F.
Add orange tabby stripes on forehead and body. Rounded stable silhouette,
soft elliptical ground shadow, transparent background, no anti-aliasing, crisp dots. Use ONLY these hex colors.
```

개체 무늬 설명 참고:
- cat_cheese / cat_mackerel: tabby stripes (태비 줄무늬)
- cat_calico: patched calico spots (오렌지+검정 얼룩)
- cat_cow: cow-like black patches on white
- cat_tuxedo: white chest and white face V (턱시도)
- cat_siamese: darker color-points on ears and face
- cat_black / cat_gray: solid color, shading only

### 애니메이션 스프라이트시트 (선택, 고완성도)
- 시트 규격: **가로 7프레임 × 32px = 224×32**, Multiple 슬라이스, 프레임 순서 `idle0, idle1, walk0, walk1, eat0, eat1, sleep`.
- idle: 눈 깜빡임 / walk: 몸 들썩임·꼬리 흔들기 / eat: 반눈 + 앞 그릇(음식 `#E6A0AC`) / sleep: 웅크림 + Zzz.
- 프롬프트에 `7-frame horizontal sprite sheet, 224x32, same cat, poses: idle-blink, walk-bob, eat, curled sleep`를 추가.

---

## 3. 가구 18종 (32×32, PPU 32)

**규격**: 32×32px, 투명 배경, 아이소·정면 절충(정면 뷰), 나무는 `#D6B28A`/`#B88E64`/`#966E4A` 3톤, 아웃라인 `#4A362F`, 발밑 접지 그림자.

각 가구의 **분위기 악센트색**을 프롬프트에 넣어 톤을 유도한다(아래 표의 악센트 열).

### 좌석 6종
| furnitureId | 한글명 | 태그 | 악센트 | 프롬프트 힌트 |
|---|---|---|---|---|
| furn_seat_normal | 일반석 | Normal | Wood | plain wooden round cafe chair + small table |
| furn_seat_window | 창가석 | Window/Quiet/OutsideView | Blue `#84B0CA` + Natural | chair by a bright window, soft blue daylight, plant hint |
| furn_seat_corner | 구석석 | Corner/Quiet | Wood + Lilac `#B09CD0` | tucked corner seat, calm shadowed nook |
| furn_seat_two | 2인석 | TwoSeat | Wood | two chairs facing a small table |
| furn_seat_sofa | 소파 | Sofa | Pink `#E6A0AC` + Warm | soft cozy two-seat sofa, warm pastel cushion |
| furn_seat_bar | 바테이블 | BarTable | Wood + Luxury | tall bar stool + counter edge |

### 시설 6종
| furnitureId | 한글명 | 태그 | 악센트 | 프롬프트 힌트 |
|---|---|---|---|---|
| furn_fac_tower | 캣타워 | Play/Height/Active | Wood + Lively | tall multi-level cat tower with platforms |
| furn_fac_cushion | 쿠션 | Soft/Relax/Quiet | Pink `#F7C8D0` | round plush floor cushion |
| furn_fac_toybox | 장난감바구니 | Play | Yellow `#E8C670` | wicker basket full of cat toys and a ball |
| furn_fac_scratch | 스크래처 | Active | Wood + Cream | vertical scratching post, rope texture |
| furn_fac_plant | 화분 | Natural | Green `#96B880` | potted leafy plant in a cream pot |
| furn_fac_window | 창문 | Natural/OutsideView | Blue `#84B0CA` | wall window showing soft sky, wooden frame |

### 장식 6종 (분위기 기여만, 좌석/시설 아님)
| furnitureId | 한글명 | 분위기 | 악센트 | 프롬프트 힌트 |
|---|---|---|---|---|
| furn_deco_clock | 벽시계 | Luxury | Cream + Wood | round wall clock, cream face |
| furn_deco_frame | 액자 | Natural | Green/Wood | small framed picture on wall |
| furn_deco_rug | 러그 | Warm | Pink/Yellow | soft patterned floor rug, warm pastel |
| furn_deco_lamp | 조명 | Warm+Luxury | Yellow `#F8E09C` glow | cozy floor lamp with warm glow |
| furn_deco_curtain | 커튼 | Quiet | Lilac/Cream | soft hanging curtain fold |
| furn_deco_vase | 화병 | (Natural) | Green/Lilac | slim vase with a few flowers |

### 가구 개별 프롬프트 템플릿
```
{앵커}
A single {프롬프트 힌트}, 32x32 cafe furniture pixel sprite, front view,
wood tones #D6B28A/#B88E64/#966E4A, accent color {악센트Hex}, unified outline #4A362F,
soft elliptical ground shadow, transparent background, crisp dots, use ONLY listed hex colors.
```

---

## 4. 배경 타일 2종 (32×32, PPU 32, **심리스**·완전 불투명)

| id | 용도 | 팔레트 | 프롬프트 힌트 |
|---|---|---|---|
| floor | 바닥 나무 판자 | Wood 3톤 | seamless tiling wooden plank cafe floor, subtle plank seams |
| wall | 벽지 | Cream 3톤 + Wood 걸레받이 | seamless tiling cream wallpaper with faint vertical stripes and a wood baseboard |

프롬프트에 반드시 `seamless tileable, fully opaque, no transparency, edges wrap perfectly`를 추가. (고양이/가구와 달리 아웃라인·그림자 없음.)

---

## 5. UI 아이콘 8종 (16×16, PPU 16)

**규격**: 16×16px, 투명 배경, 아웃라인 `#4A362F`, 굵고 명확한 실루엣(작은 크기라 디테일 최소화).

| id | 뜻 | 팔레트 | 프롬프트 힌트 |
|---|---|---|---|
| icon_star | 확정 가능(★) | Yellow `#E8C670`/`#F8E09C` | 5-point star |
| icon_check | 기록 완료(✓) | Green `#96B880`/`#BED6A8` | check mark |
| icon_question | 미확인(?) | Lilac `#B09CD0` | question mark |
| icon_gold | 골드 | Yellow `#E8C670` | round gold coin with shine |
| icon_clock | 시간 | Cream `#F5E8D4` + Wood | round clock face with hands |
| icon_magnifier | 관찰 | Blue `#84B0CA` + Wood handle | magnifying glass |
| icon_lock | 잠김 | Wood `#B88E64` | padlock |
| icon_heart | 호감/관계 | Pink `#E6A0AC`/`#F7C8D0` | heart |

### 아이콘 프롬프트 템플릿
```
{앵커(16x16으로 치환)}
A single {힌트} UI icon, 16x16 pixel, bold clear silhouette, color {팔레트Hex},
unified outline #4A362F, transparent background, no anti-aliasing, use ONLY listed hex colors.
```

---

## 6. Unity 임포트 설정 (절차적 생성물과 동일하게 맞출 것)

AI로 만든 PNG를 `Assets/Art/Generated/`의 해당 폴더(cats / furniture / icons / bg / cats_sheet)에 **같은 파일명**으로 넣으면, `SceneBuilder`의 `LinkSprites`가 데이터에 자동 연결한다. 임포트 세팅은 반드시 아래로 통일(픽셀아트 크리스프 룩):

| 항목 | 값 |
|---|---|
| Texture Type | **Sprite (2D and UI)** |
| Sprite Mode | 단일: **Single** / 고양이 시트: **Multiple** |
| Pixels Per Unit (PPU) | 고양이·가구·배경 **32**, 아이콘 **16** |
| Filter Mode | **Point (no filter)** — 크리스프 도트 |
| Compression | **None (Uncompressed)** |
| Generate Mip Maps | **끔** |
| Alpha Is Transparency | **켬** |
| (시트) Pivot | **Custom (0.5, 0.1)** — 발밑 접지 |
| (시트) 프레임 순서 | idle0, idle1, walk0, walk1, eat0, eat1, sleep |

> 파일명 규칙(자동 연결 대상):
> - 고양이: `Assets/Art/Generated/cats/{catId}.png` (예: `cat_cheese.png`)
> - 고양이 시트: `Assets/Art/Generated/cats_sheet/{catId}_sheet.png`
> - 가구: `Assets/Art/Generated/furniture/{furnitureId}.png` (예: `furn_seat_window.png`)
> - 아이콘: `Assets/Art/Generated/icons/{iconId}.png`
> - 배경: `Assets/Art/Generated/bg/floor.png`, `.../wall.png`

---

## 7. 검수 체크리스트 (AI 결과물 → 게임 투입 전)

1. **팔레트 준수**: 위 hex 밖의 색이 있으면 Aseprite 팔레트로 스냅. (특히 순검정 `#000000` 금지 → `#4A362F`)
2. **규격**: 정확히 32×32(아이콘 16×16). 리사이즈로 뭉개지지 않게 정수 배율만.
3. **아웃라인 통일**: 모든 캐릭터·가구 아웃라인이 `#4A362F` 단일색.
4. **배경 투명**: 캐릭터/가구/아이콘은 투명 배경, 배경 타일만 불투명·심리스.
5. **접지 그림자**: 캐릭터·가구 발밑에 반투명 타원 그림자(타일·아이콘 제외).
6. **PixelArtTests 통과**: 절차적 검증(`Assets/Tests/EditMode/PixelArtTests.cs`) 규칙(비어있지 않음·공용 아웃라인 포함·규격)과 어긋나지 않는지 육안 대조.

---

## 8. 요약 규격표

| 리소스 | 크기 | PPU | 배경 | 아웃라인 | 그림자 |
|---|---|---|---|---|---|
| 고양이 8종 | 32×32 | 32 | 투명 | `#4A362F` | 있음 |
| 고양이 시트 | 224×32(7프레임) | 32 | 투명 | `#4A362F` | 있음 |
| 가구 18종 | 32×32 | 32 | 투명 | `#4A362F` | 있음 |
| 배경 타일 2종 | 32×32 | 32 | 불투명·심리스 | 없음 | 없음 |
| UI 아이콘 8종 | 16×16 | 16 | 투명 | `#4A362F` | 없음 |
