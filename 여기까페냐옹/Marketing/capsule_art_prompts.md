# 정식 캡슐 아트 프롬프트 (ChatGPT 이미지용)

## 원칙
- 구성: **왼쪽 위 = 제목 로고 자리(비워 둠)** + **상징 고양이 1마리 크게** + 배경은 카페(통유리창 햇살)
- 게임 속 플레이어는 **앞발만 보인다**(주방·관찰 일지·손도장). 캡슐도 똑같이: 화면 앞쪽에 **플레이어 앞발 + 관찰 일지**, 그 너머에 **상징 손님 고양이 1마리**
  → "고양이 카페 + 관찰 추리"가 한 장에, 게임 안 인상과도 일치(없는 캐릭터를 캡슐에 넣지 않음)
- 상징 손님 = **까망냥(검은 고양이)** 추천: 크림색 카페 배경에서 실루엣이 가장 강해 아주 작게 줄여도, 흐리게 해도 보임
- 앞발 색은 게임 속 앞발 스프라이트와 같게(그림 생성 때 게임 스크린샷을 같이 붙여 넣기)
- 글자는 넣지 않는다 — 제목 로고는 내가 따로 합성
- 게임 속 고양이와 무늬가 같아야 함 → **고양이 리디자인과 같은 ChatGPT 대화방에서** 만들기(스타일이 맞음)
- 도트: UNREASON X 느낌 — 섬세한 픽셀, 색 차이는 부드럽게, 안티앨리어싱 없음

## 1. 헤더 캡슐 (가장 중요 · 920×430 → 1840×860로 만들기)
```
Pixel art key art for a cozy cat café observation/deduction game, wide banner (2.14:1), first-person view.
Foreground, lower right: two small round cat paws (the player's paws) holding an open observation notebook
with a pencil, a few doodle sketches of cats and paw-print checkmarks on the page (no readable letters).
Middle, right of center, large and clearly readable: a chibi black cat customer sitting at a café table
with a cup and a half-eaten fish cake, looking slightly away with a mysterious, picky expression,
tail curled — the guest being observed. A tiny "?" shaped thought bubble above it (no letters).
Background: small wooden café with a big glass front window, warm afternoon sunlight falling across the
wooden floor in window-pane shapes, a cat tower and cushions softly visible.
Keep the upper-left third calm and empty (soft wall and window light) for a title logo to be added later.
Style: detailed high-resolution pixel art, crisp pixels, no anti-aliasing, gentle low-contrast palette:
cream walls, warm wood, soft pink and mint accents, golden sunlight. Strong clear silhouette of the black cat
so it reads even when tiny. No text, no letters, no UI, no watermark.
```

## 2. 세로 캡슐 (748×896 → 1496×1792)
```
Same scene, same black cat customer and same player paws, re-composed vertically (5:6).
Paws with the notebook at the bottom, the black cat at a window table in the middle, large.
Glass window and sunbeams above; upper third calm for a title logo. Same style, no text.
```

## 3. 메인 캡슐 (1232×706 → 2464×1412)
```
Same scene, re-composed at 1.75:1. Player paws with notebook lower right; the black cat large in the middle;
two more customer cats smaller in the background (a calico on a window seat, an orange tabby on the cat tower).
Sunbeams on the floor, upper-left calm for the title logo. Same style, no text.
```

## 4. 라이브러리 히어로 (3840×1240, 글자 없음)
```
Same café, very wide panorama (3.1:1), no paws: the glass front window with sunlight, cat tower, cushions,
counter with cups; a few cats napping in sunbeams near the left and right edges.
Center kept simple (the game logo will float there). Same style, no text.
```

## 5. 작은 캡슐 확인용
작은 캡슐(462×174)은 헤더를 줄여서 만든다. 줄였을 때 **검은 고양이 + 앞발·일지**가 알아보이면 성공.

## 받아 오면 내가 할 것
1. 규격 크기로 자르기·줄이기
2. 제목 로고 합성(한·영)
3. 자가 테스트 3종 이미지 만들기: 블러 / 암전 / 경쟁작 5개 옆에 놓기(그리드)
