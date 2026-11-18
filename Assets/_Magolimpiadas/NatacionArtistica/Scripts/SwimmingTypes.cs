using System.Collections.Generic;

public enum NoteType { Color, White, Golden, Trap }

public enum NoteJudgment { Perfect, Good, Miss }

public enum Formation { Line, Diamond, Square, Flower }

[System.Serializable]
public struct RoutineNote
{
    public float beat;
    public int lane;
    public NoteType type;

    public RoutineNote(float beat, int lane, NoteType type) { this.beat = beat; this.lane = lane; this.type = type; }
}

[System.Serializable]
public class RoutineFigure
{
    public string displayName = "Línea";
    public Formation formation;
    public int beats = 40;
    public float fallBeats = 3f;
    public float songStart;
    public List<RoutineNote> notes = new List<RoutineNote>();
    public List<float> waves = new List<float>();
}
