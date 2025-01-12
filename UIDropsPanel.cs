using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria.UI;

namespace QuiteEnoughRecipes;

/*
 * Displays an element on the left with a grid of dropped items on the right. The offset of the
 * grid is based on the `width` value of the left element.
 */
public class UIDropsPanel : UIAutoExtend
{
	public UIDropsPanel(UIElement left, List<DropRateInfo> drops)
	{
		Width.Percent = 1;

		var grid = new UIAutoExtendGrid(){
			Width = new StyleDimension(-left.Width.Pixels - 10, 1 - left.Width.Percent),
			HAlign = 1
		};

		foreach (var drop in drops)
		{
			// TODO: Show the percentage and range of quantities.
			grid.Append(new UIItemPanel(new(drop.itemId, drop.stackMin)));
		}

		Append(left);
		Append(grid);
	}
}
