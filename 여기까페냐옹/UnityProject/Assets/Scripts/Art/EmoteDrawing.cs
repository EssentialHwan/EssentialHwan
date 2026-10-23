using UnityEngine;

namespace YeogiCafe.Art
{
    // 고양이 머리 위 감정 아이콘(12×12 도트). 문자 비트맵으로 정의 → 외곽선 자동.
    // a=주색, b=밝은색, w=하이라이트
    public static class EmoteDrawing
    {
        public enum Kind { Heart, Anger, Question, Exclaim, Sweat, Note, Zzz, Sparkle, Fight }

        static readonly string[] Heart =
        {
            "............",
            "..aa....aa..",
            ".awba..aaaa.",
            ".abbaaaaaaa.",
            ".aaaaaaaaaa.",
            ".aaaaaaaaaa.",
            "..aaaaaaaa..",
            "...aaaaaa...",
            "....aaaa....",
            ".....aa.....",
            "............",
            "............",
        };
        static readonly string[] Anger =
        {
            "............",
            "...aa..aa...",
            "...aa..aa...",
            ".aaab..baaa.",
            ".aab....baa.",
            "............",
            "............",
            ".aab....baa.",
            ".aaab..baaa.",
            "...aa..aa...",
            "...aa..aa...",
            "............",
        };
        static readonly string[] Question =
        {
            "............",
            "...aaaaa....",
            "..aawwbaa...",
            "..aa...aa...",
            ".......aa...",
            "......aa....",
            ".....aa.....",
            ".....aa.....",
            "............",
            ".....aa.....",
            ".....aa.....",
            "............",
        };
        static readonly string[] Exclaim =
        {
            "............",
            ".....aa.....",
            "....awaa....",
            "....abaa....",
            "....aaaa....",
            ".....aa.....",
            ".....aa.....",
            "............",
            ".....aa.....",
            ".....aa.....",
            "............",
            "............",
        };
        static readonly string[] Sweat =
        {
            "............",
            ".....a......",
            ".....aa.....",
            "....aaa.....",
            "....aaaa....",
            "...awaaa....",
            "...abaaa....",
            "...aaaaa....",
            "....aaa.....",
            "............",
            "............",
            "............",
        };
        static readonly string[] Note =
        {
            "............",
            "......aaaa..",
            "......abaa..",
            "......a..a..",
            "......a..a..",
            "......a..a..",
            "...aaaa.aa..",
            "..awaaa.aa..",
            "..aaaa......",
            "...aa.......",
            "............",
            "............",
        };
        static readonly string[] Zzz =
        {
            "............",
            ".......aaaa.",
            "........aa..",
            ".......aaaa.",
            "............",
            "...aaaa.....",
            ".....a......",
            "....a.......",
            "...aaaa.....",
            "............",
            "............",
            "............",
        };
        static readonly string[] Sparkle =
        {
            "............",
            ".....a......",
            ".....a......",
            "....aba.....",
            ".aaabwbaaa..",
            "....aba.....",
            ".....a....a.",
            ".....a...aba",
            "..........a.",
            "............",
            "............",
            "............",
        };
        // 싸움 먼지구름(두 고양이 사이)
        static readonly string[] Fight =
        {
            "............",
            "...bbb..bb..",
            "..bwwbbbwbb.",
            ".bwwbbbbbbb.",
            ".bbbbabbbbb.",
            "bbbbbbbbabbb",
            "bwbbbbbbbbbb",
            ".bbbabbbbbb.",
            ".bbbbbbbwbb.",
            "..bbb.bbbb..",
            "...b....b...",
            "............",
        };

        public static PixelCanvas Draw(Kind k)
        {
            string[] map; Color32 a, b;
            switch (k)
            {
                case Kind.Heart:    map = Heart;    a = new Color32(236, 112, 132, 255); b = new Color32(248, 170, 184, 255); break;
                case Kind.Anger:    map = Anger;    a = new Color32(222, 76, 70, 255);   b = new Color32(240, 130, 120, 255); break;
                case Kind.Question: map = Question; a = new Color32(120, 150, 210, 255); b = new Color32(170, 196, 236, 255); break;
                case Kind.Exclaim:  map = Exclaim;  a = new Color32(240, 186, 70, 255);  b = new Color32(250, 220, 140, 255); break;
                case Kind.Sweat:    map = Sweat;    a = new Color32(132, 186, 226, 255); b = new Color32(200, 228, 246, 255); break;
                case Kind.Note:     map = Note;     a = new Color32(150, 120, 200, 255); b = new Color32(200, 180, 236, 255); break;
                case Kind.Zzz:      map = Zzz;      a = new Color32(140, 160, 210, 255); b = a; break;
                case Kind.Sparkle:  map = Sparkle;  a = new Color32(244, 206, 96, 255);  b = new Color32(252, 236, 170, 255); break;
                default:            map = Fight;    a = new Color32(160, 140, 124, 255); b = new Color32(232, 222, 206, 255); break;
            }
            var c = new PixelCanvas(14, 14);
            var w = PixelPalette.Highlight;
            for (int r = 0; r < 12; r++)
                for (int x = 0; x < 12; x++)
                {
                    char ch = map[r][x];
                    if (ch == '.') continue;
                    c.Set(x + 1, 12 - r, ch == 'a' ? a : ch == 'b' ? b : w);
                }
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }
    }
}
