using System.IO;
using NUnit.Framework;
using YeogiCafe.Loc;

namespace YeogiCafe.Tests
{
    // strings.csv 실파일 무결성 — 파싱 성공 + 필수 키 존재 + 콤마/따옴표 처리.
    // 경로: 테스트 실행 위치 기준 상대. Unity에서는 Resources 로드로 대체됨.
    public class StringsCsvIntegrityTests
    {
        static string FindCsv()
        {
            // CWD에서 위로 올라가며 strings.csv 탐색 (러너 위치 무관)
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int depth = 0; depth < 8 && dir != null; depth++)
            {
                var hits = dir.GetFiles("strings.csv", SearchOption.AllDirectories);
                foreach (var f in hits)
                    if (f.FullName.Replace('\\', '/').Contains("Localization"))
                        return f.FullName;
                dir = dir.Parent;
            }
            return null;
        }

        [Test]
        public void Csv_ParsesAndHasKeyStrings()
        {
            var path = FindCsv();
            if (path == null) { Assert.Ignore("strings.csv 경로를 찾지 못함(러너 CWD). Unity에서는 Resources로 검증."); return; }

            L.Load(File.ReadAllText(path));
            L.Current = Language.Ko;

            Assert.AreEqual("오늘의 정산", L.Get("ui.settle.title"));
            Assert.AreEqual("치즈냥", L.Get("cat.cat_cheese.name"));
            // 따옴표+콤마 포함 라인 파싱 확인
            Assert.IsTrue(L.Get("ep.cat_cheese.regular3.02").Contains("이 자리"));
            // 토큰 치환
            Assert.IsTrue(L.Get("ui.discover.toast", ("name", "치즈냥")).Contains("치즈냥"));

            L.Current = Language.En;
            Assert.AreEqual("Today's Report", L.Get("ui.settle.title"));
        }
    }
}
