namespace BattleRoyale.Company
{
    public static class SideColorRules
    {
        public static bool TryPrimary(FieldEmblem emblem, out FieldColor color)
        {
            color = emblem.Primary;
            return color.Red > 0f || color.Green > 0f || color.Blue > 0f;
        }

        public static FieldColor ForSide(FieldEmblem emblem, int teamIndex)
        {
            return TryPrimary(emblem, out var primary)
                ? primary
                : Fallback(teamIndex);
        }

        public static FieldColor Fallback(int teamIndex)
        {
            if (teamIndex < 0)
            {
                return new FieldColor { Red = 1f, Green = 1f, Blue = 1f };
            }

            switch (teamIndex % 4)
            {
                case 0:
                    return new FieldColor { Red = 0.2f, Green = 0.45f, Blue = 1f };
                case 1:
                    return new FieldColor { Red = 1f, Green = 0.45f, Blue = 0.15f };
                case 2:
                    return new FieldColor { Red = 0.2f, Green = 0.8f, Blue = 0.35f };
                default:
                    return new FieldColor { Red = 0.75f, Green = 0.3f, Blue = 0.9f };
            }
        }
    }
}
