using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyEmblemGraphic : MonoBehaviour
    {
        [SerializeField] Image bodyFill;
        [SerializeField] Image bodyBorder;

        public void Apply(
            int bodyIndex,
            IReadOnlyList<Sprite> fills,
            IReadOnlyList<Sprite> borders,
            Color fillColor,
            Color borderColor)
        {
            if (bodyFill == null || bodyBorder == null)
            {
                Debug.LogError("Emblem graphic is missing its images.", this);
                return;
            }

            if (fills == null || borders == null)
            {
                Debug.LogError("Emblem body lists are missing.", this);
                return;
            }

            var count = fills.Count < borders.Count ? fills.Count : borders.Count;
            if (count <= 0)
            {
                Debug.LogError("Emblem body lists are empty.", this);
                return;
            }

            if (fills.Count != borders.Count)
            {
                Debug.LogError("Emblem fill and border lists are not a pair.", this);
            }

            var index = CompanyIdentityRules.StepBodyIndex(bodyIndex, 0, count);
            var fill = fills[index];
            var border = borders[index];
            if (fill == null || border == null)
            {
                Debug.LogError("Emblem body pair " + index + " is missing a sprite.", this);
                return;
            }

            bodyFill.sprite = fill;
            bodyBorder.sprite = border;
            bodyFill.color = fillColor;
            bodyBorder.color = borderColor;
        }
    }
}
