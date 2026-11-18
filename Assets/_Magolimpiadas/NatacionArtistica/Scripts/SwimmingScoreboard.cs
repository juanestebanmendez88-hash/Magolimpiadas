using System;
using UnityEngine;

public class SwimmingScoreboard
{
    public int Points { get; private set; }
    public int Combo { get; private set; }
    public int BestCombo { get; private set; }
    public int Stars { get; private set; }
    public readonly int[] LaneHits = new int[4];
    public readonly int[] LaneMisses = new int[4];

    float weighted;
    int possible;
    int pointsOnBegin, starsOnBegin;

    public event Action OnWinStar;

    public float Precision => possible <= 0 ? 1f : Mathf.Clamp01(weighted / possible);

    public void RestartAll()
    {
        Points = Combo = BestCombo = Stars = 0;
        Array.Clear(LaneHits, 0, 4);
        Array.Clear(LaneMisses, 0, 4);
    }

    public void BeginFigure(int possibleNotes)
    {
        weighted = 0f;
        possible = possibleNotes;
        Combo = 0;
        pointsOnBegin = Points;
        starsOnBegin = Stars;
    }

    public void UndoFigure()
    {
        Points = pointsOnBegin;
        Stars = starsOnBegin;
        Combo = 0;
    }

    public void Register(NoteType type, int lane, NoteJudgment judgment, SwimmingSettings a)
    {
        if (type == NoteType.Golden)
        {
            if (judgment == NoteJudgment.Miss) return;
            Points += a.goldenBonus + (judgment == NoteJudgment.Perfect ? a.perfectPoints : a.goodPoints);
            Count(lane, true);
            AddCombo(a);
            return;
        }
        int multiplier = type == NoteType.White ? Mathf.Max(1, a.whiteMultiplier) : 1;
        switch (judgment)
        {
            case NoteJudgment.Perfect:
                Points += a.perfectPoints * multiplier;
                weighted += a.perfectWeight;
                AddCombo(a);
                break;
            case NoteJudgment.Good:
                Points += a.goodPoints * multiplier;
                weighted += a.goodWeight;
                AddCombo(a);
                break;
            default:
                Points += a.missPoints;
                Combo = 0;
                break;
        }
        Count(lane, judgment != NoteJudgment.Miss);
    }

    public void Trap(int lane, SwimmingSettings a)
    {
        Points = Mathf.Max(0, Points + a.trapPoints);
        weighted = Mathf.Max(0f, weighted - a.trapPenalty);
        Combo = 0;
        Count(lane, false);
    }

    void AddCombo(SwimmingSettings a)
    {
        Combo++;
        BestCombo = Mathf.Max(BestCombo, Combo);
        if (a.hitsPerStar > 0 && Combo % a.hitsPerStar == 0)
        {
            Stars++;
            Points += a.pointsPerStar;
            OnWinStar?.Invoke();
        }
    }

    void Count(int lane, bool hit)
    {
        if (lane < 0)
        {
            for (int c = 0; c < 4; c++) { if (hit) LaneHits[c]++; else LaneMisses[c]++; }
            return;
        }
        if (lane > 3) return;
        if (hit) LaneHits[lane]++; else LaneMisses[lane]++;
    }
}
