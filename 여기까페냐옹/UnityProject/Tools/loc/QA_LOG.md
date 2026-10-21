# 현지화 표시 QA 기록 (2026-10-22)

## 방법
- 개발 콘솔(F1) → **[UI 전체 점검(현재 언어) → Build/uicheck]**
  - 31개 화면을 자동으로 열고 스크린샷을 찍는다: HUD, 의뢰, 상점 4탭, 도감 3탭, 설정, 주방, 메뉴 고르기, 정산, 추리, 단골·졸업 편지, 냥슐랭 결과, 마감 놀이, 엔딩, 오프닝 1~5, 안내 카드 5단계와 좁은 모드, 배치 모드.
  - 화면마다 `TextFitGuard.Report()`를 남긴다(`Build/uicheck/<lang>_NN_<화면>.png`, `textfit_<lang>.txt`).
- 게임 화면을 16:9(1612×907)로 맞추고 ko/en/zh/ja 네 언어를 돌렸다. 네 언어 화면을 2×2로 붙여 눈으로 비교했다.
- 1차 점검 → 수정 → 2차 점검 순서로 진행했다.
  - 2차 결과: FAIL 0건.
  - 남은 SHRUNK(자동 축소)는 1px 수준이거나, 정산 중 좁은 안내 카드처럼 의도한 축소뿐이다.

## 판단 기준
1. **번역문을 현지 표현으로 줄일 수 있으면 글을 고친다.** 버튼·탭·카드 제목처럼 칸이 고정된 곳은 짧고 자연스러운 현지 표현을 우선한다.
2. **칸에 여유가 있으면 레이아웃을 넓힌다.** 다른 요소와 겹치지 않는 범위에서만 넓힌다.
3. **위 두 가지가 모두 어려울 때만 글자를 줄인다.**
   - 한 줄 라벨은 `horizontalOverflow=Overflow`로 두고, TextFitGuard가 1~2px만 줄이게 한다.
   - 두 줄로 접히는 것보다 1px 작은 한 줄이 더 읽기 좋다고 판단했다.
4. **중국어·일본어 금칙 처리.** 줄 머리에 `。、，！？）」ー…` 같은 문장부호가 오면 한 글자 앞에서 줄을 꺾는다. 줄 끝에 여는 괄호가 오는 경우도 같다.

## 글(번역) 수정 — Tools/loc/tr6.py
| 키 | 언어 | 전 → 후 | 이유 |
|---|---|---|---|
| opt.btn | en | Options → Menu | 64px 버튼에서 두 줄("Option/s")로 접힘. 새 게임·종료도 들어 있는 창이라 Menu가 맞다 |
| ui.quest.btn / title / quest.done | en | Requests → Tasks | "Requests 0/3"이 두 줄로 접힘 |
| ui.pause.catalog / ui.catalog.title / title.hint | en | Field Guide → Journal / Cat Journal | HUD 104px 버튼에서 두 줄로 접힘. '관찰 일지' 톤과도 맞다 |
| ui.shop.tab.recipe / furnup | en | Recipe/Furniture Upgrades → Recipes ★ / Furniture ★ | 12px까지 줄어듦. ★는 게임 안 강화 표시와 같다 |
| ui.tool.tabhint | en | 짧게 | 두 줄로 접혀 아래가 잘림 |
| ui.furnup.uses | en | Used {n} times → Uses: {n} | "Used 1 times" 문법 오류 |
| ui.axis.facility / ui.shop.type.facility / narr.facility.generic / tut.settle | en | Facility → Spot | 도감 칸에서 11px까지 줄어듦. 일본어 'スポット'와 같은 뜻으로 맞춤 |
| tut.settle | en | `food · seat · spot` → `favorite food, seat, and spot` | 줄 머리에 '·'가 걸려 어색함 |
| ui.tool.pick.title / vague | en | `{tool}: what to make?` / `Hint orders: {n} — …` | 제목이 닫기(×)와 겹침. "1 hint orders" 복수 오류 |
| ui.guess.title | en | What Food does X like? → What does X like? (Food) | 대문자 축 이름이 문장 가운데 들어감 |
| nyan.report.pass | en | Keep it up. | 두 줄로 접혀 별과 붙음 |
| ui.rel.known / bicker | en | Acquainted/Squabbling → Familiar/Bickering | 관계도 태그가 11px로 줄어듦 |
| ui.settle.stamp | ja | 今日のスタンプ → 今日のハンコ | 104px 도장 칸을 넘침 |
| furn.* 이름 | en | Painting, Nook Chair, Houseplant, Window Seat, Heat Pad, Auto Wand | 상점 카드 이름이 13~15px까지 줄어듦 |
| furn.* 이름 | ja | ホットマット, 電動じゃらし, ネコ回し車 | 같은 이유(12~14px) |
| ep.cat_mackerel.* | en/ja | 가구 이름 변경 반영 | 대사와 가구 이름을 맞춤 |
| cat.cat_mackerel.name | en | Mackerel → Tabby | 졸업 편지 서명은 'Tabby'인데 이름은 'Mackerel'로 서로 달랐다. 영어 이름으로도 Tabby가 자연스럽다 |

