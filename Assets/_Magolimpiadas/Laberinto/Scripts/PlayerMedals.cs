using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerMedals
{
    public class Medal
    {
        public string sport;
        public int place;
        public Sprite sprite;
    }

    static readonly List<Medal> list = new List<Medal>();
    public static IReadOnlyList<Medal> All => list;

    public static event Action<Medal> OnWin;

    public static void Give(string sport, int place, Sprite sprite)
    {
        if (place < 0 || place > 2 || sprite == null) return;
        var m = list.Find(x => x.sport == sport);
        if (m != null)
        {
            if (place >= m.place) return;
            m.place = place; m.sprite = sprite;
        }
        else
        {
            m = new Medal { sport = sport, place = place, sprite = sprite };
            list.Add(m);
        }
        OnWin?.Invoke(m);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Restart() { list.Clear(); OnWin = null; }

    public static void RestartMatch() => list.Clear();
}
