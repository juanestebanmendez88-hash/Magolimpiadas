using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class ArtisticSwimmingSync : TrialSync
    {
        private readonly Dictionary<int, float> figureResults = new Dictionary<int, float>();
        private ArtisticSwimmingLevel level;
        private int announcedPlay = -1;

        public static ArtisticSwimmingSync Current { get; private set; }

        public static bool IsOnline => Current != null && OnlineMatch.IsActive;

        public override OnlineTrial Trial => OnlineTrial.ArtisticSwimming;

        public override void Spawned()
        {
            Current = this;
            level = FindFirstObjectByType<ArtisticSwimmingLevel>(FindObjectsInactive.Include);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Current == this)
                Current = null;
        }

        public override bool TryEnterLocally()
        {
            if (level == null)
                return true;

            if (!level.Playing)
                level.Enter();
            return level.Playing;
        }

        public IEnumerator WaitForFigureStart(int play)
        {
            if (HasStateAuthority)
                RPC_StartFigure(play);

            while (announcedPlay < play)
                yield return null;
        }

        public IEnumerator WaitForFigureResult(int play, float localAccuracy, Action<float> onResult)
        {
            if (HasStateAuthority)
                RPC_FigureResult(play, localAccuracy);

            float precision;
            while (!figureResults.TryGetValue(play, out precision))
                yield return null;

            onResult(precision);
        }

        public void SendLocalJudgment(int play, int noteIndex, int lane, NoteJudgment judgment, bool trap)
        {
            RPC_LaneJudged(play, noteIndex, lane, (int)judgment, trap);
        }

        public void FinishForEveryone(string finalScene)
        {
            if (!HasStateAuthority)
                return;

            OnlineTrials.Finished(Trial);
            OnlineMatch.LoadSceneForEveryone(finalScene);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_StartFigure(int play)
        {
            announcedPlay = Mathf.Max(announcedPlay, play);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_FigureResult(int play, float precision)
        {
            figureResults[play] = precision;
        }

        [Rpc(RpcSources.All, RpcTargets.All, TickAligned = false)]
        public void RPC_LaneJudged(int play, int noteIndex, int lane, int judgment, NetworkBool trap)
        {
            if (level != null)
                level.ApplyRemoteJudgment(play, noteIndex, lane, (NoteJudgment)judgment, trap);
        }
    }
}
