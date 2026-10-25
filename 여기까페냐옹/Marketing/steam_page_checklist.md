# 스팀 상점 페이지 입력 순서 (앱 ID 나오면 이 순서대로)

> 스팀웍스 상점 페이지 편집 화면 기준. 이미지 규격은 2024년 개정 기준 — 업로드 화면 안내와 다르면 화면 안내를 따른다.

## 1. 기본 정보
- 이름: 여기카페냐옹 (영문 Purrsonal Taste / 일 ここカフェですかニャ？ / 중 这里是咖啡馆喵？ — 언어별 이름 칸)
- 개발사·배급사: **Wintery Studio** (둘 다 같은 이름, 자체 배급). 닉네임 '겨울'에서
- 출시일: "Coming soon" + 대략 시기 표시(예: 2027년) — 확정 전엔 날짜 대신 시기
- 지원 언어: 한국어·영어·일본어·중국어(간체) — 인터페이스·자막 체크(음성 없음)
- 가격: $12.99(출시 직전 설정)

## 2. 문구 → `steam_store_copy.md`
- 짧은 설명(300자 이내): **A안(관찰 강조) 확정**
- 상세 설명: 언어별로 그대로 붙여넣기(■ 머리는 스팀 [h2] 태그로 바꿔도 됨)

## 3. 태그(최대 20, 상위 5개가 중요)
1 Cozy · 2 Cats · 3 Management · 4 Pixel Graphics · 5 Cute
그다음: Simulation · Casual · Relaxing · Wholesome · Detective · Singleplayer · 2D · Atmospheric · Colorful · Cooking · Life Sim

## 4. 이미지 (임시안: `capsules_draft/`, 게임 화면 원본 배율로 자르고 간판만 올린 것)
| 칸 | 규격 | 임시안 파일 | 정식 아트 우선순위 |
|---|---|---|---|
| 헤더 캡슐 | 920×430 | header_920x430_ko/en | ★★★ 검색·추천에 가장 많이 보임 |
| 작은 캡슐 | 462×174 | small_462x174_ko/en | ★★★ 글자가 작아도 읽혀야 함 |
| 메인 캡슐 | 1232×706 | main_1232x706_ko/en | ★★ 첫 화면 추천 |
| 세로 캡슐 | 748×896 | vertical_748x896_ko/en | ★★ 시즌 세일 |
| 페이지 배경 | 1438×810 | page_bg_1438x810 | ★ 은은하게 |
| 라이브러리 캡슐 | 600×900 | library_600x900_ko/en | ★ |
| 라이브러리 히어로 | 3840×1240 | library_hero_3840x1240 | ★ 글자 없음 |
| 라이브러리 로고 | 1280×720 투명 | (없음 — 간판 로고를 투명 PNG로 만들 것) | ★ |
| 스크린샷 | 1920×1080, 5장 이상 | `screenshots_v1/` | ★★★ 첫 4장이 중요 |

### 스크린샷 순서 제안
1. 비 오는 저녁 + 천장 조명(분위기) 2. 맑은 아침 손님 여섯 + 바닥 햇살 3. 주방 앞발 4. 마감 쓰다듬기 클로즈업(촬영 예정) 5. 밤 관찰 일지(추리) 6. 관계도 7. 여름 창밖

## 5. 정식 캡슐 아트 AI 프롬프트(헤더 기준, 도트 유지)
```
Cozy pixel-art key art for a cat café game. Front view of a small wooden café with a big glass window,
warm afternoon sunlight falling across the wooden floor in window-pane shapes. Five chibi cats with
distinct coats (black, calico, orange tabby, tuxedo, white) sitting on chairs and a cat tower, one cat
kneading a cushion. Soft low-contrast palette, cream walls, pendant lamps glowing. Leave the upper-center
third empty for a title sign. 2D pixel art, crisp 4x pixels, no anti-aliasing, no text.
```
- 만든 뒤 제목 간판은 게임 글꼴(갈무리)로 직접 올린다(AI 글자 금지).
- 고양이는 반드시 게임 속 고양이와 같은 무늬로(상점 그림과 게임이 다르면 신뢰가 깨짐).
