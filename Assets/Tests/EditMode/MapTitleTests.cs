using BattleRoyale.Maps;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class MapTitleTests
    {
        [Test]
        public void AHeadingIsTheMapName()
        {
            const string markdown = "# Maze of the Cursed Messiah \n\n*The maze.*\n";
            Assert.AreEqual("Maze of the Cursed Messiah", MapTitle.Heading(markdown));
        }

        [Test]
        public void AMissingHeadingFallsBackToTheAssetName()
        {
            Assert.IsNull(MapTitle.Heading("No heading here"));
            Assert.AreEqual(
                "Maze Of The Cursed Messiah",
                MapTitle.Readable("maze_of_the_cursed_messiah_Map"));
            Assert.AreEqual("Map", MapTitle.Readable("  "));
        }
    }
}
