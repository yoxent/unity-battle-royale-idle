using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleSightLines : MonoBehaviour
    {
        public const float LineWidth = 1.5f;

        [SerializeField] private LineRenderer line;

        private readonly List<LineRenderer> lines = new();

        public LineRenderer Template => line;

        public void Bind(LineRenderer template)
        {
            line = template;
            if (line != null && lines.Count == 0)
            {
                lines.Add(line);
            }
        }

        private void Awake()
        {
            if (line == null)
            {
                line = GetComponent<LineRenderer>();
            }

            if (line != null && lines.Count == 0)
            {
                lines.Add(line);
            }

            HideFrom(0);
        }

        public void Show(int index, Vector3 start, Vector3 end)
        {
            var renderer = Prepare(index);
            if (renderer == null)
            {
                return;
            }

            renderer.enabled = true;
            renderer.useWorldSpace = false;
            renderer.positionCount = 2;
            renderer.SetPosition(
                0,
                renderer.transform.InverseTransformPoint(start));
            renderer.SetPosition(
                1,
                renderer.transform.InverseTransformPoint(end));
        }

        public void HideFrom(int index)
        {
            for (var lineIndex = Mathf.Max(0, index);
                 lineIndex < lines.Count;
                 lineIndex++)
            {
                if (lines[lineIndex] != null)
                {
                    lines[lineIndex].enabled = false;
                }
            }
        }

        public static void ApplyVisibleDefaults(LineRenderer renderer)
        {
            renderer.useWorldSpace = false;
            renderer.alignment = LineAlignment.View;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.numCapVertices = 4;
            renderer.widthMultiplier = LineWidth;
            renderer.positionCount = 2;
            renderer.enabled = false;
        }

        private LineRenderer Prepare(int index)
        {
            if (line == null)
            {
                line = GetComponent<LineRenderer>();
            }

            if (line == null)
            {
                return null;
            }

            if (lines.Count == 0)
            {
                lines.Add(line);
            }

            while (lines.Count <= index)
            {
                var clone = Instantiate(line, line.transform.parent);
                clone.name = "SightLine";
                var extra = clone.GetComponent<BattleRoyaleSightLines>();
                if (extra != null)
                {
                    Destroy(extra);
                }

                clone.enabled = false;
                lines.Add(clone);
            }

            return lines[index];
        }
    }
}
