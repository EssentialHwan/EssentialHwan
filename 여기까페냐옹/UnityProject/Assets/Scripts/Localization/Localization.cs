using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Loc
{
    // 문서 07 — 경량 로컬라이즈. Resources/strings.csv 로드, key→언어별 값.
    // 토큰 {name}{n}{s}{a}{b} 런타임 치환. 미번역 키는 ko 폴백 + 경고.
    public enum Language { Ko, En }

    public static class L
    {
        static readonly Dictionary<string, string[]> table = new(); // key -> [ko, en]
        static Language current = Language.Ko;
        static bool loaded;

        public static Language Current
        {
            get => current;
            set => current = value;
        }

        public static void Load(string csvText)
        {
            table.Clear();
            var lines = SplitLines(csvText);
            for (int i = 1; i < lines.Count; i++) // 0=헤더
            {
                var cols = ParseCsvLine(lines[i]);
                if (cols.Count < 3 || string.IsNullOrEmpty(cols[0])) continue;
                table[cols[0]] = new[] { cols[1], cols[2] };
            }
            loaded = true;
        }

        public static void EnsureLoaded()
        {
            if (loaded) return;
            var asset = Resources.Load<TextAsset>("strings");
            if (asset != null) Load(asset.text);
            else { loaded = true; Debug.LogWarning("[L] strings.csv not found in Resources"); }
        }

        public static string Get(string key)
        {
            EnsureLoaded();
            if (table.TryGetValue(key, out var vals))
            {
                int idx = (int)current;
                string v = idx < vals.Length ? vals[idx] : null;
                if (string.IsNullOrEmpty(v)) v = vals[0]; // ko 폴백
                return v;
            }
            Debug.LogWarning($"[L] missing key: {key}");
            return key;
        }

        // 토큰 치환: L.Get("ui.discover.toast", ("name","치즈냥"))
        public static string Get(string key, params (string token, string value)[] args)
        {
            string s = Get(key);
            foreach (var (token, value) in args)
            {
                // 한국어 조사 자동 보정: "{name}가" → 받침 있으면 "이". (젖소냥가 → 젖소냥이)
                s = FixJosa(s, "{" + token + "}", value);
                s = s.Replace("{" + token + "}", value);
            }
            return s;
        }

        // 토큰 바로 뒤 조사(가/는/를/와)를 값의 받침에 맞게 바꾼다. 한글이 아닌 값은 그대로.
        public static string FixJosa(string s, string tokenLiteral, string value)
        {
            if (string.IsNullOrEmpty(value)) return s;
            char last = value[value.Length - 1];
            if (last < '가' || last > '힣') return s;
            bool batchim = (last - '가') % 28 != 0;
            s = s.Replace(tokenLiteral + "가", value + (batchim ? "이" : "가"));
            s = s.Replace(tokenLiteral + "는", value + (batchim ? "은" : "는"));
            s = s.Replace(tokenLiteral + "를", value + (batchim ? "을" : "를"));
            s = s.Replace(tokenLiteral + "와", value + (batchim ? "과" : "와"));
            return s;
        }

        // ── 간이 CSV 파서 (따옴표·콤마 대응) ──
        static List<string> SplitLines(string text)
        {
            var result = new List<string>();
            var sb = new System.Text.StringBuilder();
            bool inQuotes = false;
            foreach (char c in text)
            {
                if (c == '"') inQuotes = !inQuotes;
                if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    if (sb.Length > 0) { result.Add(sb.ToString()); sb.Clear(); }
                }
                else sb.Append(c);
            }
            if (sb.Length > 0) result.Add(sb.ToString());
            return result;
        }

        static List<string> ParseCsvLine(string line)
        {
            var cols = new List<string>();
            var sb = new System.Text.StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') { inQuotes = !inQuotes; continue; }
                if (c == ',' && !inQuotes) { cols.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            cols.Add(sb.ToString());
            return cols;
        }
    }
}
