using UnityEngine;

namespace ArcaneGames.UI
{
    public class ModelTurntable : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 30f;

        private void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f);
        }
    }
}
