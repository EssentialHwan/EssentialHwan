# X(트위터) 운영 초안 (v2, 2026-10-27)

> **결정:** X 계정 하나, **영어 메인 + 일본어 한 줄.** 한국어·중국어는 X에 쓰지 않는다.
> - 한국: 상점 페이지 공개·데모 공개 때 국내 인디게임 커뮤니티에 공지글
> - 중국: 상점 페이지 중국어로 충분. 출시가 가까워지면 빌리빌리 영상 1개 정도 검토
> - 글보다 **GIF가 주인공.** 글은 두 줄이면 된다.

---

## 1. 처음 한 번 세팅 (20분)
- **이름:** `Wintery Studio · Purrsonal Taste`
- **아이디:** `@winterystudio` (이미 있으면 `@wintery_studio`)
> 이미지는 모두 `Marketing/x_assets/` 폴더에 있다. ✅ = 이미 만들어 둠 · ⏳ = Claude가 만들 예정

| X 화면의 자리 | 넣을 파일 | 크기 | 상태 |
|---|---|---|---|
| 프로필 → **프로필 사진**(동그라미) | `x_assets/profile_400x400.png` (눈 위를 걷는 흰 고양이 로고) | 400×400 | ✅ |
| 프로필 → **헤더 사진**(맨 위 가로 배너) | `x_assets/header_1500x500.png` (햇살 드는 카페 그림) | 1500×500 | ✅ |

- 넣는 곳: X 왼쪽 메뉴 **프로필 → 프로필 수정** → 위쪽 배너 칸 카메라 아이콘 = 헤더, 동그라미 카메라 아이콘 = 프로필 사진
- **링크 칸:** 상점 페이지 전엔 비워 두기 → 공개되면 스팀 페이지 주소
- **소개(bio):**
```
A cozy cat café where you deduce what your feline guests secretly love 🐾
猫のお客さんの「好き」を観察で当てる、ちいさなカフェのゲーム。
Solo dev · Wintery Studio · Steam 2027
```

## 2. 글 하나의 모양
```
[영어 한 줄 — 가능하면 질문형]
[일본어 한 줄]

#indiedev #pixelart #インディーゲーム
```
+ GIF 1개 (3~6초, 고양이 한두 마리 확대 위주)

- **질문형이 좋다.** "왜 ~할까?"에는 답글이 달리고, 답글이 달리면 노출이 늘어난다.
- **해시태그는 3개.** 토요일엔 #ScreenshotSaturday를 추가한다(#pixelart 대신).
- **본문에 링크 금지.** 링크가 있으면 노출이 줄어든다. 링크는 내 글에 단 첫 답글에.

