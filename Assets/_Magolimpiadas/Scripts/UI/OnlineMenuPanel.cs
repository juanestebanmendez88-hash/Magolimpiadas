using ArcaneGames.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneGames.UI
{
    public class OnlineMenuPanel : MonoBehaviour
    {
        private const string PlayerNameKey = "online_player_name";

        [SerializeField] private MainMenuController menu;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button createButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Color infoColor = new Color(0.96f, 0.9f, 0.78f);
        [SerializeField] private Color errorColor = new Color(1f, 0.45f, 0.4f);

        private SessionLauncher launcher;

        private void OnEnable()
        {
            launcher = SessionLauncher.Instance;
            launcher.StatusChanged += ShowInfo;
            launcher.SessionFailed += OnSessionFailed;
            launcher.SessionStarted += OnSessionStarted;

            playerNameInput.characterLimit = PlayerNameRules.MaxLength;
            roomCodeInput.characterLimit = RoomCodeRules.Length;
            if (string.IsNullOrEmpty(playerNameInput.text))
                playerNameInput.text = PlayerPrefs.GetString(PlayerNameKey, string.Empty);

            SetBusy(launcher.IsConnecting);
            if (launcher.TryTakeDisconnectMessage(out string message))
                ShowError(message);
            else
                ShowInfo(string.Empty);
        }

        private void OnDisable()
        {
            if (launcher == null)
                return;

            launcher.StatusChanged -= ShowInfo;
            launcher.SessionFailed -= OnSessionFailed;
            launcher.SessionStarted -= OnSessionStarted;
        }

        public void CreateRoom()
        {
            if (!TryReadPlayerName(out string playerName))
                return;

            SetBusy(true);
            launcher.CreateRoom(playerName);
        }

        public void JoinRoom()
        {
            if (!TryReadPlayerName(out string playerName))
                return;

            string code = RoomCodeRules.Normalize(roomCodeInput.text);
            if (!RoomCodeRules.IsValid(code))
            {
                ShowError(SessionErrorMessages.InvalidRoomCode);
                return;
            }

            roomCodeInput.text = code;
            SetBusy(true);
            launcher.JoinRoom(code, playerName);
        }

        public void Back()
        {
            if (!launcher.IsConnecting)
                menu.ShowMain();
        }

        private bool TryReadPlayerName(out string playerName)
        {
            playerName = PlayerNameRules.Normalize(playerNameInput.text);
            if (!PlayerNameRules.IsValid(playerName))
            {
                ShowError(SessionErrorMessages.InvalidName);
                return false;
            }

            PlayerPrefs.SetString(PlayerNameKey, playerName);
            PlayerPrefs.Save();
            return true;
        }

        private void OnSessionStarted()
        {
            SetBusy(false);
            menu.ShowLobby();
        }

        private void OnSessionFailed(string message)
        {
            SetBusy(false);
            ShowError(message);
        }

        private void SetBusy(bool busy)
        {
            createButton.interactable = !busy;
            joinButton.interactable = !busy;
            playerNameInput.interactable = !busy;
            roomCodeInput.interactable = !busy;
        }

        private void ShowInfo(string message)
        {
            statusText.color = infoColor;
            statusText.text = message;
        }

        private void ShowError(string message)
        {
            statusText.color = errorColor;
            statusText.text = message;
        }
    }
}
