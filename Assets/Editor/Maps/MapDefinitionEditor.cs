using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    [CustomEditor(typeof(MapDefinition))]
    sealed class MapDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var definition = (MapDefinition)target;
            if (GUILayout.Button("Edit"))
            {
                if (string.Equals(definition.generator, "dungeon", System.StringComparison.OrdinalIgnoreCase))
                {
                    DungeonMapAuthoringWindow.Open(definition);
                }
                else
                {
                    MapAuthoringWindow.Open(definition);
                }
            }

            DrawDefaultInspector();
        }
    }
}
