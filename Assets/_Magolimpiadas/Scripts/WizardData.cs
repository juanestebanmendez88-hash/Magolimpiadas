using UnityEngine;

namespace ArcaneGames
{
    [CreateAssetMenu(fileName = "Wizard", menuName = "Juegos Arcanos/Wizard Data")]
    public class WizardData : ScriptableObject
    {
        public string wizardId;
        public string displayName;
        public string elementName;
        [TextArea] public string description;
        public Color themeColor = Color.white;
        public GameObject modelPrefab;
    }
}
