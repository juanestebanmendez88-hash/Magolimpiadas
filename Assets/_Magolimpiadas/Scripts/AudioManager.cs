using UnityEngine;

namespace ArcaneGames
{
    public class AudioManager : MonoBehaviour
    {
        public const string MusicVolumeKey = "music_volume";
        public const string SfxVolumeKey = "sfx_volume";

        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip buttonClickClip;
        [Range(0f, 1f)] [SerializeField] private float songVolume = 0.12f;

        public float MusicVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 1f;
        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            ApplyVolumes();

            if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            ApplyVolumes();
        }

        public void PlayButtonClick()
        {
            if (sfxSource != null && buttonClickClip != null)
                sfxSource.PlayOneShot(buttonClickClip);
        }

        public void PauseMusic()
        {
            if (musicSource != null)
                musicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (musicSource == null || musicSource.clip == null)
                return;

            musicSource.UnPause();
            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        private void ApplyVolumes()
        {
            if (musicSource != null) musicSource.volume = MusicVolume * songVolume;
            if (sfxSource != null) sfxSource.volume = SfxVolume;
        }
    }
}
