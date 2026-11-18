using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RoutineGenerator
{
    public const string RoutinePath = "Assets/_Magolimpiadas/NatacionArtistica/Datos/RutinaNatacion.asset";
    const float MinSeparation = 0.5f;
    const int BeatsBeforeFirst = 16;
    const int BeatsBetweenFigures = 8;

    [MenuItem("Magolimpiadas/Natación artística/Generar rutina de prueba")]
    static void GenerateMenu()
    {
        var routine = AssetDatabase.LoadAssetAtPath<SwimmingRoutine>(RoutinePath);
        if (routine == null) { Debug.LogWarning("[Rutina] No existe " + RoutinePath + ". Usa primero Preparar nivel."); return; }
        if (!EditorUtility.DisplayDialog("Rutina de prueba", "Se reemplazan todas las notas de la rutina por una generada con " + routine.bpm + " BPM. ¿Seguir?", "Generar", "Cancelar")) return;
        Generate(routine);
        Debug.Log("[Rutina] " + Validate(routine));
    }

    [MenuItem("Magolimpiadas/Natación artística/Validar rutina")]
    static void ValidateMenu()
    {
        var routine = Selection.activeObject as SwimmingRoutine ?? AssetDatabase.LoadAssetAtPath<SwimmingRoutine>(RoutinePath);
        Debug.Log("[Rutina] " + (routine != null ? Validate(routine) : "no encontré la rutina"));
    }

    public static void Generate(SwimmingRoutine routine)
    {
        Undo.RecordObject(routine, "Generar rutina");
        float spb = routine.SecondsPerBeat;
        routine.figures = new List<RoutineFigure> { Line(spb), Diamond(spb), Square(spb), Flower(spb) };
        int cursor = BeatsBeforeFirst;
        foreach (var f in routine.figures)
        {
            f.songStart = routine.song != null ? routine.songOffset + cursor * spb : 0f;
            cursor += f.beats + BeatsBetweenFigures;
        }
        EditorUtility.SetDirty(routine);
        AssetDatabase.SaveAssets();
    }

    static int Beats(float seconds, float spb) => Mathf.Max(16, Mathf.RoundToInt(seconds / spb / 4f) * 4);

    static float Step(float secondsBetweenNotes, float spb)
    {
        float p = secondsBetweenNotes / spb;
        return p < 0.75f ? 0.5f : p < 1.5f ? 1f : 2f;
    }

    static RoutineFigure Line(float spb)
    {
        var f = new RoutineFigure { displayName = "Línea", formation = Formation.Line, beats = Beats(22f, spb), fallBeats = 1.8f / spb };
        float step = Step(0.8f, spb);
        int[] pattern = { 0, 1, 2, 3, 3, 2, 1, 0, 0, 2, 1, 3, 3, 1, 2, 0 };
        int i = 0;
        for (float b = 4f; b <= f.beats - 4f; b += step) Add(f, b, pattern[i++ % pattern.Length], NoteType.Color);
        return Sort(f);
    }

    static RoutineFigure Diamond(float spb)
    {
        var f = new RoutineFigure { displayName = "Diamante", formation = Formation.Diamond, beats = Beats(24f, spb), fallBeats = 1.65f / spb };
        for (int w = 12; w < f.beats - 4; w += 12) Add(f, w, -1, NoteType.White);
        Add(f, f.beats - 3, -1, NoteType.White);
        float step = Step(0.5f, spb);
        int[] pattern = { 0, 2, 1, 3, 1, 3, 0, 2 };
        int i = 0;
        for (float b = 4f; b <= f.beats - 4f; b += step)
        {
            if (Mathf.Repeat(b, 4f) >= 3f) continue;
            Add(f, b, pattern[i++ % pattern.Length], NoteType.Color);
        }
        return Sort(f);
    }

    static RoutineFigure Square(float spb)
    {
        var f = new RoutineFigure { displayName = "Cuadrado", formation = Formation.Square, beats = Beats(25f, spb), fallBeats = 1.45f / spb };
        for (int w = 16; w < f.beats - 4; w += 16) Add(f, w, -1, NoteType.White);
        Add(f, f.beats - 2, -1, NoteType.White);
        float step = Step(0.48f, spb);
        int[] pattern = { 0, 1, 2, 3, 2, 1, 3, 2, 1, 0, 1, 2 };
        int i = 0;
        for (float b = 4f; b <= f.beats - 4f; b += step)
        {
            Add(f, b, pattern[i++ % pattern.Length], NoteType.Color);
            if (step >= 1f && Mathf.Repeat(b, 8f) == 6f) Add(f, b + 0.5f, pattern[(i + 2) % pattern.Length], NoteType.Color);
        }
        GoldenNotes(f, Distribute(f.beats, 4, 0.2f));
        Traps(f, Distribute(f.beats, 5, 0.12f).Select(x => x + 0.5f).ToArray());
        f.waves = new List<float> { Mathf.Round(f.beats * 0.4f), Mathf.Round(f.beats * 0.7f) };
        return Sort(f);
    }

    static RoutineFigure Flower(float spb)
    {
        var f = new RoutineFigure { displayName = "Flor", formation = Formation.Flower, beats = Beats(28f, spb), fallBeats = 1.3f / spb };
        for (int w = 8; w < f.beats - 12; w += 8) Add(f, w, -1, NoteType.White);
        for (int w = f.beats - 8; w < f.beats; w += 4) Add(f, w, -1, NoteType.White);
        float step = Step(0.4f, spb);
        int[] pattern = { 0, 1, 2, 3, 3, 2, 1, 0, 2, 1, 3, 0 };
        int i = 0;
        for (float b = 4f; b <= f.beats - 6f; b += step)
        {
            Add(f, b, pattern[i++ % pattern.Length], NoteType.Color);
            if (step >= 1f && Mathf.Repeat(b, 4f) == 2f) Add(f, b + 0.5f, pattern[(i + 1) % pattern.Length], NoteType.Color);
        }
        GoldenNotes(f, Distribute(f.beats, 4, 0.15f));
        Traps(f, Distribute(f.beats, 4, 0.1f).Select(x => x + 0.5f).ToArray());
        f.waves = new List<float> { Mathf.Round(f.beats * 0.35f), Mathf.Round(f.beats * 0.7f) };
        return Sort(f);
    }

    static float[] Distribute(int beats, int n, float margin)
    {
        var r = new float[n];
        for (int k = 0; k < n; k++) r[k] = Mathf.Round(Mathf.Lerp(beats * margin, beats * (1f - margin), (k + 0.5f) / n));
        return r;
    }

    static void GoldenNotes(RoutineFigure f, float[] beats)
    {
        int lane = 3;
        foreach (float b in beats)
        {
            int i = f.notes.FindIndex(n => Mathf.Approximately(n.beat, b) && n.type == NoteType.Color);
            if (i >= 0) { var n = f.notes[i]; n.type = NoteType.Golden; f.notes[i] = n; }
            else Add(f, b, lane, NoteType.Golden);
            lane = (lane + 1) % 4;
        }
    }

    static void Traps(RoutineFigure f, float[] beats)
    {
        int start = 2;
        foreach (float b in beats)
        {
            for (int k = 0; k < 4; k++)
            {
                int c = (start + k) % 4;
                if (f.notes.Any(n => (n.lane == c || n.type == NoteType.White) && Mathf.Abs(n.beat - b) < 1f)) continue;
                f.notes.Add(new RoutineNote(b, c, NoteType.Trap));
                break;
            }
            start = (start + 1) % 4;
        }
    }

    static void Add(RoutineFigure f, float beat, int lane, NoteType type)
    {
        if (beat < 1f || beat > f.beats) return;
        bool white = type == NoteType.White;
        foreach (var n in f.notes)
        {
            float d = Mathf.Abs(n.beat - beat);
            bool same = white || n.type == NoteType.White || n.lane == lane;
            if (same && d < (white || n.type == NoteType.White ? 0.75f : MinSeparation)) return;
        }
        f.notes.Add(new RoutineNote(beat, white ? -1 : lane, type));
    }

    static RoutineFigure Sort(RoutineFigure f)
    {
        f.notes = f.notes.OrderBy(n => n.beat).ThenBy(n => n.lane).ToList();
        return f;
    }

    public static string Validate(SwimmingRoutine routine)
    {
        var problems = new List<string>();
        if (routine.bpm < 40f || routine.bpm > 240f) problems.Add("BPM raro (" + routine.bpm + ")");
        if (routine.figures.Count == 0) problems.Add("no hay figuras");
        for (int fi = 0; fi < routine.figures.Count; fi++)
        {
            var f = routine.figures[fi];
            string displayName = "Figura " + (fi + 1) + " (" + f.displayName + ")";
            if (f.fallBeats * routine.SecondsPerBeat < 0.6f) problems.Add(displayName + ": las notas caen demasiado rápido");
            int possible = 0;
            for (int i = 0; i < f.notes.Count; i++)
            {
                var n = f.notes[i];
                if (n.type != NoteType.White && (n.lane < 0 || n.lane > 3)) problems.Add(displayName + ": nota en el pulso " + n.beat + " con carril " + n.lane);
                if (n.beat < 0f || n.beat > f.beats) problems.Add(displayName + ": nota fuera de la figura (pulso " + n.beat + ")");
                if (n.type == NoteType.Color || n.type == NoteType.White) possible++;
                for (int j = i + 1; j < f.notes.Count; j++)
                {
                    var m = f.notes[j];
                    bool same = n.type == NoteType.White || m.type == NoteType.White || n.lane == m.lane;
                    if (same && Mathf.Abs(n.beat - m.beat) < 0.25f) problems.Add(displayName + ": notas encimadas en el pulso " + n.beat);
                }
            }
            if (possible == 0) problems.Add(displayName + ": no tiene notas que puntúen");
            foreach (float o in f.waves) if (o < 0f || o > f.beats) problems.Add(displayName + ": ola fuera de la figura (" + o + ")");
            if (routine.song != null && f.songStart + f.beats * routine.SecondsPerBeat > routine.song.length)
                problems.Add(displayName + ": la canción se acaba antes que la figura");
        }
        int total = routine.figures.Sum(x => x.notes.Count);
        float seconds = routine.figures.Sum(x => x.beats) * routine.SecondsPerBeat;
        string summary = routine.figures.Count + " figuras, " + total + " notas, " + seconds.ToString("0") + " s de rutina a " + routine.bpm + " BPM";
        return problems.Count == 0 ? "Rutina en orden: " + summary + "." : "Rutina con " + problems.Count + " problemas (" + summary + "):\n- " + string.Join("\n- ", problems.Distinct());
    }
}
