using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
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

		// These are in reverse order of probability.
		foreach (var drop in drops.OrderByDescending(d => d.dropRate))
		{
			grid.Append(new UILootItemPanel(drop));
		}

		Append(left);
		Append(grid);
	}
}

file class UILootItemPanel : UIItemPanel
{
	private int _stackMin;
	private int _stackMax;
	private float _chance;

	public UILootItemPanel(DropRateInfo info) : base(new(info.itemId))
	{
		_stackMin = info.stackMin;
		_stackMax = info.stackMax;
		_chance = info.dropRate;
	}

	protected override void DrawOverlayText(SpriteBatch sb)
	{
		if (_stackMax > 1 || _stackMin != _stackMax)
		{
			var text = _stackMin == _stackMax ? _stackMin.ToString() : $"{_stackMin}–{_stackMax}";
			DrawText(sb, text, new Vector2(10, 26));
		}

		if (_chance < 0.9999f)
		{
			DrawText(sb, $"{_chance:p2}", new Vector2(25, 3));
		}
	}
}
