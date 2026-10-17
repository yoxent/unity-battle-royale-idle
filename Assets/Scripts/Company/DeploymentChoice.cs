using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class DeploymentChoice : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Button button;

        public Button Button => button;

        public void Show(string text)
        {
            if (label == null || button == null)
            {
                Debug.LogError("Deployment choice needs its label and button.", this);
                return;
            }

            gameObject.SetActive(true);
            label.text = text;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
