using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;

namespace YeogiCafe.UI
{
    // 마우스 커서 = 바리스타 고양이(나)의 앞발. 가만히 있으면 가끔 꼼지락(발가락 쫙·톡톡), 클릭하면 꾹.
    // 하드웨어 커서는 애니메이션이 거칠어서, 맨 위 캔버스의 이미지가 마우스를 따라다니는 방식.
    public class PawCursor : MonoBehaviour
    {
        Canvas canvas; RectTransform rt; Image img;
        float idle, fidgetT = -1f, nextFidget = 3f;
        Vector3 lastMouse;
        const float Scale = 1.7f, BaseTilt = 24f;     // 왼쪽 위를 가리키도록 기울임

        public static PawCursor Ensure()
        {
            var e = FindObjectOfType<PawCursor>();
            if (e != null) return e;
            var go = new GameObject("[PawCursor]");
            DontDestroyOnLoad(go);
            return go.AddComponent<PawCursor>();
        }

        void Awake()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            var g = new GameObject("Paw", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            g.transform.SetParent(transform, false);
            rt = (RectTransform)g.transform;
            img = g.GetComponent<Image>(); img.raycastTarget = false;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(13f / PawDrawing.CW, 27f / PawDrawing.CH);   // 손끝(가운데 발가락 끝)이 클릭 지점
            rt.sizeDelta = new Vector2(PawDrawing.CW, PawDrawing.CH) * Scale;
            Set(0);
        }

        void OnEnable() { Cursor.visible = false; }
        void OnDisable() { Cursor.visible = true; }
        void OnApplicationFocus(bool f) { if (f && enabled) Cursor.visible = false; }

        void Set(int frame) => img.sprite = RuntimeSprites.Get("cursor_paw_" + frame, () => PawDrawing.Cursor(frame));

        void Update()
        {
            var m = Input.mousePosition;
            rt.anchoredPosition = new Vector2(m.x, m.y) / Mathf.Max(0.01f, canvas.scaleFactor);
            float dt = Time.unscaledDeltaTime;
            bool moved = (m - lastMouse).sqrMagnitude > 1f; lastMouse = m;
            idle = moved ? 0f : idle + dt;

            if (Input.GetMouseButton(0)) { fidgetT = -1f; Set(2); rt.localEulerAngles = new Vector3(0, 0, BaseTilt - 6f); rt.localScale = new Vector3(1.06f, 0.92f, 1f); return; }
            rt.localScale = Vector3.one;

            // 꼼지락: 멈춰 있으면 몇 초마다 발가락을 쫙 폈다가 톡톡
            if (fidgetT < 0f)
            {
                nextFidget -= dt * (idle > 0.6f ? 1f : 0.25f);
                if (nextFidget <= 0f) { fidgetT = 0f; nextFidget = Random.Range(2.5f, 6f); }
                Set(0);
                rt.localEulerAngles = new Vector3(0, 0, BaseTilt + Mathf.Sin(Time.unscaledTime * 1.6f) * 1.5f);
                return;
            }
            fidgetT += dt;
            if (fidgetT < 0.35f) { Set(1); rt.localEulerAngles = new Vector3(0, 0, BaseTilt + 4f); }
            else if (fidgetT < 0.9f)
            {
                // 톡·톡(살짝 숙였다 폈다)
                int k = (int)((fidgetT - 0.35f) / 0.14f);
                Set(k % 2 == 0 ? 2 : 0);
                rt.localEulerAngles = new Vector3(0, 0, BaseTilt + (k % 2 == 0 ? -5f : 2f));
            }
            else { fidgetT = -1f; Set(0); }
        }
    }
}
