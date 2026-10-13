using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Agents
{
    [CreateAssetMenu(
        fileName = "AgentAvatarCatalog",
        menuName = "Battle Royale/Agents/Avatar Catalog")]
    public sealed class AgentAvatarCatalog : ScriptableObject
    {
        [SerializeField] List<AgentAvatarSprites> bodies = new();
        [SerializeField] List<Sprite> faces = new();
        [SerializeField] List<AgentAvatarSprites> hairs = new();
        [SerializeField] List<AgentAvatarSprites> kits = new();
        [SerializeField] List<Color> skinTones = new();
        [SerializeField] List<Color> hairColors = new();
        [SerializeField] List<Color> clothesColors = new();

        public bool TryGetBody(int index, out AgentAvatarSprites part)
        {
            return AgentAvatarParts.TryGet(bodies, index, out part);
        }

        public bool TryGetFace(int index, out Sprite sprite)
        {
            return AgentAvatarParts.TryGet(faces, index, out sprite);
        }

        public bool TryGetHair(int index, out AgentAvatarSprites part)
        {
            return AgentAvatarParts.TryGet(hairs, index, out part);
        }

        public bool TryGetKit(int index, out AgentAvatarSprites part)
        {
            return AgentAvatarParts.TryGet(kits, index, out part);
        }

        public bool TryGetRandom(
            AgentAvatarSlot slot,
            System.Random random,
            out AgentAvatarPartPick pick)
        {
            switch (slot)
            {
                case AgentAvatarSlot.Body:
                    return PickSprites(bodies, random, out pick);
                case AgentAvatarSlot.Face:
                    return PickFace(faces, random, out pick);
                case AgentAvatarSlot.Hair:
                    return PickSprites(hairs, random, out pick);
                case AgentAvatarSlot.Kit:
                    return PickSprites(kits, random, out pick);
                default:
                    pick = AgentAvatarPartPick.None;
                    return false;
            }
        }

        static bool PickSprites(
            List<AgentAvatarSprites> part,
            System.Random random,
            out AgentAvatarPartPick pick)
        {
            if (!AgentAvatarParts.TryGetRandom(part, random, out var index, out var sprites))
            {
                pick = AgentAvatarPartPick.None;
                return false;
            }

            pick = new AgentAvatarPartPick(index, sprites, null);
            return true;
        }

        static bool PickFace(
            List<Sprite> part,
            System.Random random,
            out AgentAvatarPartPick pick)
        {
            if (!AgentAvatarParts.TryGetRandom(part, random, out var index, out var sprite))
            {
                pick = AgentAvatarPartPick.None;
                return false;
            }

            pick = new AgentAvatarPartPick(index, default, sprite);
            return true;
        }

        public bool TryGetRandom(System.Random random, out AgentAvatarSelection selection)
        {
            return AgentAvatarParts.TryGetRandom(
                bodies,
                faces,
                hairs,
                kits,
                random,
                out selection);
        }

        public bool TryCreateAppearance(
            System.Random random,
            out AgentAvatarAppearance appearance)
        {
            appearance = null;
            if (!TryGetRandom(random, out var selection))
            {
                return false;
            }

            return AgentAvatarAppearanceRules.TryPaint(
                selection,
                skinTones,
                hairColors,
                clothesColors,
                random,
                out appearance);
        }
    }
}
