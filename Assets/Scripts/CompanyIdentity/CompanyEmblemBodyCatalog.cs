using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.CompanyIdentity
{
    [CreateAssetMenu(
        fileName = "CompanyEmblemBodyCatalog",
        menuName = "Battle Royale/Company/Emblem Body Catalog")]
    public sealed class CompanyEmblemBodyCatalog : ScriptableObject
    {
        [SerializeField] List<Sprite> bodyFills = new();
        [SerializeField] List<Sprite> bodyBorders = new();

        public IReadOnlyList<Sprite> BodyFills => bodyFills;
        public IReadOnlyList<Sprite> BodyBorders => bodyBorders;

        public int PairCount
        {
            get
            {
                if (bodyFills == null || bodyBorders == null)
                {
                    return 0;
                }

                return bodyFills.Count < bodyBorders.Count
                    ? bodyFills.Count
                    : bodyBorders.Count;
            }
        }

        public bool TryGetPair(int index, out Sprite fill, out Sprite border)
        {
            fill = null;
            border = null;
            if (index < 0 || index >= PairCount)
            {
                return false;
            }

            fill = bodyFills[index];
            border = bodyBorders[index];
            return fill != null && border != null;
        }
    }
}
