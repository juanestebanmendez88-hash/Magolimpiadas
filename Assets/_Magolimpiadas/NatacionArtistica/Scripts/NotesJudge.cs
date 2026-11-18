using System;
using System.Collections.Generic;
using UnityEngine;

public class NotesJudge
{
    public class Note
    {
        public RoutineNote data;
        public bool judged;
        public NoteJudgment judgment;
        public float judgmentBeat;
        public readonly float[] botBeat = { -1f, -1f, -1f, -1f };
        public readonly NoteJudgment[] botJudgment = new NoteJudgment[4];
        public readonly bool[] pressed = new bool[4];
        public readonly NoteJudgment[] laneJudgment = new NoteJudgment[4];

        public bool IsForEveryone => data.type == NoteType.White;
    }

    public readonly List<Note> Notes = new List<Note>();

    public event Action<Note, int, NoteJudgment> OnJudge;
    public event Action<int, bool> OnPress;
    public event Action<Note, int> OnTouchTrap;
    public event Action<int, NoteJudgment, bool> LocalLaneJudged;

    public readonly bool[] RemoteLanes = new bool[4];
    public float RemoteGraceSeconds { get; set; } = 0.45f;

    SwimmingSettings settings;
    RoutineFigure figure;
    float secondsPerBeat;

    public int PlayerLane { get; set; }
    public int Possible { get; private set; }

    public void Prepare(RoutineFigure figure, SwimmingSettings settings, float secondsPerBeat, int playerLane, int seed)
    {
        this.figure = figure;
        this.settings = settings;
        this.secondsPerBeat = secondsPerBeat;
        PlayerLane = playerLane;
        Notes.Clear();
        Possible = 0;
        var random = new System.Random(seed);
        foreach (var d in figure.notes)
        {
            var n = new Note { data = d };
            for (int c = 0; c < 4; c++) DecideBot(n, c, random);
            Notes.Add(n);
            if (d.type == NoteType.Color || d.type == NoteType.White) Possible++;
        }
        Notes.Sort((a, b) => a.data.beat.CompareTo(b.data.beat));
    }

    void DecideBot(Note n, int lane, System.Random random)
    {
        if (n.data.type == NoteType.Trap) return;
        if (!n.IsForEveryone && n.data.lane != lane) return;
        if (RemoteLanes[lane]) return;
        double hit = n.IsForEveryone ? settings.botWhiteAccuracy : settings.botAccuracy;
        if (random.NextDouble() > hit) return;
        bool perfect = random.NextDouble() < settings.botsPerfect;
        float offset = perfect
            ? (float)(random.NextDouble() * 2.0 - 1.0) * settings.perfectWindow * 0.4f
            : Mathf.Lerp(settings.perfectWindow * 1.15f, settings.goodWindow * 0.85f, (float)random.NextDouble()) * (random.NextDouble() < 0.5 ? -1f : 1f);
        n.botBeat[lane] = n.data.beat + offset / secondsPerBeat;
        n.botJudgment[lane] = perfect ? NoteJudgment.Perfect : NoteJudgment.Good;
    }

    public bool InWave(float beat)
    {
        if (figure == null) return false;
        foreach (float o in figure.waves) if (beat >= o && beat < o + settings.waveBeats) return true;
        return false;
    }

    float Factor(float beat) => InWave(beat) ? settings.windowDuringWave : 1f;

    public void PlayerPress(float beat)
    {
        int lane = PlayerLane;
        if (lane < 0 || lane > 3) return;
        float factor = Factor(beat);
        float window = settings.goodWindow * factor;
        Note best = null;
        float bestDt = float.MaxValue;
        foreach (var n in Notes)
        {
            if (n.judged) continue;
            if (!n.IsForEveryone && n.data.lane != lane) continue;
            if (n.IsForEveryone && n.pressed[lane]) continue;
            float dt = Mathf.Abs(beat - n.data.beat) * secondsPerBeat;
            if (dt <= window && dt < bestDt) { best = n; bestDt = dt; }
        }
        OnPress?.Invoke(lane, best != null && best.data.type != NoteType.Trap);
        if (best == null) return;
        if (best.data.type == NoteType.Trap)
        {
            best.judged = true;
            best.judgment = NoteJudgment.Miss;
            best.judgmentBeat = beat;
            OnTouchTrap?.Invoke(best, lane);
            LocalLaneJudged?.Invoke(Notes.IndexOf(best), NoteJudgment.Miss, true);
            return;
        }
        var judgment = bestDt <= settings.perfectWindow * factor ? NoteJudgment.Perfect : NoteJudgment.Good;
        Mark(best, lane, judgment, beat);
        LocalLaneJudged?.Invoke(Notes.IndexOf(best), judgment, false);
    }

