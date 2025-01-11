using System.Linq;
using Terraria.UI;

namespace QuiteEnoughRecipes;

/*
 * A UI element that automatically resizes itself vertically to ensure enough room for its children.
 * This is particularly useful when used in a `UIList`. Children of this element should never change
 * their height or vertical position based on their parent element's size. I.e., their
 * `Height.Percent`, `Top.Percent`, and `VAlign` should all be zero. However, `UIAutoExtend`s may be
 * nested.
 */
public class UIAutoExtend : UIElement
{
	public override void Recalculate()
	{
		/*
		 * We have to call `Recalculate` first to ensure that any nested `UIAutoExtend`s are
		 * themselves set to the right height.
		 */
		base.Recalculate();
		ReLayoutChildren();
		float innerHeight = Elements.Count == 0
			? 0
			: Elements.Select(e => e.Top.Pixels + e.Height.Pixels).Max();
		Height.Pixels = MarginTop + PaddingTop + innerHeight + PaddingBottom + MarginBottom;

		/*
		 * Unfortunately, there's no good way to recalculate only *our* dimensions without also
		 * recalculating the children, so we just have to do this again.
		 */
		base.Recalculate();
	}

	/*
	 * Overload this to do any adjustments to children that should only happen once, since
	 * `RecalculateChildren` is called twice.
	 */
	protected virtual void ReLayoutChildren() {}
}
