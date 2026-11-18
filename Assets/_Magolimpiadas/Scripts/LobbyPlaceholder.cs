using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneGames.UI
{
    public class LobbyPlaceholder : MonoBehaviour
    {
        [SerializeField] private TMP_Text selectedWizardText;

        private void Start()
        {
            if (selectedWizardText == null)
                return;

            if (GameSession.SelectedWizard != null)
            {
                selectedWizardText.text = $"Mago seleccionado: {GameSession.SelectedWizard.displayName}";
                selectedWizardText.color = GameSession.SelectedWizard.themeColor;
            }
            else
            {
                string savedId = PlayerPrefs.GetString(GameSession.SelectedWizardKey, string.Empty);
                selectedWizardText.text = string.IsNullOrEmpty(savedId)
                    ? "Ningún mago seleccionado"
                    : $"Mago seleccionado: {savedId}";
            }
        }

        public void GoToMainMenu()
        {
            SceneManager.LoadScene("00_Inicio");
        }
    }
}
