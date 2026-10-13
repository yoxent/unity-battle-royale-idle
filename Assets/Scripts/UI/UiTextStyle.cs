using TMPro;
using UnityEngine;

namespace BattleRoyale.UI
{
    /// <summary>
    /// Applies one theme style to the label on this object.
    /// The style id is chosen from the theme in the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiTextStyle : MonoBehaviour
    {
        [SerializeField] UiTextTheme theme;
        [SerializeField] string styleId;
        [SerializeField] TMP_Text label;
        [Tooltip("Off keeps this label's color. Font and size still come from the style.")]
        [SerializeField] bool applyColor = true;

        public bool Uses(UiTextTheme candidate)
        {
            return theme == candidate;
        }

        void OnEnable()
        {
            Apply(true);
        }

        void OnValidate()
        {
            Apply(false);
        }

        public void Apply()
        {
            Apply(true);
        }

        void Apply(bool logMissing)
        {
            if (label == null || theme == null || string.IsNullOrEmpty(styleId))
            {
                if (!logMissing)
                {
                    return;
                }

                if (label == null)
                {
                    Debug.LogError("UiTextStyle: assign a label.", this);
                }
                else if (theme == null)
                {
                    Debug.LogError("UiTextStyle: assign a text theme.", this);
                }
                else
                {
                    Debug.LogError("UiTextStyle: choose a style.", this);
                }

                return;
            }

            if (!theme.TryGet(styleId, out var style))
            {
                if (logMissing)
                {
                    Debug.LogError(
                        $"UiTextStyle: theme '{theme.name}' has no style '{styleId}'.",
                        this);
                }

                return;
            }

            if (style.font == null && logMissing)
            {
                Debug.LogError(
                    $"UiTextStyle: theme '{theme.name}' style '{styleId}' has no font.",
                    this);
            }

            style.ApplyTo(label, applyColor);
        }
    }
}
