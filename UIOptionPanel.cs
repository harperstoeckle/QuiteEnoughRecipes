using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public interface IOptionElement<T>
{
	// If `IsEnabled` is false, then the value won't be used.
	public bool IsEnabled { get; }
	public bool IsDefaulted { get; }

	public T Value { get; }
	public UIElement Element { get; }

	/*
	 * Reset to the default state. This should not actiate `OnValueChanged`. After calling this,
	 * `IsDefaulted` should be true.
	 */
	public void Reset();

	// Disable this element. After calling this, `IsEnabled` should be set to false.
	public void Disable();

	/*
	 * This should be activated whenever the value or enabled status of this element is changed by
	 * some external source. I.e., this should be activated when a button is clicked, but it
	 * should not be activated when `Reset` is called.
	 */
	public event Action<IOptionElement<T>>? OnValueChanged;
}

// Option element that applies a function to its value.
public class MappedOptionElement<T, U> : IOptionElement<U>
{
	private IOptionElement<T> _inner;
	private Func<T, U> _func;

	public bool IsEnabled => _inner.IsEnabled;
	public bool IsDefaulted => _inner.IsDefaulted;
	public UIElement Element => _inner.Element;
	public U Value => _func(_inner.Value);

	public event Action<IOptionElement<U>>? OnValueChanged;

	public MappedOptionElement(IOptionElement<T> inner, Func<T, U> func)
	{
		_inner = inner;
		_func = func;

		_inner.OnValueChanged += e => OnValueChanged?.Invoke(this);
	}

	public void Reset() => _inner.Reset();
	public void Disable() => _inner.Disable();
}

public static class OptionElementExtensions
{
	// Used to convert one type of `OptionElement` to another by a function.
	public static IOptionElement<U> Map<T, U>(this IOptionElement<T> e, Func<T, U> f)
	{
		return new MappedOptionElement<T, U>(e, f);
	}
}

/*
 * Group of options, placed in a grid, with an optional header. It will attempt to ensure that
 * only at most one option can be enabled at once.
 */
public class UIOptionGroup<T> : UIAutoExtendGrid, IOptionElement<IEnumerable<T>>
{
	private List<IOptionElement<IEnumerable<T>>> _subgroups = [];

	public bool IsEnabled => _subgroups.Any(o => o.IsEnabled);
	public bool IsDefaulted => _subgroups.All(o => o.IsDefaulted);
	public IEnumerable<T> Value => _subgroups.Where(o => o.IsEnabled).SelectMany(o => o.Value);
	public UIElement Element => this;

	public event Action<IOptionElement<IEnumerable<T>>>? OnValueChanged;

	public UIOptionGroup(LocalizedText? header = null)
	{
		Width.Percent = 1;

		if (header != null)
		{
			Append(new UIText(header){
				Width = new(0, 1),
				Height = new(20, 0),
				TextOriginX = 0,
				TextOriginY = 0
			});
		}
	}

	public void Reset() { foreach (var o in _subgroups) { o.Reset(); } }
	public void Disable() { foreach (var o in _subgroups) { o.Disable(); } }

	public void AddSubgroup(IOptionElement<IEnumerable<T>> group)
	{
		/*
		 * A bit of a hack. We assume that any group with proper subgroups will *only* have
		 * proper subgroups, so we give them more space.
		 */
		Padding = 20;
		DoAddSubgroup(group);
	}

	public void AddOption(IOptionElement<T> option)
	{
		// TODO: Does this add any meaningful overhead?
		DoAddSubgroup(option.Map<T, IEnumerable<T>>(v => [v]));
	}

	private void DoAddSubgroup(IOptionElement<IEnumerable<T>> group)
	{
		Append(group.Element);
		_subgroups.Add(group);

		group.OnValueChanged += g => {
			// New option selected; deselect everything else.
			if (g.IsEnabled)
			{
				foreach (var opt in _subgroups)
				{
					if (g != opt) { opt.Disable(); }
				}
			}

			OnValueChanged?.Invoke(this);
		};
	}
}

