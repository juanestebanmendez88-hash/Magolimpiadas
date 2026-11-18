using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class TrialDirector : NetworkBehaviour
    {
        private const float EnterRetryLimitSeconds = 20f;

        private int handledSession;
        private OnlineTrial pendingTrial = OnlineTrial.None;
        private float pendingSince;

        public static TrialDirector Current { get; private set; }

        [Networked] public OnlineTrial ActiveTrial { get; private set; }
        [Networked] public int Session { get; private set; }

        public override void Spawned()
        {
            Current = this;
            handledSession = Session;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Current == this)
                Current = null;
        }

        public override void Render()
        {
            if (Session != handledSession)
            {
                handledSession = Session;
                pendingTrial = ActiveTrial;
                pendingSince = Time.unscaledTime;
            }

            if (pendingTrial == OnlineTrial.None)
                return;

            TrialSync sync = SyncFor(pendingTrial);
            bool entered = sync != null && sync.TryEnterLocally();
            if (entered || Time.unscaledTime - pendingSince > EnterRetryLimitSeconds)
            {
                if (!entered)
                    Debug.LogWarning("[TrialDirector] Could not join the trial " + pendingTrial + ".");
                pendingTrial = OnlineTrial.None;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_RequestStart(OnlineTrial trial)
        {
            if (ActiveTrial != OnlineTrial.None || trial == OnlineTrial.None)
                return;

            ActiveTrial = trial;
            Session++;
        }

        public void Finish(OnlineTrial trial)
        {
            if (HasStateAuthority && ActiveTrial == trial)
                ActiveTrial = OnlineTrial.None;
        }

        private TrialSync SyncFor(OnlineTrial trial)
        {
            foreach (TrialSync sync in GetComponents<TrialSync>())
            {
                if (sync.Trial == trial)
                    return sync;
            }

            return null;
        }
    }
}
