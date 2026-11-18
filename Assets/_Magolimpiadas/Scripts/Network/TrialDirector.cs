using System.Collections.Generic;
using ArcaneGames.UI;
using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class TrialDirector : NetworkBehaviour
    {
        private const float EnterRetryLimitSeconds = 20f;
        private const string NameSeparator = ", ";

        private readonly List<string> missingNames = new List<string>();
        private int handledSession;
        private OnlineTrial pendingTrial = OnlineTrial.None;
        private float pendingSince;
        private OnlineTrial noticedGathering = OnlineTrial.None;

        public static TrialDirector Current { get; private set; }

        [Networked] public OnlineTrial ActiveTrial { get; private set; }
        [Networked] public int Session { get; private set; }
        [Networked] private OnlineTrial SessionTrial { get; set; }
        [Networked] private OnlineTrial GatheringTrial { get; set; }
        [Networked, Capacity(MageRoster.MaxPlayers)] private NetworkArray<NetworkBool> Arrived => default;

        public override void Spawned()
        {
            Current = this;
            handledSession = Session;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Current == this)
                Current = null;
            WaitingScreen.Hide();
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && GatheringTrial != OnlineTrial.None && EveryoneArrived())
                StartTrial(GatheringTrial);
        }

        public override void Render()
        {
            if (Session != handledSession)
            {
                handledSession = Session;
                pendingTrial = SessionTrial;
                pendingSince = Time.unscaledTime;
            }

            RefreshWaiting();

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
        public void RPC_ReportArrival(OnlineTrial trial, int mageIndex)
        {
            if (trial == OnlineTrial.None || ActiveTrial != OnlineTrial.None || !IsValidMage(mageIndex))
                return;
            if (GatheringTrial != OnlineTrial.None && GatheringTrial != trial)
                return;

            GatheringTrial = trial;
            Arrived.Set(mageIndex, true);
        }

        public void Finish(OnlineTrial trial)
        {
            if (HasStateAuthority && ActiveTrial == trial)
                ActiveTrial = OnlineTrial.None;
        }

        private void StartTrial(OnlineTrial trial)
        {
            for (int i = 0; i < Arrived.Length; i++)
                Arrived.Set(i, false);

            GatheringTrial = OnlineTrial.None;
            ActiveTrial = trial;
            SessionTrial = trial;
            Session++;
        }

        private bool EveryoneArrived()
        {
            MageRoster roster = MageRoster.For(Runner);
            if (roster == null)
                return true;

            for (int i = 0; i < roster.Slots.Length; i++)
            {
                RosterSlot slot = roster.Slots[i];
                if (slot.IsTaken && IsValidMage(slot.MageIndex) && !Arrived[slot.MageIndex])
                    return false;
            }

            return true;
        }

        private void RefreshWaiting()
        {
            OnlineTrial gathering = GatheringTrial;
            if (gathering == OnlineTrial.None)
            {
                noticedGathering = OnlineTrial.None;
                WaitingScreen.Hide();
                return;
            }

            int arrived = CountHumans(out int total);
            if (LocalMageArrived())
            {
                WaitingScreen.Show(gathering, arrived, total, string.Join(NameSeparator, missingNames));
                return;
            }

            WaitingScreen.Hide();
            if (noticedGathering != gathering)
            {
                noticedGathering = gathering;
                WaitingScreen.NotifyWaitingFor(gathering, arrived, total);
            }
        }

        private int CountHumans(out int total)
        {
            missingNames.Clear();
            total = 0;
            int arrived = 0;
            MageRoster roster = MageRoster.For(Runner);
            if (roster == null)
                return arrived;

            for (int i = 0; i < roster.Slots.Length; i++)
            {
                RosterSlot slot = roster.Slots[i];
                if (!slot.IsTaken || !IsValidMage(slot.MageIndex))
                    continue;

                total++;
                if (Arrived[slot.MageIndex])
                    arrived++;
                else
                    missingNames.Add(DisplayNameOf(roster, slot));
            }

            return arrived;
        }

        private static string DisplayNameOf(MageRoster roster, RosterSlot slot)
        {
            string playerName = slot.PlayerName.Value;
            if (!string.IsNullOrEmpty(playerName))
                return playerName;
            return roster.TryGetMage(slot.MageIndex, out WizardData mage) ? mage.displayName : "";
        }

        private bool LocalMageArrived()
        {
            return OnlineMatch.TryGetMageIndex(ChosenMage.Chosen, out int mageIndex) && IsValidMage(mageIndex) && Arrived[mageIndex];
        }

        private bool IsValidMage(int mageIndex)
        {
            return mageIndex >= 0 && mageIndex < Arrived.Length;
        }

        private static TrialSync SyncFor(OnlineTrial trial)
        {
            foreach (TrialSync sync in FindObjectsByType<TrialSync>(FindObjectsSortMode.None))
            {
                if (sync.Trial == trial)
                    return sync;
            }

            return null;
        }
    }
}
