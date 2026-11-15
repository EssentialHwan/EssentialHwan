using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Core;
using YeogiCafe.Art;

namespace YeogiCafe.Visual
{
    // 날씨 연출: 비 오는 날 창문 유리에 빗줄기, 흐린 날·비 오는 날 방 전체를 살짝 어둡게.
    public class WeatherView : MonoBehaviour
    {
        SpriteRenderer tint;
        readonly List<Transform> drops = new();
        static Sprite white;
        WeatherKind shown = (WeatherKind)(-1);

        static Sprite White()
        {
            if (white != null) return white;
            var t = new Texture2D(1, 1); t.SetPixel(0, 0, Color.white); t.Apply(); t.filterMode = FilterMode.Point;
            white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return white;
        }

        // 통유리 창은 이제 옮길 수 있는 기본 사물(GlassWindowView, FixtureManager가 만든다). 여기선 방 전체 날씨 덧칠만.
        void Start()
        {
            // 예전 씬의 작은 창 두 개는 숨긴다(통유리로 대체)
            var room = GameObject.Find("Room");
            if (room != null) foreach (Transform t in room.transform) if (t.name == "Window") t.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Weather.Current != shown) Apply(Weather.Current);
        }

        void Apply(WeatherKind k)
        {
            shown = k;
            if (tint == null)
            {
                var go = new GameObject("WeatherTint");
                go.transform.SetParent(transform, false);
                tint = go.AddComponent<SpriteRenderer>(); tint.sprite = White();
                go.transform.position = new Vector3(CafeLayout.CameraPos.x, 4f, 0f);
                go.transform.localScale = new Vector3(40f, 20f, 1f);
                tint.sortingOrder = 30000;
            }
            tint.color = k == WeatherKind.Rain ? new Color(0.25f, 0.3f, 0.45f, 0.13f)
                       : k == WeatherKind.Cloudy ? new Color(0.4f, 0.4f, 0.45f, 0.07f)
                       : new Color(1f, 0.9f, 0.7f, 0.04f);                      // 맑음: 아주 살짝 따뜻하게
        }
    }
}
