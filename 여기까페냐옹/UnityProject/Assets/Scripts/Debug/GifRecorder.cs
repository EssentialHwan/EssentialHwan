using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.DevTools
{
    // 개발용: 홍보 GIF 재료 녹화. 게임 화면을 일정 프레임으로 찍어 Build/gif/<이름>/f_000.png… 로 남긴다.
    //  - Time.captureFramerate로 게임 시간을 프레임마다 정확히 1/fps씩 진행 → PNG 저장이 느려도 움직임이 끊기지 않는다.
    //  - 녹화 중엔 개발 콘솔·토글을 숨긴다(DevConsole.Capturing).
    //  - GIF 변환·자르기는 PC 쪽 스크립트(ffmpeg/PIL)에서 한다.
    public class GifRecorder : MonoBehaviour
    {
        public static bool Busy { get; private set; }
        public static string LastDir { get; private set; }

        public static void Record(string name, float seconds, int fps = 15)
        {
            if (Busy) return;
            var go = new GameObject("[GifRecorder]");
            DontDestroyOnLoad(go);
            go.AddComponent<GifRecorder>().StartCoroutine(go.GetComponent<GifRecorder>().Run(name, seconds, fps));
        }

        IEnumerator Run(string name, float seconds, int fps)
        {
            Busy = true;
            DevConsole.Capturing = true;
            yield return null; yield return null;                      // 콘솔이 화면에서 사라진 뒤
            int w = Screen.width, h = Screen.height, n = Mathf.Max(1, Mathf.RoundToInt(seconds * fps));
            var frames = new List<byte[]>(n);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            Time.captureFramerate = fps;
            for (int i = 0; i < n; i++)
            {
                yield return new WaitForEndOfFrame();
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                frames.Add(tex.GetRawTextureData<byte>().ToArray());
            }
            Time.captureFramerate = 0;
            DevConsole.Capturing = false;

            string dir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Build", "gif",
                                                $"{name}_{System.DateTime.Now:MMdd_HHmmss}");
            System.IO.Directory.CreateDirectory(dir);
            for (int i = 0; i < frames.Count; i++)
            {
                var png = ImageConversion.EncodeArrayToPNG(frames[i], UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8_UNorm, (uint)w, (uint)h);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"f_{i:000}.png"), png);
                if (i % 10 == 9) yield return null;
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "info.txt"), $"fps={fps}\nframes={frames.Count}\nsize={w}x{h}\n");
            Destroy(tex);
            LastDir = dir;
            Busy = false;
            Debug.Log($"[GifRecorder] {frames.Count} frames → {dir}");
            Destroy(gameObject);
        }
    }
}
