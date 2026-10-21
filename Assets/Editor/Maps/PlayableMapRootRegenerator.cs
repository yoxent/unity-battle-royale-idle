using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    static class PlayableMapRootRegenerator
    {
        public static void Regenerate(PlayableMapCatalog catalog)
        {
            if (catalog == null || catalog.maps == null)
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Regenerate Catalog Map Roots",
                    "Stop Play Mode before regenerating map roots.",
                    "OK");
                return;
            }

            var maps = new List<MapDefinition>();
            for (var i = 0; i < catalog.maps.Count; i++)
            {
                var definition = catalog.maps[i];
                if (definition != null && !maps.Contains(definition))
                {
                    maps.Add(definition);
                }
            }

            if (maps.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Regenerate Catalog Map Roots",
                    $"{catalog.name} has no maps.",
                    "OK");
                return;
            }

            var list = new StringBuilder();
            for (var i = 0; i < maps.Count; i++)
            {
                list.AppendLine(maps[i].name);
            }

            if (!EditorUtility.DisplayDialog(
                    "Regenerate Catalog Map Roots",
                    $"Replace the baked prefab for {maps.Count} map(s) in {catalog.name}?\n\n{list}\nThe scene's active map stays where it is.",
                    "Regenerate",
                    "Cancel"))
            {
                return;
            }

            var pending = maps;
            var catalogName = catalog.name;
            EditorApplication.delayCall += () => Run(catalogName, pending);
        }

        static void Run(string catalogName, List<MapDefinition> maps)
        {
            var failed = new StringBuilder();
            var saved = 0;
            try
            {
                for (var i = 0; i < maps.Count; i++)
                {
                    var definition = maps[i];
                    EditorUtility.DisplayProgressBar(
                        "Regenerate Catalog Map Roots",
                        definition.name,
                        (float)i / maps.Count);
                    var dungeon = string.Equals(
                        definition.generator,
                        "dungeon",
                        System.StringComparison.OrdinalIgnoreCase);
                    var ok = dungeon
                        ? DungeonMapAuthoringWindow.RegenerateRoot(definition, out var message)
                        : MapAuthoringWindow.RegenerateRoot(definition, out message);
                    if (ok)
                    {
                        saved++;
                    }
                    else
                    {
                        failed.AppendLine(
                            $"{definition.name}: {message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var summary = failed.Length == 0
                ? $"Regenerated {saved} map root(s) from {catalogName}."
                : $"Regenerated {saved} of {maps.Count} map root(s) from {catalogName}.\n\n{failed}";
            EditorUtility.DisplayDialog("Regenerate Catalog Map Roots", summary, "OK");
        }
    }
}
