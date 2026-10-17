using BattleRoyale.Company;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Battle Royale/UI/Spider Chart")]
    public sealed class SpiderChartGraphic : MaskableGraphic
    {
        [SerializeField] Color webColor = new Color(0.03529412f, 0.17254902f, 0.29803923f, 0.55f);
        [SerializeField] Color fillColor = new Color(0.231f, 0.51f, 0.965f, 0.45f);
        [SerializeField] float lineWidth = 1.5f;
        [SerializeField] TMP_Text[] valueLabels;

        int[] rolls;

        public void Show(int[] source)
        {
            if (!SpiderChartLayout.HasValues(source))
            {
                Clear();
                return;
            }

            if (rolls == null || rolls.Length != SpiderChartLayout.AxisCount)
            {
                rolls = new int[SpiderChartLayout.AxisCount];
            }

            for (var i = 0; i < rolls.Length; i++)
            {
                rolls[i] = source[i];
            }

            PlaceValues();
            SetVerticesDirty();
        }

        public void Clear()
        {
            rolls = null;
            PlaceValues();
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            PlaceValues();
        }

        void PlaceValues()
        {
            if (valueLabels == null || valueLabels.Length != SpiderChartLayout.AxisCount)
            {
                Debug.LogError("Spider chart is missing its stat labels.", this);
                return;
            }

            var shown = SpiderChartLayout.HasValues(rolls);
            for (var axis = 0; axis < SpiderChartLayout.AxisCount; axis++)
            {
                var label = valueLabels[axis];
                if (label == null)
                {
                    Debug.LogError("Spider chart is missing its stat labels.", this);
                    continue;
                }

                var stat = StatName(label.text);
                label.text = shown ? stat + " (" + rolls[axis] + ")" : stat;
            }
        }

        static string StatName(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var open = text.LastIndexOf(" (", System.StringComparison.Ordinal);
            if (open < 0 || !text.EndsWith(")"))
            {
                return text;
            }

            for (var i = open + 2; i < text.Length - 1; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                {
                    return text;
                }
            }

            return text.Substring(0, open);
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (!SpiderChartLayout.HasValues(rolls))
            {
                return;
            }

            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (radius <= 0f)
            {
                return;
            }

            var center = rect.center;
            var points = new Vector2[SpiderChartLayout.AxisCount];
            for (var axis = 0; axis < points.Length; axis++)
            {
                if (!SpiderChartLayout.TryUnitPoint(axis, rolls[axis], out var x, out var y))
                {
                    return;
                }

                points[axis] = Local(center, radius, x, y);
            }

            AddFill(vertices, center, points, fillColor);
            AddWeb(vertices, center, radius, webColor);
        }

        void AddWeb(VertexHelper vertices, Vector2 center, float radius, Color color)
        {
            for (var ring = 0; ring < SpiderChartLayout.RingCount; ring++)
            {
                for (var axis = 0; axis < SpiderChartLayout.AxisCount; axis++)
                {
                    var next = (axis + 1) % SpiderChartLayout.AxisCount;
                    if (!SpiderChartLayout.TryRingPoint(ring, axis, out var x0, out var y0) ||
                        !SpiderChartLayout.TryRingPoint(ring, next, out var x1, out var y1))
                    {
                        continue;
                    }

                    AddLine(vertices, Local(center, radius, x0, y0), Local(center, radius, x1, y1), color);
                }
            }

            for (var axis = 0; axis < SpiderChartLayout.AxisCount; axis++)
            {
                if (!SpiderChartLayout.TryUnitPoint(axis, 100, out var x, out var y))
                {
                    continue;
                }

                AddLine(vertices, center, Local(center, radius, x, y), color);
            }
        }

        static Vector2 Local(Vector2 center, float radius, float x, float y)
        {
            return new Vector2(center.x + x * radius, center.y + y * radius);
        }

        static void AddFill(VertexHelper vertices, Vector2 center, Vector2[] points, Color color)
        {
            var start = vertices.currentVertCount;
            vertices.AddVert(center, color, Vector2.zero);
            for (var i = 0; i < points.Length; i++)
            {
                vertices.AddVert(points[i], color, Vector2.zero);
            }

            for (var sector = 0; sector < SpiderChartLayout.AxisCount; sector++)
            {
                if (!SpiderChartLayout.TryFillTriangle(sector, out var hub, out var from, out var to))
                {
                    continue;
                }

                vertices.AddTriangle(start + hub, start + from, start + to);
            }
        }

        void AddLine(VertexHelper vertices, Vector2 from, Vector2 to, Color color)
        {
            var direction = to - from;
            var length = direction.magnitude;
            if (length <= 0.001f || lineWidth <= 0f)
            {
                return;
            }

            direction /= length;
            var extra = direction * (lineWidth * 0.5f);
            from -= extra;
            to += extra;
            var normal = new Vector2(-direction.y, direction.x) * (lineWidth * 0.5f);
            var start = vertices.currentVertCount;
            vertices.AddVert(from - normal, color, Vector2.zero);
            vertices.AddVert(from + normal, color, Vector2.zero);
            vertices.AddVert(to + normal, color, Vector2.zero);
            vertices.AddVert(to - normal, color, Vector2.zero);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start, start + 2, start + 3);
        }
    }
}
