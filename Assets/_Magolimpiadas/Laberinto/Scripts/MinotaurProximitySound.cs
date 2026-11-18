using UnityEngine;

public class MinotaurProximitySound : MonoBehaviour
{
    private const float FadeSeconds = 1.2f;
    private const float MinimumFadeStep = 0.05f;
    private const float FullVolumeDistance = 3f;
    private const float SilentDistance = 20f;

    private AudioSource source;
    private PlayerMage player;
    private float catalogVolume;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMage>();
        AudioClip clip = GameAudio.Clip(GameAudio.Sound.MinotaurNear, out catalogVolume);
        if (clip == null)
            return;

        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = FullVolumeDistance;
        source.maxDistance = SilentDistance;
        source.volume = 0f;
    }

    private void Update()
    {
        if (source == null)
            return;

        bool audible = IsPlayerInLabyrinth();
        float targetVolume = audible ? catalogVolume * GameSound.EffectsVolume : 0f;
        float step = Time.unscaledDeltaTime / FadeSeconds * Mathf.Max(MinimumFadeStep, catalogVolume);
        source.volume = Mathf.MoveTowards(source.volume, targetVolume, step);

        if (audible && !source.isPlaying)
            source.Play();
        else if (!audible && source.isPlaying && source.volume <= 0.001f)
            source.Stop();
    }

    private void OnDisable()
    {
        if (source == null)
            return;

        source.Stop();
        source.volume = 0f;
    }

    private bool IsPlayerInLabyrinth()
    {
        WorldSwitcher world = WorldSwitcher.Instance;
        bool inAnotherWorld = world != null && (world.InWorld2D || world.InOtherLevel);
        return player != null && player.InLabyrinth && !inAnotherWorld;
    }
}
