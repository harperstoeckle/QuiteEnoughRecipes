using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

// A panel with a grid of filters that can be toggled on and off.
public class UIFilterPanel : UIPanel
{
	// A button that can be toggled on or off.
	public class FilterButton : UIElement
	{
		private string _hoverText;

		public bool Selected = false;
		public int Index { get; private set; }

		public FilterButton(string hoverText, int index)
		{
			_hoverText = hoverText;
			Width.Pixels = 40;
			Height.Pixels = 40;
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			// TODO: Maybe this should just be done with the greyscale panel and a mask.
			var texture = Selected
				? Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale").Value
				: Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanel").Value;

			var dims = GetDimensions();
			Utils.DrawSplicedPanel(sb, texture, (int) dims.X, (int) dims.Y, (int) dims.Width,
				(int) dims.Height, 10, 10, 10, 10, Color.White);

			if (IsMouseHovering)
			{
				Main.instance.MouseText(_hoverText);
			}
		}

		public override void LeftClick(UIMouseEvent e)
		{
			Selected = !Selected;
			base.LeftClick(e);
		}
	}

	// Used for "normal" filters so they don't need custom icons.
	public class ItemIconFilterButton : FilterButton
	{
		private Item _item;

		public ItemIconFilterButton(Item item, string hoverText, int index) : base(hoverText, index)
		{
			_item = item;
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			base.DrawSelf(sb);
			var dims = GetDimensions();
			ItemSlot.DrawItemIcon(_item, -1, sb, dims.Center(), dims.Width / 50, 32, Color.White);
		}
	}

	private List<FilterButton> _buttons;
	private UIGrid _filterGrid;
	private int _nextFilterIndex = 0;

	public event Action OnFiltersChanged;

	public UIFilterPanel()
	{
		const float ScrollBarWidth = 20;

		var scroll = new UIScrollbar();
		scroll.Height.Percent = 1;
		scroll.HAlign = 1;

		_filterGrid = new();
		_filterGrid.Height.Percent = 1;
		_filterGrid.Width = new StyleDimension(-scroll.Width.Pixels, 1);
		_filterGrid.SetScrollbar(scroll);

		Append(_filterGrid);
		Append(scroll);
	}

	/*
	 * Add a filter button with an item icon and return its index. Filter buttons are assigned
	 * indices starting from 0 in the order they were added.
	 */
	public int AddItemIconFilter(Item item, string name)
	{
		var button = new ItemIconFilterButton(item, name, _nextFilterIndex);
		button.OnLeftClick += (b, e) => OnFiltersChanged?.Invoke();

		_filterGrid.Add(button);
		return _nextFilterIndex++;
	}

	/*
	 * Check whether the filter with index `i` is currently enabled. If given an index not
	 * associated with a button, this will just return false.
	 */
	public bool IsFilterEnabled(int i)
	{
		if (i < _filterGrid.Count && _filterGrid._items[i] is FilterButton b)
		{
			return b.Selected;
		}

		return false;
	}
}
