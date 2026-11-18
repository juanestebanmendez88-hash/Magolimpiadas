using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NoteLanes : MonoBehaviour
{
    public Transform[] lanes = new Transform[4];
    public Transform hitLine;
    public SpriteRenderer[] bullseyes = new SpriteRenderer[4];
    public SpriteRenderer[] beams = new SpriteRenderer[4];
    public SpriteRenderer playerFrame;
    public TMP_Text playerLabel;
    public Color[] colors = new Color[4];

    public float fallHeight = 10.4f;
    public float laneWidth = 1.45f;
    public float belowLine = 1f;
    public int notesOrder = 60;

    readonly List<SpriteRenderer> notes = new List<SpriteRenderer>();
    readonly List<SpriteRenderer> glows = new List<SpriteRenderer>();
    readonly float[] flashImage = new float[4];
    readonly Vector3[] bullseyeScale = new Vector3[4];
    Transform notesLayer;
    Sprite pill, bar, golden, trap, glow;

    void Awake()
    {
        pill = PixelShapes.Pill(20, 9);
        bar = PixelShapes.Pill(96, 9, false, true);
        golden = PixelShapes.Pill(20, 9, false, true);
        trap = PixelShapes.Pill(20, 9, true);
        glow = PixelShapes.Glow();
        notesLayer = new GameObject("Notas").transform;
        notesLayer.SetParent(transform, false);
        for (int c = 0; c < 4; c++)
        {
            if (bullseyes[c] != null) bullseyeScale[c] = bullseyes[c].transform.localScale;
            if (beams[c] != null) beams[c].color = Transparent(LaneColor(c));
        }
    }

    Color LaneColor(int c) => colors != null && c >= 0 && c < colors.Length ? colors[c] : Color.white;

    static Color Transparent(Color c) { c.a = 0f; return c; }

    float LaneX(int c) => lanes[c] != null ? lanes[c].localPosition.x : c * 1.6f;
    float LineY => hitLine != null ? hitLine.localPosition.y : 0f;

    public void MarkPlayer(int lane, string displayName)
    {
        if (playerFrame != null)
        {
            var p = playerFrame.transform.localPosition;
            playerFrame.transform.localPosition = new Vector3(LaneX(lane), p.y, p.z);
            playerFrame.color = LaneColor(lane);
        }
        if (playerLabel != null)
        {
            var p = playerLabel.transform.localPosition;
            playerLabel.transform.localPosition = new Vector3(LaneX(lane), p.y, p.z);
            playerLabel.text = "Tú";
            playerLabel.color = Color.Lerp(LaneColor(lane), Color.white, 0.35f);
        }
    }

    public void Flash(int lane, bool withNote)
    {
        if (lane < 0 || lane > 3) return;
        flashImage[lane] = withNote ? 1f : 0.45f;
    }

    public void Clear()
    {
        foreach (var n in notes) if (n != null) n.gameObject.SetActive(false);
        foreach (var b in glows) if (b != null) b.gameObject.SetActive(false);
    }

    public void Draw(List<NotesJudge.Note> list, float beat, float fallBeats)
    {
        int used = 0, usedGlows = 0;
        float yLine = LineY;
        foreach (var n in list)
        {
            if (n.judged && n.judgment != NoteJudgment.Miss) continue;
            float dp = n.data.beat - beat;
            if (dp > fallBeats + 0.3f) break;
            float y = yLine + dp / Mathf.Max(0.1f, fallBeats) * fallHeight;
            if (y < yLine - belowLine) continue;
            if (n.judged && n.data.type == NoteType.Trap) continue;

            float alpha = Mathf.Clamp01((yLine + fallHeight + 0.6f - y) / 1.2f);
            if (y < yLine) alpha *= Mathf.Clamp01(1f - (yLine - y) / belowLine);
            bool failed = n.judged && n.judgment == NoteJudgment.Miss;

            var sr = Note(used++);
            float x;
            Color c;
            float width = laneWidth * 0.86f;
            switch (n.data.type)
            {
                case NoteType.White:
                    sr.sprite = bar;
                    x = (LaneX(0) + LaneX(3)) * 0.5f;
                    width = LaneX(3) - LaneX(0) + laneWidth * 0.9f;
                    c = new Color(1f, 0.98f, 0.9f);
                    break;
                case NoteType.Golden:
                    sr.sprite = golden;
                    x = LaneX(n.data.lane);
                    c = new Color(1f, 0.8f, 0.25f);
                    break;
                case NoteType.Trap:
                    sr.sprite = trap;
                    x = LaneX(n.data.lane);
                    c = new Color(0.95f, 0.25f, 0.3f);
                    break;
                default:
                    sr.sprite = pill;
                    x = LaneX(n.data.lane);
                    c = LaneColor(n.data.lane);
                    break;
            }
            if (failed) c = Color.Lerp(c, new Color(0.35f, 0.35f, 0.4f), 0.7f);
            c.a = alpha * (failed ? 0.6f : 1f);
            sr.color = c;
            sr.transform.localPosition = new Vector3(x, y, 0f);
            float heightValue = sr.sprite.bounds.size.y;
            sr.transform.localScale = new Vector3(width / sr.sprite.bounds.size.x, 0.55f / Mathf.Max(0.01f, heightValue), 1f);
            sr.sortingOrder = notesOrder + (n.data.type == NoteType.White ? 0 : 2);

            if (!failed && (n.data.type == NoteType.Golden || n.data.type == NoteType.White))
            {
                var b = Glow(usedGlows++);
                b.transform.localPosition = new Vector3(x, y, 0f);
                float glowBeat = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 10f);
                b.transform.localScale = new Vector3(width * 1.35f, 1.3f, 1f) * glowBeat;
                var cb = n.data.type == NoteType.Golden ? new Color(1f, 0.75f, 0.2f) : new Color(0.8f, 0.9f, 1f);
                cb.a = 0.55f * alpha;
                b.color = cb;
                b.sortingOrder = notesOrder - 1;
            }
        }
        for (int i = used; i < notes.Count; i++) notes[i].gameObject.SetActive(false);
        for (int i = usedGlows; i < glows.Count; i++) glows[i].gameObject.SetActive(false);
    }

    void Update()
    {
        for (int c = 0; c < 4; c++)
        {
            float d = flashImage[c];
            if (bullseyes[c] != null) bullseyes[c].transform.localScale = bullseyeScale[c] * (1f + d * 0.35f);
            if (beams[c] != null)
            {
                var col = LaneColor(c); col.a = d * 0.75f;
                beams[c].color = col;
            }
            flashImage[c] = Mathf.MoveTowards(d, 0f, Time.unscaledDeltaTime * 4f);
        }
        if (playerFrame != null)
        {
            var c = playerFrame.color; c.a = 0.55f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f);
            playerFrame.color = c;
        }
    }

    SpriteRenderer Note(int i)
    {
        while (notes.Count <= i)
        {
            var go = new GameObject("Nota", typeof(SpriteRenderer));
            go.transform.SetParent(notesLayer, false);
            go.layer = gameObject.layer;
            notes.Add(go.GetComponent<SpriteRenderer>());
        }
        var sr = notes[i];
        if (!sr.gameObject.activeSelf) sr.gameObject.SetActive(true);
        return sr;
    }

    SpriteRenderer Glow(int i)
    {
        while (glows.Count <= i)
        {
            var go = new GameObject("Brillo nota", typeof(SpriteRenderer));
            go.transform.SetParent(notesLayer, false);
            go.layer = gameObject.layer;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = glow;
            glows.Add(sr);
        }
        var b = glows[i];
        if (!b.gameObject.activeSelf) b.gameObject.SetActive(true);
        return b;
    }
}
