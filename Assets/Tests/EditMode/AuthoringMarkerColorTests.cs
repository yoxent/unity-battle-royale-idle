using BattleRoyale.Maps.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class AuthoringMarkerColorTests
    {
        const string Key = "BattleRoyale.MarkerColor.Test";

        [Test]
        public void StoredColorReplacesTheFallbackAndPaintsTheDisc()
        {
            var previous = EditorPrefs.GetString(Key, string.Empty);
            var parent = new GameObject("Marker Color");
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "MarkerDisc";
            disc.transform.SetParent(parent.transform, false);
            var fallback = new Color(0.2f, 0.2f, 0.2f, 1f);
            var picked = new Color(0.15f, 0.45f, 0.9f, 1f);

            try
            {
                EditorPrefs.DeleteKey(Key);
                Assert.AreEqual(fallback, AuthoringMarkerColor.Get(Key, fallback));
                Assert.IsTrue(AuthoringMarkerColor.StoreIfChanged(Key, fallback, picked));
                Assert.IsFalse(AuthoringMarkerColor.StoreIfChanged(Key, picked, picked));

                var stored = AuthoringMarkerColor.Get(Key, fallback);
                Assert.AreEqual(picked.r, stored.r, 0.01f);
                Assert.AreEqual(picked.g, stored.g, 0.01f);
                Assert.AreEqual(picked.b, stored.b, 0.01f);

                AuthoringMarkerColor.Apply(disc.GetComponent<MeshRenderer>(), picked);
                var block = new MaterialPropertyBlock();
                disc.GetComponent<MeshRenderer>().GetPropertyBlock(block);
                var painted = block.GetColor("_BaseColor");
                Assert.AreEqual(picked.r, painted.r, 0.001f);
                Assert.AreEqual(picked.g, painted.g, 0.001f);
                Assert.AreEqual(picked.b, painted.b, 0.001f);
            }
            finally
            {
                if (string.IsNullOrEmpty(previous))
                {
                    EditorPrefs.DeleteKey(Key);
                }
                else
                {
                    EditorPrefs.SetString(Key, previous);
                }

                Object.DestroyImmediate(parent);
            }
        }
    }
}
