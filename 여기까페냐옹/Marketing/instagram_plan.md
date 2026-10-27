# 인스타그램 운영 초안 (v2, 2026-10-27)

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
1. 왼쪽 **＋ 만들기 → 게시물** → `IG_reels` 폴더에서 영상 선택 → 자르기 화면 왼쪽 아래 비율 아이콘 → **원본** 선택(기본값이 1:1이라 꼭 바꾸기!)
2. 커버: 문구가 다 보이는 3초쯤 장면
3. 아래 글(캡션) 붙여넣기 → **공유**
- 올리는 시간: **한국 저녁 8~10시** (한국 사람이 가장 많이 보는 시간. 영어권은 덤)
- 빈도: 주 2회(수·토)

## 3. 글(캡션) — 점장 고양이 말투("~냥"), 한국어 먼저 · 영어 아래 · 해시태그 마지막
> 말하는 사람 = 카페 점장 고양이(=플레이어). 게임 설명을 "내 일기"처럼. 영어는 끝에만 가볍게 "meow".

**1. 인사** (`v2_reel01_hello.mp4`)
```
어서 오라냥! 여기는 고양이 손님만 오는 작은 카페다냥 ☕
그런데 손님들이 자기가 뭘 좋아했는지 다 잊어버렸다냥…
어디 앉는지, 뭘 남기는지, 어디서 낮잠 자는지 보고 좋아하던 걸 찾아 주라냥 🐾

Welcome! This little café only serves cat guests ☕
But they've all forgotten what they love…
Watch where they sit, what they leave, where they nap — and help them remember, meow 🐾

#인디게임 #고양이게임 #카페게임 #카페경영 #도트 #indiegame #indiedev #cozygames #cozygamer #pixelart #catgame
```

**2. 마감 놀아주기** (`v2_reel02_closing.mp4`)
```
영업 끝나기 직전, 마지막까지 남은 손님이랑 노는 시간이다냥 🐈
좋아하는 데를 쓰다듬으면 하트가 뿅, 싫어하는 데를 만지면… 흥! 하고 삐진다냥.
손님마다 좋아하는 곳이 다르니까 잘 기억해 두라냥.

Closing time — I get to play with the last guest 🐈
Pet the right spot and hearts pop out. The wrong spot… hmph.
Every guest likes something different, so remember well, meow.

#인디게임 #고양이게임 #카페게임 #도트 #indiegame #indiedev #cozygames #cozygamer #pixelart #catgame
```

**3. 주방 앞발** (`v2_reel03_kitchen.mp4`)
```
이 카페 점장인 나도 고양이라냥.
그래서 두 손이 다 냥손이다냥 🐾
요리하다가 나도 모르게 그루밍하고, 쭙쭙이하고, 꾹꾹이까지 해 버려서… 가끔 주문이 밀린다냥.
이해해 주라냥…ㅎ

I run this café, and I'm a cat too — so my hands are paws 🐾
Sometimes I catch myself grooming, suckling on a paw, or kneading mid-order… and the orders pile up.
Please be patient with me, meow 🍮

#인디게임 #고양이게임 #카페게임 #카페경영 #도트 #indiegame #indiedev #cozygames #cozygamer #pixelart #catgame
```

## 4. 지킬 것
- 리디자인(새로 그림) 얘기는 하지 않는다 — 보는 사람은 옛 그림을 모르니 의미가 없다
- 새 그림 고양이만 나오는 장면으로(옛 그림 고양이가 보이는 카페 전체 화면은 리디자인 끝난 뒤)
- 댓글엔 전부 답하기(한국어 댓글은 한국어로)
- 아늑한 게임 소개 계정(#cozygamer 큰 계정들)을 팔로우해 두고, 상점 페이지 열리면 데모 키 연락
- 위시리스트 부탁은 상점 페이지 열린 뒤부터(프로필 링크 칸에 스팀 주소)
