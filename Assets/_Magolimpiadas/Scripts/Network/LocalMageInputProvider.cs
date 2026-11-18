using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class LocalMageInputProvider : MonoBehaviour
    {
        private MageBody localBody;

        public void Attach(NetworkEvents events)
        {
            if (events.OnInput == null)
                events.OnInput = new NetworkEvents.InputEvent();
            events.OnInput.AddListener(ProvideInput);
        }

        private void ProvideInput(NetworkRunner runner, NetworkInput input)
        {
            if (localBody == null || !localBody.IsAlive)
                localBody = MageBody.Find(ChosenMage.Chosen);
            if (localBody != null)
                input.Set(localBody.ReadPose());
        }
    }
}
