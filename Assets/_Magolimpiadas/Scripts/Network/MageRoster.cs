using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class MageRoster : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        public const int MaxPlayers = 4;
        public const int NoMage = -1;

        private static readonly Dictionary<NetworkRunner, MageRoster> rostersByRunner = new Dictionary<NetworkRunner, MageRoster>();

        [SerializeField] private WizardData[] mages;

        private ChangeDetector changeDetector;

        [Networked, Capacity(MaxPlayers)] public NetworkArray<RosterSlot> Slots => default;
        [Networked] public PlayerRef HostPlayer { get; private set; }
        [Networked] public NetworkBool MatchStarted { get; private set; }

        public event Action Changed;

        public WizardData[] Mages => mages;

        public static MageRoster For(NetworkRunner runner)
        {
            return runner != null && rostersByRunner.TryGetValue(runner, out MageRoster roster) ? roster : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            rostersByRunner.Clear();
        }

        public override void Spawned()
        {
            rostersByRunner[Runner] = this;
            Runner.MakeDontDestroyOnLoad(gameObject);
            changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            if (HasStateAuthority)
            {
                HostPlayer = Runner.LocalPlayer;
                foreach (PlayerRef player in Runner.ActivePlayers)
                    AddPlayer(player);
            }

            NotifyChanged();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (For(runner) == this)
                rostersByRunner.Remove(runner);
        }

        public override void Render()
        {
            foreach (string _ in changeDetector.DetectChanges(this))
            {
                NotifyChanged();
                return;
            }
        }

        public void PlayerJoined(PlayerRef player)
        {
            if (HasStateAuthority)
                AddPlayer(player);
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (HasStateAuthority)
                RemovePlayer(player);
        }

        public bool CanStartMatch
        {
            get
            {
                int players = 0;
                for (int i = 0; i < Slots.Length; i++)
                {
                    RosterSlot slot = Slots[i];
                    if (!slot.IsTaken)
                        continue;

                    players++;
                    if (!IsValidMage(slot.MageIndex))
                        return false;
                    if (slot.Player != HostPlayer && !slot.IsReady)
                        return false;
                }

                return players > 0;
            }
        }

        public int IndexOf(PlayerRef player)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].IsTaken && Slots[i].Player == player)
                    return i;
            }

            return -1;
        }

        public bool IsMageTakenByOther(int mageIndex, PlayerRef player)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                RosterSlot slot = Slots[i];
                if (slot.IsTaken && slot.Player != player && slot.MageIndex == mageIndex)
                    return true;
            }

            return false;
        }

        public bool TryGetMage(int mageIndex, out WizardData mage)
        {
            mage = IsValidMage(mageIndex) ? mages[mageIndex] : null;
            return mage != null;
        }

        public void BeginMatch()
        {
            if (HasStateAuthority)
                MatchStarted = true;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_SetPlayerName(string playerName, RpcInfo info = default)
        {
            int index = IndexOf(SenderOf(info));
            if (index < 0 || !PlayerNameRules.IsValid(playerName))
                return;

            RosterSlot slot = Slots[index];
            slot.PlayerName = PlayerNameRules.Normalize(playerName);
            Slots.Set(index, slot);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_ChooseMage(int mageIndex, RpcInfo info = default)
        {
            PlayerRef sender = SenderOf(info);
            int index = IndexOf(sender);
            if (index < 0 || MatchStarted || !IsValidMage(mageIndex) || IsMageTakenByOther(mageIndex, sender))
                return;

            RosterSlot slot = Slots[index];
            slot.MageIndex = mageIndex;
            slot.IsReady = false;
            Slots.Set(index, slot);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_SetReady(NetworkBool isReady, RpcInfo info = default)
        {
            int index = IndexOf(SenderOf(info));
            if (index < 0 || MatchStarted)
                return;

            RosterSlot slot = Slots[index];
            slot.IsReady = isReady && IsValidMage(slot.MageIndex);
            Slots.Set(index, slot);
        }

        private void AddPlayer(PlayerRef player)
        {
            if (IndexOf(player) >= 0)
                return;

            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].IsTaken)
                    continue;

                Slots.Set(i, new RosterSlot { Player = player, MageIndex = FirstFreeMage(), IsReady = false });
                return;
            }
        }

        private void RemovePlayer(PlayerRef player)
        {
            int index = IndexOf(player);
            if (index >= 0)
                Slots.Set(index, new RosterSlot { Player = PlayerRef.None, MageIndex = NoMage });
        }

        private int FirstFreeMage()
        {
            for (int mageIndex = 0; mageIndex < mages.Length; mageIndex++)
            {
                if (!IsMageTakenByOther(mageIndex, PlayerRef.None))
                    return mageIndex;
            }

            return NoMage;
        }

        private bool IsValidMage(int mageIndex)
        {
            return mages != null && mageIndex >= 0 && mageIndex < mages.Length && mages[mageIndex] != null;
        }

        private PlayerRef SenderOf(RpcInfo info)
        {
            return info.Source.IsRealPlayer ? info.Source : Runner.LocalPlayer;
        }

        private void NotifyChanged()
        {
            ApplyLocalSelection();
            Changed?.Invoke();
        }

        private void ApplyLocalSelection()
        {
            int index = IndexOf(Runner.LocalPlayer);
            if (index >= 0 && TryGetMage(Slots[index].MageIndex, out WizardData mage))
                GameSession.SelectedWizard = mage;
        }
    }
}
