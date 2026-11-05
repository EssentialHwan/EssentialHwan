// 체험판 익명 통계 — 스토브 게임 서포트(스탯)로 보낸다. 스토브 빌드(YEOGI_STOVE)에서만 실제로 보내고, 다른 빌드에선 아무것도 안 한다.
//  "며칠째에 그만두나"를 말이 아니라 숫자로 보기 위함(5일차 지루함 판단, 11/05).
//  개인 정보 없음: 스토브 계정별 숫자 몇 개뿐(최고 일차·끝낸 일차·플레이 분·추리 정답·체험판 완료).
//  Studio → 게임 서포트 → 스탯에 아래 ID를 같은 이름·타입으로 먼저 등록해야 한다(등록 안 된 ID는 오류만 나고 게임엔 영향 없음).
//    MAX_DAY   (MAX)        시작한 최고 일차
//    DONE_DAY  (MAX)        영업을 끝까지 마친 최고 일차
//    PLAY_MIN  (INCREMENT)  영업 중 플레이 분(하루 끝날 때 더함)
//    GUESS_OK  (INCREMENT)  추리 정답 수
//    DEMO_DONE (MAX)        체험판 끝 화면을 봤으면 1
//  퍼널은 업적으로 본다: 업적 "2일차"~"7일차"(MAX_DAY ≥ n), "체험판 완주"(DEMO_DONE ≥ 1) → Studio의 업적 달성률 = 일차별 남은 비율.
//  스토브 권장: 스탯 하나당 1분에 1번 이하로 갱신 → 같은 스탯은 60초 안에 다시 안 보내고 모아 둔다.
using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Platform
{
    public static class StoveStats
    {
        public const string MaxDay = "MAX_DAY", DoneDay = "DONE_DAY", PlayMin = "PLAY_MIN", GuessOk = "GUESS_OK", DemoDone = "DEMO_DONE";

        // 큰 값만 남김(MAX 타입)
        public static void Max(string id, int v)
        {
#if YEOGI_STOVE
            if (pendingMax.TryGetValue(id, out int o) && o >= v) return;
            pendingMax[id] = v;
#endif
        }
        // 더하기(INCREMENT 타입)
        public static void Add(string id, int v)
        {
#if YEOGI_STOVE
            if (v <= 0) return;
            pendingAdd.TryGetValue(id, out int o); pendingAdd[id] = o + v;
#endif
        }

#if YEOGI_STOVE
        static readonly Dictionary<string, int> pendingMax = new(), pendingAdd = new();
        static readonly Dictionary<string, float> lastSent = new();
        static readonly Dictionary<string, int> sentMaxVal = new();   // 이번 실행에서 보낸 MAX 값(같은 값 반복 전송 방지)
        static bool hooked;

        public static void Hook()
        {
            if (hooked) return; hooked = true;
            YeogiCafe.Core.GameEvents.OnEvent += (id, _) => { if (id == YeogiCafe.Core.GameEvents.GuessCorrect) Add(GuessOk, 1); };
        }

        // StoveSdk 콜백 루프(콜백 바깥)에서 부른다. 보낼 게 있으면 하나 꺼내 준다.
        public static bool TryTake(out string id, out int value)
        {
            float now = Time.realtimeSinceStartup;
            bool Ok(string k) => !lastSent.TryGetValue(k, out float t) || now - t >= 60f;
            foreach (var k in new List<string>(pendingAdd.Keys))
                if (Ok(k)) { id = k; value = pendingAdd[k]; pendingAdd.Remove(k); lastSent[k] = now; return true; }
            foreach (var k in new List<string>(pendingMax.Keys))
            {
                if (!Ok(k)) continue;
                int v = pendingMax[k]; pendingMax.Remove(k);
                if (sentMaxVal.TryGetValue(k, out int s) && s >= v) continue;   // 이미 더 큰 값을 보냄
                sentMaxVal[k] = v; lastSent[k] = now; id = k; value = v; return true;
            }
            id = null; value = 0; return false;
        }
        // 보내기 실패 시 다시 넣는다
        public static void Requeue(string id, int value, bool isAdd)
        {
            if (isAdd) Add(id, value);
            else { sentMaxVal.Remove(id); Max(id, value); }
        }
        public static bool IsAdd(string id) => id == PlayMin || id == GuessOk;
#endif
    }
}
