using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * An group of options that can put in a `UIOptionPanel`. An `IOptionGroup` keeps track of whether
 * the option is in its default state, and can be reset back to the default state.
 */
public interface IOptionGroup
{
	// `IsDefaulted` is true if all non-locked options are in their default states.
	public bool IsDefaulted { get; }
	public bool HasLocks { get; }

	// Get the element to be displayed in the panel.
	public UIElement Element { get; }

	/*
	 * Reset to the default state. If this element has any events that trigger when the value
	 * changes, they should be triggered by this.
	 */
	public void Reset();
	public void ClearLocks();
}

/*
 * When `UIOptionPanel` detects a right click in its bounds, it will first check that the target
 * does not implement this interface. It will then reset all of the option groups in the panel only
 * if the target doesn't implement `IDoNotClearTag`.
 */
public interface IDoNotClearTag {}

/*
 * Auto-extending grid that allows groups of elements to be given section headers. Sections are
 * addressed by their localization keys. If they key doesn't exist, the header will not display any
 * text.
 */
public class UIAutoExtendSectionGrid : UIAutoExtendGrid
{
	private class Section
	{
		public required string Key;
		public List<UIElement> Elems = [];
		public required UIText HeaderElem;
	}

	private bool _hasChanged = false;
	private List<Section> _sections = [];

	public void AddToSection(UIElement e, string sectionNameKey)
	{
		var section = _sections.FirstOrDefault(s => s.Key == sectionNameKey);

		if (section is null)
		{
			var text = Language.Exists(sectionNameKey)
				? Language.GetText(sectionNameKey)
				: Language.GetText("");

			section = new(){
				Key = sectionNameKey,
				HeaderElem = new(text){
					Width = new(0, 1),

					// Sections after the first one have extra space at the top.
					PaddingTop = _sections.Count == 0 ? 0 : 20,
					TextOriginX = 0,
					TextOriginY = 1
				}
			};
			_sections.Add(section);
		}

		section.Elems.Add(e);
		_hasChanged = true;
	}

	public override void Recalculate()
	{
		if (_hasChanged)
		{
			RemoveAllChildren();
			foreach (var s in _sections)
			{
				Append(s.HeaderElem);
				foreach (var elem in s.Elems) { Append(elem); }
			}

			_hasChanged = false;
		}

		base.Recalculate();
	}
}

public class UIFilterGroup<T> : UIAutoExtendSectionGrid, IOptionGroup
{
	private enum FilterState
	{
		Unselected,
		Yes,
		No
	}

	private class FilterData
	{
		public required Predicate<T> Pred;
		public FilterState State = FilterState.Unselected;
		public bool Locked = false;
	}

	private List<UIOptionButton<FilterData>> _optionButtons = [];

	public bool IsDefaulted =>
		_optionButtons.All(b => b.Value.State == FilterState.Unselected || b.IsLocked);
	public bool HasLocks => _optionButtons.Any(b => b.IsLocked);
	public UIElement Element => this;

	// Activated when any filters are changed.
	public event Action? OnFiltersChanged;

	public UIFilterGroup()
	{
		Width.Percent = 1;
	}

	public void AddFilter(Predicate<T> pred, LocalizedText name, UIElement icon, string sectionNameKey)
	{
		_optionButtons.Add(new(icon){
			Value = new(){ Pred = pred },
			HoverText = name,
		});

		AddToSection(_optionButtons[_optionButtons.Count - 1], sectionNameKey);
	}

	public void Reset()
	{
		foreach (var button in _optionButtons)
		{
			if (!button.IsLocked)
			{
				SetButtonState(button, FilterState.Unselected);
			}
		}
		OnFiltersChanged?.Invoke();
	}

	public void ClearLocks()
	{
		foreach (var button in _optionButtons)
		{
			button.IsLocked = false;
		}
	}

	public override void LeftClick(UIMouseEvent e)
	{
		base.LeftClick(e);

		if (e.Target is UIOptionButton<FilterData> b)
		{
			bool isLocking = Main.keyState.IsKeyDown(Main.FavoriteKey);
			var (newState, newLock) = ApplyButtonStateTransition(b.Value.State, b.IsLocked,
				isLocking, false);

			if (newState == FilterState.Yes && !newLock && !Main.keyState.PressingShift())
			{
				foreach (var button in _optionButtons)
				{
					if (button.Value.State == FilterState.Yes && !button.IsLocked)
					{
						SetButtonState(button, FilterState.Unselected);
					}
				}
			}

			SetButtonState(b, newState);
			b.IsLocked = newLock;
			OnFiltersChanged?.Invoke();
		}
	}

	public override void RightClick(UIMouseEvent e)
	{
		base.RightClick(e);

		if (e.Target is UIOptionButton<FilterData> b)
		{
			bool isLocking = Main.keyState.IsKeyDown(Main.FavoriteKey);
			var (newState, newLock) = ApplyButtonStateTransition(b.Value.State, b.IsLocked,
				isLocking, true);

			SetButtonState(b, newState);
			b.IsLocked = newLock;
			OnFiltersChanged?.Invoke();
		}
	}

	// Get all filters that are enabled and not negated.
	public IEnumerable<Predicate<T>> GetPositiveFilters()
	{
		return _optionButtons
			.Where(b => b.Value.State == FilterState.Yes)
			.Select(b => b.Value.Pred);
	}

	// Get all filters that are negated (the predicates themselves are not negated.
	public IEnumerable<Predicate<T>> GetNegativeFilters()
	{
		return _optionButtons
			.Where(b => b.Value.State == FilterState.No)
			.Select(b => b.Value.Pred);
	}

