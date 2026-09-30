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
            GUILayout.BeginArea(new Rect(10, 10, 240, 300), GUI.skin.box);
            GUILayout.Label("== DevConsole (F1) ==");
            if (economy != null)
            {
                GUILayout.Label($"Gold: {economy.Gold}");
                if (GUILayout.Button("+1000 Gold")) economy.AddGold(1000);
            }
            if (day != null)
            {
                GUILayout.Label($"Day {day.CurrentDay} / {day.Phase}");
                if (GUILayout.Button("배속 x3")) day.speed = 3f;
                if (GUILayout.Button("배속 x1")) day.speed = 1f;
            }
            GUILayout.Label("(치트는 빌드에서 자동 제외)");
            GUILayout.EndArea();
#endif
        }
    }
}
