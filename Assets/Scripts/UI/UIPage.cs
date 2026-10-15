using UnityEngine;

namespace BattleRoyale.UI
{
    /// <summary>
    /// One full-screen page. The transition fades its canvas group.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIPage : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;

        public CanvasGroup Group => canvasGroup;

        public float Alpha => canvasGroup == null ? 0f : canvasGroup.alpha;

        public bool IsFullyShown =>
            canvasGroup != null &&
            gameObject.activeSelf &&
            canvasGroup.alpha >= 1f &&
            canvasGroup.interactable;

        public bool HasGroup()
        {
            if (canvasGroup != null)
            {
                return true;
            }

            Debug.LogError("UIPage needs its canvas group.", this);
            return false;
        }

        public void ShowImmediate()
        {
            if (!HasGroup())
            {
                return;
            }

            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void HideImmediate()
        {
            if (!HasGroup())
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void PrepareFadeIn()
        {
            if (!HasGroup())
            {
                return;
            }

            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void SetInteractive(bool interactive)
        {
            if (!HasGroup())
            {
                return;
            }

            canvasGroup.interactable = interactive;
            canvasGroup.blocksRaycasts = interactive;
        }
    }
}
