using System.Reflection;
using BattleRoyale.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class LayoutControlPassTests
    {
        [Test]
        public void ParentRebuildStillDrivesTheNestedGroup()
        {
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var panels = new GameObject(
                    "Panels",
                    typeof(RectTransform),
                    typeof(HorizontalLayoutGroup));
                panels.transform.SetParent(canvas.transform, false);

                var column = new GameObject(
                    "Stats",
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(LayoutControlPass));
                column.transform.SetParent(panels.transform, false);
                column.GetComponent<LayoutElement>().preferredWidth = 100f;

                var roster = new GameObject(
                    "Roster",
                    typeof(RectTransform),
                    typeof(VerticalLayoutGroup));
                roster.transform.SetParent(column.transform, false);
                var group = roster.GetComponent<VerticalLayoutGroup>();
                group.childControlWidth = true;
                group.childControlHeight = true;

                var row = new GameObject("Row", typeof(RectTransform), typeof(LayoutElement));
                row.transform.SetParent(roster.transform, false);
                row.GetComponent<LayoutElement>().preferredHeight = 40f;

                LayoutRebuilder.ForceRebuildLayoutImmediate(panels.GetComponent<RectTransform>());

                var drivenBy = typeof(RectTransform).GetProperty(
                    "drivenByObject",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.AreSame(group, drivenBy.GetValue(row.GetComponent<RectTransform>()));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
