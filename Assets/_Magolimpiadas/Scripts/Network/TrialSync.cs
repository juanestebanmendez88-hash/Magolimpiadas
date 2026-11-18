using Fusion;

namespace ArcaneGames.Network
{
    public abstract class TrialSync : NetworkBehaviour
    {
        public abstract OnlineTrial Trial { get; }

        public abstract bool TryEnterLocally();
    }
}
