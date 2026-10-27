# 인스타그램 운영 초안 (v1, 2026-10-27)

> **결정:** 인스타 = **한국어 + 영어**(X = 영어 + 일본어). 주력은 **릴스(세로 1080×1920)**.
> 계정: @winterystudio · 이름 `Wintery Studio · 여기카페냐옹` · 소개 3줄(한/영/1인 개발 · Steam 2027) — 10/27 설정 완료.

## 1. 릴스 파일
PC: `Desktop\YeogiCafe\IG_reels\` · 저장소: `Marketing/ig_assets/`

| # | 올리는 날 | 파일 | 길이 | 화면 문구 | 소리 |
|---|---|---|---|---|---|
| 1 | 1주 수 | `reel01_hello.mp4` | 8초 | 고양이 손님들은 좋아하던 걸 잊어버렸어요 | 아침 BGM + 야옹·트릴 |
| 2 | 1주 토 | `reel02_closing.mp4` | 10.6초 | 마감 전, 마지막 손님이랑 놀아 주기 | 저녁 BGM + 골골 + 작은 야옹 |
| 3 | 2주 수 | `reel03_kitchen.mp4` | 8초 | 바리스타도 고양이라서 앞발로 만들어요 | 점심 BGM + 지글지글 + 조르는 야옹 |

- 다시 만들기: `python3 Marketing/ig_assets/make_reel.py <클립|newcats> <출력> <초> "<한국어 / 줄바꿈>" "<English>" <효과음@초,...> [bgm]`
- 소리는 게임 안 소리(합성음, 골골은 BigSoundBank CC0) — 저작권 문제 없음. 출시 전 소리를 바꾸면 릴스도 다시 뽑기.
- 인스타 음원(유행 음악)을 덧입히면 노출이 더 잘 되는 편 — 올릴 때 '오디오' 단계에서 골라도 됨(게임 소리는 작게).

## 2. 올리는 법 (PC 웹)
1. 왼쪽 **＋ 만들기 → 게시물** → `IG_reels` 폴더에서 영상 선택 → 자르기 화면에서 **원본(9:16)** 그대로
2. 커버: 문구가 다 보이는 3초쯤 장면
3. 아래 글(캡션) 붙여넣기 → **공유**
- 올리는 시간: **한국 저녁 8~10시** (한국 사람이 가장 많이 보는 시간. 영어권은 덤)
- 빈도: 주 2회(수·토)

## 3. 글(캡션) — 한국어 먼저, 영어 아래, 해시태그 마지막

**1. 인사**
```
고양이 손님들은 자기가 뭘 좋아했는지 잊어버렸어요.
어디 앉는지, 뭘 남기는지, 어디서 낮잠 자는지 보고 좋아하던 걸 찾아 주세요 🐾
혼자 만들고 있는 고양이 카페 게임 《여기카페냐옹》이에요.

Your cat guests forgot what they love.
Watch where they sit, what they leave, where they nap — and help them remember 🐾
A cozy cat café game I'm making solo.

#인디게임 #고양이게임 #도트 #픽셀아트 #게임개발 #cozygames #cozygamer #pixelart #indiegame #catgame
```

**2. 마감 놀아주기**
```
마감 직전, 마지막까지 남은 손님이랑 놀아 주는 시간 🐈
좋아하는 곳을 쓰다듬으면 하트가, 싫어하는 곳을 만지면… 흥!

Before closing, you get to play with the last guest.
Pet the right spot for hearts. The wrong spot… hmph.

#인디게임 #고양이게임 #도트 #고양이 #cozygames #cozygamer #pixelart #indiegame #catgame
```

**3. 주방 앞발**
```
이 카페 바리스타도 고양이예요. 그래서 앞발로만 만들어요 🍮
가끔 앞발이 딴짓을 하기도 하고요.

The barista is a cat too. Paws only 🍮
Sometimes the paws get distracted.

#인디게임 #고양이게임 #도트 #게임개발 #cozygames #cozygamer #pixelart #indiegame #catgame
```

## 4. 지킬 것
- 리디자인(새로 그림) 얘기는 하지 않는다 — 보는 사람은 옛 그림을 모르니 의미가 없다
- 새 그림 고양이만 나오는 장면으로(옛 그림 고양이가 보이는 카페 전체 화면은 리디자인 끝난 뒤)
- 댓글엔 전부 답하기(한국어 댓글은 한국어로)
- 아늑한 게임 소개 계정(#cozygamer 큰 계정들)을 팔로우해 두고, 상점 페이지 열리면 데모 키 연락
- 위시리스트 부탁은 상점 페이지 열린 뒤부터(프로필 링크 칸에 스팀 주소)
