using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Magolimpiadas/Catálogo de audios", fileName = "AudiosJuego")]
public class GameAudio : ScriptableObject
{
    public enum Sound
    {
        Click, Checkpoint, Medal, Applause, Ovation, Hit, Jump, Step,
        FirePower, IcePower, PlantPower, LightningPower, NebulaPower,
        MonsterRoar, MinotaurRoar, MinotaurNear, Torch, Stroke, FailedNote, PingPong,
    }

    public enum Track { Menu, Island, Labyrinth, Temple, PingPong, Swimming, ArtisticSwimming, Ceremony }

    [System.Serializable]
    public class SoundEntrance
    {
        public Sound sound;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [System.Serializable]
    public class TrackEntrance
    {
        public Track track;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
    }

    public List<SoundEntrance> sounds = new List<SoundEntrance>();
    public List<TrackEntrance> tracks = new List<TrackEntrance>();

    static GameAudio catalog;
    static bool searched;

    public static GameAudio Catalog
    {
        get
        {
            if (!searched) { catalog = Resources.Load<GameAudio>("AudiosJuego"); searched = true; }
            return catalog;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Restart() { catalog = null; searched = false; }

    public static AudioClip Clip(Sound s, out float volume)
    {
        volume = 1f;
        var c = Catalog;
        if (c == null) return null;
        foreach (var e in c.sounds)
            if (e != null && e.sound == s && e.clip != null) { volume = e.volume; return e.clip; }
        return null;
    }

    public static AudioClip Music(Track p, out float volume)
    {
        volume = 0.7f;
        var c = Catalog;
        if (c == null) return null;
        foreach (var e in c.tracks)
            if (e != null && e.track == p && e.clip != null) { volume = e.volume; return e.clip; }
        return null;
    }

    public static Sound PowerOf(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return Sound.FirePower;
            case MagicElement.Ice: return Sound.IcePower;
            case MagicElement.Plant: return Sound.PlantPower;
            case MagicElement.Lightning: return Sound.LightningPower;
        }
        return Sound.NebulaPower;
    }

    public static Sound MagePower(string mageId)
    {
        switch (mageId)
        {
            case "elemental": return Sound.FirePower;
            case "druida": return Sound.PlantPower;
            case "rayo": return Sound.LightningPower;
        }
        return Sound.NebulaPower;
    }
}
