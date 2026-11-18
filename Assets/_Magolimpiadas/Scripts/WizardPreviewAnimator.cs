using UnityEngine;

namespace ArcaneGames.UI
{
    public class WizardPreviewAnimator : MonoBehaviour
    {
        public RuntimeAnimatorController idle;

        private void OnEnable()
        {
            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator != null && idle != null)
            {
                animator.runtimeAnimatorController = idle;
                animator.applyRootMotion = false;
                return;
            }

            Animation legacyAnimation = GetComponentInChildren<Animation>(true);
            if (legacyAnimation == null)
                return;

            AnimationClip chosen = null;
            foreach (AnimationState state in legacyAnimation)
            {
                if (state.clip == null)
                    continue;

                if (chosen == null)
                    chosen = state.clip;

                if (state.clip.name.ToLowerInvariant().Contains("idle"))
                {
                    chosen = state.clip;
                    break;
                }
            }

            if (chosen == null)
                return;

            legacyAnimation.wrapMode = WrapMode.Loop;
            legacyAnimation.clip = chosen;
            legacyAnimation.Play(chosen.name);
        }
    }
}
