using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Loc
{
    // 문서 07 — 경량 로컬라이즈. Resources/strings.csv 로드, key→언어별 값(열: key,ko,en,zh,ja).
    // 토큰 {name}{n}{s}{a}{b} 런타임 치환. 미번역 칸은 ko 폴백(영어가 있으면 영어 우선).
    // 언어는 설정에 저장(opt.lang). 처음 실행 때는 OS 언어를 따른다.
    public enum Language { Ko, En, Zh, Ja }

    public static class L
    {
        static readonly Dictionary<string, string[]> table = new(); // key -> [ko, en, zh, ja]
        static Language current = Language.Ko;
        static bool loaded, langInit;

        public static Language Current
        {
            get { InitLang(); return current; }
            set { langInit = true; current = value; }
        }

        public const string PrefKey = "opt.lang";
        static void InitLang()
        {
            if (langInit) return;
            langInit = true;
            int saved = PlayerPrefs.GetInt(PrefKey, -1);
            if (saved >= 0 && saved <= (int)Language.Ja) { current = (Language)saved; return; }
            current = Application.systemLanguage switch
            {
                SystemLanguage.Korean => Language.Ko,
                SystemLanguage.ChineseSimplified or SystemLanguage.Chinese or SystemLanguage.ChineseTraditional => Language.Zh,
                SystemLanguage.Japanese => Language.Ja,
                _ => Language.En,
            };
        }

        // 설정에서 바꿀 때: 저장만 하고, 화면은 다시 불러와 새 언어로 다시 만든다(호출하는 쪽에서)
        public static void SetAndSave(Language lang)
        {
            Current = lang;
            PlayerPrefs.SetInt(PrefKey, (int)lang);
            PlayerPrefs.Save();
        }

        public static void Load(string csvText)
        {
            table.Clear();
            var lines = SplitLines(csvText);
            for (int i = 1; i < lines.Count; i++) // 0=헤더
            {
                var cols = ParseCsvLine(lines[i]);
                if (cols.Count < 3 || string.IsNullOrEmpty(cols[0])) continue;
                var v = new string[4];
                for (int c = 0; c < 4; c++) v[c] = c + 1 < cols.Count ? cols[c + 1] : null;
                table[cols[0]] = v;
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

        public static bool Has(string key) { EnsureLoaded(); return table.ContainsKey(key); }

        public static string Get(string key)
        {
            EnsureLoaded();
            if (table.TryGetValue(key, out var vals))
            {
                int idx = (int)Current;
                string v = idx < vals.Length ? vals[idx] : null;
                if (string.IsNullOrEmpty(v) && idx != 0 && vals.Length > 1 && !string.IsNullOrEmpty(vals[1])) v = vals[1]; // 영어 폴백
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
                if (Current == Language.Ko) s = FixJosa(s, "{" + token + "}", value);
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
