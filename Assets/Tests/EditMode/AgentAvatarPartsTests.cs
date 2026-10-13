using System.Collections.Generic;
using BattleRoyale.Agents;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class AgentAvatarPartsTests
    {
        Texture2D _texture;
        Sprite _outline;
        Sprite _fill;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(1, 1);
            _outline = Sprite.Create(_texture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
            _fill = Sprite.Create(_texture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_outline);
            Object.DestroyImmediate(_fill);
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void TryGetReturnsTheSetAtTheIndex()
        {
            var parts = new List<AgentAvatarSprites>
            {
                new AgentAvatarSprites(_fill, new List<Sprite>()),
                new AgentAvatarSprites(_outline, new List<Sprite> { _fill })
            };

            var found = AgentAvatarParts.TryGet(parts, 1, out var part);

            Assert.IsTrue(found);
            Assert.AreSame(_outline, part.Outline);
            Assert.AreEqual(1, part.Sprites.Count);
            Assert.AreSame(_fill, part.Sprites[0]);
        }

        [Test]
        public void TryGetReturnsTheEmptySlot()
        {
            var parts = new List<AgentAvatarSprites>
            {
                new AgentAvatarSprites(null, null)
            };

            var found = AgentAvatarParts.TryGet(parts, 0, out var part);

            Assert.IsTrue(found);
            Assert.IsNull(part.Outline);
            Assert.AreEqual(0, part.Sprites.Count);
        }

        [Test]
        public void TryGetRejectsAnIndexOutsideTheList()
        {
            var parts = new List<AgentAvatarSprites>
            {
                new AgentAvatarSprites(_outline, new List<Sprite> { _fill })
            };

            Assert.IsFalse(AgentAvatarParts.TryGet(parts, -1, out var negative));
            Assert.IsNull(negative.Outline);
            Assert.IsFalse(AgentAvatarParts.TryGet(parts, 1, out var pastEnd));
            Assert.IsNull(pastEnd.Outline);
            Assert.IsFalse(AgentAvatarParts.TryGet((IReadOnlyList<AgentAvatarSprites>)null, 0, out var missing));
            Assert.IsNull(missing.Outline);
        }

        [Test]
        public void TryGetReturnsTheSpriteAtTheIndex()
        {
            var found = AgentAvatarParts.TryGet(new[] { _outline, _fill }, 1, out var sprite);

            Assert.IsTrue(found);
            Assert.AreSame(_fill, sprite);
        }

        [Test]
        public void TryGetReturnsAnEmptySpriteSlot()
        {
            var found = AgentAvatarParts.TryGet(new Sprite[] { null }, 0, out var sprite);

            Assert.IsTrue(found);
            Assert.IsNull(sprite);
        }

        [Test]
        public void TryGetRejectsASpriteIndexOutsideTheList()
        {
            Assert.IsFalse(AgentAvatarParts.TryGet(new[] { _outline }, -1, out var negative));
            Assert.IsNull(negative);
            Assert.IsFalse(AgentAvatarParts.TryGet(new[] { _outline }, 1, out var pastEnd));
            Assert.IsNull(pastEnd);
            Assert.IsFalse(AgentAvatarParts.TryGet((IReadOnlyList<Sprite>)null, 0, out var missing));
            Assert.IsNull(missing);
        }

        [Test]
        public void TryGetRandomPicksTheSameEntryForTheSameSeed()
        {
            var faces = new List<Sprite> { _outline, _fill, null };
            var expected = new System.Random(4).Next(faces.Count);

            var found = AgentAvatarParts.TryGetRandom(
                faces,
                new System.Random(4),
                out var index,
                out var sprite);

            Assert.IsTrue(found);
            Assert.AreEqual(expected, index);
            Assert.AreSame(faces[index], sprite);
        }

        [Test]
        public void TryGetRandomRejectsAnEmptyPart()
        {
            var found = AgentAvatarParts.TryGetRandom(
                new List<Sprite>(),
                new System.Random(1),
                out var index,
                out var sprite);

            Assert.IsFalse(found);
            Assert.AreEqual(-1, index);
            Assert.IsNull(sprite);
            Assert.IsFalse(AgentAvatarParts.TryGetRandom(
                new List<Sprite> { _outline },
                null,
                out _,
                out _));
        }

        [Test]
        public void TryGetRandomRollsOneEntryFromEachPart()
        {
            var bodies = new List<AgentAvatarSprites>
            {
                new AgentAvatarSprites(_outline, new List<Sprite>()),
                new AgentAvatarSprites(_fill, new List<Sprite> { _outline })
            };
            var faces = new List<Sprite> { _outline, _fill, null };
            var hairs = new List<AgentAvatarSprites>
            {
                new AgentAvatarSprites(_fill, new List<Sprite> { _outline })
            };
            var kits = new List<AgentAvatarSprites>
            {
                new AgentAvatarSprites(null, null),
                new AgentAvatarSprites(_outline, new List<Sprite> { _fill }),
                new AgentAvatarSprites(_fill, new List<Sprite>()),
                new AgentAvatarSprites(_outline, new List<Sprite>())
            };
            var expected = new System.Random(9);

            var found = AgentAvatarParts.TryGetRandom(
                bodies,
                faces,
                hairs,
                kits,
                new System.Random(9),
                out var selection);

            Assert.IsTrue(found);
            Assert.AreEqual(expected.Next(bodies.Count), selection.BodyIndex);
            Assert.AreSame(bodies[selection.BodyIndex].Outline, selection.Body.Outline);
            Assert.AreEqual(expected.Next(faces.Count), selection.FaceIndex);
            Assert.AreSame(faces[selection.FaceIndex], selection.Face);
            Assert.AreEqual(expected.Next(hairs.Count), selection.HairIndex);
            Assert.AreEqual(expected.Next(kits.Count), selection.KitIndex);
            Assert.AreSame(kits[selection.KitIndex].Outline, selection.Kit.Outline);
        }

        [Test]
        public void TryGetRandomRejectsWhenAnyPartIsEmpty()
        {
            var body = new List<AgentAvatarSprites>
            {
                new AgentAvatarSprites(_outline, new List<Sprite>())
            };
            var face = new List<Sprite> { _fill };

            var found = AgentAvatarParts.TryGetRandom(
                body,
                face,
                body,
                new List<AgentAvatarSprites>(),
                new System.Random(1),
                out var selection);

            Assert.IsFalse(found);
            Assert.AreEqual(AgentAvatarSelection.None.KitIndex, selection.KitIndex);
            Assert.AreEqual(-1, selection.BodyIndex);
            Assert.IsNull(selection.Face);
        }
    }
}
