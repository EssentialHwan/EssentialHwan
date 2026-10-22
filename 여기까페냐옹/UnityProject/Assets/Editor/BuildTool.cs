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
        public static void BuildWindows()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build"));
            string outDir = Path.Combine(root, "YeogiCafe_Windows");
            Directory.CreateDirectory(root);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            PlayerSettings.productName = "여기카페냐옹";   // 바뀌면 저장 폴더도 바뀜 → SaveManager가 예전 폴더 세이브·사진을 가져옴
            PlayerSettings.companyName = "EssentialHwan";
            PlayerSettings.bundleVersion = "0.9.0";
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
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            string log = $"result={s.result}\nerrors={s.totalErrors}\nwarnings={s.totalWarnings}\nsizeMB={s.totalSize / 1048576f:0.0}\ntime={s.totalTime}\nout={opts.locationPathName}\nscenes={string.Join(",", scenes)}\n";
            foreach (var step in report.steps)
                foreach (var m in step.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception) log += "ERR: " + m.content + "\n";
            File.WriteAllText(Path.Combine(root, "build_log.txt"), log);
            Debug.Log("[BuildTool] " + log);
        }
    }
}
