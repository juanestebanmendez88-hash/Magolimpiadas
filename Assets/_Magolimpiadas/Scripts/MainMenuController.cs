using ArcaneGames.Network;
using UnityEngine;

namespace ArcaneGames.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject characterSelectPanel;
        [SerializeField] private GameObject onlinePanel;
        [SerializeField] private GameObject lobbyPanel;

        private void Start()
        {
            if (SessionLauncher.Instance.Runner != null)
                ShowLobby();
            else if (SessionLauncher.Instance.HasDisconnectMessage)
                ShowOnline();
            else
                ShowMain();
        }

        public void ShowMain()
        {
            SetActivePanel(mainPanel);
        }

        public void ShowSettings()
        {
            SetActivePanel(settingsPanel);
        }

        public void ShowCharacterSelect()
        {
            SetActivePanel(characterSelectPanel);
        }

        public void ShowOnline()
        {
            SetActivePanel(onlinePanel);
        }

        public void ShowLobby()
        {
            SetActivePanel(lobbyPanel);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetActivePanel(GameObject panel)
        {
            foreach (GameObject candidate in new[] { mainPanel, settingsPanel, characterSelectPanel, onlinePanel, lobbyPanel })
            {
                if (candidate != null)
                    candidate.SetActive(candidate == panel);
            }
        }
    }
}
