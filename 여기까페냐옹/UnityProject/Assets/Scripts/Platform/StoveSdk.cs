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
using static Stove.PCSDK.GameSupport;

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

        // 콜백 안에서 SDK를 다시 부르지 않는다(재진입 → 런처 실행 시 튕김, 11/03).
        // 콜백은 상태만 바꾸고, 다음 SDK 호출은 콜백 루프(RunCallback 바깥)에서 한다.
        // 델리게이트는 static으로 잡아 둔다(네이티브가 들고 있는 동안 GC되지 않게).
        static Base.OnRestartAppIfNecessaryAsyncFinished onRestart;
        static Base.OnInitializeFinished onInit;
        static OnModifyStatFinished onModify;     // 스탯 갱신 결과(체험판 통계, 11/05)
        bool gsReady, statBusy; string busyId; int busyVal; float nextStat;
        enum Step { Idle, WaitRestart, NeedInit, WaitInit, Done, Quit }
        Step step = Step.Idle;

        void Start()
        {
#if YEOGI_DEMO
            const string cfgPath = "Stove/stove_config_demo";   // 체험판은 별도 상품(게임 ID·앱 키가 다름)
#else
            const string cfgPath = "Stove/stove_config";
#endif
            var ta = Resources.Load<TextAsset>(cfgPath);
            if (ta == null) { Debug.LogError($"[StoveSdk] Resources/{cfgPath}.json 없음 → 스토브 연동 안 함"); return; }
            var cfg = JsonUtility.FromJson<Config>(ta.text);
            StoveStats.Hook();   // 추리 정답 수는 SDK 준비 전에도 모아 둔다

            onRestart = (CallbackResult r, bool restart) =>
            {
                Debug.Log($"[StoveSdk] 런처 확인 결과: restart={restart} code={r.result.resultCode} {r.errorMessage}");
                step = restart ? Step.Quit : Step.NeedInit;
            };
            onInit = (CallbackResult cr) =>
            {
                if (cr.result.IsSuccessful()) { initialized = true; Ready = true; Debug.Log("[StoveSdk] 초기화 성공"); }
                else Debug.LogError($"[StoveSdk] 초기화 실패: {cr.result.resultCode} {cr.errorMessage}");
                step = Step.Done;
            };

            var p = new StovePCInitializeParamEx2
            {
                environment = cfg.environment, gameId = cfg.gameId, applicationKey = cfg.applicationKey,
                waitTimeMillisec = 60000, launchLauncher = true
            };
            Debug.Log($"[StoveSdk] ① 런처 확인 요청 ({cfg.environment})");
            step = Step.WaitRestart;
            Base_RestartAppIfNecessaryAsyncEx2(p, onRestart);   // ① exe를 직접 켰으면 런처로 다시 실행해야 함
            loop = StartCoroutine(RunCallbackLoop());           // ② 콜백 루프는 요청 '뒤'에 시작
        }

        IEnumerator RunCallbackLoop()
        {
            var w = new WaitForSecondsRealtime(0.2f);                    // 일시정지(timeScale 0) 중에도 돈다
            while (true)
            {
                yield return w;
                Base_RunCallback();
                if (step == Step.Quit) { Debug.Log("[StoveSdk] 런처를 통해 다시 실행 → 종료"); step = Step.Done; Application.Quit(); }
                else if (step == Step.NeedInit) { step = Step.WaitInit; Debug.Log("[StoveSdk] ② 초기화 요청"); Base_InitializeEx(onInit); }
                else if (step == Step.Done && initialized) PumpStats();
            }
        }

        // 체험판 통계: 게임 서포트 초기화 → 모아 둔 스탯을 하나씩(2초 간격, 같은 스탯은 1분에 1번) 보낸다. 콜백 안에선 SDK를 안 부른다.
        void PumpStats()
        {
            if (!gsReady)
            {
                if (Time.realtimeSinceStartup < nextStat) return;
                var r = GameSupport_Initialize();
                gsReady = r.IsSuccessful();
                Debug.Log($"[StoveSdk] 게임 서포트 초기화: {r.resultCode}");
                if (!gsReady) { nextStat = Time.realtimeSinceStartup + 30f; return; }   // 실패면 30초 뒤 다시
                onModify = (CallbackResult cr, StovePCModifyStatValue v) =>
                {
                    if (!cr.result.IsSuccessful())
                    {
                        Debug.LogWarning($"[StoveSdk] 스탯 {busyId} 실패: {cr.result.resultCode} {cr.errorMessage}");
                        StoveStats.Requeue(busyId, busyVal, StoveStats.IsAdd(busyId));
                    }
                    statBusy = false;
                };
            }
            if (statBusy && Time.realtimeSinceStartup - nextStat > 30f) { statBusy = false; StoveStats.Requeue(busyId, busyVal, StoveStats.IsAdd(busyId)); }   // 응답이 안 오면 30초 뒤 다시
            if (statBusy || Time.realtimeSinceStartup < nextStat) return;
            if (!StoveStats.TryTake(out busyId, out busyVal)) return;
            statBusy = true; nextStat = Time.realtimeSinceStartup + 2f;
            GameSupport_ModifyStat(busyId, busyVal, onModify);
        }

        // ③ 종료 직전 정리
        void OnApplicationQuit() => Shutdown();
        void OnDestroy() => Shutdown();
        void Shutdown()
        {
            if (loop != null) { StopCoroutine(loop); loop = null; }
            if (!initialized) return;
            initialized = false; Ready = false;
            if (gsReady) { GameSupport_UnInitialize(); gsReady = false; }
            var res = Base_UnInitialize();
            Debug.Log($"[StoveSdk] 정리: {res.resultCode}");
        }
    }
}
#endif
