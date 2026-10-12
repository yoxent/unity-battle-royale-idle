using UnityEngine;

namespace BattleRoyale.CompanyIdentity
{
    public enum CompanyInsigniaShape
    {
        Shield,
        Circle,
        Banner,
        Diamond
    }

    public enum CompanyIcon
    {
        Star,
        Crown,
        Blade,
        Eye
    }

    public enum CompanyFlagPattern
    {
        Solid,
        Split,
        Stripe,
        Cross
    }

    public enum CompanyIdentityColorSlot
    {
        Primary,
        Secondary,
        Accent
    }

    [System.Serializable]
    public sealed class CompanyIdentityData
    {
        public string companyName = "";
        public CompanyInsigniaShape insigniaShape = CompanyInsigniaShape.Shield;
        public CompanyIcon icon = CompanyIcon.Star;
        public CompanyFlagPattern flagPattern = CompanyFlagPattern.Solid;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.white;
        public Color accentColor = Color.white;

        public CompanyIdentityData Clone()
        {
            return new CompanyIdentityData
            {
                companyName = this.companyName,
                insigniaShape = this.insigniaShape,
                icon = this.icon,
                flagPattern = this.flagPattern,
                primaryColor = this.primaryColor,
                secondaryColor = this.secondaryColor,
                accentColor = this.accentColor
            };
        }

        public Color GetColor(CompanyIdentityColorSlot slot)
        {
            return slot switch
            {
                CompanyIdentityColorSlot.Primary => primaryColor,
                CompanyIdentityColorSlot.Secondary => secondaryColor,
                CompanyIdentityColorSlot.Accent => accentColor,
                _ => throw new System.ArgumentOutOfRangeException(nameof(slot))
            };
        }

        public void SetColor(CompanyIdentityColorSlot slot, Color color)
        {
            switch (slot)
            {
                case CompanyIdentityColorSlot.Primary:
                    primaryColor = color;
                    break;
                case CompanyIdentityColorSlot.Secondary:
                    secondaryColor = color;
                    break;
                case CompanyIdentityColorSlot.Accent:
                    accentColor = color;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(slot));
            }
        }
    }
}
