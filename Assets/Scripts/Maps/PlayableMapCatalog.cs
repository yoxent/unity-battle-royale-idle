using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    [CreateAssetMenu(
        fileName = "PlayableMaps",
        menuName = "Battle Royale/Maps/Playable Map Catalog")]
    public sealed class PlayableMapCatalog : ScriptableObject
    {
        [Tooltip(
            "Maps that are ready to run. Order is the fallback order when a game mode needs a map. Each map must already have a baked prefab.")]
        public List<MapDefinition> maps = new();

        public bool IsPlayable(MapDefinition definition, MapGameMode mode)
        {
            return definition != null &&
                   maps != null &&
                   maps.Contains(definition) &&
                   definition.bakedPrefab != null &&
                   definition.SupportsMode(mode);
        }

        public bool TryGetBakedPrefab(MapDefinition definition, out GameObject prefab)
        {
            prefab = null;
            if (definition == null || maps == null || !maps.Contains(definition))
            {
                return false;
            }

            prefab = definition.bakedPrefab;
            return prefab != null;
        }

        public MapDefinition FirstPlayable(MapGameMode mode)
        {
            if (maps == null)
            {
                return null;
            }

            foreach (var definition in maps)
            {
                if (IsPlayable(definition, mode))
                {
                    return definition;
                }
            }

            return null;
        }
    }
}
