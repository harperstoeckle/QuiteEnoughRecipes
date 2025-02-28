using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public struct OptionGroup<T>
{
	public record struct Option(UIElement Icon, LocalizedText Name, T Value) {}

	/*
	 * If this is null, then the group won't have a header. This might be desirable if the option
	 * panel only has one option group (like sorts).
	 */
	public LocalizedText? Name = null;
	public List<Option> Options = [];

	public OptionGroup() {}
}

/*
 * A panel that displays several groups of options. At most one option from each group can be
 * selected at once.
 */
public class UIOptionPanel<T> : UIPanel
{
	// A button that can be toggled on or off.
	public class OptionButton : UIElement
	{
		private LocalizedText _hoverText;

		public bool Selected = false;
		public T Val;
		public required int GroupIndex;

		public OptionButton(in OptionGroup<T>.Option o)
		{
			_hoverText = o.Name;
			Val = o.Value;

			Width.Pixels = 40;
			Height.Pixels = 40;

			o.Icon.VAlign = o.Icon.HAlign = 0.5f;
			o.Icon.IgnoresMouseInteraction = true;

			Append(o.Icon);
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
				Main.instance.MouseText(_hoverText.Value);
			}
		}
	}

	private List<List<OptionButton>> _groups = [];
	private UIAutoExtendGrid _grid = new();

	/*
	 * This will be activated when the option selection changes. This will be called with a list of
	 * all enabled options.
	 */
	public event Action<List<T>>? OnSelectionChanged;

	public UIOptionPanel()
	{
		_grid.Width.Percent = 1;

		var scroll = new UIScrollbar();
		scroll.Height.Percent = 1;
		scroll.HAlign = 1;

		/*
		 * Instead of using `UIList` or `UIGrid` directly, we wrap a `UIAutoExtendGrid` in a
		 * `UIList`. `UIList` and `UIGrid` try to reorder their elements, which is a pain, so we can
		 * instead just borrow the scrolling capabilities of `UIList` and use our own grid.
		 */
		var list = new UIList();
		list.Height.Percent = 1;
		list.Width = new StyleDimension(-scroll.Width.Pixels, 1);
		list.SetScrollbar(scroll);
		list.Add(_grid);

		Append(list);
		Append(scroll);
	}

	public void AddGroup(in OptionGroup<T> group)
	{
		if (group.Name != null)
		{
			var text = new UIText(group.Name){
				Width = new(0, 1),
				TextOriginX = 0,
				TextOriginY = 1
			};

			// Add padding to groups after the first.
			if (_groups.Count != 0)
			{
				text.Height.Pixels = 30;
			}

			_grid.Append(text);
		}

		var buttons = group.Options
			.Select(g => new OptionButton(g){ GroupIndex = _groups.Count })
			.ToList();
		_groups.Add(buttons);

		foreach (var button in buttons)
		{
			_grid.Append(button);
		}
	}

	public override void LeftClick(UIMouseEvent e)
	{
		base.LeftClick(e);
		if (e.Target is OptionButton pressedButton)
		{
			bool newSelectedState = !pressedButton.Selected;

			foreach (var b in _groups[pressedButton.GroupIndex])
			{
				b.Selected = false;
			}

			pressedButton.Selected = newSelectedState;

			OnSelectionChanged?.Invoke(GetSelectedValues());
		}
	}

	// Disable all active options. This function *will* activate the `OnSelectionChanged` event.
	public void DisableAllOptions()
	{
		foreach (var b in _groups.SelectMany(b => b))
		{
			b.Selected = false;
		}

		OnSelectionChanged?.Invoke([]);
	}

	private List<T> GetSelectedValues()
	{
		return _groups.SelectMany(b => b)
			.Where(b => b.Selected)
			.Select(b => b.Val)
			.ToList();
	}
}
