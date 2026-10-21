#if UNITY_EDITOR
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.Movement;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    internal static class MapGeometrySection
    {
        internal struct Values
        {
            public float WorldScale;
            public float BaseHeight;
            public float WallHeight;
            public float WallThickness;
            public float UnitScale;
        }

        public static bool Draw(
            ref Values values,
            out bool changed,
            out bool applyRequested)
        {
            if (!AuthoringSectionHeader.Draw("Geometry"))
            {
                changed = false;
                applyRequested = false;
                return false;
            }

            EditorGUI.BeginChangeCheck();
            values.WorldScale = Mathf.Max(
                0.0001f,
                EditorGUILayout.FloatField(
                    new GUIContent(
                        "World Scale",
                        "One generator unit becomes this many Unity world units."),
                    values.WorldScale));
            values.BaseHeight = EditorGUILayout.FloatField("Base Height", values.BaseHeight);
            values.WallHeight = Mathf.Max(
                0f,
                EditorGUILayout.FloatField("Wall Height", values.WallHeight));
            values.WallThickness = Mathf.Max(
                0.001f,
                EditorGUILayout.FloatField("Wall Thickness", values.WallThickness));
            values.UnitScale = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField(
                    new GUIContent(
                        "Unit Scale",
                        "Saved on the map by either authoring window. A match applies it to each spawned agent's root, and the spawn clearance grows with it. Apply also writes it onto UnitMovement and BattleRoyaleAgent objects in the loaded scene."),
                    values.UnitScale));
            changed = EditorGUI.EndChangeCheck();
            applyRequested = GUILayout.Button("Apply Unit Scale To Loaded Scene");
            return true;
        }

        public static int ApplyToLoadedUnits(float unitScale)
        {
            var scale = DungeonMapGeometry.UnitLocalScale(unitScale);
            return ApplyScale<UnitMovement>(scale) + ApplyScale<BattleRoyaleAgent>(scale);
        }

        static int ApplyScale<T>(Vector3 scale) where T : Component
        {
            var scaledUnitCount = 0;
            foreach (var unit in Resources.FindObjectsOfTypeAll<T>())
            {
                if (!unit.gameObject.scene.IsValid() ||
                    !unit.gameObject.scene.isLoaded)
                {
                    continue;
                }

                Undo.RecordObject(unit.transform, "Set Unit Scale");
                unit.transform.localScale = scale;
                EditorUtility.SetDirty(unit.transform);
                scaledUnitCount++;
            }

            return scaledUnitCount;
        }
    }
}
#endif
