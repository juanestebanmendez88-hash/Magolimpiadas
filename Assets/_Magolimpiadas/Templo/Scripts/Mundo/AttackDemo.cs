using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttackDemo : MonoBehaviour
{
    public Image mage;
    public Image enemy;
    public Image ballSprite;
    public Image arrow;
    public TMP_Text number;
    public TMP_Text legend;
    public Image key;
    public TMP_Text keyLetter;
    public Sprite clickSprite;
    public Sprite keySprite;

    [Range(0.3f, 2f)] public float speed = 1f;

    MageData data;
    Sprite[] framesSlime, bullseyeFrames, breakFrames;
    Vector2 magePos, enemyPos;
    Vector2 mageBox, enemyBox;
    Vector2 mageAnchor, enemyAnchor;
    float mageScale = 1f, enemyScale = 1f;
    Vector2 shake;

    class Anim { public Sprite[] frames; public float fps; public bool repeat; public float t; }
    readonly Anim mageAnim = new Anim(), enemyAnim = new Anim();

    public void Configure(MageData d, Sprite[] slime, Sprite[] bullseye, Sprite[] targetBreak)
    {
        data = d;
        framesSlime = slime;
        bullseyeFrames = bullseye;
        breakFrames = targetBreak;
        if (ballSprite != null && data != null) ballSprite.color = data.power.color;
        if (arrow != null && data != null && data.arrow != null) arrow.sprite = data.arrow;
    }

    void Awake()
    {
        magePos = mage.rectTransform.anchoredPosition;
        enemyPos = enemy.rectTransform.anchoredPosition;
        mageBox = mage.rectTransform.sizeDelta;
        enemyBox = enemy.rectTransform.sizeDelta;
        mage.preserveAspect = false;
        enemy.preserveAspect = false;
    }

    void Measure()
    {
        var m = data.idle.frames != null && data.idle.frames.Length > 0 ? data.idle.frames[0] : null;
        var e = framesSlime != null && framesSlime.Length > 0 ? framesSlime[0] : null;
        mageScale = m != null ? mageBox.y / Mathf.Max(1f, m.rect.height) : 1f;
        enemyScale = e != null ? enemyBox.y / Mathf.Max(1f, e.rect.height) : 1f;
        mageAnchor = Anchor(magePos, mageBox, m, mageScale);
        enemyAnchor = Anchor(enemyPos, enemyBox, e, enemyScale);
    }

    static Vector2 Anchor(Vector2 center, Vector2 box, Sprite s, float scale)
    {
        Vector2 down = center - new Vector2(0f, box.y * 0.5f);
        if (s == null) return down;
        Vector2 p = s.pivot / s.rect.size;
        return down + new Vector2((p.x - 0.5f) * s.rect.width, p.y * s.rect.height) * scale;
    }

    void OnEnable()
    {
        StopAllCoroutines();
        if (data != null) StartCoroutine(Loop());
    }

    void OnDisable() { StopAllCoroutines(); }

    public void Restart()
    {
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        if (data != null) StartCoroutine(Loop());
    }

    float Dt => Time.unscaledDeltaTime * speed;

    IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Dt) yield return null; }

    IEnumerator Loop()
    {
        Measure();
        while (true)
        {
            Put(mageAnim, data.idle);
            Put(enemyAnim, framesSlime, 8f, true);
            enemy.color = Color.white;
            enemy.rectTransform.localScale = Vector3.one;
            shake = Vector2.zero;
            ballSprite.gameObject.SetActive(false);
            arrow.gameObject.SetActive(false);
            number.gameObject.SetActive(false);
            Legend("Clic izquierdo: lanza tu poder", true, "");
            yield return Wait(0.9f);

            for (int hit = 0; hit < 2; hit++)
            {
                Put(mageAnim, data.attack);
                float launch = data.attackLaunchFrame / Mathf.Max(1f, data.attack.fps);
                yield return Wait(launch);
                yield return Fly(ballSprite, Hand(), EnemyCenter(), 0.35f, true);
                yield return Hit(Mathf.RoundToInt(data.power.damage * 2f) + "  x2", data.color);
                yield return Wait(Mathf.Max(0f, data.attack.Duration - launch - 0.35f));
                Put(mageAnim, data.idle);
                yield return Wait(0.35f);
            }

            Legend("¡Se volvió diana! Tienes 5 segundos", false, "");
            yield return Pop(enemy.rectTransform);
            Put(enemyAnim, bullseyeFrames, 8f, true);
            yield return Wait(1.1f);

            Legend("Q: cambia al arco", false, "Q");
            yield return Wait(0.4f);
            Put(mageAnim, data.bowIdle);
            yield return Pop(mage.rectTransform);
            yield return Wait(0.8f);

            Legend("Clic izquierdo: dispara la flecha", true, "");
            yield return Wait(0.5f);
            Put(mageAnim, data.bowShot);
            float released = data.arrowLaunchFrame / Mathf.Max(1f, data.bowShot.fps);
            yield return Wait(released);
            yield return Fly(arrow, Hand(), EnemyCenter(), 0.25f, false);
            Put(enemyAnim, breakFrames, 11f, false);
            yield return Hit("¡Eliminado!", data.color);
            Put(mageAnim, data.bowIdle);
            yield return Wait(1.4f);

            for (float t = 0f; t < 1f; t += Dt / 0.4f) { enemy.color = new Color(1f, 1f, 1f, 1f - t); yield return null; }
            yield return Wait(0.4f);
        }
    }

    Vector2 Hand() => magePos + new Vector2(mageBox.x * 0.32f, mageBox.y * 0.05f);
    Vector2 EnemyCenter() => enemyPos;

    void Legend(string text, bool click, string letter)
    {
        legend.text = text;
        bool hasKey = click || !string.IsNullOrEmpty(letter);
        key.gameObject.SetActive(hasKey);
        if (!hasKey) return;
        key.sprite = click ? clickSprite : keySprite;
        key.color = click ? new Color(1f, 0.92f, 0.8f) : Color.white;
        key.rectTransform.sizeDelta = click ? new Vector2(34f, 48f) : new Vector2(48f, 48f);
        keyLetter.text = letter;
        keyLetter.gameObject.SetActive(!click);
    }

    IEnumerator Fly(Image img, Vector2 fromPoint, Vector2 until, float duration, bool glow)
    {
        img.gameObject.SetActive(true);
        var rt = img.rectTransform;
        for (float t = 0f; t < 1f; t += Dt / duration)
        {
            rt.anchoredPosition = Vector2.Lerp(fromPoint, until, t);
            if (glow) rt.localScale = Vector3.one * (0.85f + Mathf.Sin(t * 30f) * 0.15f);
            yield return null;
        }
        img.gameObject.SetActive(false);
    }

    IEnumerator Hit(string text, Color color)
    {
        number.gameObject.SetActive(true);
        number.text = text;
        number.color = color;
        var rn = number.rectTransform;
        Vector2 start = enemyPos + new Vector2(0f, enemyBox.y * 0.55f);
        for (float t = 0f; t < 1f; t += Dt / 0.6f)
        {
            enemy.color = Color.Lerp(new Color(1f, 0.55f, 0.55f), Color.white, t);
            shake = new Vector2(Mathf.Sin(t * 40f) * 8f * (1f - t), 0f);
            rn.anchoredPosition = start + Vector2.up * t * 40f;
            rn.localScale = Vector3.one * (t < 0.2f ? Mathf.Lerp(0.6f, 1.2f, t / 0.2f) : Mathf.Lerp(1.2f, 1f, (t - 0.2f) / 0.8f));
            yield return null;
        }
        enemy.color = Color.white;
        shake = Vector2.zero;
        number.gameObject.SetActive(false);
    }

    IEnumerator Pop(RectTransform r)
    {
        for (float t = 0f; t < 1f; t += Dt / 0.25f)
        {
            r.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.15f);
            yield return null;
        }
        r.localScale = Vector3.one;
    }

    void Put(Anim a, SpriteAnimation s) => Put(a, s != null ? s.frames : null, s != null ? s.fps : 10f, s == null || s.repeat);

    void Put(Anim a, Sprite[] frames, float fps, bool repeat)
    {
        a.frames = frames;
        a.fps = fps;
        a.repeat = repeat;
        a.t = 0f;
    }

    void Update()
    {
        Advance(mageAnim, mage, mageScale, mageAnchor);
        Advance(enemyAnim, enemy, enemyScale, enemyAnchor + shake);
    }

    void Advance(Anim a, Image img, float scale, Vector2 anchor)
    {
        if (a.frames == null || a.frames.Length == 0) return;
        a.t += Dt;
        int i = (int)(a.t * a.fps);
        i = a.repeat ? i % a.frames.Length : Mathf.Min(i, a.frames.Length - 1);
        var s = a.frames[i];
        if (img.sprite != s) img.sprite = s;
        var rt = img.rectTransform;
        rt.pivot = s.pivot / s.rect.size;
        rt.sizeDelta = s.rect.size * scale;
        rt.anchoredPosition = anchor;
    }
}