## 3. 올리는 시간·빈도
- **한국 시간 밤 10시~자정** = 미국 동부 아침, 일본 밤 10시~자정. 두 곳을 다 잡는 시간.
- 그 시간에 직접 못 올리면 X **예약 게시**(글쓰기 창의 달력 아이콘).
- **주 2~3회:** 화·목 밤 10시 + 토요일(#ScreenshotSaturday).

## 4. 제일 중요한 것: 다른 개발자 글에 반응하기 (하루 10분)
- #indiedev #cozygames #pixelart #インディーゲーム 태그에서 비슷한 아늑한 게임에 **구체적인 답글**을 단다. 예: "Love the lighting — how did you do the window glow?"
- 토요일 #ScreenshotSaturday 글타래에 참여해 서로 리트윗한다.
- 내 글에 달린 답글엔 **전부** 답한다(일본어 답글엔 일본어로. 필요하면 Claude에게 번역 요청).

---

## 5. 첫 2주 글 6개 (화·목·토)

| # | 올리는 날 | 첨부할 파일 (`Marketing/x_assets/`) | 무엇이 보이나 | 상태 |
|---|---|---|---|---|
| 1 | 1주 화 | `post01_hello.gif` | 새 그림 고양이 4마리가 타이틀 화면을 걸어다님 | ⏳ |
| 2 | 1주 목 | `post02_window.gif` | 치즈냥이 들어와 창가석을 골라 앉음 | ⏳ |
| 3 | 1주 토 | `post03_before_after.png` | 까망냥 옛 그림(왼쪽) → 새 그림(오른쪽) | ⏳ |
| 4 | 2주 화 | `post04_closing.gif` | 마감 놀아주기: 쓰다듬기 → 싫은 곳 만지면 "흥" | ⏳ |
| 5 | 2주 목 | `post05_kitchen.gif` | 주방 1인칭 앞발이 요리하다 딴짓 | ⏳ |
| 6 | 2주 토 | `post06_rain.gif` | 비 오는 통창 앞에서 조는 고양이 | ⏳ |

- 첨부 방법: 글쓰기 창 아래 **사진 아이콘(🖼)** → 파일 선택. GIF는 GIF 그대로 올리면 자동 재생된다(한 글에 GIF는 1개만).
- 모든 GIF는 새 그림 고양이만 나오게 찍는다. 옛 그림 고양이는 화면에 넣지 않는다.

**1. 인사** — 첨부: `x_assets/post01_hello.gif`
```
Hi! I'm making a cozy cat café game solo — your guests are cats, and you have to figure out what they secretly love 🐾
はじめまして！猫がお客さんのカフェゲームをひとりで作っています。猫たちの「好き」を当ててあげてください🐾

#indiedev #pixelart #インディーゲーム
```

**2. 창가 손님** — 첨부: `x_assets/post02_window.gif`
```
Why does this guest always pick the window seat?
このお客さん、どうしていつも窓ぎわなんだろう？

#indiedev #pixelart #インディーゲーム
```

**3. 리디자인 전 → 후** (가장 잘 퍼질 글) — 첨부: `x_assets/post03_before_after.png`
```
Before → after. Redrawing all the cat guests one by one 🐈‍⬛
リデザイン前 → 後。お客さんの猫を一匹ずつ描き直しています🐈‍⬛

#indiedev #pixelart #インディーゲーム
```

**4. 마감 놀아주기** — 첨부: `x_assets/post04_closing.gif`
```
Before closing, you get to play with the last guest. Just… don't touch the wrong spot.
閉店前は、最後のお客さんと遊べます。…さわる場所には気をつけて。

#indiedev #pixelart #インディーゲーム
```

**5. 주방 앞발** — 첨부: `x_assets/post05_kitchen.gif`
```
The barista is a cat too. Sometimes it gets distracted.
バリスタも猫なので、ときどき気が散ります。

#indiedev #pixelart #インディーゲーム
```

**6. 비 오는 날 (토요일)** — 첨부: `x_assets/post06_rain.gif`
```
Rainy days make everyone sleepy ☔
雨の日は、みんな早めにうとうと☔

#ScreenshotSaturday #indiedev #インディーゲーム
```

## 6. 상점 페이지가 열리면 (고정 글 교체)
```
Your cat guests don't remember what they like.
Watch where they sit, what they leave, where they nap — and help them find it again 🐾
猫のお客さんは、自分の「好き」を忘れています。
座る場所、残すもの、お昼寝の場所を見て、思い出させてあげてください🐾

Wishlist on Steam ↓ (링크는 첫 답글)
#indiedev #cozygames #インディーゲーム
```
첨부: `x_assets/pinned_loop.gif` ⏳ (손님 입장 → 자리 고르기 → 일지에 단서 → 정답 하트, 10초 안)
- 고정 방법: 글 오른쪽 위 **⋯ → 프로필에 고정하기**
- 세계관 정본(기획/15)에 맞춰 "모른다"를 "**잊었다 → 되찾는다**"로 바꿨다.

---

## 7. 지킬 것
- **새 그림 고양이만 화면에 나오게.** 옛 그림이 섞이면 그게 첫인상이 된다. 카페 전체 화면은 리디자인이 끝난 뒤에 쓴다.
- **위시리스트 부탁은 상점 페이지가 열린 뒤부터.**
- 밈이나 유명 캐릭터 패러디는 금지(권리 문제).
- 비교작 이름은 칭찬으로만 쓴다("inspired by …").

## 8. 역할
- **형:** 계정 만들기 · 글 올리기 · 답글 달기 · 하루 10분 다른 개발자 글에 반응하기
- **Claude:** GIF와 전/후 이미지 만들기(데브콘솔로 장면 연출 → 녹화 → GIF) · 영어·일본어 문장 다듬기 · 일본어 답글 번역
