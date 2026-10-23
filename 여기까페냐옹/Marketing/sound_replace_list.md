# 사운드 교체 목록 (v1, 2026-10-24)

지금 게임에는 **사운드 시스템과 51개 자리**가 이미 있다. 들어 있는 소리는 코드로 합성한 임시음.
→ 같은 파일 이름(.ogg)으로 `Assets/Resources/Audio/`에 덮어쓰기만 하면 바로 바뀐다. 코드 수정 없음.

## 1순위 — 데모 인상에 직접 영향 (먼저 구하기)
| 파일 | 쓰이는 곳 | 원하는 느낌 |
|---|---|---|
| bgm_0_0 / bgm_1_0 / bgm_2_0 | 아침·점심·저녁, 맑음 | 로파이·어쿠스틱 카페, 60~90초 루프. 저녁은 느리고 따뜻하게 |
| bgm_night | 정산(밤) | 오르골·피아노, 조용히 |
| amb_rain | 비 오는 날 배경 | 창밖 빗소리(실내에서 듣는 느낌), 루프 |
| sfx_meow_0~2, sfx_meow_small, sfx_meow_demand | 손님 울음·재촉 | 실제 고양이 울음. 짧게(0.3~0.8초) |
| sfx_purr | 쓰다듬기·만족 | 골골송 루프 1~2초 |
| sfx_bell | 손님 입장(문 종) | 가게 문 종 |
| sfx_coin | 결제 | 작고 귀여운 동전 |
| sfx_click / sfx_boop | UI 버튼 / 알림 | 부드러운 톡 |

## 2순위 — 주방·놀이
sfx_pour(음료) · sfx_sizzle(조리) · sfx_chime(완성) · sfx_knead(꾹꾹이) · sfx_suck(쭙쭙이) · sfx_lick(그루밍) · sfx_stamp(손도장) · sfx_eat_0/1(먹기)

## 3순위 — 고양이 행동
sfx_jump · sfx_land · sfx_climb · sfx_descend · sfx_scratch · sfx_wheel · sfx_box · sfx_pounce · sfx_swat · sfx_zoom(우다다) · sfx_hiss · sfx_chatter · sfx_trill/_1 · sfx_mrrow · sfx_mrrng · sfx_yawn · sfx_catch · sfx_scurry · sfx_squeak/_2(쥐)

## 나머지 BGM (흐림·비 버전)
bgm_0_1, bgm_1_1, bgm_2_1(흐림) · bgm_0_2, bgm_1_2, bgm_2_2(비) — 1순위 곡의 편곡(악기 줄이기·필터)으로 충분. 같은 곡을 복사해 둬도 됨.

## 구하는 방법(형이 고를 것)
- **효과음:** 상업 이용 가능한 라이선스만(CC0 또는 로열티 프리). 출처·라이선스를 이 파일 아래에 꼭 적어 두기 — 스팀 출시 때 필요.
- **BGM:** 로열티 프리 음원 구매 또는 작곡 의뢰. AI 작곡 도구를 쓰면 그 도구의 상업 이용 조건 확인.
- 고양이 울음은 직접 녹음(지인 고양이)도 좋다 — 게임 개성이 된다.

## 라이선스 기록
| 파일 | 출처 | 라이선스 |
|---|---|---|
| | | |
