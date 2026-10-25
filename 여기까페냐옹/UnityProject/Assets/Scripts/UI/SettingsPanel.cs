using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Audio;
using YeogiCafe.Loc;

namespace YeogiCafe.UI
{
    // 설정 창(열면 게임 일시정지). 값은 PlayerPrefs에 저장되어 다음 실행에도 유지된다.
    //  · 소리: 배경음악 / 효과음 / 빗소리 볼륨, 전체 음소거
    //  · 화면: 전체화면, 창 크기(1280·1600·1920), 고양이 손 커서 켜기/끄기
    //  · 게임: 하루 진행 속도(1x·2x·3x)
    //  · 새 게임(세이브 삭제, 두 번 눌러 확인) · 게임 종료
    public class SettingsPanel
    {
        readonly GameUiController owner;
        readonly GameObject rootGo;
        readonly RectTransform box;
        Text fullLabel, resLabel, speedLabel, cursorLabel, muteLabel, newGameLabel, quitLabel, langLabel;
        float prevTimeScale = 1f; bool confirmNew, confirmQuit;

        static readonly Vector2Int[] Resolutions = { new(1280, 720), new(1600, 900), new(1920, 1080) };

        public bool IsOpen => rootGo.activeSelf;

        // ── 저장된 설정 적용(게임 시작 시 한 번) ──
        public static void ApplySaved(Core.DayManager day)
        {
            SoundManager.BgmVolume = PlayerPrefs.GetFloat("opt.bgm", 0.5f);
            SoundManager.SfxVolume = PlayerPrefs.GetFloat("opt.sfx", 0.8f);
            SoundManager.AmbVolume = PlayerPrefs.GetFloat("opt.amb", 0.35f);
            SoundManager.Muted = PlayerPrefs.GetInt("opt.mute", 0) == 1;
            if (day != null) day.speed = PlayerPrefs.GetFloat("opt.speed6", 1f);
            var cur = Object.FindObjectOfType<PawCursor>();
            if (cur != null) cur.enabled = PlayerPrefs.GetInt("opt.paw", 1) == 1;
        }

        public SettingsPanel(GameUiController owner, Transform canvas)
        {
            this.owner = owner;
            var scrim = UiFactory.Panel("SettingsScrim", canvas, UiFactory.Scrim);
            rootGo = scrim.gameObject;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            box = UiFactory.Panel("Settings", scrim, UiFactory.PanelBg);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 640));
            UiFactory.Frame(box, UiFactory.FrameStyle.Window);
            UiFactory.TitleTab(box, L.Get("opt.title"), 200);

            float y = -50;
            Section(L.Get("opt.sound"), ref y);
            SliderRow(L.Get("opt.bgm"), "opt.bgm", 0.5f, v => SoundManager.BgmVolume = v, ref y);
            SliderRow(L.Get("opt.sfx"), "opt.sfx", 0.8f, v => { SoundManager.SfxVolume = v; SoundManager.Sfx("sfx_meow_small", 0.6f, 0.05f, "opt_preview", 0.35f); }, ref y);
            SliderRow(L.Get("opt.amb"), "opt.amb", 0.35f, v => SoundManager.AmbVolume = v, ref y);
            muteLabel = ButtonRow(L.Get("opt.mute"), "", () => { SoundManager.Muted = !SoundManager.Muted; PlayerPrefs.SetInt("opt.mute", SoundManager.Muted ? 1 : 0); Refresh(); }, ref y);

            if (!Application.isMobilePlatform)                     // 휴대폰엔 전체화면·창 크기·고양이 손 커서가 의미 없다
            {
                Section(L.Get("opt.screen"), ref y);
                fullLabel = ButtonRow(L.Get("opt.full"), "", () => { Screen.fullScreen = !Screen.fullScreen; Invoke(Refresh); }, ref y);
                resLabel = ButtonRow(L.Get("opt.res"), "", CycleResolution, ref y);
                cursorLabel = ButtonRow(L.Get("opt.paw"), "", () =>
                {
                    var cur = Object.FindObjectOfType<PawCursor>();
                    bool on = PlayerPrefs.GetInt("opt.paw", 1) == 0;
                    PlayerPrefs.SetInt("opt.paw", on ? 1 : 0);
                    if (cur != null) cur.enabled = on;
                    Refresh();
                }, ref y);
            }

            Section(L.Get("opt.game"), ref y);
            speedLabel = ButtonRow(L.Get("opt.speed"), "", () =>
            {
                var day = owner.Day; if (day == null) return;
                day.speed = day.speed >= 3f ? 1f : day.speed + 1f;
                PlayerPrefs.SetFloat("opt.speed6", day.speed);
                Refresh();
            }, ref y);
            ButtonRow(L.Get("opt.opening"), L.Get("opt.opening.btn"), () => owner.PlayOpening(), ref y);
            langLabel = ButtonRow(L.Get("opt.lang"), L.Get("lang.name"), CycleLanguage, ref y);

