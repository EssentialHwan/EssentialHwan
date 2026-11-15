#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;

namespace YeogiCafe.DevTools
{
    // 에디터 전용: 원격 조작·윈도우 알림 등으로 에디터가 포커스를 잠깐 잃으면
    // EventSystem이 isFocused=false로 남아 타이틀 화면까지 모든 UI 클릭을 무시한다.
    // DevConsole은 카페 씬에만 있으므로, 씬과 상관없이 항상 도는 작은 오브젝트로 되살린다.
    public class EditorFocusKeeper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("[EditorFocusKeeper]") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<EditorFocusKeeper>();
        }

        void Update()
        {
            var es = EventSystem.current;
            if (es != null && !es.isFocused)
                es.SendMessage("OnApplicationFocus", true, SendMessageOptions.DontRequireReceiver);
        }
    }
}
#endif
