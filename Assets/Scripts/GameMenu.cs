using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Caveman
{
    /// <summary>
    /// The game shell: a main menu at startup, a pause menu (Esc) with Save / Load / Settings / Quit, and a
    /// settings panel (volumes + fullscreen). Drawn in IMGUI on top of the HUD; while a menu screen is open the
    /// game is paused (Time.timeScale = 0) and InventoryHud skips its gameplay hotkeys (see GameMenu.MenuOpen).
    /// Created by GameBootstrap after the world is built.
    /// </summary>
    public class GameMenu : MonoBehaviour
    {
        public static GameMenu Instance { get; private set; }
        public enum Screen { None, Main, Pause, Settings }
        private Screen _screen = Screen.Main;
        private Screen _settingsReturn = Screen.Main;
        private const int SlotCount = 3;

        private GUIStyle _title, _sub, _btn, _row, _box;
        private bool _styles;

        /// <summary>True while any menu screen is up — InventoryHud checks this to suppress gameplay input.</summary>
        public static bool MenuOpen => Instance != null && Instance._screen != Screen.None;

        public static GameMenu Ensure()
        {
            if (Instance != null) return Instance;
            return new GameObject("GameMenu").AddComponent<GameMenu>();
        }

        void Awake()
        {
            Instance = this;
            GameSettings.Load();
            _screen = Screen.Main;
            Time.timeScale = 0f; // start paused on the main menu
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            switch (_screen)
            {
                case Screen.Settings: GameSettings.Save(); _screen = _settingsReturn; break;
                case Screen.Pause: Resume(); break;
                case Screen.Main: break; // Esc does nothing on the title screen
                case Screen.None:
                    if (InventoryHud.Instance != null && InventoryHud.Instance.AnyPanelOpen) InventoryHud.Instance.CloseAllPanels();
                    else OpenPause();
                    break;
            }
        }

        private void OpenPause() { _screen = Screen.Pause; Time.timeScale = 0f; AudioManager.Click(); }
        private void Resume() { _screen = Screen.None; Time.timeScale = 1f; AudioManager.Click(); }

        // ---------------- drawing ----------------
        void OnGUI()
        {
            if (_screen == Screen.None) return;
            EnsureStyles();
            GUI.depth = -1000; // above the HUD

            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            switch (_screen)
            {
                case Screen.Main: DrawMain(); break;
                case Screen.Pause: DrawPause(); break;
                case Screen.Settings: DrawSettings(); break;
            }

            // Swallow clicks that MISSED a menu button — drawn AFTER the panel so the panel's own buttons get the
            // event first (IMGUI gives the click to the first control drawn that contains it; a full-screen button
            // drawn BEFORE the panel would eat every press — that was the "buttons don't work" bug).
            GUI.Button(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), GUIContent.none, GUIStyle.none);
        }

        private Rect Panel(float w, float h) =>
            new Rect(UnityEngine.Screen.width * 0.5f - w * 0.5f, UnityEngine.Screen.height * 0.5f - h * 0.5f, w, h);

        private void DrawMain()
        {
            var r = Panel(460, 430);
            GUILayout.BeginArea(r, _box);
            GUILayout.Space(10);
            GUILayout.Label("CAVEMAN", _title);
            GUILayout.Label("a factory from the Stone Age up", _sub);
            GUILayout.Space(22);
            if (GUILayout.Button("New Game", _btn)) { AudioManager.Click(); Resume(); }
            GUILayout.Space(6);
            bool any = AnySave();
            GUI.enabled = any;
            if (GUILayout.Button(any ? "Continue" : "Continue (no saves)", _btn)) { AudioManager.Click(); LoadNewest(); }
            GUI.enabled = true;
            GUILayout.Space(6);
            if (GUILayout.Button("Settings", _btn)) { AudioManager.Click(); _settingsReturn = Screen.Main; _screen = Screen.Settings; }
            GUILayout.Space(6);
            if (GUILayout.Button("Quit", _btn)) { AudioManager.Click(); Quit(); }
            GUILayout.EndArea();
        }

        private void DrawPause()
        {
            var r = Panel(500, 560);
            GUILayout.BeginArea(r, _box);
            GUILayout.Space(8);
            GUILayout.Label("Paused", _title);
            GUILayout.Space(10);
            if (GUILayout.Button("Resume", _btn)) { AudioManager.Click(); Resume(); }
            GUILayout.Space(10);
            GUILayout.Label("Save / Load", _sub);
            for (int i = 0; i < SlotCount; i++) DrawSlotRow(i);
            GUILayout.Space(10);
            if (GUILayout.Button("Settings", _btn)) { AudioManager.Click(); _settingsReturn = Screen.Pause; _screen = Screen.Settings; }
            GUILayout.Space(6);
            if (GUILayout.Button("Restart (new world)", _btn)) { AudioManager.Click(); Restart(); }
            GUILayout.Space(6);
            if (GUILayout.Button("Quit to Desktop", _btn)) { AudioManager.Click(); Quit(); }
            GUILayout.EndArea();
        }

        private void DrawSlotRow(int i)
        {
            GUILayout.BeginHorizontal();
            string info = SaveSystem.SlotInfo(i);
            GUILayout.Label($"Slot {i + 1}:  {(string.IsNullOrEmpty(info) ? "<empty>" : info)}", _row, GUILayout.Width(300));
            if (GUILayout.Button("Save", _row, GUILayout.Width(70))) { if (SaveSystem.Save(i)) AudioManager.Chime(); }
            GUI.enabled = SaveSystem.HasSave(i);
            if (GUILayout.Button("Load", _row, GUILayout.Width(70))) { if (SaveSystem.Load(i)) { AudioManager.Chime(); Resume(); } }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
        }

        private void DrawSettings()
        {
            var r = Panel(460, 360);
            GUILayout.BeginArea(r, _box);
            GUILayout.Space(8);
            GUILayout.Label("Settings", _title);
            GUILayout.Space(14);
            GameSettings.Master = Slider("Master volume", GameSettings.Master);
            GameSettings.Sfx = Slider("Sound effects", GameSettings.Sfx);
            GameSettings.Music = Slider("Music", GameSettings.Music);
            GameSettings.Apply();
            GUILayout.Space(8);
            bool fs = GUILayout.Toggle(GameSettings.Fullscreen, "  Fullscreen", _row);
            if (fs != GameSettings.Fullscreen) { GameSettings.Fullscreen = fs; GameSettings.Apply(); }
            GUILayout.Space(18);
            if (GUILayout.Button("Back", _btn)) { AudioManager.Click(); GameSettings.Save(); _screen = _settingsReturn; }
            GUILayout.EndArea();
        }

        private float Slider(string label, float v)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _row, GUILayout.Width(150));
            float nv = GUILayout.HorizontalSlider(v, 0f, 1f, GUILayout.Width(220));
            GUILayout.Label($"{Mathf.RoundToInt(nv * 100)}%", _row, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            return nv;
        }

        // ---------------- actions ----------------
        private static bool AnySave()
        {
            for (int i = 0; i < SlotCount; i++) if (SaveSystem.HasSave(i)) return true;
            return false;
        }
        private void LoadNewest()
        {
            for (int i = 0; i < SlotCount; i++) if (SaveSystem.HasSave(i)) { if (SaveSystem.Load(i)) { AudioManager.Chime(); Resume(); } return; }
        }
        private void Restart()
        {
            Time.timeScale = 1f;
            try { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
            catch { Toast.Show("<color=#f99>Restart needs the scene in Build Settings — reopen it to start over.</color>"); Resume(); }
        }
        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void EnsureStyles()
        {
            if (_styles) return;
            _styles = true;
            _box = new GUIStyle(GUI.skin.box) { padding = new RectOffset(24, 24, 18, 18) };
            _title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            _sub = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 20, fixedHeight = 42 };
            _row = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft, richText = true };
        }
    }
}
