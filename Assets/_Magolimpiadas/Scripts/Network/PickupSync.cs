using Fusion;

namespace ArcaneGames.Network
{
    public class PickupSync : NetworkBehaviour
    {
        private const int MageSlots = MageRoster.MaxPlayers;

        private readonly bool[] bowsShown = new bool[MageSlots];
        private readonly bool[] racketsShown = new bool[MageSlots];

        [Networked, Capacity(MageSlots)] private NetworkArray<NetworkBool> BowsCollected => default;
        [Networked, Capacity(MageSlots)] private NetworkArray<NetworkBool> RacketsCollected => default;

        public override void Spawned()
        {
            Collectible.CollectedLocally += OnBowCollectedLocally;
            Racket.CollectedLocally += OnRacketCollectedLocally;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Collectible.CollectedLocally -= OnBowCollectedLocally;
            Racket.CollectedLocally -= OnRacketCollectedLocally;
        }

        public override void Render()
        {
            for (int mageIndex = 0; mageIndex < MageSlots; mageIndex++)
            {
                if (BowsCollected[mageIndex] && !bowsShown[mageIndex])
                    bowsShown[mageIndex] = ShowBowOf(mageIndex);
                if (RacketsCollected[mageIndex] && !racketsShown[mageIndex])
                    racketsShown[mageIndex] = ShowRacketOf(mageIndex);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_ReportPickup(PickupKind kind, int mageIndex)
        {
            if (mageIndex < 0 || mageIndex >= MageSlots)
                return;

            if (kind == PickupKind.Bow)
                BowsCollected.Set(mageIndex, true);
            else
                RacketsCollected.Set(mageIndex, true);
        }

        private void OnBowCollectedLocally(Collectible bow)
        {
            Report(PickupKind.Bow, bow.WizardId);
        }

        private void OnRacketCollectedLocally(Racket racket)
        {
            Report(PickupKind.Racket, racket.mageId);
        }

        private void Report(PickupKind kind, string wizardId)
        {
            if (Object != null && Object.IsValid && OnlineMatch.TryGetMageIndex(wizardId, out int mageIndex))
                RPC_ReportPickup(kind, mageIndex);
        }

        private static bool ShowBowOf(int mageIndex)
        {
            if (!OnlineMatch.TryGetWizardId(mageIndex, out string wizardId))
                return true;

            Collectible bow = Collectible.OfWizard(wizardId);
            if (bow == null || bow.Collected)
                return true;

            MageBody body = MageBody.Find(wizardId);
            if (body == null || body.Animator == null)
                return false;

            bow.CollectRemotely(body.Animator, body.Root);
            return true;
        }

        private static bool ShowRacketOf(int mageIndex)
        {
            if (!OnlineMatch.TryGetWizardId(mageIndex, out string wizardId))
                return true;

            Racket racket = Racket.OfWizard(wizardId);
            if (racket == null || racket.Collected)
                return true;

            MageBody body = MageBody.Find(wizardId);
            if (body == null || body.Animator == null)
                return false;

            racket.CollectRemotely(body.Animator);
            return true;
        }
    }
}
