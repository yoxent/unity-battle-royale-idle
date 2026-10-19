using BattleRoyale.CompanyIdentity;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class CompanyIdentityRulesTests
    {
        [Test]
        public void BlankCompanyNameIsRejected()
        {
            Assert.IsFalse(
                CompanyIdentityRules.TryNormalizeCompanyName(
                    "   ",
                    out _));
        }

        [Test]
        public void CompanyNameIsTrimmed()
        {
            Assert.IsTrue(
                CompanyIdentityRules.TryNormalizeCompanyName(
                    "  Iron Moon  ",
                    out var normalized));
            Assert.AreEqual("Iron Moon", normalized);
        }

        [Test]
        public void CompanyNameOverTheLimitIsRejected()
        {
            var value = new string('x', CompanyIdentityRules.MaxCompanyNameLength + 1);
            Assert.IsFalse(
                CompanyIdentityRules.TryNormalizeCompanyName(
                    value,
                    out _));
        }

        [Test]
        public void SeededRandomizationIsRepeatable()
        {
            Assert.IsTrue(
                CompanyIdentityRandomizer.TryCreate(
                    "Iron Moon",
                    new System.Random(17),
                    out var first));
            Assert.IsTrue(
                CompanyIdentityRandomizer.TryCreate(
                    "Iron Moon",
                    new System.Random(17),
                    out var second));

            Assert.AreEqual(first.companyName, second.companyName);
            Assert.AreEqual(first.insigniaShape, second.insigniaShape);
            Assert.AreEqual(first.icon, second.icon);
            Assert.AreEqual(first.flagPattern, second.flagPattern);
            Assert.AreEqual(0, first.bodyIndex);
            Assert.AreEqual(CompanyIdentityRules.BodyFillColor, first.bodyFillColor);
            Assert.AreEqual(CompanyIdentityRules.BodyBorderColor, first.bodyBorderColor);
            Assert.AreEqual(1f, first.symbolScale);
            Assert.AreEqual(first.primaryColor, second.primaryColor);
            Assert.AreEqual(first.secondaryColor, second.secondaryColor);
            Assert.AreEqual(first.accentColor, second.accentColor);
            Assert.AreEqual(0, first.bodyIndex);
        }

        [Test]
        public void BodyIndexWrapsAsOnePair()
        {
            Assert.AreEqual(0, CompanyIdentityRules.StepBodyIndex(0, 0, 25));
            Assert.AreEqual(1, CompanyIdentityRules.StepBodyIndex(0, 1, 25));
            Assert.AreEqual(24, CompanyIdentityRules.StepBodyIndex(0, -1, 25));
            Assert.AreEqual(0, CompanyIdentityRules.StepBodyIndex(24, 1, 25));
            Assert.AreEqual(0, CompanyIdentityRules.StepBodyIndex(4, 1, 0));
            Assert.AreEqual(Color.white, CompanyIdentityRules.BodyFillColor);
            Assert.AreEqual(CompanyIdentityRules.Palette[5], CompanyIdentityRules.BodyBorderColor);
        }

        [Test]
        public void OldSaveKeepsWhiteFillAndCharcoalBorder()
        {
            var identity = new CompanyIdentityData();
            CompanyIdentityRules.ReadEmblemPaint(
                identity,
                false,
                1f, 0f, 0f,
                0f, 1f, 0f,
                3,
                0f, 0f, 1f,
                9f, -9f,
                400f,
                0f);

            Assert.AreEqual(Color.white, identity.bodyFillColor);
            Assert.AreEqual(CompanyIdentityRules.BodyBorderColor, identity.bodyBorderColor);
            Assert.AreEqual(0, identity.symbolIndex);
            Assert.AreEqual(Color.white, identity.symbolColor);
            Assert.AreEqual(Vector2.zero, identity.symbolPosition);
            Assert.AreEqual(0f, identity.symbolRotation);
            Assert.AreEqual(1f, identity.symbolScale);
        }

        [Test]
        public void EmblemPaintAndSymbolTransformStayInRange()
        {
            var identity = new CompanyIdentityData();
            CompanyIdentityRules.ReadEmblemPaint(
                identity,
                true,
                0.1f, 0.2f, 0.3f,
                0.4f, 0.5f, 0.6f,
                2,
                1f, 1f, 1f,
                4f, -4f,
                400f,
                0f);

            Assert.AreEqual(new Color(0.1f, 0.2f, 0.3f, 1f), identity.bodyFillColor);
            Assert.AreEqual(new Color(0.4f, 0.5f, 0.6f, 1f), identity.bodyBorderColor);
            Assert.AreEqual(2, identity.symbolIndex);
            Assert.AreEqual(1f, identity.symbolPosition.x);
            Assert.AreEqual(-1f, identity.symbolPosition.y);
            Assert.AreEqual(180f, identity.symbolRotation);
            Assert.AreEqual(1f, identity.symbolScale);

            CompanyIdentityRules.Paint(
                identity,
                EmblemPaintTarget.Border,
                CompanyIdentityRules.Palette[0]);
            CompanyIdentityRules.Paint(
                identity,
                EmblemPaintTarget.Symbol,
                CompanyIdentityRules.Palette[2]);
            Assert.AreEqual(
                CompanyIdentityRules.Palette[0],
                CompanyIdentityRules.PaintedColor(identity, EmblemPaintTarget.Border));
            Assert.AreEqual(
                CompanyIdentityRules.Palette[2],
                identity.symbolColor);
            Assert.AreEqual(
                new Color(0.1f, 0.2f, 0.3f, 1f),
                identity.bodyFillColor);

            identity.symbolScale = 0.5f;
            CompanyIdentityRules.ReadEmblemPaint(
                identity,
                true,
                identity.bodyFillColor.r,
                identity.bodyFillColor.g,
                identity.bodyFillColor.b,
                identity.bodyBorderColor.r,
                identity.bodyBorderColor.g,
                identity.bodyBorderColor.b,
                identity.symbolIndex,
                identity.symbolColor.r,
                identity.symbolColor.g,
                identity.symbolColor.b,
                0f,
                0f,
                0f,
                0.5f);
            Assert.AreEqual(0.5f, identity.symbolScale);

            CompanyIdentityRules.ResetSymbolTransform(identity);
            Assert.AreEqual(Vector2.zero, identity.symbolPosition);
            Assert.AreEqual(0f, identity.symbolRotation);
            Assert.AreEqual(1f, identity.symbolScale);
            Assert.AreEqual(0, CompanyIdentityRules.StepBodyIndex(identity.symbolIndex, 1, 0));
        }

        [Test]
        public void EmblemPaletteIsSixteenLightAndSixteenDark()
        {
            var colors = CompanyIdentityRules.EmblemPalette;
            Assert.AreEqual(32, colors.Length);
            Assert.AreEqual(Color.white, colors[0]);
            Assert.AreEqual(CompanyIdentityRules.BodyBorderColor, colors[16]);

            var darkestLight = 1f;
            var lightestDark = 0f;
            for (var index = 0; index < colors.Length; index++)
            {
                var color = colors[index];
                Assert.AreEqual(1f, color.a);
                var luminance = (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
                if (index < 16)
                {
                    if (luminance < darkestLight)
                    {
                        darkestLight = luminance;
                    }
                }
                else if (luminance > lightestDark)
                {
                    lightestDark = luminance;
                }
            }

            Assert.Greater(darkestLight, lightestDark);
        }

        [Test]
        public void RandomizedSelectionsBelongToSupportedSets()
        {
            Assert.IsTrue(
                CompanyIdentityRandomizer.TryCreate(
                    "Iron Moon",
                    new System.Random(23),
                    out var identity));

            Assert.IsTrue(
                CompanyIdentityRules.IsSupported(identity.insigniaShape));
            Assert.IsTrue(CompanyIdentityRules.IsSupported(identity.icon));
            Assert.IsTrue(CompanyIdentityRules.IsSupported(identity.flagPattern));
            CollectionAssert.Contains(
                CompanyIdentityRules.Palette,
                identity.primaryColor);
            CollectionAssert.Contains(
                CompanyIdentityRules.Palette,
                identity.secondaryColor);
            CollectionAssert.Contains(
                CompanyIdentityRules.Palette,
                identity.accentColor);
        }

        [Test]
        public void IdentityColorSlotsCanBeChangedIndependently()
        {
            var identity = new CompanyIdentityData();
            var color = new Color(0.2f, 0.4f, 0.8f, 1f);

            identity.SetColor(CompanyIdentityColorSlot.Accent, color);

            Assert.AreEqual(color, identity.GetColor(
                CompanyIdentityColorSlot.Accent));
            Assert.AreNotEqual(color, identity.GetColor(
                CompanyIdentityColorSlot.Primary));
        }

        [Test]
        public void BlankNameReturnsExactValidationMessage()
        {
            var message = CompanyIdentityRules.GetNameValidationMessage("");
            Assert.AreEqual("Enter a company name.", message);
        }

        [Test]
        public void WhitespaceOnlyNameReturnsExactValidationMessage()
        {
            var message = CompanyIdentityRules.GetNameValidationMessage("   ");
            Assert.AreEqual("Enter a company name.", message);
        }

        [Test]
        public void OverLimitNameReturnsExactValidationMessage()
        {
            var value = new string('x', CompanyIdentityRules.MaxCompanyNameLength + 1);
            var message = CompanyIdentityRules.GetNameValidationMessage(value);
            Assert.AreEqual("Company names must be 32 characters or fewer.", message);
        }

        [Test]
        public void ValidNameReturnsNullValidationMessage()
        {
            var message = CompanyIdentityRules.GetNameValidationMessage("Iron Moon");
            Assert.IsNull(message);
        }

        [Test]
        public void NullRandomIsRejected()
        {
            Assert.IsFalse(
                CompanyIdentityRandomizer.TryCreate(
                    "Iron Moon",
                    null,
                    out _));
        }

        [Test]
        public void SeededRandomizationReturnsThreeDistinctColors()
        {
            Assert.IsTrue(
                CompanyIdentityRandomizer.TryCreate(
                    "Iron Moon",
                    new System.Random(42),
                    out var identity));

            Assert.AreNotEqual(identity.primaryColor, identity.secondaryColor);
            Assert.AreNotEqual(identity.primaryColor, identity.accentColor);
            Assert.AreNotEqual(identity.secondaryColor, identity.accentColor);
        }
    }
}
