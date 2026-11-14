using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YeogiCafe.EditorTools
{
    // Windows 실행 파일(.exe) 빌드. 결과는 프로젝트 옆 Build/YeogiCafe_Windows/ 에, 로그는 Build/build_log.txt 에 남는다.
    public static class BuildTool
    {
        const string ExeName = "YeogiCafe.exe";

        [MenuItem("여기카페냐옹/■ Windows 빌드 (exe)", priority = 200)]
        public static void BuildWindows() => Build(false);

        // 체험판(넥스트 페스트 데모): 봄 1~7일차, 끝나면 위시리스트 안내. 결과는 Build/YeogiCafe_Demo/ (스팀 데모 앱에 그대로 올린다)
        [MenuItem("여기카페냐옹/■ 체험판 빌드 (데모 exe)", priority = 201)]
        public static void BuildDemo() => Build(true);

        // 휴대폰 테스트 빌드(안드로이드 APK) — PC 빌드와 따로 관리: Build/YeogiCafe_Android/, 앱 ID도 .test 로 따로.
        // 가로 화면 고정, ARM64(IL2CPP). 끝나면 편집기를 다시 Windows 대상으로 돌려놓는다.
        [MenuItem("여기카페냐옹/■ 휴대폰 테스트 빌드 (안드로이드 apk)", priority = 202)]
        public static void BuildAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("휴대폰 테스트 빌드",
                    "이 Unity에는 안드로이드 빌드 모듈이 없어요.\nUnity Hub → 설치 → 2022.3.40f1 ⚙ → 모듈 추가 → Android Build Support(OpenJDK, Android SDK & NDK 포함)를 설치한 뒤 다시 눌러 주세요.", "확인");
                return;
            }
            Branding.Apply();
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build"));
            string outDir = Path.Combine(root, "YeogiCafe_Android");
            Directory.CreateDirectory(outDir);

            PlayerSettings.productName = "여기카페냐옹";
            PlayerSettings.companyName = "Wintery Studio";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.essentialhwan.yeogicafe.test");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            EditorUserBuildSettings.buildAppBundle = false;

            var scenes = EditorBuildSettings.scenes.Where(sc => sc.enabled).Select(sc => sc.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/Cafe.unity" };
            string apk = Path.Combine(outDir, $"YeogiCafe_test_v{PlayerSettings.bundleVersion}.apk");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = apk, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android, options = BuildOptions.None,
            });
            var s = report.summary;
            string log = $"result={s.result}\nerrors={s.totalErrors}\nsizeMB={s.totalSize / 1048576f:0.0}\ntime={s.totalTime}\nout={apk}\n";
            foreach (var step in report.steps)
                foreach (var m in step.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception) log += "ERR: " + m.content + "\n";
            File.WriteAllText(Path.Combine(root, "build_log_android.txt"), log);
            Debug.Log("[BuildTool] " + log);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);   // PC 작업으로 복귀
            if (s.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(apk);
        }

        // 스토브 빌드(11/02): 스토브 PC SDK를 넣은 판매용 빌드. YEOGI_STOVE 기호를 켜고, 스토브 Unity 모듈 요구대로 64비트 IL2CPP.
        //  필요: ① Assets/Plugins/StovePCSDK/ 에 PCSDK_Studio_net46_CSharp_x64 압축 푼 파일 ② Resources/Stove/stove_config.json
        //        ③ Unity Hub 'Windows Build Support (IL2CPP)' 모듈 + Visual Studio 'C++를 사용한 데스크톱 개발'
        [MenuItem("여기카페냐옹/■ 스토브 빌드 (스토브 SDK 포함)", priority = 203)]
        public static void BuildStove() => BuildStoveVariant(false);
        [MenuItem("여기카페냐옹/■ 스토브 체험판 빌드", priority = 204)]
        public static void BuildStoveDemo() => BuildStoveVariant(true);

        // 11/12: 0.9.9 = 사운드 전면 교체·연필 ○ 추리·취향 하트·누끼 정리·세이브 안전장치·STOVE 통계·영어 다듬기
        //  (예전엔 스토브 빌드가 버전을 안 넣어서 마지막 일반 빌드 버전을 따라갔음 → 두 빌드 모두 여기서 넣는다)
        public const string Version = "0.9.9";

        static void BuildStoveVariant(bool demo)
        {
            PlayerSettings.productName = "여기카페냐옹";
            PlayerSettings.companyName = "Wintery Studio";
            PlayerSettings.bundleVersion = Version;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build"));
            string cfgName = demo ? "stove_config_demo.json" : "stove_config.json";   // 체험판은 별도 상품 키
            if (!File.Exists(Path.Combine(Application.dataPath, "Resources", "Stove", cfgName)))
            { EditorUtility.DisplayDialog("스토브 빌드", $"Assets/Resources/Stove/{cfgName} 이 없어요.\n(게임 ID·앱 키 파일)", "확인"); return; }
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Plugins", "StovePCSDK")))
            { EditorUtility.DisplayDialog("스토브 빌드", "Assets/Plugins/StovePCSDK 폴더가 없어요.\n스토브 개발자 사이트의 Unity용 PC SDK(x64)를 풀어 넣어 주세요.", "확인"); return; }
            Branding.Apply();
            string outDir = Path.Combine(root, demo ? "YeogiCafe_Stove_Demo" : "YeogiCafe_Stove");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);
            var prevBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
            var scenes = EditorBuildSettings.scenes.Where(sc => sc.enabled).Select(sc => sc.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/Cafe.unity" };
            var defs = demo ? new[] { "YEOGI_STOVE", "YEOGI_DEMO" } : new[] { "YEOGI_STOVE" };
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes, locationPathName = Path.Combine(outDir, ExeName), target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None, extraScriptingDefines = defs,
                });
            }
            finally { PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, prevBackend); }   // 친구 테스트·일반 빌드는 그대로 Mono
            var s = report.summary;
            string log = $"result={s.result}\nerrors={s.totalErrors}\nsizeMB={s.totalSize / 1048576f:0.0}\ntime={s.totalTime}\nout={outDir}\n";
            foreach (var step in report.steps)
                foreach (var m in step.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception) log += "ERR: " + m.content + "\n";
            File.WriteAllText(Path.Combine(root, demo ? "build_log_stove_demo.txt" : "build_log_stove.txt"), log);
            Debug.Log("[BuildTool] " + log);
            if (s.result != BuildResult.Succeeded) return;
            string up = MakeStoveUploadFolder(outDir);
            File.AppendAllText(Path.Combine(root, demo ? "build_log_stove_demo.txt" : "build_log_stove.txt"), "upload=" + up + "\n");
            EditorUtility.RevealInFinder(up);
        }

        // 스토브 업로드용 폴더: 디버그 폴더(…DoNotShip, …ButDontShipItWithYourGame) 빼고 + VC++ 런타임 DLL 동봉(스토브 문서: SDK 의존 DLL 필수)
        static readonly string[] VcDlls = { "vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll", "msvcp140_atomic_wait.dll", "concrt140.dll" };
        static string MakeStoveUploadFolder(string outDir)
        {
            string up = outDir + "_Upload";
            if (Directory.Exists(up)) Directory.Delete(up, true);
            Directory.CreateDirectory(up);
            foreach (var f in Directory.GetFiles(outDir)) File.Copy(f, Path.Combine(up, Path.GetFileName(f)));
            foreach (var d in Directory.GetDirectories(outDir))
            {
                string n = Path.GetFileName(d);
                if (n.Contains("DoNotShip") || n.Contains("DontShip")) continue;
                CopyDir(d, Path.Combine(up, n));
            }
            // 스토브 SDK 3.4.2는 VS 14.44 툴셋으로 빌드됨 → msvcp140 14.40 미만을 같이 넣으면 SDK 스레드에서 튕긴다(11/03 확인).
            // 그래서 14.40 이상일 때만 동봉하고, 아니면 넣지 않는다(실행 설정의 'VC++ Redist 2022 x64'로 런처가 최신 런타임을 설치).
            string crt = FindVcRuntimeDir();
            if (crt == null) Debug.LogWarning("[BuildTool] VC++ 런타임 DLL 폴더를 못 찾음 → 동봉 안 함(런처의 VC++ Redist 설정 사용)");
            else
            {
                var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(crt, "msvcp140.dll"));
                if (vi.FileMajorPart > 14 || (vi.FileMajorPart == 14 && vi.FileMinorPart >= 40))
                    foreach (var n in VcDlls) { var src = Path.Combine(crt, n); if (File.Exists(src)) File.Copy(src, Path.Combine(up, n), true); }
                else Debug.LogWarning($"[BuildTool] VC++ 런타임 {vi.FileVersion} 은 너무 오래됨(14.40 미만) → 동봉 안 함");
            }
            return up;
        }
        static string FindVcRuntimeDir()
        {
            string manual = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "StoveRuntime", "x64"));
            if (File.Exists(Path.Combine(manual, "vcruntime140.dll"))) return manual;
            var roots = new System.Collections.Generic.List<string> { @"D:\visualstudio" };
            foreach (var pf in new[] { System.Environment.GetEnvironmentVariable("ProgramFiles"), System.Environment.GetEnvironmentVariable("ProgramFiles(x86)") })
                if (!string.IsNullOrEmpty(pf)) foreach (var ed in new[] { "Community", "Professional", "Enterprise", "BuildTools" })
                    roots.Add(Path.Combine(pf, "Microsoft Visual Studio", "2022", ed));
            foreach (var r in roots)
            {
                string redist = Path.Combine(r, "VC", "Redist", "MSVC");
                if (!Directory.Exists(redist)) continue;
                foreach (var ver in Directory.GetDirectories(redist).OrderByDescending(x => x))
                {
                    string c = Path.Combine(ver, "x64", "Microsoft.VC143.CRT");
                    if (File.Exists(Path.Combine(c, "vcruntime140.dll"))) return c;
                }
            }
            return null;
        }
        static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)));
            foreach (var d in Directory.GetDirectories(src)) CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        static void Build(bool demo)
        {
            Branding.Apply();                                    // 11/02: 아이콘·시작 화면(Unity 기본 → 우리 것)
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build"));
            string outDir = Path.Combine(root, demo ? "YeogiCafe_Demo" : "YeogiCafe_Windows");
            Directory.CreateDirectory(root);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            PlayerSettings.productName = "여기카페냐옹";   // 바뀌면 저장 폴더도 바뀜 → SaveManager가 예전 폴더 세이브·사진을 가져옴
            PlayerSettings.companyName = "Wintery Studio";
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/Cafe.unity" };

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                extraScriptingDefines = demo ? new[] { "YEOGI_DEMO" } : null,   // Demo.IsDemo = true
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            string log = $"result={s.result}\nerrors={s.totalErrors}\nwarnings={s.totalWarnings}\nsizeMB={s.totalSize / 1048576f:0.0}\ntime={s.totalTime}\nout={opts.locationPathName}\nscenes={string.Join(",", scenes)}\n";
            foreach (var step in report.steps)
                foreach (var m in step.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception) log += "ERR: " + m.content + "\n";
            File.WriteAllText(Path.Combine(root, demo ? "build_log_demo.txt" : "build_log.txt"), log);
            Debug.Log("[BuildTool] " + log);
            if (s.result != BuildResult.Succeeded) return;
            if (demo) EditorUtility.RevealInFinder(Path.Combine(outDir, ExeName));
            else PackPlaytest(root, outDir);
        }

        // 친구 플레이테스트 배포용: 안내문을 넣고 zip 하나로 묶는다(Build/여기카페냐옹_플레이테스트_v버전.zip)
        const string SurveyUrl = "https://docs.google.com/forms/d/e/1FAIpQLSegQcW2RML-cJh5B2SuDy8c8Bfq4tGJytXLuZQpj5VeKjN5ow/viewform";
        static void PackPlaytest(string root, string outDir)
        {
            try
            {
                string v = PlayerSettings.bundleVersion;
                string guide =
$@"여기카페냐옹 플레이테스트 (v{v})
================================

플레이해 줘서 고마워요! 아래만 읽고 시작하면 돼요.

1) 실행
   - 압축을 푼 폴더에서 YeogiCafe.exe 를 더블클릭하세요.
   - ""Windows의 PC 보호"" 파란 창이 뜨면: [추가 정보] → [실행] 을 누르면 됩니다.
     (개인이 만든 게임이라 서명이 없어서 뜨는 경고예요. 바이러스 아님!)

2) 이렇게 해 주세요
   - 설명서 없이 그냥 해 보는 게 테스트예요. 모르는 게 있으면 그게 고칠 점이에요.
   - 최소 1시간, 또는 7일차까지 해 주시면 좋아요. 그 뒤는 자유!
   - 배경 음악·몇몇 효과음은 새로 만든 거예요. 고양이 울음 등 나머지는 아직 임시음이에요.
   - 영업 중에 끄면 그날은 아침부터 다시 시작해요. (하루 끝 정산 때 저장돼요)

