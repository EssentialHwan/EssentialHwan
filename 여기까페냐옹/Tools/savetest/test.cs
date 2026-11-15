using System; using System.IO; using YeogiCafe.Save;
class T { static void Check(string name, bool ok){ Console.WriteLine((ok?"PASS ":"FAIL ")+name); }
 static void Main(){
  var dir=UnityEngine.Application.persistentDataPath; if(Directory.Exists("/tmp/sv")) Directory.Delete("/tmp/sv",true); Directory.CreateDirectory(dir);
  var m=new SaveManager(); string P=Path.Combine(dir,"save.json");

  var g=new SaveGame{currentDay=3}; m.Save(g); Check("first save", m.LoadOrNew().currentDay==3 && !File.Exists(P+".bak"));
  g.currentDay=4; m.Save(g); Check("second save keeps bak(3)", m.LoadOrNew().currentDay==4 && File.Exists(P+".bak"));
  File.WriteAllText(P,"{\"version\":1,\"currentD"); var r=m.LoadOrNew(); Check("corrupt main -> bak day3", r.currentDay==3); Check("corrupt quarantined", Directory.GetFiles(dir,"save.json.broken-*").Length==1 && !File.Exists(P));
  m.Save(new SaveGame{currentDay=5}); Check("save after recovery", m.LoadOrNew().currentDay==5);
  // 지우고-옮기기 사이에 꺼진 상황: 본 파일 없음 + tmp만 있음
  File.Delete(P); File.Delete(P+".bak"); File.WriteAllText(P+".tmp","{\"version\":1,\"currentDay\":6,\"cafeLevel\":1}"); Check("tmp only -> day6", m.LoadOrNew().currentDay==6); Check("HasSave with tmp only", m.HasSave());
  m.DeleteSave(); Check("delete clears all", !m.HasSave()); Check("no save -> new day1", m.LoadOrNew().currentDay==1);
}}
