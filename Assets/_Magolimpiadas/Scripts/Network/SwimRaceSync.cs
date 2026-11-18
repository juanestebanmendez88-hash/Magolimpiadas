using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class SwimRaceSync : TrialSync
    {
        public const int Lanes = 4;

        private readonly int[] pendingStrokes = new int[Lanes];
        private readonly float[] hostDistances = new float[Lanes];
        private readonly float[] hostTimes = new float[Lanes];
        private bool hostFinished;
        private SwimmingRace race;

        public static SwimRaceSync Current { get; private set; }

        public static bool IsOnline => Current != null && OnlineMatch.IsActive;

        public override OnlineTrial Trial => OnlineTrial.SwimRace;

        [Networked, Capacity(Lanes)] private NetworkArray<float> Distances => default;
        [Networked, Capacity(Lanes)] private NetworkArray<float> FinishTimes => default;
        [Networked] private NetworkBool Finished { get; set; }

        public bool IsFinished => HasStateAuthority ? hostFinished : Finished;

        public override void Spawned()
        {
            Current = this;
            race = FindFirstObjectByType<SwimmingRace>(FindObjectsInactive.Include);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Current == this)
                Current = null;
        }

        public override bool TryEnterLocally()
        {
            if (race == null || race.Finished)
                return true;

            if (!race.InProgress)
                race.Begin();
            return race.InProgress;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            for (int lane = 0; lane < Lanes; lane++)
            {
                Distances.Set(lane, hostDistances[lane]);
                FinishTimes.Set(lane, hostTimes[lane]);
            }

            Finished = hostFinished;
        }

        public void BeginRace()
        {
            if (!HasStateAuthority)
                return;

            for (int lane = 0; lane < Lanes; lane++)
            {
                pendingStrokes[lane] = 0;
                hostDistances[lane] = 0f;
                hostTimes[lane] = 0f;
            }

            hostFinished = false;
        }

        public void SendStroke(int lane)
        {
            RPC_Stroke(lane);
        }

        public int TakeStrokes(int lane)
        {
            if (!IsValidLane(lane))
                return 0;

            int strokes = pendingStrokes[lane];
            pendingStrokes[lane] = 0;
            return strokes;
        }

        public void Publish(int lane, float distance, float finishTime)
        {
            if (!HasStateAuthority || !IsValidLane(lane))
                return;

            hostDistances[lane] = distance;
            hostTimes[lane] = finishTime > 0f ? finishTime : 0f;
        }

        public float DistanceOf(int lane)
        {
            if (!IsValidLane(lane))
                return 0f;
            return HasStateAuthority ? hostDistances[lane] : Distances[lane];
        }

        public float FinishTimeOf(int lane)
        {
            if (!IsValidLane(lane))
                return -1f;

            float time = HasStateAuthority ? hostTimes[lane] : FinishTimes[lane];
            return time > 0f ? time : -1f;
        }

        public void MarkFinished()
        {
            if (!HasStateAuthority)
                return;

            hostFinished = true;
            OnlineTrials.Finished(Trial);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, TickAligned = false)]
        public void RPC_Stroke(int lane)
        {
            if (IsValidLane(lane))
                pendingStrokes[lane]++;
        }

        private static bool IsValidLane(int lane)
        {
            return lane >= 0 && lane < Lanes;
        }
    }
}
