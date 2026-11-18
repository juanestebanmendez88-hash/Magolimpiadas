using System.Collections.Generic;
using System.Linq;

public static class GameResults
{
    public class Test
    {
        public string displayName;
        public List<string> order;
        public int teamMedal = -1;
        public bool IsTeam => order == null;
    }

    static readonly List<Test> tests = new List<Test>();
    public static IReadOnlyList<Test> Tests => tests;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void RestartMatch() => tests.Clear();

    public static void RegisterPlaces(string test, IEnumerable<string> order)
    {
        var p = Find(test);
        p.order = order.ToList();
        p.teamMedal = -1;
    }

    public static void RegisterTeam(string test, int medal)
    {
        if (medal < 0 || medal > 2) return;
        var p = Find(test);
        if (p.order == null && p.teamMedal >= 0 && p.teamMedal <= medal) return;
        p.order = null;
        p.teamMedal = medal;
    }

    static Test Find(string displayName)
    {
        var p = tests.Find(x => x.displayName == displayName);
        if (p == null) { p = new Test { displayName = displayName }; tests.Add(p); }
        return p;
    }

    public static int MedalOf(Test p, string id)
    {
        if (p.IsTeam) return p.teamMedal;
        int place = p.order.IndexOf(id);
        return place >= 0 && place < 3 ? place : -1;
    }

    public static int[] Medals(string id)
    {
        var count = new int[3];
        foreach (var p in tests)
        {
            int m = MedalOf(p, id);
            if (m >= 0) count[m]++;
        }
        return count;
    }

    public static List<string> Ranking(IEnumerable<string> ids) =>
        ids.OrderByDescending(id => Medals(id)[0])
           .ThenByDescending(id => Medals(id)[1])
           .ThenByDescending(id => Medals(id)[2])
           .ToList();
}
