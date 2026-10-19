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

        public static readonly Color[] EmblemPalette =
        {
            Color.white,
            new Color(0.93f, 0.90f, 0.78f, 1f),
            new Color(0.96f, 0.72f, 0.70f, 1f),
            new Color(0.98f, 0.78f, 0.58f, 1f),
            new Color(0.98f, 0.86f, 0.45f, 1f),
            new Color(0.98f, 0.94f, 0.55f, 1f),
            new Color(0.78f, 0.92f, 0.55f, 1f),
            new Color(0.62f, 0.90f, 0.72f, 1f),
            new Color(0.55f, 0.88f, 0.82f, 1f),
            new Color(0.62f, 0.84f, 0.96f, 1f),
            new Color(0.70f, 0.74f, 0.96f, 1f),
            new Color(0.82f, 0.70f, 0.94f, 1f),
            new Color(0.96f, 0.68f, 0.84f, 1f),
            new Color(0.96f, 0.80f, 0.86f, 1f),
            new Color(0.90f, 0.82f, 0.68f, 1f),
            new Color(0.82f, 0.86f, 0.88f, 1f),
            new Color(0.10f, 0.12f, 0.16f, 1f),
            new Color(0.05f, 0.05f, 0.07f, 1f),
            new Color(0.42f, 0.10f, 0.12f, 1f),
            new Color(0.55f, 0.22f, 0.12f, 1f),
            new Color(0.38f, 0.24f, 0.12f, 1f),
            new Color(0.28f, 0.32f, 0.12f, 1f),
            new Color(0.10f, 0.32f, 0.16f, 1f),
            new Color(0.08f, 0.28f, 0.24f, 1f),
            new Color(0.08f, 0.16f, 0.36f, 1f),
            new Color(0.12f, 0.24f, 0.48f, 1f),
            new Color(0.18f, 0.14f, 0.40f, 1f),
            new Color(0.32f, 0.12f, 0.32f, 1f),
            new Color(0.40f, 0.10f, 0.24f, 1f),
            new Color(0.22f, 0.26f, 0.30f, 1f),
            new Color(0.18f, 0.22f, 0.28f, 1f),
            new Color(0.02f, 0.02f, 0.03f, 1f)
        };

        public static readonly Color BodyFillColor = Color.white;

        public static Color BodyBorderColor => Palette[5];

        public static int StepBodyIndex(int index, int direction, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            var stepped = index + direction;
            var wrapped = stepped % count;
            if (wrapped < 0)
            {
                wrapped += count;
            }

            return wrapped;
        }

        public const float SymbolPositionMin = -1f;
        public const float SymbolPositionMax = 1f;
        public const float SymbolRotationMin = -180f;
        public const float SymbolRotationMax = 180f;
        public const float SymbolScaleMin = 0.5f;
        public const float SymbolScaleMax = 2f;

        public static float ClampSymbolPosition(float value)
        {
            return Mathf.Clamp(value, SymbolPositionMin, SymbolPositionMax);
        }

        public static float ClampSymbolRotation(float value)
        {
            return Mathf.Clamp(value, SymbolRotationMin, SymbolRotationMax);
        }

        public static float ClampSymbolScale(float value)
        {
            return Mathf.Clamp(value, SymbolScaleMin, SymbolScaleMax);
        }

        public static float ResolveSymbolScale(float stored)
        {
            return stored <= 0f ? 1f : ClampSymbolScale(stored);
        }

        public static void Paint(
            CompanyIdentityData identity,
            EmblemPaintTarget target,
            Color color)
        {
            color.a = 1f;
            switch (target)
            {
                case EmblemPaintTarget.Body:
                    identity.bodyFillColor = color;
                    break;
                case EmblemPaintTarget.Border:
                    identity.bodyBorderColor = color;
                    break;
                case EmblemPaintTarget.Symbol:
                    identity.symbolColor = color;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(target));
            }
        }

        public static Color PaintedColor(
            CompanyIdentityData identity,
            EmblemPaintTarget target)
        {
            switch (target)
            {
                case EmblemPaintTarget.Body:
                    return identity.bodyFillColor;
                case EmblemPaintTarget.Border:
                    return identity.bodyBorderColor;
                case EmblemPaintTarget.Symbol:
                    return identity.symbolColor;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(target));
            }
        }

        public static void ResetSymbolTransform(CompanyIdentityData identity)
        {
            identity.symbolPosition = Vector2.zero;
            identity.symbolRotation = 0f;
            identity.symbolScale = 1f;
        }

        public static void ReadEmblemPaint(
            CompanyIdentityData identity,
            bool hasEmblemPaint,
            float fillR,
            float fillG,
            float fillB,
            float borderR,
            float borderG,
            float borderB,
            int symbolIndex,
            float symbolR,
            float symbolG,
            float symbolB,
            float positionX,
            float positionY,
            float rotation,
            float scale)
        {
            if (!hasEmblemPaint)
            {
                identity.bodyFillColor = BodyFillColor;
                identity.bodyBorderColor = BodyBorderColor;
                identity.symbolIndex = 0;
                identity.symbolColor = Color.white;
                identity.symbolPosition = Vector2.zero;
                identity.symbolRotation = 0f;
                identity.symbolScale = 1f;
                return;
            }

            identity.bodyFillColor = new Color(fillR, fillG, fillB, 1f);
            identity.bodyBorderColor = new Color(borderR, borderG, borderB, 1f);
            identity.symbolIndex = symbolIndex;
            identity.symbolColor = new Color(symbolR, symbolG, symbolB, 1f);
            identity.symbolPosition = new Vector2(
                ClampSymbolPosition(positionX),
                ClampSymbolPosition(positionY));
            identity.symbolRotation = ClampSymbolRotation(rotation);
            identity.symbolScale = ResolveSymbolScale(scale);
        }

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
