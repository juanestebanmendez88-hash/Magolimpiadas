using UnityEngine;
using UnityEngine.EventSystems;

namespace ArcaneGames.UI
{
    public class UIButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private float hoverScale = 1.05f;

        private Vector3 baseScale = Vector3.one;
        private bool cached;

        private void Awake()
        {
            CacheBaseScale();
        }

        private void OnDisable()
        {
            if (cached)
                transform.localScale = baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            CacheBaseScale();
            transform.localScale = baseScale * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (cached)
                transform.localScale = baseScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
        }

        private void CacheBaseScale()
        {
            if (!cached)
            {
                baseScale = transform.localScale;
                cached = true;
            }
        }
    }
}