★ 이번 버전(v{v})에서 바뀐 것 — 지난번 설문 고마워요!
   - 배경 음악을 새로 만들었어요: 카페 주제곡 하나를 아침·점심·저녁·밤, 날씨마다 다르게 편곡
   - 문 종(딸랑딸랑)·결제·버튼·알림·요리 완성 소리도 새로
   - 햇살 속 고양이·가구 그림자가 햇살 방향을 따라가요 · 카페를 다 넓히면 '최대 레벨'로 표시
   - 카페 넓히기가 눈에 보여요: 레벨 1은 가까이, 2는 옆 공간, 3은 다락·캣워크까지
   - 창·칠판·조명·화분을 옮기거나 빼고, 큰 통창을 새로 살 수 있어요(배치 모드)
   - 오른쪽 주문대를 없앴어요 · 메뉴 창을 열면 시간이 멈춘다는 표시가 떠요
   - 설정에서 '타이틀로' 가기 · 손님이 주문을 너무 많이 쌓지 않게(최대 3개)
   - 세 취향을 다 맞히면 그 고양이의 '기억'이 열려요 · 카페 곳곳에서 옛 사장님의 노트를 찾아요
   - 도감에 '이야기' 탭: 지금까지 본 편지·기억·노트 다시 보기
   - 고양이 8마리가 새 그림이에요(나머지는 아직 옛 그림 — 섞여 보여도 정상!)
   - 고양이 걸음이 자연스러워졌어요: 발이 바닥에 붙어 걷고, 천천히 출발·멈춤, 의자에 폴짝 올라앉아요
   - 마감 후 놀아주기 화면의 고양이가 더 선명한 그림이 됐어요

3) 다 하고 나서 (중요!)
   - 설문 (5분): {SurveyUrl}
   - 게임 폴더에 생긴 playtest_log.txt 파일을 보내 주세요.
     (어디서 멈췄는지·오류가 났는지 보려고 남기는 기록이에요. 개인 정보는 없어요)
   - 버그나 이상한 화면은 캡처해서 보내 주면 최고예요.

조작
   - 마우스로 거의 다 돼요. 단축키: B 구매 · C 도감 · K 주방 · P 사진 · ESC 설정
";
                File.WriteAllText(Path.Combine(outDir, "먼저 읽어 주세요.txt"), guide, new System.Text.UTF8Encoding(true));
                string zip = Path.Combine(root, $"여기카페냐옹_플레이테스트_v{v}.zip");
                if (File.Exists(zip)) File.Delete(zip);
                System.IO.Compression.ZipFile.CreateFromDirectory(outDir, zip, System.IO.Compression.CompressionLevel.Optimal, false);
                Debug.Log("[BuildTool] 배포 zip: " + zip);
                EditorUtility.RevealInFinder(zip);
            }
            catch (System.Exception e) { Debug.LogError("[BuildTool] zip 실패: " + e.Message); }
        }
    }
}
