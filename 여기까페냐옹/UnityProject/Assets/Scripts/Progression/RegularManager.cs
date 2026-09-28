using System;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Progression
{
    // 사양서 §3.8 — 단골 판정 (1.0은 3단계로 축소, 단 코드는 5단계 확장 가능).
    // 강등 없음(부록 J-H). 파생값 사용(P5), Recorded 축 수 사용(P4).
    public class RegularManager
    {
        public event Action<string, int> OnRegularStageUp;

        public void Evaluate(CatSaveData cat, bool episodeCompleted = false)
        {
            int v = cat.visitCount;
            float avg = cat.AvgSatisfaction;
            int recorded = cat.observation.RecordedAxisCount();
            int stage = cat.regularStage;

            // 1.0 축소판: 1→2→3 (에피소드는 3단계 도달 시)
            if (stage < 2 && v >= 3 && avg >= 60) stage = 2;
            if (stage < 3 && v >= 6 && recorded >= 2) stage = 3;
            // (포스트) 4/5단계는 확장 시 활성:
            // if (stage < 4 && v >= 10 && recorded >= 3) stage = 4;
            // if (stage < 5 && v >= 15 && episodeCompleted) stage = 5;

            if (stage != cat.regularStage)
            {
                cat.regularStage = stage;
                OnRegularStageUp?.Invoke(cat.catId, stage);
            }
            // 강등 없음
        }
    }
}
