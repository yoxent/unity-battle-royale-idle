using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Agents
{
    [Serializable]
    public sealed class AgentAvatarAppearance
    {
        public int version = AgentAvatarAppearanceRules.Version;
        public int bodyIndex = -1;
        public int faceIndex = -1;
        public int hairIndex = -1;
        public int kitIndex = -1;
        public Color[] bodyColors = Array.Empty<Color>();
        public Color[] hairColors = Array.Empty<Color>();
        public Color[] kitColors = Array.Empty<Color>();
    }

    public static class AgentAvatarAppearanceRules
    {
        public const int Version = 1;

        public static bool TryPaint(
            AgentAvatarSelection selection,
            IReadOnlyList<Color> skinTones,
            IReadOnlyList<Color> hairColors,
            IReadOnlyList<Color> clothesColors,
            System.Random random,
            out AgentAvatarAppearance appearance)
        {
            appearance = null;
            if (random == null ||
                !HasColors(skinTones) ||
                !HasColors(hairColors) ||
                !HasColors(clothesColors) ||
                selection.BodyIndex < 0 ||
                selection.FaceIndex < 0 ||
                selection.HairIndex < 0 ||
                selection.KitIndex < 0)
            {
                return false;
            }

            appearance = new AgentAvatarAppearance
            {
                version = Version,
                bodyIndex = selection.BodyIndex,
                faceIndex = selection.FaceIndex,
                hairIndex = selection.HairIndex,
                kitIndex = selection.KitIndex,
                bodyColors = Paint(selection.Body.Sprites, skinTones, random),
                hairColors = Paint(selection.Hair.Sprites, hairColors, random),
                kitColors = Paint(selection.Kit.Sprites, clothesColors, random)
            };
            return true;
        }

        public static string Write(AgentAvatarAppearance appearance)
        {
            if (appearance == null)
            {
                return string.Empty;
            }

            appearance.version = Version;
            return JsonUtility.ToJson(appearance);
        }

        public static bool TryRead(string data, out AgentAvatarAppearance appearance)
        {
            appearance = null;
            if (string.IsNullOrWhiteSpace(data) ||
                data.IndexOf("\"version\":", StringComparison.Ordinal) < 0)
            {
                return false;
            }

            var read = JsonUtility.FromJson<AgentAvatarAppearance>(data);
            if (read == null || read.version != Version)
            {
                return false;
            }

            appearance = read;
            return true;
        }

        public static bool TryLoad(
            IAgentAvatarStore store,
            string agentId,
            out AgentAvatarAppearance appearance)
        {
            appearance = null;
            if (store == null ||
                string.IsNullOrWhiteSpace(agentId) ||
                !store.TryLoad(agentId, out var data))
            {
                return false;
            }

            return TryRead(data, out appearance);
        }

        public static bool Save(
            IAgentAvatarStore store,
            string agentId,
            AgentAvatarAppearance appearance)
        {
            if (store == null ||
                string.IsNullOrWhiteSpace(agentId) ||
                appearance == null)
            {
                return false;
            }

            store.Save(agentId, Write(appearance));
            store.Flush();
            return true;
        }

        public static bool TryLoadOrCreate(
            IAgentAvatarStore store,
            string agentId,
            AgentAvatarSelection selection,
            IReadOnlyList<Color> skinTones,
            IReadOnlyList<Color> hairColors,
            IReadOnlyList<Color> clothesColors,
            System.Random random,
            out AgentAvatarAppearance appearance)
        {
            appearance = null;
            if (store == null || string.IsNullOrWhiteSpace(agentId))
            {
                return false;
            }

            if (store.TryLoad(agentId, out var data))
            {
                return TryRead(data, out appearance);
            }

            if (!TryPaint(
                    selection,
                    skinTones,
                    hairColors,
                    clothesColors,
                    random,
                    out appearance))
            {
                return false;
            }

            return Save(store, agentId, appearance);
        }

        static Color[] Paint(
            IReadOnlyList<Sprite> sprites,
            IReadOnlyList<Color> palette,
            System.Random random)
        {
            var count = sprites == null ? 0 : sprites.Count;
            var colors = new Color[count];
            for (var index = 0; index < count; index++)
            {
                colors[index] = palette[random.Next(palette.Count)];
            }

            return colors;
        }

        static bool HasColors(IReadOnlyList<Color> palette)
        {
            return palette != null && palette.Count > 0;
        }
    }
}
