using System;
using TMPro;
using UnityEngine;

namespace BattleRoyale.UI
{
    [Serializable]
    public struct UiTextStyleDefinition
    {
        public string id;
        public Color color;
        public float fontSize;
        public TMP_FontAsset font;

        public void ApplyTo(TMP_Text text, bool applyColor = true)
        {
            if (applyColor && text.color != color)
            {
                text.color = color;
            }

            if (fontSize > 0f && !Mathf.Approximately(text.fontSize, fontSize))
            {
                text.fontSize = fontSize;
            }

            if (font != null && text.font != font)
            {
                text.font = font;
            }
        }
    }

    [CreateAssetMenu(fileName = "Ui Text Theme", menuName = "Battle Royale/UI/Text Theme")]
    public sealed class UiTextTheme : ScriptableObject
    {
        [SerializeField] UiTextStyleDefinition[] styles;

        public int StyleCount => styles == null ? 0 : styles.Length;

        public string StyleId(int index)
        {
            return styles[index].id;
        }

        public static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            return id.Trim().ToLowerInvariant();
        }

        void OnValidate()
        {
            if (styles == null)
            {
                return;
            }

            for (var i = 0; i < styles.Length; i++)
            {
                var style = styles[i];
                var normalized = NormalizeId(style.id);
                if (normalized == style.id)
                {
                    continue;
                }

                style.id = normalized;
                styles[i] = style;
            }
        }

        public bool TryGet(string id, out UiTextStyleDefinition style)
        {
            id = NormalizeId(id);
            if (styles != null && id.Length > 0)
            {
                for (var i = 0; i < styles.Length; i++)
                {
                    if (NormalizeId(styles[i].id) != id)
                    {
                        continue;
                    }

                    style = styles[i];
                    return true;
                }
            }

            style = default;
            return false;
        }
    }
}
