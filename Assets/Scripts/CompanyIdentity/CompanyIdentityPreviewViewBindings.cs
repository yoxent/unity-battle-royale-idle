using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentityPreviewViewBindings : MonoBehaviour
    {
        [SerializeField] private TMP_Text companyNameText;
        [SerializeField] private Image emblemPanel;
        [SerializeField] private CompanyEmblemGraphic emblem;
        [SerializeField] private TMP_Text shapeLabel;
        [SerializeField] private TMP_Text iconLabel;
        [SerializeField] private TMP_Text flagLabel;
        [SerializeField] private Image primaryColorBar;
        [SerializeField] private Image secondaryColorBar;
        [SerializeField] private Image accentColorBar;
        [SerializeField] private List<Sprite> shapeSprites = new List<Sprite>();
        [SerializeField] private List<Sprite> shapeBorderSprites = new List<Sprite>();

        public TMP_Text CompanyNameText => companyNameText;
        public Image EmblemPanel => emblemPanel;
        public CompanyEmblemGraphic Emblem => emblem;
        public TMP_Text ShapeLabel => shapeLabel;
        public TMP_Text IconLabel => iconLabel;
        public TMP_Text FlagLabel => flagLabel;
        public Image PrimaryColorBar => primaryColorBar;
        public Image SecondaryColorBar => secondaryColorBar;
        public Image AccentColorBar => accentColorBar;
        public IReadOnlyList<Sprite> ShapeSprites => shapeSprites;
        public IReadOnlyList<Sprite> ShapeBorderSprites => shapeBorderSprites;

        public int BodyPairCount
        {
            get
            {
                if (shapeSprites == null || shapeBorderSprites == null)
                {
                    return 0;
                }

                return shapeSprites.Count < shapeBorderSprites.Count
                    ? shapeSprites.Count
                    : shapeBorderSprites.Count;
            }
        }
    }
}
