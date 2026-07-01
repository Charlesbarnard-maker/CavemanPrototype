using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Player options, persisted in PlayerPrefs (independent of a game save). Volumes drive the
    /// AudioManager + the global AudioListener; fullscreen toggles the window mode.
    /// </summary>
    public static class GameSettings
    {
        public static float Master = 0.8f;
        public static float Sfx = 0.9f;
        public static float Music = 0.35f;   // gentle by default — a soft ambient bed, not a soundtrack
        public static bool Fullscreen;

        private const string KMaster = "cv_master", KSfx = "cv_sfx", KMusic = "cv_music", KFull = "cv_fullscreen";

        public static void Load()
        {
            Master = PlayerPrefs.GetFloat(KMaster, 0.8f);
            Sfx = PlayerPrefs.GetFloat(KSfx, 0.9f);
            Music = PlayerPrefs.GetFloat(KMusic, 0.35f);
            Fullscreen = PlayerPrefs.GetInt(KFull, 0) == 1;
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(KMaster, Master);
            PlayerPrefs.SetFloat(KSfx, Sfx);
            PlayerPrefs.SetFloat(KMusic, Music);
            PlayerPrefs.SetInt(KFull, Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Push the current values to the engine (global volume + window mode).</summary>
        public static void Apply()
        {
            AudioListener.volume = Mathf.Clamp01(Master);
            if (Screen.fullScreen != Fullscreen) Screen.fullScreen = Fullscreen;
        }

        // Per-channel volumes already fold in master via AudioListener.volume, so these are the channel trims.
        public static float SfxVolume => Mathf.Clamp01(Sfx);
        public static float MusicVolume => Mathf.Clamp01(Music);
    }
}
