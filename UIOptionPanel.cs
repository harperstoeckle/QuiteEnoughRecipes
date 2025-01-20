using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * A panel with a grid of options where at most one can be selected. Each option is associated with
 * a value of type `T`.
 */
public class UIOptionPanel<T> : UIPanel
{
	// A button that can be toggled on or off.
	public class OptionButton : UIElement
	{
		private static int NextIndex = 0;

		private string _hoverText;
		private int _index = NextIndex++;

		public bool Selected = false;
		public T Val;

		public OptionButton(string hoverText, T val)
		{
			_hoverText = hoverText;
			Val = val;
			Width.Pixels = 40;
			Height.Pixels = 40;
		}

		// Hack to prevent `UIGrid` from reordering these buttons.
		public override int CompareTo(object? other)
		{
			if (other is OptionButton b)
			{
				return _index.CompareTo(b._index);
			}
			else
			{
				return 0;
			}
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
	}

	// An option button that displays an item as an icon.
	public class ItemIconOptionButton : OptionButton
	{
		private Item _item;

		public ItemIconOptionButton(Item item, string hoverText, T val) : base(hoverText, val)
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

	private UIGrid _optionGrid;

	/*
	 * This will be activated when the option selection changes. If an option was enabled, this will
	 * be called with the value associated with that option. If all options were disabled, this will
	 * be called with null.
	 */
	public event Action<T?> OnSelectionChanged;

	public UIOptionPanel()
	{
		const float ScrollBarWidth = 20;

		var scroll = new UIScrollbar();
		scroll.Height.Percent = 1;
		scroll.HAlign = 1;

		_optionGrid = new();
		_optionGrid.Height.Percent = 1;
		_optionGrid.Width = new StyleDimension(-scroll.Width.Pixels, 1);
		_optionGrid.SetScrollbar(scroll);

		Append(_optionGrid);
		Append(scroll);
	}

	// Add an option button with the value `val`. When hovered, it will display the text `name`.
	public void AddItemIconOption(Item item, string name, T val)
	{
		_optionGrid.Add(new ItemIconOptionButton(item, name, val));
	}

	public override void LeftClick(UIMouseEvent e)
	{
		base.LeftClick(e);
		if (e.Target is OptionButton pressedButton)
		{
			bool newSelectedState = !pressedButton.Selected;
			foreach (var b in _optionGrid._items.OfType<OptionButton>())
			{
				b.Selected = false;
			}

			pressedButton.Selected = newSelectedState;
			OnSelectionChanged?.Invoke(newSelectedState ? pressedButton.Val : default(T?));
		}
	}
}
