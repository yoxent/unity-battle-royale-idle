using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattleRoyale.UI
{
    /// <summary>
    /// Keeps a parent layout rebuild walking into nested layout groups.
    /// A LayoutElement on this object is measured, and that measurement clears
    /// the driven rects of a layout group further down. The control pass then
    /// stops, because a LayoutElement is not a controller. An empty controller
    /// lets the walk continue so the nested group can take those rects back.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LayoutControlPass : UIBehaviour, ILayoutController
    {
        public void SetLayoutHorizontal()
        {
        }

        public void SetLayoutVertical()
        {
        }
    }
}
