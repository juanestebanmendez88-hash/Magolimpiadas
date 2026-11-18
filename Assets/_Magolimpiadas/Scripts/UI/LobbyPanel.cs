using Fusion;
using ArcaneGames.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneGames.UI
{
    public class LobbyPanel : MonoBehaviour
    {
        private const string WaitingRosterText = "Entrando a la sala...";
        private const string HostHintText = "Cuando todos estén listos, pulsa Empezar.";
        private const string ClientHintText = "Elige tu mago y pulsa Listo. El anfitrión empieza la partida.";
        private const string NotEveryoneReadyText = "Todavía hay jugadores que no están listos.";
        private const string ReadyLabel = "¡Listo!";
        private const string NotReadyLabel = "Cambiar";

        [SerializeField] private MainMenuController menu;
        [SerializeField] private TMP_Text roomCodeText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private LobbySlotView[] slotViews;
        [SerializeField] private Button[] mageButtons;
        [SerializeField] private Image[] mageSelectedFrames;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyLabel;
        [SerializeField] private Button startButton;

        private SessionLauncher launcher;
        private MageRoster roster;
        private bool nameSent;

        private void OnEnable()
        {
            launcher = SessionLauncher.Instance;
            launcher.SessionEnded += OnSessionEnded;
            roomCodeText.text = launcher.RoomCode;
            statusText.text = WaitingRosterText;
            startButton.gameObject.SetActive(launcher.IsHost);
            readyButton.gameObject.SetActive(!launcher.IsHost);
        }

        private void OnDisable()
        {
            if (launcher != null)
                launcher.SessionEnded -= OnSessionEnded;
            DetachRoster();
        }

        private void Update()
        {
            if (roster != null)
                return;

            roster = MageRoster.For(launcher.Runner);
            if (roster == null)
                return;

            roster.Changed += Refresh;
            Refresh();
        }

        public void ChooseMage(int mageIndex)
        {
            if (roster != null)
                roster.RPC_ChooseMage(mageIndex);
        }

        public void ToggleReady()
        {
            if (roster == null || !TryGetLocalSlot(out RosterSlot slot))
                return;

            roster.RPC_SetReady(!slot.IsReady);
        }

        public void StartMatch()
        {
            if (!launcher.StartMatch())
                statusText.text = NotEveryoneReadyText;
        }

        public void Leave()
        {
            launcher.Leave();
            menu.ShowOnline();
        }

        private void Refresh()
        {
            if (roster == null)
                return;

            SendNameOnce();
            RefreshSlots();
            RefreshMageButtons();

            bool hasSlot = TryGetLocalSlot(out RosterSlot localSlot);
            readyLabel.text = hasSlot && localSlot.IsReady ? NotReadyLabel : ReadyLabel;
            startButton.interactable = roster.CanStartMatch;
            statusText.text = launcher.IsHost ? HostHintText : ClientHintText;
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                RosterSlot slot = i < roster.Slots.Length ? roster.Slots[i] : default;
                if (!slot.IsTaken)
                {
                    slotViews[i].ShowEmpty();
                    continue;
                }

                roster.TryGetMage(slot.MageIndex, out WizardData mage);
                bool isLocal = slot.Player == launcher.Runner.LocalPlayer;
                bool isHost = slot.Player == roster.HostPlayer;
                slotViews[i].ShowPlayer(slot.PlayerName.Value, mage, isLocal, isHost, slot.IsReady);
            }
        }

        private void RefreshMageButtons()
        {
            PlayerRef localPlayer = launcher.Runner.LocalPlayer;
            bool hasSlot = TryGetLocalSlot(out RosterSlot localSlot);
            for (int i = 0; i < mageButtons.Length; i++)
            {
                bool mine = hasSlot && localSlot.MageIndex == i;
                mageButtons[i].interactable = mine || !roster.IsMageTakenByOther(i, localPlayer);
                if (i < mageSelectedFrames.Length)
                    mageSelectedFrames[i].enabled = mine;
            }
        }

        private void SendNameOnce()
        {
            if (nameSent || !TryGetLocalSlot(out _))
                return;

            roster.RPC_SetPlayerName(launcher.LocalPlayerName);
            nameSent = true;
        }

        private bool TryGetLocalSlot(out RosterSlot slot)
        {
            slot = default;
            if (roster == null || launcher.Runner == null)
                return false;

            int index = roster.IndexOf(launcher.Runner.LocalPlayer);
            if (index < 0)
                return false;

            slot = roster.Slots[index];
            return true;
        }

        private void OnSessionEnded(string message)
        {
            DetachRoster();
            menu.ShowOnline();
        }

        private void DetachRoster()
        {
            if (roster != null)
                roster.Changed -= Refresh;
            roster = null;
            nameSent = false;
        }
    }
}