## 코드(레이아웃) 수정
- **HUD**
  - 의뢰 버튼을 100→112px로 넓히고, 설정 버튼을 오른쪽으로 옮겼다.
  - 모든 HUD 버튼 라벨을 한 줄로 고정했다(넘치면 1~2px 자동 축소).
- **상점 탭**: 기존 FitOneLine을 유지했다.
- **정산 일지**
  - 고양이 이름은 104px에 한 줄로 맞춘다(최소 13px).
  - 기록된 칸은 축 이름 없이 값만 보여 준다(체크 + 'Fish Cake'). 축 이름까지 붙이면 11px까지 줄었다.
  - 도장 칸 제목을 한 줄로 맞췄다.
  - 안내 문구를 가운데 정렬하고 안쪽 여백을 넣었다.
- **도감**: 축 라벨을 40→52px로 넓혔다('スポット'를 13px 그대로 표시). 아이콘 위치도 함께 옮겼다.
- **안내 카드**
  - 글 칸을 태그 아래로 분리했다. 세 줄이면 태그와 겹치던 문제를 고쳤다.
  - 카드 높이를 글 길이에 맞춰 96~150px로 자동 조절한다.
- **버튼 폭**: 마감 '잘 가 또 와!' 200→236, 엔딩 '계속 영업하기' 220→260. 영어 문구가 테두리를 넘던 문제다.
- **메뉴 고르기 창**: 제목 칸을 닫기 버튼에서 떨어뜨렸다.
- **배치 모드**: 안내 글에 왼쪽 여백 30px를 넣었다.
- **금칙 처리**
  - `TextFitGuard.Kinsoku()`를 추가해 zh/ja의 줄 머리 문장부호를 처리한다. 리치 텍스트와 한 줄 라벨은 제외한다.
  - 오프닝은 타자 효과 중 줄이 튀지 않게 문장 전체에 먼저 적용한다.
  - 확인 사례: 중국어 졸업 편지에서 "…这些你都懂。"의 '。'가 줄 머리로 떨어지던 문제가 해결됐다.
- **리뷰 다국어화(버그)**
  - 문제: 리뷰를 쓴 순간의 언어로 완성된 문장만 저장해서, 언어를 바꾸면 다른 언어 문장이 섞여 보였다.
  - 수정: 이제 `parts`("문장키|인자=@번역키")로 저장하고 볼 때 현재 언어로 다시 그린다.
  - 구버전 리뷰는 문장에 쓰인 글자로 언어를 추정해, 같은 언어 화면에서만 보인다.
- **missing key 경고 정리**: 리뷰 변형 문장이 없는 키를 조회할 때 'missing key' 경고가 찍혀서 `L.Has()`를 추가했다.

## 남은 것(의도적으로 둔 것)
- **글자 크기 차이**
  - 중국어 픽셀 폰트(12px 설계)는 같은 크기에서도 글자가 조금 작아 보인다. 읽는 데는 지장이 없다.
  - 'キャットタワー'와 'スタンドライト'는 17→16px로 1px 줄어든다. 널리 쓰이는 이름이라 바꾸지 않았다.
- **관계도 이름 겹침**: 13마리를 원형으로 배치해서, 가까운 고양이끼리 이름표가 조금 겹친다. 언어와 무관한 배치 문제다.
- **정산 중 좁은 안내 카드(196px)**: 14~15px로 줄어든다. 의도한 축소다.

## 2026-10-22 테마·제철 / 보호소 / 포토 화면 4개 언어 점검
- UI 투어에 shop4(테마·제철), shelter, photo_live/edit/album 추가(총 36장).
- 테마·제철 카드의 3번째 줄 안내(ui.theme.hint, ui.season.hint)가 140px 칸을 넘쳐 버튼 밑으로 깔림 → 카드에서는 빼고 하단 안내 한 줄로만 표시.
- en "Returns to the menu in {season}" 넘침 → "Served in {season}". ko 도구 필요 문구 "{tool} 필요"로 축약.
- zh/ja 새 화면 FAIL 0.
- DevConsole +1000 Gold가 오늘 매출로 잡혀, 영업 중 종료 시 되돌림 규칙에 걸려 사라지던 문제 → SetGold로 변경(매출 미포함).
