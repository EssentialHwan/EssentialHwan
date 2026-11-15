using System.IO;
using UnityEngine;

namespace YeogiCafe.Save
{
    // 사양서 §15.7 — JSON 단일 오토세이브(MVP). 임시파일→교체(무결성), version 필드.
    public class SaveManager : MonoBehaviour
    {
        const string FileName = "save.json";

        string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);
        string TempPath => Path + ".tmp";
        string BakPath => Path + ".bak";

        public SaveGame LoadOrNew()
        {
            TryImportLegacy();
            return ReadBest(out _) ?? new SaveGame();
        }

        // 디스크에 있는 마지막 저장본(오늘 아침 상태). 없으면 null.
        public SaveGame LoadDisk() => ReadBest(out _);

        // 11/09 세이브 안전장치: 본 파일 → 직전 백업(.bak) → 쓰다 만 임시파일(.tmp) 순서로 읽는다.
        // 본 파일이 깨졌으면 지우지 않고 save.json.broken-날짜 로 옮겨 둔다(새 게임 자동저장이 덮어쓰지 않게).
        SaveGame ReadBest(out string from)
        {
            from = null;
            foreach (var p in new[] { Path, BakPath, TempPath })
            {
                if (!File.Exists(p)) continue;
                try
                {
                    var sg = JsonUtility.FromJson<SaveGame>(File.ReadAllText(p));
                    if (sg != null && sg.currentDay > 0)
                    {
                        if (p != Path) Debug.LogWarning($"[SaveManager] 본 세이브 대신 {System.IO.Path.GetFileName(p)} 에서 불러왔어요.");
                        from = p; return Migrate(sg);
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[SaveManager] {System.IO.Path.GetFileName(p)} read failed: {e.Message}"); }
                if (p == Path) Quarantine(p);
            }
            return null;
        }

        void Quarantine(string p)
        {
            try { File.Copy(p, p + ".broken-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss"), true); File.Delete(p); }
            catch (System.Exception e) { Debug.LogWarning($"[SaveManager] quarantine failed: {e.Message}"); }
        }

        public void Save(SaveGame save)
        {
            try
            {
                string json = JsonUtility.ToJson(save, prettyPrint: true);
                File.WriteAllText(TempPath, json);       // 임시파일에 먼저 기록
                // 지우고 옮기면 그 사이에 꺼질 때 세이브가 통째로 없어진다 → 한 번에 바꾸고 직전 것은 .bak 으로 남긴다
                if (File.Exists(Path))
                {
                    try { File.Replace(TempPath, Path, BakPath); }
                    catch (System.Exception) { File.Copy(Path, BakPath, true); File.Copy(TempPath, Path, true); File.Delete(TempPath); }
                }
                else File.Move(TempPath, Path);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] save failed: {e.Message}");
            }
        }

        // 빌드 설정에서 회사·제품 이름을 바꾸면 저장 폴더(LocalLow/회사/제품)가 바뀐다.
        // 예전 폴더(예: DefaultCompany/YeogiCafe_Unity)에 이 게임 세이브가 있으면, 지금 세이브가 없거나 더 앞선 날이면 한 번 가져온다.
        void TryImportLegacy()
        {
            try
            {
                string marker = Path + ".imported";
                if (File.Exists(marker)) return;
                var here = new DirectoryInfo(Application.persistentDataPath);
                var root = here.Parent?.Parent;
                if (root == null || !root.Exists) return;
                string best = null; int bestDay = 0;
                foreach (var company in root.GetDirectories())
                    foreach (var product in company.GetDirectories())
                    {
                        if (product.FullName.TrimEnd('/', '\\') == here.FullName.TrimEnd('/', '\\')) continue;
                        string f = System.IO.Path.Combine(product.FullName, FileName);
                        if (!File.Exists(f)) continue;
                        string json = File.ReadAllText(f);
                        if (!json.Contains("\"cafeLevel\"") || !json.Contains("\"currentDay\"")) continue;   // 이 게임 세이브만
                        var sg = JsonUtility.FromJson<SaveGame>(json);
                        if (sg != null && sg.currentDay > bestDay) { best = f; bestDay = sg.currentDay; }
                    }
                if (best == null) return;
                bool take = !File.Exists(Path);
                if (!take)
                {
                    var cur = JsonUtility.FromJson<SaveGame>(File.ReadAllText(Path));
                    take = cur == null || cur.currentDay < bestDay;
                }
                Directory.CreateDirectory(Application.persistentDataPath);
                if (take)
                {
                    File.Copy(best, Path, true);
                    // 포토 모드 사진도 같이(세이브가 파일 이름으로 가리킨다)
                    string oldPhotos = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(best), "Photos");
                    string newPhotos = System.IO.Path.Combine(Application.persistentDataPath, "Photos");
                    if (Directory.Exists(oldPhotos))
                    {
                        Directory.CreateDirectory(newPhotos);
                        foreach (var f in Directory.GetFiles(oldPhotos))
                        {
                            string dst = System.IO.Path.Combine(newPhotos, System.IO.Path.GetFileName(f));
                            if (!File.Exists(dst)) File.Copy(f, dst);
                        }
                    }
                    Debug.Log($"[SaveManager] 예전 세이브를 가져왔어요: {best} ({bestDay}일차)");
                }
                File.WriteAllText(marker, best);
            }
            catch (System.Exception e) { Debug.LogWarning($"[SaveManager] legacy import skipped: {e.Message}"); }
        }

        SaveGame Migrate(SaveGame sg)
        {
            // 버전 마이그레이션 훅 (현재 v1)
            if (sg.version < 1) sg.version = 1;
            return sg;
        }

        public bool HasSave() => File.Exists(Path) || File.Exists(BakPath) || File.Exists(TempPath);
        public void DeleteSave() { foreach (var p in new[] { Path, BakPath, TempPath }) if (File.Exists(p)) File.Delete(p); }
    }
}
