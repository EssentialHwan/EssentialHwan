# 콘텐츠 저작 가이드 (SO 에셋 생성용 데이터표)

> 아래 표를 그대로 ScriptableObject 에셋으로 만든다. 코드 무수정(태그 기반). id는 코드/세이브와 일치해야 한다.
> **주의**: 취향 4축은 Truth(숨김). 이 표는 개발자만 본다. 게임 UI에 절대 노출 금지.

## 1. CatData (8마리) — 출시 스코프 §C 확정본

| catId | 표시명 | 성격1(즉시) | 성격2(관찰) | foodFavoriteMenuId | seatPreference | facilityPreference | atmospherePreference | spawnWeight |
|---|---|---|---|---|---|---|---|---|
| cat_cheese   | 치즈냥   | Relaxed   | Glutton   | menu_fishcake   | Window  | Soft(쿠션) | Quiet   | 1.2 |
| cat_calico   | 삼색냥   | Curious   | Fickle    | menu_parfait    | Corner  | Play(캣타워) | Lively | 1.0 |
| cat_cow      | 젖소냥   | Social    | Lonely    | menu_tuna_sand  | TwoSeat | Social(장난감) | Lively | 1.0 |
| cat_black    | 까망냥   | Introvert | Quiet*    | menu_milk       | Corner  | Natural(화분) | Quiet | 0.9 |
| cat_tuxedo   | 턱시도냥 | Active    | Playful   | menu_americano  | Sofa    | Play(스크래처) | Lively | 0.9 |
| cat_mackerel | 고등어냥 | Glutton   | Relaxed   | menu_fishcake   | Sofa    | Soft(쿠션) | Warm   | 0.8 |
| cat_siamese  | 샴냥     | Fickle    | Curious   | menu_parfait    | Window  | Play(캣타워) | Luxury | 0.7 |
| cat_gray     | 회색냥   | Lonely    | Social    | menu_tuna_sand  | TwoSeat | Social(장난감) | Warm  | 0.7 |

*까망냥 성격2는 "조용함 선호" 성향 → PersonalityTag.Introvert 중복 대신 행동 노트로 표현(또는 태그 확장).

**의도된 겹침(후반 난이도)**: 치즈냥·고등어냥(생선케이크), 삼색냥·샴냥(파르페), 젖소냥·회색냥(참치샌드) → "어느 신호가 누구 것인지" 구분이 관찰 재미.

## 2. MenuData (8) — 출시 스코프 §E.1

| menuId | 표시명 | price | cost | cookTime | foodTags |
|---|---|---|---|---|---|
| menu_americano   | 아메리카노   | 30 | 8  | 3 | (음료) |
| menu_milk        | 우유         | 30 | 10 | 3 | Warm |
| menu_fishcake    | 생선케이크   | 60 | 20 | 5 | Fish,Sweet |
| menu_parfait     | 딸기파르페   | 70 | 25 | 6 | Fruit,Sweet |
| menu_tuna_sand   | 참치샌드위치 | 80 | 30 | 6 | Fish,Meal |
| menu_catnip_tea  | 캣닢차       | 40 | 12 | 4 | Warm |
| menu_pudding     | 푸딩         | 50 | 18 | 5 | Sweet |
| menu_grilled_fish| 구운생선     | 90 | 35 | 8 | Fish,Meal,Warm |

## 3. FurnitureData — 좌석5/시설6/장식6 (§E.2)

### 좌석
| furnitureId | 표시명 | seatTags | atmosphere | price |
|---|---|---|---|---|
| furn_seat_normal | 일반석 | Normal | — | 80 |
| furn_seat_window | 창가석 | Window,Quiet,OutsideView | Natural+3,Quiet+2 | 200 |
| furn_seat_corner | 구석석 | Corner,Quiet | Quiet+3 | 150 |
| furn_seat_two    | 2인석  | TwoSeat,Social | Lively+2 | 220 |
| furn_seat_sofa   | 소파   | Sofa,Soft | Warm+3,Luxury+1 | 300 |

### 시설
| furnitureId | 표시명 | facilityTags | actions | atmosphere | price |
|---|---|---|---|---|---|
| furn_fac_tower    | 캣타워       | Play,Height,Active | ClimbTop,Watch | Lively+4 | 400 |
| furn_fac_cushion  | 쿠션         | Soft,Relax,Quiet   | Sleep,Knead    | Warm+3,Quiet+2 | 200 |
| furn_fac_toybox   | 장난감바구니 | Play,Social,Active | PlaySolo,PlayWithFriend | Lively+3 | 350 |
| furn_fac_scratch  | 스크래처     | Play,Active        | Scratch        | Lively+1 | 250 |
| furn_fac_plant    | 화분         | Quiet,Natural      | Hide           | Natural+3,Quiet+1 | 180 |
| furn_fac_window   | 창문         | Window,OutsideView,Natural | GazeOut | Natural+2 | 150 |

### 장식 (분위기 기여만)
벽시계(Luxury+1) / 액자(Natural+1) / 러그(Warm+2) / 조명(Warm+1,Luxury+1) / 커튼(Quiet+1) / 화병(Natural+2). 각 60~150 G.

## 4. 단골 에피소드 (8, 고양이당 1) — 3단계 도달 시 짧은 텍스트
포맷: `eventId, catId, trigger=RegularStage>=3, scriptLines[1~2], reward`. 컷신/분기 없음.
예) `ev_cheese_1 / cat_cheese / "치즈냥이 창밖을 보며 '여기가 제일 좋아…'라고 중얼거렸다." / reward=도감스탬프+골드50`
