using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentityEditorViewBindings : MonoBehaviour
    {
        [SerializeField] private Transform previewParent;
        [SerializeField] private Button doneButton;
        [SerializeField] private CompanyIdentityEditorTabBinding[] tabs;

        public Transform PreviewParent => previewParent;
        public Button DoneButton => doneButton;
        public CompanyIdentityEditorTabBinding[] Tabs => tabs;
    }
}
