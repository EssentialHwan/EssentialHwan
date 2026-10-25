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

        static void Build(bool demo)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build"));
            string outDir = Path.Combine(root, demo ? "YeogiCafe_Demo" : "YeogiCafe_Windows");
            Directory.CreateDirectory(root);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            PlayerSettings.productName = "여기카페냐옹";   // 바뀌면 저장 폴더도 바뀜 → SaveManager가 예전 폴더 세이브·사진을 가져옴
            PlayerSettings.companyName = "Wintery Studio";
            PlayerSettings.bundleVersion = "0.9.3";   // 친구 플레이테스트 2차: 조명·첫 주 연출·여름·환경음
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
   - 소리는 아직 임시음이에요(나중에 바뀌어요).
   - 영업 중에 끄면 그날은 아침부터 다시 시작해요. (하루 끝 정산 때 저장돼요)

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
