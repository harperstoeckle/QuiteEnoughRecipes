using System;
using Terraria.UI;

namespace QuiteEnoughRecipes;

/*
 * Similar to `UIGrid`, but the actual height of the container increases instead of it being
 * scrollable. Children can just be added with `Append`.
 */
public class UIAutoExtendGrid : UIAutoExtend
{
	public float Padding = 5;

	protected override void ReLayoutChildren()
	{
		float innerWidth = GetInnerDimensions().Width;

		float leftOffset = 0;
		float topOffset = 0;
		float lowestPointInRow = 0;

		foreach (var e in Elements)
		{
			var dims = e.GetOuterDimensions();

			/*
			 * If we would reach the end of the row, we instead go to the next row and put the
			 * element there. If every child is too big, they will each get their own line.
			 */
			if (leftOffset + dims.Width > innerWidth)
			{
				leftOffset = 0;
				lowestPointInRow = topOffset = lowestPointInRow + Padding;
			}

			e.Left.Pixels = leftOffset;
			e.Top.Pixels = topOffset;

			leftOffset += dims.Width + Padding;
			lowestPointInRow = Math.Max(lowestPointInRow, topOffset + dims.Height);
		}
	}
}
