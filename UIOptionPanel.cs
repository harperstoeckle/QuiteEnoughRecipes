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

public interface IOptionElement<T>
{
	/*
	 * If this is null, then the element is considered to be in a "default state", and it won't be
	 * included in the list of selected options by `UIOptionPanel`.
	 */
	public T? Value { get; }
	public UIElement Element { get; }

	// `Value` should be null after this is called.
	public void Deselect();

	/*
	 * This should be activated whenever the value or selection status of this element is changed by
	 * some external source. I.e., this should be activated when a button is clicked, but it should
	 * not be activated when `Deselect` is called.
	 */
	public event Action<IOptionElement<T>>? OnValueChanged;
}

/*
 * Group of options, placed in a grid, with an optional header. At most one of the contained options
 * can be enabled at once.
 */
public class UIOptionGroup<T> : UIAutoExtendGrid, IOptionElement<T>
{
	private List<IOptionElement<T>> _options = [];

	public T? Value { get; private set; } = default(T?);
	public UIElement Element => this;

	public event Action<IOptionElement<T>>? OnValueChanged;

	public UIOptionGroup(LocalizedText? header)
	{
		Width.Percent = 1;

		if (header != null)
		{
			Append(new UIText(header){
				Width = new(0, 1),
				TextOriginX = 0
			});
		}
	}

	public void Deselect()
	{
		Value = default(T?);
		foreach (var o in _options) { o.Deselect(); }
	}

	public void AddOption(IOptionElement<T> option)
	{
		Append(option.Element);
		_options.Add(option);

		option.OnValueChanged += o => {
			Value = o.Value;

			// New option selected; deselect everything else.
			if (o.Value != null)
			{
				foreach (var opt in _options)
				{
					if (o != opt) { opt.Deselect(); }
				}
			}

			OnValueChanged?.Invoke(this);
		};
	}
}

// A button that can be toggled on or off.
public class UIOptionToggleButton<T> : UIElement, IOptionElement<T>
{
	private bool _isSelected = false;
	private T _value;

	public required LocalizedText HoverText;

	public T? Value => _isSelected ? _value : default(T?);
	public UIElement Element => this;

	public event Action<IOptionElement<T>>? OnValueChanged;

	public UIOptionToggleButton(T value, UIElement icon)
	{
		_value = value;

		Width.Pixels = 40;
		Height.Pixels = 40;

		icon.VAlign = icon.HAlign = 0.5f;
		icon.IgnoresMouseInteraction = true;

		Append(icon);
	}

	public void Deselect() => _isSelected = false;

	public override void LeftClick(UIMouseEvent e)
	{
		base.LeftClick(e);
		_isSelected = !_isSelected;

		OnValueChanged?.Invoke(this);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		// TODO: Maybe this should just be done with the greyscale panel and a mask.
		var texture = _isSelected
			? Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale").Value
			: Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanel").Value;

		var dims = GetDimensions();
		Utils.DrawSplicedPanel(sb, texture, (int) dims.X, (int) dims.Y, (int) dims.Width,
			(int) dims.Height, 10, 10, 10, 10, Color.White);

		if (IsMouseHovering)
		{
			Main.instance.MouseText(HoverText.Value);
		}
	}
}

/*
 * Contains a scrollable list of UI elements that each might provide a value of type `T`. When one
 * of these values is changed (usually by user input), the `UIOptionPanel` will send out a
 * notification via `OnSelectionChanged` with a list of all active options.
 */
public class UIOptionPanel<T> : UIPanel
{
	private List<IOptionElement<T>> _options = [];
	private UIList _list = new(){ ListPadding = 20 };

	/*
	 * This will be activated when the option selection changes. This will be called with a list of
	 * all enabled options.
	 */
	public event Action<List<T>>? OnSelectionChanged;

	public UIOptionPanel()
	{
		var scroll = new UIScrollbar();
		scroll.Height.Percent = 1;
		scroll.HAlign = 1;

		/*
		 * Instead of using `UIList` or `UIGrid` directly, we wrap a `UIAutoExtendGrid` in a
		 * `UIList`. `UIList` and `UIGrid` try to reorder their elements, which is a pain, so we can
		 * instead just borrow the scrolling capabilities of `UIList` and use our own grid.
		 */
		_list.Height.Percent = 1;
		_list.Width = new StyleDimension(-scroll.Width.Pixels, 1);
		_list.SetScrollbar(scroll);
		_list.ManualSortMethod = l => {};

		Append(_list);
		Append(scroll);
	}

	public void AddOption(IOptionElement<T> option)
	{
		_options.Add(option);
		_list.Add(option.Element);
		option.OnValueChanged += o => OnSelectionChanged?.Invoke(GetSelectedOptions());
	}

	// Disable all active options. This function *will* activate the `OnSelectionChanged` event.
	public void DeselectAllOptions()
	{
		foreach (var o in _options) { o.Deselect(); }
		OnSelectionChanged?.Invoke([]);
	}

	private List<T> GetSelectedOptions()
	{
		return _options.Where(o => o.Value != null).Select(o => o.Value!).ToList();
	}
}
