using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class IslandArrivalSync : NetworkBehaviour
    {
        private IslandCinematic cinematic;
        private bool playedLocally;

        public static IslandArrivalSync Current { get; private set; }

        [Networked] private NetworkBool Opened { get; set; }
        [Networked] private int OpenerMageIndex { get; set; }

        public static bool TryRequest(string wizardId)
        {
            IslandArrivalSync sync = Current;
            if (!OnlineMatch.IsActive || sync == null || sync.playedLocally)
                return false;

            if (!sync.Opened)
            {
                OnlineMatch.TryGetMageIndex(wizardId, out int mageIndex);
                sync.RPC_RequestOpen(mageIndex);
            }

            return true;
        }

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

        public override void Render()
        {
            if (!Opened || playedLocally || cinematic == null)
                return;

            playedLocally = cinematic.TryStartShared(OpenerNameForLocalPlayer());
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_RequestOpen(int openerMageIndex)
        {
            if (Opened)
                return;

            OpenerMageIndex = openerMageIndex;
            Opened = true;
        }

        private string OpenerNameForLocalPlayer()
        {
            if (OnlineMatch.TryGetWizardId(OpenerMageIndex, out string openerId) && OnlineMatch.IsLocalWizard(openerId))
                return null;
            return OnlineMatch.DisplayNameOf(OpenerMageIndex);
        }
    }
}
