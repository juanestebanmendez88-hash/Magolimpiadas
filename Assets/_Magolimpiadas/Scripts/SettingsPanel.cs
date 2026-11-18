using UnityEngine;
using UnityEngine.UI;

namespace ArcaneGames.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        public const string FullscreenKey = "fullscreen";

        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle fullscreenToggle;

        private bool initializing;

        private void OnEnable()
        {
            initializing = true;

            if (AudioManager.Instance != null)
            {
                if (musicSlider != null) musicSlider.value = AudioManager.Instance.MusicVolume;
                if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.SfxVolume;
            }

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = Screen.fullScreen;

            initializing = false;
        }

        public void OnMusicChanged(float value)
        {
            if (!initializing && AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
        }

        public void OnSfxChanged(float value)
        {
            if (initializing || AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetSfxVolume(value);
            AudioManager.Instance.PlayButtonClick();
        }

        public void OnFullscreenChanged(bool value)
        {
            if (initializing)
                return;

            Screen.fullScreen = value;
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        }
    }
}
