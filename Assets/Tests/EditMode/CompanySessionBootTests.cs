using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class CompanySessionBootTests
    {
        [Test]
        public void AMissingSaveOrAMissingFileBootsTheAuthoredCompany()
        {
            Assert.AreEqual(CompanyBoot.Authored, CompanySessionBoot.Choose(false, false, false));
            Assert.AreEqual(CompanyBoot.Authored, CompanySessionBoot.Choose(false, true, true));
            Assert.AreEqual(CompanyBoot.Authored, CompanySessionBoot.Choose(true, false, false));
        }

        [Test]
        public void AReadableFileLoadsAndABrokenFileIsRejected()
        {
            Assert.AreEqual(CompanyBoot.Saved, CompanySessionBoot.Choose(true, true, true));
            Assert.AreEqual(CompanyBoot.Reject, CompanySessionBoot.Choose(true, true, false));
        }
    }
}
