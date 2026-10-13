using System.Collections.Generic;
using BattleRoyale.Agents;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class AgentAvatarAppearanceTests
    {
        readonly List<Color> _skin = new()
        {
            new Color(0.9f, 0.7f, 0.5f, 1f),
            new Color(0.4f, 0.25f, 0.15f, 1f)
        };

        readonly List<Color> _hair = new()
        {
            new Color(0.1f, 0.08f, 0.06f, 1f),
            new Color(0.7f, 0.5f, 0.2f, 1f)
        };

        readonly List<Color> _clothes = new()
        {
            new Color(0.2f, 0.4f, 0.8f, 1f),
            new Color(0.8f, 0.3f, 0.2f, 1f),
            new Color(0.3f, 0.7f, 0.4f, 1f)
        };

        [Test]
        public void TryPaintUsesSkinHairAndClothesSeparately()
        {
            var selection = Selection(2, 1, 3);
            var expected = new System.Random(3);
            var body = new[]
            {
                _skin[expected.Next(_skin.Count)],
                _skin[expected.Next(_skin.Count)]
            };
            var hair = new[] { _hair[expected.Next(_hair.Count)] };
            var kit = new[]
            {
                _clothes[expected.Next(_clothes.Count)],
                _clothes[expected.Next(_clothes.Count)],
                _clothes[expected.Next(_clothes.Count)]
            };

            var found = Paint(selection, new System.Random(3), out var appearance);

            Assert.IsTrue(found);
            Assert.AreEqual(1, appearance.bodyIndex);
            CollectionAssert.AreEqual(body, appearance.bodyColors);
            CollectionAssert.AreEqual(hair, appearance.hairColors);
            CollectionAssert.AreEqual(kit, appearance.kitColors);
            Assert.AreEqual(selection.Body.Sprites.Count, appearance.bodyColors.Length);
        }

        [Test]
        public void TryPaintRejectsAMissingPaletteOrRandom()
        {
            var selection = Selection(1, 0, 1);

            Assert.IsFalse(AgentAvatarAppearanceRules.TryPaint(
                selection,
                new List<Color>(),
                _hair,
                _clothes,
                new System.Random(1),
                out _));
            Assert.IsFalse(AgentAvatarAppearanceRules.TryPaint(
                selection,
                _skin,
                _hair,
                _clothes,
                null,
                out _));
        }

        [Test]
        public void WriteAndReadKeepTheColors()
        {
            var found = Paint(Selection(1, 2, 1), new System.Random(8), out var appearance);
            Assert.IsTrue(found);

            var read = AgentAvatarAppearanceRules.TryRead(
                AgentAvatarAppearanceRules.Write(appearance),
                out var loaded);

            Assert.IsTrue(read);
            Assert.AreEqual(appearance.bodyIndex, loaded.bodyIndex);
            Assert.AreEqual(appearance.faceIndex, loaded.faceIndex);
            CollectionAssert.AreEqual(appearance.bodyColors, loaded.bodyColors);
            CollectionAssert.AreEqual(appearance.kitColors, loaded.kitColors);
            Assert.IsFalse(AgentAvatarAppearanceRules.TryRead("nope", out _));
        }

        [Test]
        public void LoadOrCreateKeepsTheFirstRoll()
        {
            var store = new MemoryAvatarStore();
            var first = LoadOrCreate(
                store,
                "hunter",
                Selection(2, 1, 1),
                new System.Random(5),
                out var created);
            var second = LoadOrCreate(
                store,
                "hunter",
                Selection(0, 0, 0),
                new System.Random(99),
                out var loaded);

            Assert.IsTrue(first);
            Assert.IsTrue(second);
            Assert.AreEqual(created.bodyIndex, loaded.bodyIndex);
            CollectionAssert.AreEqual(created.bodyColors, loaded.bodyColors);
            CollectionAssert.AreEqual(created.hairColors, loaded.hairColors);
            Assert.IsFalse(LoadOrCreate(
                store,
                " ",
                Selection(1, 0, 0),
                new System.Random(1),
                out _));
        }

        [Test]
        public void LoadOrCreateDoesNotReplaceACorruptRecord()
        {
            var store = new MemoryAvatarStore();
            store.Save("hunter", "nope");

            var found = LoadOrCreate(
                store,
                "hunter",
                Selection(1, 1, 1),
                new System.Random(2),
                out _);

            Assert.IsFalse(found);
            Assert.IsTrue(store.TryLoad("hunter", out var data));
            Assert.AreEqual("nope", data);
        }

        bool Paint(
            AgentAvatarSelection selection,
            System.Random random,
            out AgentAvatarAppearance appearance)
        {
            return AgentAvatarAppearanceRules.TryPaint(
                selection,
                _skin,
                _hair,
                _clothes,
                random,
                out appearance);
        }

        bool LoadOrCreate(
            IAgentAvatarStore store,
            string agentId,
            AgentAvatarSelection selection,
            System.Random random,
            out AgentAvatarAppearance appearance)
        {
            return AgentAvatarAppearanceRules.TryLoadOrCreate(
                store,
                agentId,
                selection,
                _skin,
                _hair,
                _clothes,
                random,
                out appearance);
        }

        static AgentAvatarSelection Selection(int bodyFills, int hairFills, int kitFills)
        {
            return new AgentAvatarSelection(
                1,
                Part(bodyFills),
                0,
                null,
                2,
                Part(hairFills),
                3,
                Part(kitFills));
        }

        static AgentAvatarSprites Part(int fills)
        {
            var sprites = new List<Sprite>();
            for (var index = 0; index < fills; index++)
            {
                sprites.Add(null);
            }

            return new AgentAvatarSprites(null, sprites);
        }

        sealed class MemoryAvatarStore : IAgentAvatarStore
        {
            readonly Dictionary<string, string> records = new();

            public bool TryLoad(string agentId, out string data)
            {
                return records.TryGetValue(agentId, out data);
            }

            public void Save(string agentId, string data)
            {
                records[agentId] = data;
            }

            public void Flush()
            {
            }
        }
    }
}