	private static void SetButtonState(UIOptionButton<FilterData> button, FilterState newState)
	{
		button.Value.State = newState;
		button.PanelOverrideColor = newState switch {
			FilterState.No => Colors.RarityDarkRed,
			FilterState.Yes => Color.White,
			_ => null
		};
	}

	private static (FilterState, bool) ApplyButtonStateTransition(FilterState s, bool isLocked,
		bool isLocking, bool isRightClick)
	{
		var destState = isRightClick ? FilterState.No : FilterState.Yes;

		return (s, isLocked, isLocking) switch {
			(_, true, _) => (FilterState.Unselected, false),
			(_, _, true) => (destState, true),
			(FilterState.Unselected, _, _) => (destState, isLocking),
			_ => (FilterState.Unselected, false),
		};
	}
}

public class UISortGroup<T> : UIAutoExtendSectionGrid, IOptionGroup
{
	private int _activeSortIndex = 0;
	private List<UIOptionButton<Comparison<T>>> _sortButtons = [];

	public bool IsDefaulted => _activeSortIndex == 0;
	public bool HasLocks => false;
	public UIElement Element => this;

	public event Action? OnSortChanged;

	/*
	 * Unlike `UIFilterGroup`, exactly one sort option must be active at a time. This means that
	 * there must be at least one sort option already available at the start.
	 */
	public UISortGroup(Comparison<T> defaultComp, LocalizedText defaultName, UIElement defaultIcon,
		string defaultSectionNameKey)
	{
		Width.Percent = 1;
		AddSort(defaultComp, defaultName, defaultIcon, defaultSectionNameKey);
	}

	public void AddSort(Comparison<T> comp, LocalizedText name, UIElement icon, string sectionNameKey)
	{
		_sortButtons.Add(new(icon){
			Value = comp,
			HoverText = name
		});

		AddToSection(_sortButtons[_sortButtons.Count - 1], sectionNameKey);
	}

	public void Reset()
	{
		SetIndex(0);
		OnSortChanged?.Invoke();
	}

	public void ClearLocks() {}

	public override void LeftClick(UIMouseEvent e)
	{
		base.LeftClick(e);

		if (e.Target is UIOptionButton<Comparison<T>> b)
		{
			int index = _sortButtons.FindIndex(button => button == b);
			if (index != -1) { SetIndex(index); }
			OnSortChanged?.Invoke();
		}
	}

	public Comparison<T> GetActiveSort() => _sortButtons[_activeSortIndex].Value;

	private void SetIndex(int i)
	{
		if (i >= _sortButtons.Count) { return; }

		foreach (var button in _sortButtons) { button.PanelOverrideColor = null; }
		_sortButtons[i].PanelOverrideColor = Color.White;
		_activeSortIndex = i;
	}
}

/*
 * A simple button with an icon. A lot like `GroupOptionButton`, but it allows for any arbitrary
 * element as the icon.
 */
public class UIOptionButton<T> : UIElement, IDoNotClearTag
{
	private UIImageFramed _lockIcon;
	private bool _isLocked;

	public required LocalizedText HoverText;
	public required T Value;
	public UIElement Element => this;

	// Override the color of the panel in the back.
	public Color? PanelOverrideColor = null;

	// When true, displays a lock icon at the top left of the button.
	public bool IsLocked
	{
		get => _isLocked;
		set
		{
			_isLocked = value;
			var tex = _isLocked ? TextureAssets.HbLock[0] : TextureAssets.MagicPixel;
			var frame = _isLocked ? tex.Frame(2) : Rectangle.Empty;
			_lockIcon.SetImage(tex, frame);
		}
	}

	public UIOptionButton(UIElement icon)
	{
		_lockIcon = new(TextureAssets.MagicPixel, Rectangle.Empty){
			Left = new(-5, 0),
			Top = new(-5, 0),
			IgnoresMouseInteraction = true
		};

		Width.Pixels = 40;
		Height.Pixels = 40;

		icon.VAlign = icon.HAlign = 0.5f;
		icon.IgnoresMouseInteraction = true;

		Append(icon);
		Append(_lockIcon);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		// TODO: Maybe this should just be done with the greyscale panel and a mask.
		var texture = PanelOverrideColor is null
			? Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanel").Value
			: Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale").Value;

		var dims = GetDimensions();
		Utils.DrawSplicedPanel(sb, texture, (int) dims.X, (int) dims.Y, (int) dims.Width,
			(int) dims.Height, 10, 10, 10, 10, PanelOverrideColor ?? Color.White);

		if (IsMouseHovering)
		{
			Main.instance.MouseText(HoverText.Value);
		}
	}
}

// Contains a scrollable list of option groups, each separated by a horizontal line.
public class UIOptionPanel : UIPanel, IOptionGroup
{
	private List<IOptionGroup> _groups = [];
	private UIList _list = new(){ ListPadding = 20 };

	public bool IsDefaulted => _groups.All(g => g.IsDefaulted);
	public bool HasLocks => _groups.Any(g => g.HasLocks);
	public UIElement Element => this;

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

	public void AddGroup(IOptionGroup group)
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
	}

	public override void RightClick(UIMouseEvent e)
	{
		base.RightClick(e);
		if (e.Target is not IDoNotClearTag)
		{
			if (Main.keyState.PressingShift())
			{
				ClearLocks();
			}
			Reset();
		}
	}

	public void Reset() { foreach (var g in _groups) { g.Reset(); } }
	public void ClearLocks() { foreach (var g in _groups) { g.ClearLocks(); } }
}