            // 아래 버튼: 새 게임 / 게임 종료 / 닫기
            var newBtn = UiFactory.Button("NewGame", box, L.Get("opt.newgame"), UiFactory.Pink, UiFactory.Cream, out newGameLabel);
            newGameLabel.fontSize = 17;
            UiFactory.SetRect((RectTransform)newBtn.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 26), new Vector2(150, 48));
            newBtn.onClick.AddListener(NewGame);
            var quit = UiFactory.Button("Quit", box, L.Get("opt.quit"), UiFactory.Wood, UiFactory.Cream, out quitLabel);
            quitLabel.fontSize = 17;
            UiFactory.SetRect((RectTransform)quit.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(150, 48));
            quit.onClick.AddListener(() =>
            {
                // 영업 중(오늘 뭔가 했으면): 한 번 더 눌러야 종료 — 오늘은 아침부터 다시 한다고 알려 준다
                var fl = Object.FindObjectOfType<YeogiCafe.Core.GameFlowController>();
                if (fl != null && fl.HasProgressToday && !confirmQuit)
                {
                    confirmQuit = true;
                    quitLabel.text = L.Get("opt.quit.confirm"); UiFactory.FitOneLine(quitLabel, 140, 11);
                    owner.ShowToast(L.Get("opt.quit.warn"), true);
                    return;
                }
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
            var close = UiFactory.Button("Close", box, L.Get("opt.close"), UiFactory.Green, UiFactory.Ink, out var cl);
            cl.fontSize = 17;
            UiFactory.SetRect((RectTransform)close.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 26), new Vector2(150, 48));
            close.onClick.AddListener(Hide);

            rootGo.SetActive(false);
        }

        // 화면 모드 전환은 다음 프레임에 반영되므로 라벨은 살짝 늦게
        void Invoke(System.Action a) { refreshLater = 2; }
        int refreshLater;
        public void Tick() { if (refreshLater > 0 && --refreshLater == 0) Refresh(); }

        void Section(string title, ref float y)
        {
            var t = UiFactory.Label("Sec", box, title, 18, TextAnchor.MiddleLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)t.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(34, y), new Vector2(-68, 26));
            var line = UiFactory.Panel("Line", box, new Color(0.59f, 0.43f, 0.29f, 0.35f));
            UiFactory.SetRect(line, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(34, y - 27), new Vector2(-68, 2));
            y -= 34;
        }

        void SliderRow(string label, string key, float def, System.Action<float> onChange, ref float y)
        {
            var t = UiFactory.Label("L", box, label, 17, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)t.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, y), new Vector2(150, 34));
            var pct = UiFactory.Label("P", box, "", 16, TextAnchor.MiddleRight, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)pct.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, y), new Vector2(60, 34));
            var s = UiFactory.Slider("S", box, PlayerPrefs.GetFloat(key, def));
            UiFactory.SetRect((RectTransform)s.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(200, y - 9), new Vector2(-310, 16));
            pct.text = Mathf.RoundToInt(s.value * 100) + "%";
            s.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(key, v); pct.text = Mathf.RoundToInt(v * 100) + "%"; onChange(v); });
            y -= 40;
        }

        Text ButtonRow(string label, string value, UnityEngine.Events.UnityAction onClick, ref float y)
        {
            var t = UiFactory.Label("L", box, label, 17, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)t.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, y), new Vector2(200, 34));
            var b = UiFactory.Button("B", box, value, UiFactory.Cream, UiFactory.Ink, out var bl);
            bl.fontSize = 16;
            UiFactory.SetRect((RectTransform)b.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, y + 2), new Vector2(190, 36));
            b.onClick.AddListener(onClick);
            y -= 40;
            return bl;
        }

        void CycleResolution()
        {
            int cur = 0;
            for (int i = 0; i < Resolutions.Length; i++) if (Screen.width == Resolutions[i].x) cur = i;
            var r = Resolutions[(cur + 1) % Resolutions.Length];
            Screen.SetResolution(r.x, r.y, Screen.fullScreen);
            refreshLater = 2;
        }

        // 언어: 한국어 → English → 简体中文 → 日本語. 바꾸면 저장하고 화면을 새 언어로 다시 만든다(시작 화면부터).
        void CycleLanguage()
        {
            // 언어를 바꾸면 화면을 처음부터 다시 만든다 → 영업 중엔 그날이 아침부터 다시 시작되므로 막는다
            var busy = Object.FindObjectOfType<YeogiCafe.Core.GameFlowController>();
            if (busy != null && busy.HasProgressToday)
            {
                owner.ShowToast(L.Get("opt.lang.busy"), true);
                return;
            }
            var next = (Language)(((int)L.Current + 1) % 4);
            L.SetAndSave(next);
            var flow = Object.FindObjectOfType<YeogiCafe.Core.GameFlowController>();
            if (flow != null) flow.SaveOnQuit();            // (영업 전·정산 중에만 여기 온다)
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        void NewGame()
        {
            if (!confirmNew) { confirmNew = true; newGameLabel.text = L.Get("opt.newgame.confirm"); return; }
            var sm = Object.FindObjectOfType<YeogiCafe.Save.SaveManager>();
            if (sm != null) sm.DeleteSave();
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        void Refresh()
        {
            muteLabel.text = L.Get(SoundManager.Muted ? "opt.on" : "opt.off");
            if (fullLabel != null) fullLabel.text = L.Get(Screen.fullScreen ? "opt.on" : "opt.off");
            if (resLabel != null) resLabel.text = $"{Screen.width} × {Screen.height}";
            if (cursorLabel != null) cursorLabel.text = L.Get(PlayerPrefs.GetInt("opt.paw", 1) == 1 ? "opt.on" : "opt.off");
            var day = owner.Day;
            speedLabel.text = day != null ? L.Get("opt.speedval", ("x", day.speed.ToString("0")), ("m", Mathf.RoundToInt(720f / day.Rate / 60f).ToString())) : "-";
            newGameLabel.text = L.Get("opt.newgame");
        }

        public void Toggle() { if (IsOpen) Hide(); else Show(); }

        public void Show()
        {
            confirmNew = false; confirmQuit = false;
            if (quitLabel != null) { quitLabel.text = L.Get("opt.quit"); quitLabel.fontSize = 17; }
            prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;                       // 설정 중엔 영업 일시정지
            rootGo.SetActive(true); rootGo.transform.SetAsLastSibling();
            Refresh();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            rootGo.SetActive(false);
            Time.timeScale = prevTimeScale;
            PlayerPrefs.Save();
        }
    }
}
