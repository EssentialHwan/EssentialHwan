# Unity로 열기 (연결 가이드)

## 0. 준비물
- **Unity 2022.3 LTS** (예: 2022.3.40f1). Unity Hub에서 설치.
  - 다른 2022.3.x 버전이면 `ProjectSettings/ProjectVersion.txt`를 본인 버전에 맞게 바꿔도 됩니다(그대로 열어도 "버전 업그레이드" 안내만 뜸).

## 1. 프로젝트 열기
1. Unity Hub → **Add → Add project from disk**
2. 이 폴더(`여기까페냐옹/UnityProject`)를 선택
3. 처음 열면 패키지 임포트로 수 분 소요.
   - **"Enter Safe Mode?" 팝업이 떠도 당황 금지.** 이 저장소에는 `.meta` 파일이 없어 첫 임포트에서 잠깐 에러가 날 수 있습니다. → **Ignore**를 눌러 계속 임포트하면 Unity가 `.meta`를 자동 생성하고, `Packages/manifest.json`의 패키지가 설치되면서 컴파일이 정상화됩니다.
   - 만약 Safe Mode로 들어갔다면 우상단 **Exit Safe Mode** 후 잠시 기다리면 재컴파일됩니다.

## 2. 컴파일 확인
- 하단 콘솔에 에러 0이면 성공. (경고 몇 개는 무방)
- 필요 패키지는 `manifest.json`에 이미 지정: AI Navigation(NavMesh) · 2D · TextMeshPro · Test Framework 등.

## 3. 콘텐츠·아트 생성 (원클릭)
- 상단 메뉴 **여기까페냐옹 ▸ 1.0 콘텐츠 에셋 생성** → `Assets/Data/`에 고양이·메뉴·가구 SO 생성
- 상단 메뉴 **여기까페냐옹 ▸ 픽셀 아트 생성** → `Assets/Art/Generated/`에 PNG 44개 생성
- `Assets/Localization/strings.csv`를 `Assets/Resources/strings.csv`로 복사(런타임 로드용)

## 4. 씬 배선 → 플레이
- 상세: `../기획/13_M1_씬셋업_가이드.md`
- 요약:
  1. 새 씬 생성
  2. 빈 GameObject에 `S1Bootstrap` 컴포넌트 추가
  3. 인스펙터에 데이터 연결(balance/starterCats/starterMenus/starterSeats/windowSeatData/cafeLevelConfig)
  4. 바닥 Plane + NavMesh Bake, 좌석 오브젝트 배치
  5. **Play** → 치즈냥 스폰·착석·관찰 루프 확인
  6. `F1` DevConsole로 골드/배속 치트하며 빠르게 검증

## 5. 테스트 실행
- **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All** → 76개 통과 확인

## 자주 겪는 문제
| 증상 | 해결 |
|---|---|
| "compilation errors / Safe Mode" | 첫 임포트 정상 현상 → Ignore로 계속. 패키지 설치 후 자동 해결 |
| NavMesh 관련 에러 | Package Manager에서 **AI Navigation** 설치 확인(manifest에 이미 포함) |
| 로컬라이즈 텍스트가 키로 표시 | `strings.csv`를 `Assets/Resources/`로 복사했는지 확인 |
| 메뉴 '여기까페냐옹'이 안 보임 | 스크립트 컴파일 완료 대기(에러 0 후 상단 메뉴 갱신) |
