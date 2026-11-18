using System.Collections.Generic;
using UnityEngine;

public static class MinotaurPreyRegistry
{
    private static readonly List<IMinotaurPrey> remotePrey = new List<IMinotaurPrey>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        remotePrey.Clear();
    }

    public static void Register(IMinotaurPrey prey)
    {
        if (!remotePrey.Contains(prey))
            remotePrey.Add(prey);
    }

    public static void Unregister(IMinotaurPrey prey)
    {
        remotePrey.Remove(prey);
    }

    public static void CopyRemoteTo(List<IMinotaurPrey> destination)
    {
        destination.AddRange(remotePrey);
    }
}
