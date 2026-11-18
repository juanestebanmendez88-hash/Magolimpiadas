using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class IslandArrivalSync : TrialSync
    {
        private IslandCinematic cinematic;
        private bool playedLocally;

        public static IslandArrivalSync Current { get; private set; }

        public static bool AlreadyPlayed => Current != null && Current.playedLocally;

        public override OnlineTrial Trial => OnlineTrial.IslandArrival;

        public override void Spawned()
        {
            Current = this;
            cinematic = FindFirstObjectByType<IslandCinematic>(FindObjectsInactive.Include);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Current == this)
                Current = null;
        }

        public override bool TryEnterLocally()
        {
            if (cinematic == null || playedLocally)
                return true;
            if (!cinematic.TryStartShared())
                return false;

            playedLocally = true;
            OnlineTrials.Finished(Trial);
            return true;
        }
    }
}
