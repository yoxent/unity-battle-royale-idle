using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class MapWorldPointTests
    {
        [Test]
        public void WorldPoint_AddsTheInstanceOrigin()
        {
            var host = new GameObject("Map");
            host.transform.position = new Vector3(1000f, 0f, 0f);
            var map = host.AddComponent<MapInstance>();

            var world = map.WorldPoint(new Vector3(12f, 3f, 4f));

            Assert.AreEqual(new Vector3(1012f, 3f, 4f), world);
            Object.DestroyImmediate(host);
        }
    }
}
