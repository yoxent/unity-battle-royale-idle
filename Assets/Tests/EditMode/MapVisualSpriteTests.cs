using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class MapVisualSpriteTests
    {
        const string HallMap =
            "Assets/Maps/hall_of_the_raven_savant/Definitions/hall_of_the_raven_savant_Map.asset";

        [Test]
        public void HallModeSelectPictureIsTheWholeVisualMap()
        {
            var map = AssetDatabase.LoadAssetAtPath<MapDefinition>(HallMap);
            Assert.IsNotNull(map);
            Assert.IsNotNull(map.visualMap);
            Assert.IsNotNull(map.visualSprite);
            Assert.AreEqual(map.visualMap, map.visualSprite.texture);

            var importer = AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(map.visualMap)) as TextureImporter;
            Assert.IsNotNull(importer);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.AreEqual(map.visualMap.width, Mathf.RoundToInt(map.visualSprite.rect.width));
            Assert.AreEqual(map.visualMap.height, Mathf.RoundToInt(map.visualSprite.rect.height));
        }
    }
}
