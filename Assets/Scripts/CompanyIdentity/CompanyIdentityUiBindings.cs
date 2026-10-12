using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    [Serializable]
    public sealed class CompanyIdentityEditorOptionBinding
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image image;

        public Button Button => button;
        public TMP_Text Label => label;
        public Image Image => image;

        public void SetReferences(Button button, TMP_Text label, Image image)
        {
            this.button = button;
            this.label = label;
            this.image = image;
        }
    }

    [Serializable]
    public sealed class CompanyIdentityEditorSwatchBinding
    {
        [SerializeField] private Button button;
        [SerializeField] private Image image;
        [SerializeField] private Outline outline;

        public Button Button => button;
        public Image Image => image;
        public Outline Outline => outline;

        public void SetReferences(Button button, Image image, Outline outline)
        {
            this.button = button;
            this.image = image;
            this.outline = outline;
        }
    }

    [Serializable]
    public sealed class CompanyIdentityEditorTabBinding
    {
        [SerializeField] private Button tabButton;
        [SerializeField] private Image tabImage;
        [SerializeField] private GameObject content;
        [SerializeField] private CompanyIdentityEditorOptionBinding[] options;
        [SerializeField] private CompanyIdentityEditorSwatchBinding[] colorSlots;
        [SerializeField] private CompanyIdentityEditorSwatchBinding[] palette;

        public Button TabButton => tabButton;
        public Image TabImage => tabImage;
        public GameObject Content => content;
        public CompanyIdentityEditorOptionBinding[] Options => options;
        public CompanyIdentityEditorSwatchBinding[] ColorSlots => colorSlots;
        public CompanyIdentityEditorSwatchBinding[] Palette => palette;

        public void SetReferences(
            Button tabButton,
            Image tabImage,
            GameObject content,
            CompanyIdentityEditorOptionBinding[] options,
            CompanyIdentityEditorSwatchBinding[] colorSlots,
            CompanyIdentityEditorSwatchBinding[] palette)
        {
            this.tabButton = tabButton;
            this.tabImage = tabImage;
            this.content = content;
            this.options = options;
            this.colorSlots = colorSlots;
            this.palette = palette;
        }
    }

}
