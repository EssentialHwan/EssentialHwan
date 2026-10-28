using System.Collections;
using UnityEngine;

namespace YeogiCafe.DevTools
{
    // 개발용: 홍보 영상(릴스·쇼츠·GIF) 재료 녹화. 게임 화면을 Build/gif/<이름>_<시각>/f_000.jpg… 로 남긴다.
    //  - Time.captureFramerate로 게임 시간을 프레임마다 정확히 1/fps씩 진행 → 저장이 느려 실제 화면이 버벅여도 결과 영상은 매끈하고 속도도 정확.
    //  - v2(10/28): 15fps → 30fps(예전 GIF가 뚝뚝 끊겨 보였음). 프레임을 메모리에 모으지 않고 바로 JPG로 저장(1080p 30fps도 메모리 걱정 없음).
    //  - 녹화 중엔 개발 콘솔·토글을 숨긴다(DevConsole.Capturing). info.txt에 fps 기록 → 변환할 때 반드시 그 fps로.
    public class GifRecorder : MonoBehaviour
    {
        public static bool Busy { get; private set; }
        public static string LastDir { get; private set; }
        public const int DefaultFps = 30;
        static GifRecorder current;
        bool stopRequested;

        public static void Record(string name, float seconds, int fps = DefaultFps)
        {
            if (Busy) return;
            var go = new GameObject("[GifRecorder]");
            DontDestroyOnLoad(go);
            current = go.AddComponent<GifRecorder>();
            current.StartCoroutine(current.Run(name, seconds, fps));
        }

        // F11: 누르면 시작, 다시 누르면 끝(최대 40초). 장면을 연출하면서 찍을 때.
        public static void Toggle()
        {
            if (Busy) { if (current != null) current.stopRequested = true; return; }
            Record("rec", 40f);
        }

        IEnumerator Run(string name, float seconds, int fps)
        {
            Busy = true;
            DevConsole.Capturing = true;
            yield return null; yield return null;                      // 콘솔이 화면에서 사라진 뒤
            string dir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Build", "gif",
                                                $"{name}_{System.DateTime.Now:MMdd_HHmmss}");
            System.IO.Directory.CreateDirectory(dir);
            int w = Screen.width, h = Screen.height, n = Mathf.Max(1, Mathf.RoundToInt(seconds * fps));
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            Time.captureFramerate = fps;
            int count = 0;
            for (int i = 0; i < n && !stopRequested; i++)
            {
                yield return new WaitForEndOfFrame();
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"f_{i:000}.jpg"), tex.EncodeToJPG(95));
                count++;
            }
            Time.captureFramerate = 0;
            DevConsole.Capturing = false;
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "info.txt"), $"fps={fps}\nframes={count}\nsize={w}x{h}\n");
            Destroy(tex);
            LastDir = dir;
            Busy = false;
            Debug.Log($"[GifRecorder] {count} frames @{fps}fps → {dir}");
            Destroy(gameObject);
        }
    }
}
