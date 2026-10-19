using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentityEditorViewBindings : MonoBehaviour
    {
        [SerializeField] private Transform previewParent;
        [SerializeField] private Button doneButton;
        [SerializeField] private Button previousBodyButton;
        [SerializeField] private Button nextBodyButton;
        [SerializeField] private TMP_Text bodyIndexLabel;
        [SerializeField] private Image bodySpriteImage;
        [SerializeField] private Button shapeTabButton;
        [SerializeField] private Button symbolTabButton;
        [SerializeField] private Image shapeTabImage;
        [SerializeField] private Image symbolTabImage;
        [SerializeField] private GameObject shapeOptions;
        [SerializeField] private GameObject symbolOptions;
        [SerializeField] private Button bodyPaintButton;
        [SerializeField] private Button borderPaintButton;
        [SerializeField] private Image bodyPaintImage;
        [SerializeField] private Image borderPaintImage;
        [SerializeField] private Button previousSymbolButton;
        [SerializeField] private Button nextSymbolButton;
        [SerializeField] private TMP_Text symbolIndexLabel;
        [SerializeField] private Slider positionXSlider;
        [SerializeField] private Slider positionYSlider;
        [SerializeField] private Slider rotationSlider;
        [SerializeField] private Slider scaleSlider;
        [SerializeField] private Button resetSymbolButton;
        [SerializeField] private CompanyIdentityEditorSwatchBinding[] emblemSwatches;
        [SerializeField] private CompanyIdentityEditorTabBinding[] tabs;

        public Transform PreviewParent => previewParent;
        public Button DoneButton => doneButton;
        public Button PreviousBodyButton => previousBodyButton;
        public Button NextBodyButton => nextBodyButton;
        public TMP_Text BodyIndexLabel => bodyIndexLabel;
        public Image BodySpriteImage => bodySpriteImage;
        public Button ShapeTabButton => shapeTabButton;
        public Button SymbolTabButton => symbolTabButton;
        public Image ShapeTabImage => shapeTabImage;
        public Image SymbolTabImage => symbolTabImage;
        public GameObject ShapeOptions => shapeOptions;
        public GameObject SymbolOptions => symbolOptions;
        public Button BodyPaintButton => bodyPaintButton;
        public Button BorderPaintButton => borderPaintButton;
        public Image BodyPaintImage => bodyPaintImage;
        public Image BorderPaintImage => borderPaintImage;
        public Button PreviousSymbolButton => previousSymbolButton;
        public Button NextSymbolButton => nextSymbolButton;
        public TMP_Text SymbolIndexLabel => symbolIndexLabel;
        public Slider PositionXSlider => positionXSlider;
        public Slider PositionYSlider => positionYSlider;
        public Slider RotationSlider => rotationSlider;
        public Slider ScaleSlider => scaleSlider;
        public Button ResetSymbolButton => resetSymbolButton;
        public CompanyIdentityEditorSwatchBinding[] EmblemSwatches => emblemSwatches;
        public CompanyIdentityEditorTabBinding[] Tabs => tabs;
    }
}
