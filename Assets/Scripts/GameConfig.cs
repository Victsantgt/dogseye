using UnityEngine;
using UnityEngine.UI;

namespace Patterns.Singleton
{
    public class GameConfig : ASingleton<GameConfig>
    {
        private float noteSpeed = 4;
        private float bpm = 210;
        private float masterVolume;
        private float bgmVolume;
        private float sfxVolume;
        private string playerPrefsKey = "PlayerPrefsKeyName";

        void Start()
        {
            Screen.SetResolution(1920, 1080, true);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }

        public float GetNoteSpeed() { return noteSpeed; }
        public float GetBPM() { return bpm; }
        public string GetplayerPrefsKey() { return playerPrefsKey; }
        public void setNoteSpeed(float value) { this.noteSpeed = value / 10; }
        public void setMasterVolume(float volume) { this.masterVolume = volume; }
        public void setBGMVolume(float volume) { this.bgmVolume = volume; }
        public void setSFXVolume(float volume) { this.sfxVolume = volume; }
        public void setBPM(float value) { this.bpm = value; }
        public void setBPM(string playerPrefsKey) { this.playerPrefsKey = playerPrefsKey; }
    }
}
