using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Agents
{
    public enum AgentAvatarSlot
    {
        Body,
        Face,
        Hair,
        Kit
    }

    public readonly struct AgentAvatarPartPick
    {
        public AgentAvatarPartPick(int index, AgentAvatarSprites sprites, Sprite face)
        {
            Index = index;
            Sprites = sprites;
            Face = face;
        }

        public static AgentAvatarPartPick None { get; } =
            new AgentAvatarPartPick(-1, default, null);

        public int Index { get; }

        public AgentAvatarSprites Sprites { get; }

        public Sprite Face { get; }
    }

    public readonly struct AgentAvatarSelection
    {
        public AgentAvatarSelection(
            int bodyIndex,
            AgentAvatarSprites body,
            int faceIndex,
            Sprite face,
            int hairIndex,
            AgentAvatarSprites hair,
            int kitIndex,
            AgentAvatarSprites kit)
        {
            BodyIndex = bodyIndex;
            Body = body;
            FaceIndex = faceIndex;
            Face = face;
            HairIndex = hairIndex;
            Hair = hair;
            KitIndex = kitIndex;
            Kit = kit;
        }

        public static AgentAvatarSelection None { get; } = new AgentAvatarSelection(
            -1,
            default,
            -1,
            null,
            -1,
            default,
            -1,
            default);

        public int BodyIndex { get; }

        public AgentAvatarSprites Body { get; }

        public int FaceIndex { get; }

        public Sprite Face { get; }

        public int HairIndex { get; }

        public AgentAvatarSprites Hair { get; }

        public int KitIndex { get; }

        public AgentAvatarSprites Kit { get; }
    }

    [Serializable]
    public struct AgentAvatarSprites
    {
        [SerializeField] Sprite outline;
        [SerializeField] List<Sprite> sprites;

        public AgentAvatarSprites(Sprite outline, List<Sprite> sprites)
        {
            this.outline = outline;
            this.sprites = sprites ?? new List<Sprite>();
        }

        public Sprite Outline => outline;

        public IReadOnlyList<Sprite> Sprites =>
            sprites ?? (IReadOnlyList<Sprite>)Array.Empty<Sprite>();
    }

    public static class AgentAvatarParts
    {
        public static bool TryGet(
            IReadOnlyList<AgentAvatarSprites> parts,
            int index,
            out AgentAvatarSprites part)
        {
            if (parts == null || index < 0 || index >= parts.Count)
            {
                part = default;
                return false;
            }

            part = parts[index];
            return true;
        }

        public static bool TryGet(IReadOnlyList<Sprite> parts, int index, out Sprite sprite)
        {
            if (parts == null || index < 0 || index >= parts.Count)
            {
                sprite = null;
                return false;
            }

            sprite = parts[index];
            return true;
        }

        public static bool TryGetRandom<T>(
            IReadOnlyList<T> part,
            System.Random random,
            out int index,
            out T item)
        {
            if (part == null || part.Count == 0 || random == null)
            {
                index = -1;
                item = default;
                return false;
            }

            index = random.Next(part.Count);
            item = part[index];
            return true;
        }

        public static bool TryGetRandom(
            IReadOnlyList<AgentAvatarSprites> bodies,
            IReadOnlyList<Sprite> faces,
            IReadOnlyList<AgentAvatarSprites> hairs,
            IReadOnlyList<AgentAvatarSprites> kits,
            System.Random random,
            out AgentAvatarSelection selection)
        {
            if (random == null ||
                bodies == null || bodies.Count == 0 ||
                faces == null || faces.Count == 0 ||
                hairs == null || hairs.Count == 0 ||
                kits == null || kits.Count == 0)
            {
                selection = AgentAvatarSelection.None;
                return false;
            }

            var bodyIndex = random.Next(bodies.Count);
            var faceIndex = random.Next(faces.Count);
            var hairIndex = random.Next(hairs.Count);
            var kitIndex = random.Next(kits.Count);
            selection = new AgentAvatarSelection(
                bodyIndex,
                bodies[bodyIndex],
                faceIndex,
                faces[faceIndex],
                hairIndex,
                hairs[hairIndex],
                kitIndex,
                kits[kitIndex]);
            return true;
        }
    }
}
