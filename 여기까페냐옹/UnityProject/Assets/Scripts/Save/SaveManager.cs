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

        public SaveGame LoadOrNew()
        {
            TryImportLegacy();
            try
            {
                if (File.Exists(Path))
                {
                    string json = File.ReadAllText(Path);
                    var sg = JsonUtility.FromJson<SaveGame>(json);
                    if (sg != null) return Migrate(sg);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveManager] load failed, starting new: {e.Message}");
            }
            return new SaveGame();
        }

        public void Save(SaveGame save)
        {
            try
            {
                string json = JsonUtility.ToJson(save, prettyPrint: true);
                File.WriteAllText(TempPath, json);       // 임시파일에 먼저 기록
                if (File.Exists(Path)) File.Delete(Path);
                File.Move(TempPath, Path);               // 원자적 교체
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

        public bool HasSave() => File.Exists(Path);
        public void DeleteSave() { if (File.Exists(Path)) File.Delete(Path); }
    }
}
