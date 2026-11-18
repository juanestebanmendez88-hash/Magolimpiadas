using Fusion;

namespace ArcaneGames.Network
{
    public struct RosterSlot : INetworkStruct
    {
        public PlayerRef Player;
        public int MageIndex;
        public NetworkString<_16> PlayerName;
        public NetworkBool IsReady;

        public bool IsTaken => Player.IsRealPlayer;
    }
}
