using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EffectAnimation2D : MonoBehaviour
{
    Sprite[] frameSprites;
    float fps = 24f, t;
    SpriteRenderer drawing;

    public void Setup(Sprite[] frames, float framesPerSecond, Color tint, int order)
    {
        frameSprites = frames;
        fps = Mathf.Max(1f, framesPerSecond);
        drawing = GetComponent<SpriteRenderer>();
        drawing.color = tint;
        drawing.sortingOrder = order;
        if (frameSprites != null && frameSprites.Length > 0) drawing.sprite = frameSprites[0];
    }

    void Update()
    {
        if (frameSprites == null || frameSprites.Length == 0) { Destroy(gameObject); return; }
        t += Time.deltaTime;
        int i = (int)(t * fps);
        if (i >= frameSprites.Length) { Destroy(gameObject); return; }
        drawing.sprite = frameSprites[i];
    }
}
