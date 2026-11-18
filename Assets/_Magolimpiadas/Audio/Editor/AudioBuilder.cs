using System.IO;
using UnityEditor;
using UnityEngine;

public static class AudioBuilder
{
    const string Root = "Assets/_Magolimpiadas/Audio";
    const string CatalogPath = Root + "/Resources/AudiosJuego.asset";

    static readonly (GameAudio.Track track, string file, float volume)[] Tracks =
    {
        (GameAudio.Track.Menu, "Musica/Mus_Menu.mp3", 0.7f),
        (GameAudio.Track.Island, "Musica/Mus_Isla.mp3", 0.55f),
        (GameAudio.Track.Labyrinth, "Musica/Mus_Laberinto.mp3", 0.55f),
        (GameAudio.Track.Temple, "Musica/Mus_Templo.mp3", 0.5f),
        (GameAudio.Track.PingPong, "Musica/Mus_PingPong.mp3", 0.55f),
        (GameAudio.Track.Swimming, "Musica/Mus_Natacion.mp3", 0.55f),
        (GameAudio.Track.ArtisticSwimming, "Musica/Mus_NatacionArtistica_151BPM.mp3", 0.8f),
        (GameAudio.Track.Ceremony, "Musica/Mus_Ceremonia.mp3", 0.65f),
    };

    static readonly (GameAudio.Sound sound, string file, float volume)[] Sounds =
    {
        (GameAudio.Sound.Click, "SFX/SFX_Click.wav", 1f),
        (GameAudio.Sound.Checkpoint, "SFX/SFX_Checkpoint.wav", 0.8f),
        (GameAudio.Sound.Medal, "SFX/SFX_Medalla.wav", 0.8f),
        (GameAudio.Sound.Applause, "SFX/SFX_Aplauso.ogg", 0.7f),
        (GameAudio.Sound.Ovation, "SFX/SFX_Ovacion.wav", 0.6f),
        (GameAudio.Sound.Hit, "SFX/SFX_Golpe.wav", 0.7f),
        (GameAudio.Sound.Jump, "SFX/SFX_Salto.wav", 0.7f),
        (GameAudio.Sound.Step, "SFX/SFX_Paso.wav", 0.6f),
        (GameAudio.Sound.FirePower, "SFX/SFX_PoderFuego.wav", 0.7f),
        (GameAudio.Sound.IcePower, "SFX/SFX_PoderHielo.wav", 0.7f),
        (GameAudio.Sound.PlantPower, "SFX/SFX_PoderPlanta.wav", 0.7f),
        (GameAudio.Sound.LightningPower, "SFX/SFX_PoderRayo.wav", 0.6f),
        (GameAudio.Sound.NebulaPower, "SFX/SFX_PoderNebuloso.wav", 0.7f),
        (GameAudio.Sound.MonsterRoar, "SFX/SFX_RugidoMonstruo.wav", 0.8f),
        (GameAudio.Sound.MinotaurRoar, "SFX/SFX_RugidoMinotauro.mp3", 0.9f),
        (GameAudio.Sound.MinotaurNear, "SFX/SFX_MinotauroCerca.ogg", 0.8f),
        (GameAudio.Sound.Torch, "SFX/SFX_Antorcha.wav", 0.9f),
        (GameAudio.Sound.Stroke, "SFX/SFX_Brazada.wav", 0.8f),
        (GameAudio.Sound.FailedNote, "SFX/SFX_NotaFallida.wav", 0.7f),
        (GameAudio.Sound.PingPong, "SFX/SFX_PingPong.wav", 0.8f),
    };

    [MenuItem("Magolimpiadas/Audio/Preparar catálogo y música")]
    static void Menu() => Debug.Log("[Audio] " + Prepare() + " Guarda la escena (Ctrl+S).");

    public static string Prepare()
    {
        Directory.CreateDirectory(Root + "/Resources");
        AssetDatabase.Refresh();
        var catalog = AssetDatabase.LoadAssetAtPath<GameAudio>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<GameAudio>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        int remaining = 0;
        catalog.tracks.Clear();
        foreach (var (track, file, volume) in Tracks)
        {
            var clip = Import(Root + "/" + file, true);
            if (clip == null) { remaining++; continue; }
            catalog.tracks.Add(new GameAudio.TrackEntrance { track = track, clip = clip, volume = volume });
        }
        var beforeVolumes = new System.Collections.Generic.Dictionary<GameAudio.Sound, float>();
        foreach (var e in catalog.sounds) if (e != null) beforeVolumes[e.sound] = e.volume;
        catalog.sounds.Clear();
        foreach (var (sound, file, volume) in Sounds)
        {
            var clip = Import(Root + "/" + file, false);
            if (clip == null) { remaining++; continue; }
            catalog.sounds.Add(new GameAudio.SoundEntrance { sound = sound, clip = clip, volume = beforeVolumes.TryGetValue(sound, out float v) ? v : volume });
        }
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return catalog.tracks.Count + " pistas y " + catalog.sounds.Count + " sonidos en el catálogo" + (remaining > 0 ? " (faltan " + remaining + ")" : "") + "; " + EnsureMusic(false);
    }

    public static string EnsureMusic(bool fixedTrack, GameAudio.Track track = GameAudio.Track.Ceremony)
    {
        var m = Object.FindFirstObjectByType<GameMusic>(FindObjectsInactive.Include);
        if (m == null)
        {
            var go = new GameObject("Música del juego");
            Undo.RegisterCreatedObjectUndo(go, "Música");
            m = go.AddComponent<GameMusic>();
        }
        m.useFixedTrack = fixedTrack;
        m.fixedTrack = track;
        EditorUtility.SetDirty(m);
        return "música del juego lista" + (fixedTrack ? " (" + track + ")" : " (por zonas)");
    }

    static AudioClip Import(string path, bool music)
    {
        var ai = AssetImporter.GetAtPath(path) as AudioImporter;
        if (ai == null) { Debug.LogWarning("[Audio] No encontré " + path); return null; }
        long size = new FileInfo(path).Length;
        var s = ai.defaultSampleSettings;
        if (music)
        {
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.55f;
            s.preloadAudioData = false;
            ai.loadInBackground = true;
        }
        else
        {
            bool length = size > 400 * 1024;
            s.loadType = length ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = length ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            s.quality = 0.6f;
            s.preloadAudioData = true;
            ai.forceToMono = true;
        }
        ai.defaultSampleSettings = s;
        ai.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
