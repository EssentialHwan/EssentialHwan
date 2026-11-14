using System; using System.IO; using System.Text.RegularExpressions;
namespace UnityEngine {
  public class MonoBehaviour {}
  public static class Debug { public static void Log(object o)=>Console.WriteLine("LOG "+o); public static void LogWarning(object o)=>Console.WriteLine("WARN "+o); public static void LogError(object o)=>Console.WriteLine("ERR "+o); }
  public static class Application { public static string persistentDataPath = "/tmp/sv/LocalLow/Wintery Studio/Game"; }
  public static class JsonUtility {
    public static string ToJson(object o, bool prettyPrint){ var s=(YeogiCafe.Save.SaveGame)o; return "{\"version\":"+s.version+",\"currentDay\":"+s.currentDay+",\"cafeLevel\":1}"; }
    public static T FromJson<T>(string j) where T: class { var m=Regex.Match(j,"\"currentDay\":(\\d+)\\s*,\\s*\"cafeLevel\":1}\\s*$"); if(!m.Success) throw new ArgumentException("JSON parse error"); var s=new YeogiCafe.Save.SaveGame(); s.currentDay=int.Parse(m.Groups[1].Value); return s as T; }
  }
}
namespace YeogiCafe.Save { public class SaveGame { public int version=1; public int currentDay=1; } }
