using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace YeogiCafe.UI
{
    // 글자 넘침 안전망: 화면의 모든 UI Text를 주기적으로 살펴, 글자가 자기 칸(RectTransform)을 벗어나면
    // 칸에 들어갈 때까지 글자 크기를 줄인다(원래 크기의 60%·최소 9까지). 글이 바뀌면 원래 크기에서 다시 맞춘다.
    // · 이미 BestFit을 쓰는 글자, 크기가 0인 칸(레이아웃이 정하는 칸)은 건드리지 않는다.
    // · 최소 크기로도 안 들어가는 글자는 Report()에 모아 둔다(개발 콘솔 [글자 넘침 검사]).
    public class TextFitGuard : MonoBehaviour
    {
        struct State { public string text; public Vector2 size; public int baseSize; public bool failed; }
        static readonly Dictionary<Text, State> states = new();
        static TextFitGuard inst;
        float t, scanT = 99f;
        Canvas[] canvases = new Canvas[0];

        public static void Ensure()
        {
            if (inst != null) return;
            var go = new GameObject("[TextFitGuard]");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<TextFitGuard>();
        }

        void LateUpdate()
        {
            t += Time.unscaledDeltaTime;
            if (t < 0.15f) return;
            t = 0f;
            scanT += 0.15f;
            if (scanT > 1f) { scanT = 0f; canvases = FindObjectsOfType<Canvas>(); }   // 새로 생긴 캔버스(타이틀 등)도 잡게
            foreach (var c in canvases)
            {
                if (c == null || !c.isRootCanvas) continue;
                foreach (var tx in c.GetComponentsInChildren<Text>(false)) Check(tx);
            }
        }

        static void Check(Text tx)
        {
            if (tx == null || tx.resizeTextForBestFit || string.IsNullOrEmpty(tx.text)) return;
            if (LayoutDriven(tx)) return;           // 글에 맞춰 칸이 커지는 곳(말풍선 등)은 칸이 정답
            var rt = tx.rectTransform;
            var size = rt.rect.size;
            if (size.x < 4f || size.y < 4f) return;
            states.TryGetValue(tx, out var st);
            bool known = st.baseSize > 0;
            if (known && st.text == tx.text && st.size == size) return;
            // 글이나 칸이 바뀌었으면 원래 크기에서 다시 맞춘다(바깥에서 크기를 바꿨으면 그걸 새 기준으로)
            int baseSize = known && (tx.fontSize < st.baseSize) ? st.baseSize : tx.fontSize;
            if (known && tx.fontSize > st.baseSize) baseSize = tx.fontSize;
            int min = Mathf.Max(9, Mathf.RoundToInt(baseSize * 0.6f));
            int fs = baseSize;
            while (fs > min && !Fits(tx, size, fs)) fs--;
            // 중국어·일본어: 줄 머리에 。、！ 같은 문장부호가 오지 않게 줄을 한 글자 앞에서 꺾는다(금칙 처리)
            string k = Kinsoku(tx, tx.text, size, fs);
            if (k != tx.text) { tx.text = k; while (fs > min && !Fits(tx, size, fs)) fs--; }
            tx.fontSize = fs;
            st.failed = !Fits(tx, size, fs);
            st.text = tx.text; st.size = size; st.baseSize = baseSize;
            states[tx] = st;
            Note(tx, st);
        }

        // ── 금칙 처리(行頭禁則) — UGUI Text는 CJK 문장부호를 줄 맨 앞에 두기도 해서 직접 고친다 ──
        const string NoStart = "、。，．,.!?！？）)」』】〉》〕］}ー…‥・：；:;ゝゞヽヾ々ぁぃぅぇぉっゃゅょゎァィゥェォッャュョヮヵヶ～〜—”’％%";
        const string NoEnd = "（(「『【〈《〔［{“‘";
        static readonly TextGenerator kgen = new TextGenerator();

        public static string Kinsoku(Text tx, string s, Vector2 size, int fs)
        {
            var lang = YeogiCafe.Loc.L.Current;
            if (lang != YeogiCafe.Loc.Language.Zh && lang != YeogiCafe.Loc.Language.Ja) return s;
            if (string.IsNullOrEmpty(s) || s.IndexOf('<') >= 0 || tx.horizontalOverflow == HorizontalWrapMode.Overflow) return s;
            if (size.x < 4f) return s;
            for (int iter = 0; iter < 8; iter++)
            {
                var gs = tx.GetGenerationSettings(new Vector2(size.x, 0));
                gs.fontSize = fs; gs.resizeTextForBestFit = false; gs.verticalOverflow = VerticalWrapMode.Overflow;
                kgen.Populate(s, gs);
                var lines = kgen.lines;
                bool changed = false;
                for (int i = 1; i < lines.Count; i++)
                {
                    int idx = lines[i].startCharIdx, prevStart = lines[i - 1].startCharIdx;
                    if (idx <= 0 || idx >= s.Length || s[idx - 1] == '\n') continue;
                    int cut = -1;
                    if (NoStart.IndexOf(s[idx]) >= 0) { cut = idx - 1; while (cut > prevStart && NoStart.IndexOf(s[cut]) >= 0) cut--; }
                    else if (NoEnd.IndexOf(s[idx - 1]) >= 0) cut = idx - 1;
                    if (cut > prevStart && s[cut - 1] != '\n') { s = s.Insert(cut, "\n"); changed = true; break; }
                }
                if (!changed) break;
            }
            return s;
        }
        public static string Kinsoku(Text tx, string s) => Kinsoku(tx, s, tx.rectTransform.rect.size, tx.fontSize);

        static readonly Dictionary<Text, bool> driven = new();
        static bool LayoutDriven(Text tx)
        {
            if (driven.TryGetValue(tx, out var d)) return d;
            d = tx.GetComponent<ContentSizeFitter>() != null;
            var p = tx.transform.parent;
            if (!d && p != null)
            {
                if (p.GetComponent<ContentSizeFitter>() != null) d = true;
                var hv = p.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (hv != null && (hv.childControlWidth || hv.childControlHeight)) d = true;
            }
            driven[tx] = d;
            return d;
        }

        static bool Fits(Text tx, Vector2 size, int fs)
        {
            var gs = tx.GetGenerationSettings(size);
            gs.fontSize = fs; gs.resizeTextForBestFit = false;
            var gen = tx.cachedTextGeneratorForLayout;
            float ppu = tx.pixelsPerUnit <= 0 ? 1f : tx.pixelsPerUnit;
            float tol = 1.5f;
            if (tx.horizontalOverflow == HorizontalWrapMode.Overflow)
            {
                gs.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (gen.GetPreferredWidth(tx.text, gs) / ppu > size.x + tol) return false;
            }
            gs.horizontalOverflow = tx.horizontalOverflow;
            gs.verticalOverflow = VerticalWrapMode.Overflow;
            return gen.GetPreferredHeight(tx.text, gs) / ppu <= size.y + tol;
        }

        // 개발용: 최소 크기로도 안 들어간 글자(FAIL)와 줄여서 맞춘 글자(SHRUNK) 목록 — 패널을 닫아도 남는다
        static readonly Dictionary<string, (Text t, string msg)> log = new();
        static void Note(Text tx, State st)
        {
            string key = Path(tx.transform) + "#" + tx.GetInstanceID();
            if (st.failed) log[key] = (tx, "FAIL   | " + tx.text.Replace("\n", " "));
            else if (tx.fontSize < st.baseSize) log[key] = (tx, $"SHRUNK {st.baseSize}->{tx.fontSize} | " + tx.text.Replace("\n", " "));
            else log.Remove(key);                                    // 칸이 제대로 잡힌 뒤 다시 맞으면 기록 지움
        }
        public static string Report()
        {
            var sb = new StringBuilder();
            foreach (var kv in log) if (kv.Value.t != null && kv.Value.msg.StartsWith("FAIL")) sb.Append(kv.Value.msg).Append("  @ ").Append(kv.Key).Append('\n');
            foreach (var kv in log) if (kv.Value.t != null && !kv.Value.msg.StartsWith("FAIL")) sb.Append(kv.Value.msg).Append("  @ ").Append(kv.Key).Append('\n');
            return sb.ToString();
        }

        static string Path(Transform t)
        {
            var s = t.name;
            for (var p = t.parent; p != null && p.parent != null; p = p.parent) s = p.name + "/" + s;
            return s;
        }
    }
}