[Flags]
public enum OptionRules
{
	AllowDisable = 1 << 0,
	AllowEnable = 1 << 1,
	EnabledByDefault = 1 << 2,
}

// A button that can be toggled on or off.
public class UIOptionToggleButton<T> : UIElement, IOptionElement<T>
{
	private OptionRules _rules;

	public required LocalizedText HoverText;
	public bool IsDefaulted => IsEnabled == ((_rules & OptionRules.EnabledByDefault) != 0);
	public bool IsEnabled { get; private set; } = false;
	public T Value { get; set; }
	public UIElement Element => this;

	public event Action<IOptionElement<T>>? OnValueChanged;

	public UIOptionToggleButton(T value, UIElement icon,
		OptionRules rules = OptionRules.AllowDisable | OptionRules.AllowEnable)
	{
		Value = value;
		_rules = rules;

		Reset();

		Width.Pixels = 40;
		Height.Pixels = 40;

		icon.VAlign = icon.HAlign = 0.5f;
		icon.IgnoresMouseInteraction = true;

		Append(icon);
	}

	public void Reset() => IsEnabled = (_rules & OptionRules.EnabledByDefault) != 0;
	public void Disable() => IsEnabled = false;

	public override void LeftClick(UIMouseEvent e)
	{
		base.LeftClick(e);

		if ((_rules & OptionRules.AllowEnable) != 0 && !IsEnabled
			|| (_rules & OptionRules.AllowDisable) != 0 && IsEnabled)
		{
			IsEnabled = !IsEnabled;
			OnValueChanged?.Invoke(this);
		}
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		// TODO: Maybe this should just be done with the greyscale panel and a mask.
		var texture = IsEnabled
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
 * Contains a scrollable list of option groups, each separated by a horizontal line. Unlike
 * `UIOptionGroup`, this will *not* disable other options when an option is enabled.
 */
public class UIOptionPanel<T> : UIPanel, IOptionElement<IEnumerable<T>>
{
	private List<IOptionElement<IEnumerable<T>>> _groups = [];
	private UIList _list = new(){ ListPadding = 20 };

	public bool IsEnabled => _groups.Any(g => g.IsEnabled);
	public bool IsDefaulted => _groups.All(g => g.IsDefaulted);
	public IEnumerable<T> Value => _groups.Where(o => o.IsEnabled).SelectMany(o => o.Value);
	public UIElement Element => this;

	/*
	 * This will be activated when the option selection changes. This will be called with a list of
	 * all enabled options.
	 */
	public event Action<IOptionElement<IEnumerable<T>>>? OnValueChanged;

	public UIOptionPanel()
	{
		BackgroundColor = new Color(33, 43, 79) * 0.8f;

		var scroll = new UIScrollbar();
		scroll.Height.Percent = 1;
		scroll.HAlign = 1;

		/*
		 * Instead of using `UIList` or `UIGrid` directly, we wrap a `UIAutoExtendGrid` in a
		 * `UIList`. `UIList` and `UIGrid` try to reorder their elements, which is a pain, so we can
		 * instead just borrow the scrolling capabilities of `UIList` and use our own grid.
		 */
		_list.Height.Percent = 1;
		_list.Width = new StyleDimension(-30, 1);
		_list.SetScrollbar(scroll);
		_list.ManualSortMethod = l => {};

		Append(_list);
		Append(scroll);
	}

	public void AddGroup(IOptionElement<IEnumerable<T>> group)
	{
		_groups.Add(group);

		if (_list.Count > 0)
		{
			_list.Add(new UIHorizontalSeparator(){
				Width = new(0, 1),
				Color = new Color(66, 86, 158) * 0.8f
			});
		}
		_list.Add(group.Element);

		group.OnValueChanged += o => OnValueChanged?.Invoke(this);
	}

	public void Reset() { foreach (var g in _groups) { g.Reset(); } }
	public void Disable() { foreach (var o in _groups) { o.Disable(); } }

	// This will reset the options in this panel, but also activate the `OnValueChanged` event.
	public void ResetWithEvent()
	{
		Reset();
		OnValueChanged?.Invoke(this);
	}
}
