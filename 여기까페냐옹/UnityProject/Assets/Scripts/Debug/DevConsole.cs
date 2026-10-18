using UnityEngine;
using YeogiCafe.Core;
using YeogiCafe.Economy;
using YeogiCafe.Data;

namespace YeogiCafe.DevTools
{
    // 개발/테스트 가속용 치트. 빌드에서는 제외되도록 DEVELOPMENT_BUILD/에디터 가드.
    // F1: 패널 토글. 플레이테스트·QA 가속.
    public class DevConsole : MonoBehaviour
    {
        public EconomyManager economy;
        public DayManager day;
        public CatManager cats;
        public GameFlowController flow;

        bool show;

        void Start()
        {
            // S1Bootstrap이 Awake에서 만든 매니저를 자동 탐색(인스펙터 미연결 시).
            if (economy == null) economy = FindObjectOfType<EconomyManager>();
            if (day == null) day = FindObjectOfType<DayManager>();
            if (cats == null) cats = FindObjectOfType<CatManager>();
            if (flow == null) flow = FindObjectOfType<GameFlowController>();
        }

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F1)) show = !show;
#endif
        }

        void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!show) return;
            GUILayout.BeginArea(new Rect(10, 80, 260, 420), GUI.skin.box);
            GUILayout.Label("== DevConsole (F1) ==");
            if (economy != null)
            {
                GUILayout.Label($"Gold: {economy.Gold}");
                if (GUILayout.Button("+1000 Gold")) economy.AddGold(1000);
            }
            if (day != null)
            {
                GUILayout.Label($"Day {day.CurrentDay} / {day.Phase}  (timeScale {Time.timeScale:0}x)");
                // 전체 시뮬레이션 배속: 고양이 이동·착석 시간까지 같이 빨라져야 테스트가 의미 있다.
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("x1")) Time.timeScale = 1f;
                if (GUILayout.Button("x3")) Time.timeScale = 3f;
                if (GUILayout.Button("x10")) Time.timeScale = 10f;
                GUILayout.EndHorizontal();
                if (GUILayout.Button("영업 즉시 종료 → 정산")) day.DebugEndDayNow();
            }
            var dir = FindObjectOfType<YeogiCafe.Visual.CatInteractionDirector>();
            if (dir != null)
            {
                GUILayout.Label("고양이끼리(앉은 아무 둘)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("부비")) dir.DebugForce(YeogiCafe.AI.SocialKind.Rub);
                if (GUILayout.Button("핥기")) dir.DebugForce(YeogiCafe.AI.SocialKind.Groom);
                if (GUILayout.Button("놀이")) dir.DebugForce(YeogiCafe.AI.SocialKind.Play);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("킁킁")) dir.DebugForce(YeogiCafe.AI.SocialKind.Sniff);
                if (GUILayout.Button("하악")) dir.DebugForce(YeogiCafe.AI.SocialKind.Hiss);
                if (GUILayout.Button("싸움")) dir.DebugForce(YeogiCafe.AI.SocialKind.Fight);
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("세이브 삭제 후 종료(새 게임)"))
            {
                var p = System.IO.Path.Combine(Application.persistentDataPath, "save.json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;   // 계속 돌면 오토세이브가 다시 만든다
#else
                Application.Quit();
#endif
            }
            GUILayout.Label("(치트는 빌드에서 자동 제외)");
            GUILayout.EndArea();
#endif
        }
    }
}
