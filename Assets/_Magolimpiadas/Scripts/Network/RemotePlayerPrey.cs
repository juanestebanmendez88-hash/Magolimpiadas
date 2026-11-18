using UnityEngine;

namespace ArcaneGames.Network
{
    public class RemotePlayerPrey : IMinotaurPrey
    {
        private readonly MageAvatarState state;

        public RemotePlayerPrey(MageAvatarState state)
        {
            this.state = state;
        }

        public Transform Body => state.BodyRoot;
        public Vector3 Position => state.transform.position;
        public bool CanBeHunted => state != null && state.Object != null && state.Object.IsValid && state.Huntable;

        public void Catch(float respawnSeconds)
        {
            state.RPC_Catch(respawnSeconds);
        }
    }
}
