using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneGames.UI
{
    public class LobbySlotView : MonoBehaviour
    {
        private const string WaitingText = "Esperando jugador...";
        private const string BotText = "Si nadie entra, lo maneja un bot";
        private const string HostText = "Anfitrión";
        private const string LocalText = "Tú";
        private const string ReadyText = "¡Listo!";
        private const string NotReadyText = "Eligiendo...";

        [SerializeField] private Image frame;
        [SerializeField] private Image readyIcon;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text mageNameText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private Color stateColor = new Color(0.96f, 0.9f, 0.78f);
        [SerializeField] private Color readyColor = new Color(0.55f, 1f, 0.6f);

        public void ShowEmpty()
        {
            frame.color = emptyColor;
            readyIcon.enabled = false;
            playerNameText.text = WaitingText;
            mageNameText.text = string.Empty;
            stateText.text = BotText;
            stateText.color = stateColor;
        }

        public void ShowPlayer(string playerName, WizardData mage, bool isLocal, bool isHost, bool isReady)
        {
            frame.color = Color.white;
            playerNameText.text = string.IsNullOrEmpty(playerName) ? "..." : playerName;
            mageNameText.text = mage != null ? mage.displayName : string.Empty;
            mageNameText.color = mage != null ? mage.themeColor : Color.white;

            bool ready = isHost || isReady;
            readyIcon.enabled = ready;
            stateText.color = ready ? readyColor : stateColor;
            stateText.text = Role(isLocal, isHost) + (ready ? ReadyText : NotReadyText);
        }

        private static string Role(bool isLocal, bool isHost)
        {
            if (isLocal && isHost)
                return LocalText + " · " + HostText + " · ";
            if (isLocal)
                return LocalText + " · ";
            return isHost ? HostText + " · " : string.Empty;
        }
    }
}
