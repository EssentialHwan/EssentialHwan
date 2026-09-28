using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Loc;
using YeogiCafe.Observation;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 정산 화면의 고양이별 관찰 카드 뷰(로직). 실제 프리팹은 이 컴포넌트를 붙여 사용.
    // Truth 미접근 — ObservationCardModel(=ObservationRecord 기반)만 렌더.
    // UI 위젯(TMP/Button)은 인스펙터에서 연결하는 것을 전제로, 여기서는 데이터→표시문자열 변환을 제공.
    public class SettlementCardView : MonoBehaviour
    {
        public ObservationManager obs;   // 기록 액션용
        public string catId;

        // 뷰가 표시할 문자열 3축 (UI 바인딩 헬퍼)
        public struct AxisDisplay { public string label; public string icon; public bool canRecord; }

        public AxisDisplay GetAxisDisplay(CatData data, CatSaveData save, PrefAxis axis)
        {
            var rec = save.observation.Get(axis);
            return new AxisDisplay
            {
                label = L.Get(AxisKey(axis)),
                icon = ObservationCardBuilder.StateIcon(rec.state),
                canRecord = rec.state == ObsState.Confirmable
            };
        }

        // [기록] 버튼 핸들러
        public bool OnClickRecord(PrefAxis axis, out int bonusGold)
        {
            bonusGold = 0;
            if (obs == null || string.IsNullOrEmpty(catId)) return false;
            return obs.TryRecord(catId, axis, out bonusGold);
        }

        public string ToastText(CatData data)
            => L.Get("ui.discover.toast", ("name", L.Get($"cat.{data.catId}.name")));

        static string AxisKey(PrefAxis a) => a switch
        {
            PrefAxis.Food => "ui.axis.food",
            PrefAxis.Seat => "ui.axis.seat",
            _ => "ui.axis.facility"
        };
    }
}
