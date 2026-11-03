// 스토브(STOVE) PC SDK 연동 — '■ 스토브 빌드'에서만 켜진다(YEOGI_STOVE). 일반·친구 테스트·스팀 빌드에는 들어가지 않는다.
//  스토브 개발자 문서(PC SDK 3, Unity) 필수 3가지:
//   ① 시작 시 런처 확인(Base_RestartAppIfNecessaryAsyncEx2) → Base 초기화(Base_InitializeEx)
//   ② 초기화 후 콜백 루프(Base_RunCallback 주기 호출)
//   ③ 종료 직전 정리(Base_UnInitialize)
//  상품키는 Resources/Stove/stove_config.json (저장소 제외, PC에만):
//   { "environment": "SANDBOX", "gameId": "...", "applicationKey": "..." }   ← 출시 빌드는 "LIVE"
//  Application Secret은 PC SDK에 쓰지 않는다(서버용). 절대 넣지 말 것.
#if YEOGI_STOVE
using System.Collections;
using UnityEngine;
using Stove.PCSDK;
using static Stove.PCSDK.Base;

namespace YeogiCafe.Platform
{
    public class StoveSdk : MonoBehaviour
    {
        [System.Serializable] class Config { public string environment = "SANDBOX", gameId = "", applicationKey = ""; }

        public static bool Ready { get; private set; }
        static StoveSdk inst;
        Coroutine loop;
        bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("[StoveSdk]"); DontDestroyOnLoad(go);
            inst = go.AddComponent<StoveSdk>();
        }

        void Start()
        {
            var ta = Resources.Load<TextAsset>("Stove/stove_config");
            if (ta == null) { Debug.LogError("[StoveSdk] Resources/Stove/stove_config.json 없음 → 스토브 연동 안 함"); return; }
            var cfg = JsonUtility.FromJson<Config>(ta.text);
            loop = StartCoroutine(RunCallbackLoop());                     // ② 런처 확인 콜백도 이 루프로 받는다

            var p = new StovePCInitializeParamEx2
            {
                environment = cfg.environment, gameId = cfg.gameId, applicationKey = cfg.applicationKey,
                waitTimeMillisec = 60000, launchLauncher = true
            };
            // ① 런처 확인: exe를 직접 켰으면 스토브 런처로 다시 실행해야 한다 → 게임 종료
            Base_RestartAppIfNecessaryAsyncEx2(p, (CallbackResult r, bool restart) =>
            {
                if (restart) { Debug.Log($"[StoveSdk] 런처를 통해 다시 실행 → 종료 ({r.result.resultCode})"); Application.Quit(); return; }
                Base_InitializeEx((CallbackResult cr) =>
                {
                    if (cr.result.IsSuccessful()) { initialized = true; Ready = true; Debug.Log("[StoveSdk] 초기화 성공"); }
                    else Debug.LogError($"[StoveSdk] 초기화 실패: {cr.result.resultCode} {cr.errorMessage}");
                });
            });
        }

        IEnumerator RunCallbackLoop()
        {
            var w = new WaitForSecondsRealtime(0.2f);                    // 일시정지(timeScale 0) 중에도 돈다
            while (true) { Base_RunCallback(); yield return w; }
        }

        // ③ 종료 직전 정리
        void OnApplicationQuit() => Shutdown();
        void OnDestroy() => Shutdown();
        void Shutdown()
        {
            if (loop != null) { StopCoroutine(loop); loop = null; }
            if (!initialized) return;
            initialized = false; Ready = false;
            var res = Base_UnInitialize();
            Debug.Log($"[StoveSdk] 정리: {res.resultCode}");
        }
    }
}
#endif
