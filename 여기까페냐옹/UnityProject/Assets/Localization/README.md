# Localization

- `strings.csv` — 한/영 문자열 (문서 07). `key,ko,en` 스키마.
- **Unity 설정**: `L.EnsureLoaded()`가 `Resources.Load<TextAsset>("strings")`를 쓰므로,
  런타임 로드하려면 이 CSV를 **`Assets/Resources/strings.csv`** 로 복사(또는 이 폴더를 Resources로)해야 한다.
  (에디터 테스트는 `L.Load(csvText)`로 직접 주입하므로 무관.)
- 사용: `L.Get("ui.settle.title")`, `L.Get("ui.discover.toast", ("name", catName))`.
- 언어 전환: `L.Current = Language.En;` (설정 메뉴에서).
- 신규 언어(일/중): CSV에 열 추가 + `Language` enum 확장 + 폰트 확인.
