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
