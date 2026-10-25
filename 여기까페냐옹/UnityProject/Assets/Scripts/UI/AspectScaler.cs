using UnityEngine;
using UnityEngine.UI;

namespace YeogiCafe.UI
{
    // 화면 비율이 16:9가 아닐 때 UI가 잘리거나 겹치지 않게: 더 넓으면(휴대폰 20:9·울트라와이드) 세로 기준,
    // 더 좁으면(태블릿 4:3) 가로 기준으로 맞춘다 → 기준 화면 1280×720이 항상 통째로 들어간다.
    // 휴대폰에서는 화면 꺼짐 방지·60fps도 여기서 켠다.
    [RequireComponent(typeof(CanvasScaler))]
    public class AspectScaler : MonoBehaviour
    {
        CanvasScaler s; int lastW, lastH;
        static bool mobileSet;

        void Awake()
        {
            s = GetComponent<CanvasScaler>();
            if (Application.isMobilePlatform && !mobileSet)
            {
                mobileSet = true;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Application.targetFrameRate = 60;
            }
            Apply();
        }

        void Update() { if (Screen.width != lastW || Screen.height != lastH) Apply(); }

        void Apply()
        {
            lastW = Screen.width; lastH = Screen.height;
            if (s == null || lastH == 0) return;
            float a = lastW / (float)lastH, refA = s.referenceResolution.x / s.referenceResolution.y;
            s.matchWidthOrHeight = a > refA + 0.01f ? 1f : a < refA - 0.01f ? 0f : 0.5f;
        }
    }
}