    public void ApplyRemoteJudgment(int noteIndex, int lane, NoteJudgment judgment, bool trap)
    {
        if (noteIndex < 0 || noteIndex >= Notes.Count || lane < 0 || lane > 3 || lane == PlayerLane) return;
        var n = Notes[noteIndex];
        if (n.judged) return;
        if (trap)
        {
            if (n.data.type != NoteType.Trap) return;
            n.judged = true;
            n.judgment = NoteJudgment.Miss;
            n.judgmentBeat = n.data.beat;
            OnPress?.Invoke(lane, false);
            OnTouchTrap?.Invoke(n, lane);
            return;
        }
        if (n.data.type == NoteType.Trap || (!n.IsForEveryone && n.data.lane != lane) || (n.IsForEveryone && n.pressed[lane])) return;
        OnPress?.Invoke(lane, true);
        Mark(n, lane, judgment, n.data.beat);
    }

    bool WaitsForRemoteLane(Note n)
    {
        if (!n.IsForEveryone) return n.data.lane >= 0 && n.data.lane < 4 && RemoteLanes[n.data.lane];
        for (int c = 0; c < 4; c++) if (RemoteLanes[c] && !n.pressed[c]) return true;
        return false;
    }

    public void Refresh(float beat)
    {
        foreach (var n in Notes)
        {
            if (n.judged) continue;
            for (int c = 0; c < 4; c++)
            {
                if (c == PlayerLane || n.botBeat[c] < 0f || beat < n.botBeat[c]) continue;
                if (n.IsForEveryone && n.pressed[c]) continue;
                if (!n.IsForEveryone && n.data.lane != c) continue;
                OnPress?.Invoke(c, true);
                Mark(n, c, n.botJudgment[c], beat);
                if (n.judged) break;
            }
            if (n.judged) continue;
            float past = (beat - n.data.beat) * secondsPerBeat;
            float limit = settings.goodWindow * Factor(n.data.beat) + (WaitsForRemoteLane(n) ? RemoteGraceSeconds : 0f);
            if (past <= limit) continue;
            n.judged = true;
            n.judgment = NoteJudgment.Miss;
            n.judgmentBeat = beat;
            if (n.data.type != NoteType.Trap) OnJudge?.Invoke(n, n.IsForEveryone ? -1 : n.data.lane, NoteJudgment.Miss);
        }
    }

    void Mark(Note n, int lane, NoteJudgment judgment, float beat)
    {
        if (!n.IsForEveryone)
        {
            n.judged = true;
            n.judgment = judgment;
            n.judgmentBeat = beat;
            OnJudge?.Invoke(n, lane, judgment);
            return;
        }
        n.pressed[lane] = true;
        n.laneJudgment[lane] = judgment;
        for (int c = 0; c < 4; c++) if (!n.pressed[c]) return;
        var worst = NoteJudgment.Perfect;
        for (int c = 0; c < 4; c++) if (n.laneJudgment[c] == NoteJudgment.Good) worst = NoteJudgment.Good;
        n.judged = true;
        n.judgment = worst;
        n.judgmentBeat = beat;
        OnJudge?.Invoke(n, -1, worst);
    }

    public bool Finished(float beat)
    {
        if (figure == null) return true;
        if (beat < figure.beats) return false;
        foreach (var n in Notes) if (!n.judged && n.data.type != NoteType.Trap) return false;
        return true;
    }
}
