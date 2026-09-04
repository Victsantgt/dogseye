using UnityEngine;
using UnityEngine.UI;

namespace Patterns.Singleton
{
    public class GameConfig : ASingleton<GameConfig>
    {
        private bool invincible = false;
        private float noteSpeed = 4;
        private float bpm = 210;
        private float masterVolume;
        private float bgmVolume;
        private float sfxVolume;
        private bool maxCombo = false;
        private string playerPrefsKey = "PlayerPrefsKeyName";

        public Image iNormal;
        public Image iInv;
        public Sprite on;
        public Sprite off;

        void Start()
        {
            Screen.SetResolution(1920, 1080, true);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }

        public bool GetInvincible() { return invincible; }
        public float GetNoteSpeed() { return noteSpeed; }
        public float GetBPM() { return bpm; }
        public string GetplayerPrefsKey() { return playerPrefsKey; }
        public void setNoteSpeed(float value) { this.noteSpeed = value / 10; }
        public void setMasterVolume(float volume) { this.masterVolume = volume; }
        public void setBGMVolume(float volume) { this.bgmVolume = volume; }
        public void setSFXVolume(float volume) { this.sfxVolume = volume; }
        public void setBPM(float value) { this.bpm = value; }
        public void setBPM(string playerPrefsKey) { this.playerPrefsKey = playerPrefsKey; }
        public bool IsMaxCombo() { return maxCombo; }
        public void SetMaxCombo(bool value) { maxCombo = value; }

        public void alternateInvincible(bool value)
        {
            if (invincible == value) return;
            invincible = value;
            if (value)
            {
                iNormal.sprite = off;
                iInv.sprite = on;
            }
            else
            {
                iNormal.sprite = on;
                iInv.sprite = off;
            }
        }
    }
}
