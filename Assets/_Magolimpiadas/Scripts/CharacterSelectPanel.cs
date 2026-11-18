using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArcaneGames.UI
{
    public class CharacterSelectPanel : MonoBehaviour
    {
        [SerializeField] private WizardData[] wizards;
        [SerializeField] private GameObject[] previewModels;
        [SerializeField] private Image[] selectionFrames;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text elementText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private string nextSceneName = "LaberintoMinotauro";

        private int currentIndex;

        private void OnEnable()
        {
            Select(LoadSavedIndex());
        }

        public void Select(int index)
        {
            if (wizards == null || wizards.Length == 0)
                return;

            currentIndex = Mathf.Clamp(index, 0, wizards.Length - 1);
            Refresh();
        }

        public void SelectNext()
        {
            Select((currentIndex + 1) % wizards.Length);
        }

        public void SelectPrevious()
        {
            Select((currentIndex - 1 + wizards.Length) % wizards.Length);
        }

        public void Confirm()
        {
            if (wizards == null || wizards.Length == 0)
                return;

            WizardData wizard = wizards[currentIndex];
            NewMatch.Restart();
            GameSession.SelectedWizard = wizard;
            PlayerPrefs.SetString(GameSession.SelectedWizardKey, wizard.wizardId);
            PlayerPrefs.Save();

            if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            else
                Debug.LogWarning($"Scene '{nextSceneName}' is not in Build Settings yet.");
        }

        private void Refresh()
        {
            WizardData wizard = wizards[currentIndex];

            if (previewModels != null)
            {
                for (int i = 0; i < previewModels.Length; i++)
                {
                    if (previewModels[i] != null)
                        previewModels[i].SetActive(i == currentIndex);
                }
            }

            if (selectionFrames != null)
            {
                for (int i = 0; i < selectionFrames.Length; i++)
                {
                    if (selectionFrames[i] != null)
                        selectionFrames[i].enabled = i == currentIndex;
                }
            }

            if (nameText != null)
            {
                nameText.text = wizard.displayName;
                nameText.color = wizard.themeColor;
            }

            if (elementText != null)
            {
                elementText.text = wizard.elementName;
                elementText.color = wizard.themeColor;
            }

            if (descriptionText != null)
                descriptionText.text = wizard.description;
        }

        private int LoadSavedIndex()
        {
            string savedId = PlayerPrefs.GetString(GameSession.SelectedWizardKey, string.Empty);
            if (string.IsNullOrEmpty(savedId) || wizards == null)
                return 0;

            for (int i = 0; i < wizards.Length; i++)
            {
                if (wizards[i] != null && wizards[i].wizardId == savedId)
                    return i;
            }

            return 0;
        }
    }
}
