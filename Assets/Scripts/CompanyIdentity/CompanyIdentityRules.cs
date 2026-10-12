using UnityEngine;

namespace BattleRoyale.CompanyIdentity
{
    public static class CompanyIdentityRules
    {
        public const int MaxCompanyNameLength = 32;

        public static readonly CompanyInsigniaShape[] InsigniaShapes =
            (CompanyInsigniaShape[])System.Enum.GetValues(
                typeof(CompanyInsigniaShape));

        public static readonly CompanyIcon[] Icons =
            (CompanyIcon[])System.Enum.GetValues(typeof(CompanyIcon));

        public static readonly CompanyFlagPattern[] FlagPatterns =
            (CompanyFlagPattern[])System.Enum.GetValues(
                typeof(CompanyFlagPattern));

        public static readonly Color[] Palette =
        {
            new Color(0.72f, 0.20f, 0.14f, 1f), // rust red
            new Color(0.82f, 0.60f, 0.16f, 1f), // gold
            new Color(0.18f, 0.35f, 0.62f, 1f), // blue
            new Color(0.12f, 0.52f, 0.48f, 1f), // teal
            new Color(0.86f, 0.82f, 0.68f, 1f), // ivory
            new Color(0.10f, 0.12f, 0.16f, 1f)  // charcoal
        };

        public static bool TryNormalizeCompanyName(string input, out string normalized)
        {
            normalized = null;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var trimmed = input.Trim();

            if (string.IsNullOrEmpty(trimmed))
                return false;

            if (trimmed.Length > MaxCompanyNameLength)
                return false;

            normalized = trimmed;
            return true;
        }

        public static string GetNameValidationMessage(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Enter a company name.";

            if (input.Trim().Length > MaxCompanyNameLength)
                return "Company names must be 32 characters or fewer.";

            return null;
        }

        public static bool IsSupported(CompanyInsigniaShape shape)
        {
            return System.Array.Exists(InsigniaShapes, s => s == shape);
        }

        public static bool IsSupported(CompanyIcon icon)
        {
            return System.Array.Exists(Icons, i => i == icon);
        }

        public static bool IsSupported(CompanyFlagPattern pattern)
        {
            return System.Array.Exists(FlagPatterns, p => p == pattern);
        }

        public static string GetDisplayName(CompanyInsigniaShape shape)
        {
            return shape switch
            {
                CompanyInsigniaShape.Shield => "Shield",
                CompanyInsigniaShape.Circle => "Circle",
                CompanyInsigniaShape.Banner => "Banner",
                CompanyInsigniaShape.Diamond => "Diamond",
                _ => "Unknown"
            };
        }

        public static string GetDisplayName(CompanyIcon icon)
        {
            return icon switch
            {
                CompanyIcon.Star => "Star",
                CompanyIcon.Crown => "Crown",
                CompanyIcon.Blade => "Blade",
                CompanyIcon.Eye => "Eye",
                _ => "Unknown"
            };
        }

        public static string GetDisplayName(CompanyFlagPattern pattern)
        {
            return pattern switch
            {
                CompanyFlagPattern.Solid => "Solid",
                CompanyFlagPattern.Split => "Split",
                CompanyFlagPattern.Stripe => "Diagonal Stripe",
                CompanyFlagPattern.Cross => "Cross",
                _ => "Unknown"
            };
        }
    }
}
