using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    public int amountValue = 1;
    bool collected;
    float time;
    Vector3 start;

    static readonly Dictionary<MageData, int> counts = new Dictionary<MageData, int>();
    public static event Action OnChange;
    public static event Action<Coin, Mage> AnyCollected;

    public static int For(MageData d) => d != null && counts.TryGetValue(d, out int n) ? n : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void RestartMatch() => counts.Clear();

    public static string Summary(out MageData winner)
    {
        winner = null; int best = -1; bool tie = false;
        var parts = new List<string>();
        foreach (var m in Mage.All)
        {
            if (m == null || m.data == null) continue;
            int n = For(m.data);
            parts.Add(m.data.displayName + " " + n);
            if (n > best) { best = n; winner = m.data; tie = false; }
            else if (n == best) tie = true;
        }
        if (tie || best <= 0) winner = null;
        return string.Join(" · ", parts);
    }

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        var m = other.GetComponentInParent<Mage>();
        if (m == null || m.IsNetworkReplica || !m.Alive) return;
        Collect(m);
    }

    public void CollectFromNetwork(Mage m) => Collect(m);

    void Collect(Mage m)
    {
        if (collected || m == null || m.data == null) return;
        collected = true;
        AnyCollected?.Invoke(this, m);
        counts[m.data] = For(m.data) + amountValue;
        OnChange?.Invoke();
        DamageNumbers.Message(transform.position + Vector3.up * 1.2f, "+" + amountValue, m.data.color);
        GameSound.Play(SoundSynthesizer.Star(), 0.35f, 1.1f);
        GetComponent<Collider2D>().enabled = false;
        start = transform.position;
    }

    void Update()
    {
        if (!collected) return;
        time += Time.deltaTime;
        float k = time / 0.35f;
        transform.position = start + Vector3.up * k * 0.8f;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) { var c = sr.color; c.a = 1f - k; sr.color = c; }
        if (k >= 1f) gameObject.SetActive(false);
    }
}
