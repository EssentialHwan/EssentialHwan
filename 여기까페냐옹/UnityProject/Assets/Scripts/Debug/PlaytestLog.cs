using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace YeogiCafe.DevTools
{
    // 친구 플레이테스트용 기록. 게임 폴더(exe 옆)의 playtest_log.txt에 이어 쓴다.
    // "어디서 멈췄나·얼마나 걸렸나·뭘 열어 봤나·오류가 났나"를 말이 아니라 데이터로 보기 위함.
    // 개인 정보는 남기지 않는다(시간·일차·화면 이름·이벤트 수·오류 메시지만).
    public static class PlaytestLog
    {
        public static bool Enabled => !YeogiCafe.Core.Demo.IsDemo;   // 친구 테스트용 기록: 체험판 빌드에서는 끈다(정식 출시 때는 여기를 false로)
        static StreamWriter w;
        static float t0;
        static readonly Dictionary<string, string> last = new();
        static readonly Dictionary<string, int> eventsToday = new();
        static readonly HashSet<string> seenErrors = new();
        static int errorCount;
        static float dayStartT = -1f;

        public static string FilePath
        {
            get
            {
                string dir = Application.isEditor
                    ? Path.Combine(Application.dataPath, "..", "Build")
                    : Path.Combine(Application.dataPath, "..");      // exe가 있는 폴더
                return Path.GetFullPath(Path.Combine(dir, "playtest_log.txt"));
            }
        }

        public static void Init(string lang)
        {
            if (!Enabled || w != null) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                w = new StreamWriter(FilePath, true, new UTF8Encoding(false)) { AutoFlush = true };
            }
            catch
            {
                try   // exe 폴더에 못 쓰면(읽기 전용 위치 등) 세이브 폴더로
                {
                    string alt = Path.Combine(Application.persistentDataPath, "playtest_log.txt");
                    w = new StreamWriter(alt, true, new UTF8Encoding(false)) { AutoFlush = true };
                }
                catch { w = null; return; }
            }
            t0 = Time.realtimeSinceStartup;
            w.WriteLine();
            w.WriteLine($"===== 세션 시작 {System.DateTime.Now:yyyy-MM-dd HH:mm} · v{Application.version} · 언어 {lang} · 화면 {Screen.width}x{Screen.height} =====");
            YeogiCafe.Core.GameEvents.OnEvent += (id, _) => { eventsToday.TryGetValue(id, out int n); eventsToday[id] = n + 1; };
            Application.logMessageReceived += OnLog;
            Application.quitting += () => Line("게임 종료");
        }

        static string Stamp()
        {
            int s = Mathf.FloorToInt(Time.realtimeSinceStartup - t0);
            return $"{s / 60:00}:{s % 60:00}";
        }

        public static void Line(string text)
        {
            if (w == null) return;
            try { w.WriteLine($"[{Stamp()}] {text}"); } catch { }
        }

        // 값이 바뀔 때만 한 줄(매 프레임 불러도 됨)
        public static void Track(string key, string value)
        {
            if (w == null || value == null) return;
            if (last.TryGetValue(key, out var v) && v == value) return;
            last[key] = value;
            Line($"{key}: {value}");
        }

        // 하루 동안 센 숫자(방문·서빙·주문 놓침 등) — 영업 끝 줄에 함께 남는다
        public static void Count(string key)
        {
            if (w == null) return;
            eventsToday.TryGetValue(key, out int n); eventsToday[key] = n + 1;
        }

        static int startedDay = -1;
        public static void DayStart(int day)
        {
            if (day == startedDay) return;        // 시작 화면에서 언어를 바꾸면 장면을 다시 불러 같은 날이 또 시작됨 → 한 번만
            startedDay = day;
            dayStartT = Time.realtimeSinceStartup;
            eventsToday.Clear();
            Line($"── {day}일차 영업 시작");
        }

        public static void DayEnd(int day, int revenue, int cost, int gold)
        {
            string dur = dayStartT >= 0 ? $"{(Time.realtimeSinceStartup - dayStartT) / 60f:0.0}분" : "?";
            var sb = new StringBuilder();
            foreach (var kv in eventsToday) sb.Append(kv.Key).Append('=').Append(kv.Value).Append(' ');
            Line($"── {day}일차 영업 끝 · 걸린 시간 {dur} · 매출 {revenue}G 재료비 {cost}G 잔액 {gold}G · 이벤트 {sb}");
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            if (errorCount >= 40) return;
            string key = msg.Length > 120 ? msg.Substring(0, 120) : msg;
            if (!seenErrors.Add(key)) return;
            errorCount++;
            string top = stack != null ? stack.Split('\n')[0] : "";
            Line($"!! 오류({type}): {key} @ {top}");
        }
    }
}
