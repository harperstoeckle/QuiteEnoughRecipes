using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * Displays a grid of items that can be scrolled. Somewhat similar to the journey mode
 * item duplication list.
 */
public class UIItemList : UIElement
{
	private List<UIItemPanel> _grid = new();
	private List<Item> _items = new();

	// Width of item squares.
	public float ItemWidth = 50;

	// *all* items to be displayed. These can be scrolled through.
	public List<Item> Items
	{
		get => _items;
		set
		{
			_items = value;
			if (Scrollbar != null) { Scrollbar.ViewPosition = 0; }
			Recalculate();
		}
	}
	public float Padding = 5;
	public int NumCols { get; private set; } = 0;
	public int NumRows { get; private set; } = 0;

	// If this is not present, scrolling will not happen.
	public UIScrollbar? Scrollbar = null;

	// Offset into the list of items for displaying.
	private int _itemsOffset => (int) ((Scrollbar?.ViewPosition ?? 0) / (ItemWidth + Padding)) * NumCols;

	public override void Recalculate()
	{
		base.Recalculate();

		if (NumRows <= 0 || NumCols <= 0) { return; }

		int totalRows = (Items.Count + NumCols - 1) / NumCols;

		Scrollbar?.SetView(NumRows * ItemWidth + (NumRows - 1) * Padding,
			totalRows * ItemWidth + (totalRows - 1) * Padding);
	}

	public override void RecalculateChildren()
	{
		ReLayoutGrid();
		base.RecalculateChildren();
	}

	public override void ScrollWheel(UIScrollWheelEvent e)
	{
		base.ScrollWheel(e);
		if (Scrollbar != null)
		{
			Scrollbar.ViewPosition -= e.ScrollWheelValue;
		}
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		SetDisplayedItems();
	}

	// Restructure the children based on the current size of this element.
	private void ReLayoutGrid()
	{
		var d = GetInnerDimensions();

		NumCols = (int) ((d.Width + Padding) / (ItemWidth + Padding));
		NumRows = (int) ((d.Height + Padding) / (ItemWidth + Padding));

		// Resize the grid without making a whole new list.
		int oldGridCount = _grid.Count;
		int newGridCount = NumRows * NumCols;
		if (oldGridCount > newGridCount)
		{
			_grid.RemoveRange(newGridCount, oldGridCount - newGridCount);
		}
		else if (oldGridCount < newGridCount)
		{
			_grid.AddRange(Enumerable.Repeat(default(UIItemPanel), newGridCount - oldGridCount));
			for (int i = oldGridCount; i < newGridCount; ++i) { _grid[i] = new(); }
		}

		/*
		 * TODO: We don't have to re-add *all* children every time the size changes; I'm just doing
		 * it this way because I'm lazy. This should eventually be fixed.
		 */
		if (oldGridCount != newGridCount)
		{
			RemoveAllChildren();
			foreach (var child in _grid) { Append(child); }
		}

		// Extra space added on the top and left to keep the contents centered.
		float leftOffset = (d.Width - NumCols * ItemWidth - (NumCols - 1) * Padding) / 2;
		float topOffset = 0;

		for (int i = 0; i < NumRows; ++i)
		{
			for (int j = 0; j < NumCols; ++j)
			{
				var itemPanel = _grid[i * NumCols + j];

				itemPanel.Left.Pixels = leftOffset + j * (ItemWidth + Padding);
				itemPanel.Top.Pixels = topOffset + i * (ItemWidth + Padding);
			}
		}
	}

	// Show displayed items based on the current offset into the item array.
	private void SetDisplayedItems()
	{
		int numToShow = Math.Min(Items.Count - _itemsOffset, _grid.Count);

		for (int i = 0; i < _grid.Count; ++i)
		{
			_grid[i].DisplayedItem = i < numToShow ? Items[i + _itemsOffset] : null;
		}
	}
}
