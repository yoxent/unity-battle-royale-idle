using BattleRoyale.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class UITextThemeTests
    {
        UITextTheme _theme;
        TMP_FontAsset _bodyFont;
        TMP_FontAsset _titleFont;
        GameObject _labelObject;

        [SetUp]
        public void SetUp()
        {
            _bodyFont = ScriptableObject.CreateInstance<TMP_FontAsset>();
            _titleFont = ScriptableObject.CreateInstance<TMP_FontAsset>();
            _theme = ScriptableObject.CreateInstance<UITextTheme>();
            var serialized = new SerializedObject(_theme);
            var styles = serialized.FindProperty("styles");
            styles.arraySize = 2;
            SetStyle(styles.GetArrayElementAtIndex(0), "company-body", Color.black, 14f, _bodyFont);
            SetStyle(styles.GetArrayElementAtIndex(1), "company-title", Color.black, 24f, _titleFont);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (_labelObject != null)
            {
                Object.DestroyImmediate(_labelObject);
            }

            Object.DestroyImmediate(_theme);
            Object.DestroyImmediate(_bodyFont);
            Object.DestroyImmediate(_titleFont);
        }

        [Test]
        public void TryGetMatchesAnIdIgnoringCaseAndSpace()
        {
            var found = _theme.TryGet("  Company-Title ", out var style);

            Assert.IsTrue(found);
            Assert.AreSame(_titleFont, style.font);
            Assert.AreEqual(24f, style.fontSize);
        }

        [Test]
        public void TryGetReturnsFalseForAnUnknownId()
        {
            var found = _theme.TryGet("missing", out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void TryGetReturnsTheFirstRowWhenIdsRepeat()
        {
            var serialized = new SerializedObject(_theme);
            var styles = serialized.FindProperty("styles");
            styles.arraySize = 2;
            SetStyle(styles.GetArrayElementAtIndex(0), "company-body", Color.black, 14f, _bodyFont);
            SetStyle(styles.GetArrayElementAtIndex(1), "company-body", Color.white, 30f, _titleFont);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var found = _theme.TryGet("company-body", out var style);

            Assert.IsTrue(found);
            Assert.AreSame(_bodyFont, style.font);
            Assert.AreEqual(14f, style.fontSize);
        }

        [Test]
        public void ApplyToWritesFontColorAndSize()
        {
            _labelObject = new GameObject("Label");
            var label = _labelObject.AddComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Art/Fonts/Baloo/SDF/Baloo2-Regular SDF.asset");
            var style = new UITextStyleDefinition
            {
                id = "company-body",
                color = Color.black,
                fontSize = 14f,
                font = font
            };

            style.ApplyTo(label);

            Assert.AreEqual(Color.black, label.color);
            Assert.AreEqual(14f, label.fontSize);
            Assert.AreSame(font, label.font);
        }

        static void SetStyle(
            SerializedProperty row,
            string id,
            Color color,
            float fontSize,
            TMP_FontAsset font)
        {
            row.FindPropertyRelative("id").stringValue = id;
            row.FindPropertyRelative("color").colorValue = color;
            row.FindPropertyRelative("fontSize").floatValue = fontSize;
            row.FindPropertyRelative("font").objectReferenceValue = font;
        }
    }
}
