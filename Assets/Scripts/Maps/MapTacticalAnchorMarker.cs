using UnityEngine;

namespace BattleRoyale.Maps
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class MapTacticalAnchorMarker : MonoBehaviour
    {
        public const string DefaultColorHtml = "#A659FF";

        public static readonly Color DefaultColor = ParseDefaultColor();

        public string anchorId = "Anchor";
        public float weight = 1f;
        public int index;
        public Color color = DefaultColor;

        private void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, 1.25f);
        }

        static Color ParseDefaultColor()
        {
            ColorUtility.TryParseHtmlString(DefaultColorHtml, out var color);
            color.a = 1f;
            return color;
        }
    }
}
