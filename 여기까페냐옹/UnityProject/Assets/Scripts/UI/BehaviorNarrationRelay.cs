using System;
using UnityEngine;
using YeogiCafe.Core;

namespace YeogiCafe.UI
{
    // ObservationManager의 서술 이펙트("창밖을 오래 본다")를 UI로 중계.
    // 뷰(TMP/말풍선)는 OnNarration을 구독. 실시간엔 수치 절대 표시 금지(서술만).
    public class BehaviorNarrationRelay : MonoBehaviour
    {
        public GameFlowController flow;

        // 뷰가 구독: (narrationText)
        public event Action<string> OnNarration;

        bool hooked;

        void Update()
        {
            if (hooked || flow == null || flow.Observation == null) return;
            flow.Observation.OnBehaviorNarration += HandleNarration;
            hooked = true;
        }

        void HandleNarration(string text) => OnNarration?.Invoke(text);

        void OnDisable()
        {
            if (hooked && flow != null && flow.Observation != null)
                flow.Observation.OnBehaviorNarration -= HandleNarration;
            hooked = false;
        }
    }
}
