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
            Assert.AreEqual(first.primaryColor, second.primaryColor);
            Assert.AreEqual(first.secondaryColor, second.secondaryColor);
            Assert.AreEqual(first.accentColor, second.accentColor);
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
