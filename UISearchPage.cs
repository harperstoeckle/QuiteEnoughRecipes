using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public interface IQueryable<T>
{
	public void SetSearchText(string text);
	public void SetFilters(List<Predicate<T>> filters);
	public void SetSortComparison(Comparison<T>? comparison);

	// These will be added to the filter and sort panels.
	public IEnumerable<UIOptionGroup<Predicate<T>>> GetFilterGroups();
	public IEnumerable<UIOptionGroup<Comparison<T>>> GetSortGroups();
}

// A page containing a search bar that can automatically be focused.
public interface IFocusableSearchPage
{
	public void FocusSearchBar();
}

/*
 * TODO: This is only public so that `UIQERState` can avoid closing the option popup when one of
 * these buttons is pressed by checking the type. There should be a better way of checking whether
 * clicking an element should close the popup.
 */
public class OptionPanelToggleButton : UIElement
{
	private string _iconPath;
	private string _name;

	/*
	 * Should be set to true to indicate that an option is currently enabled; this will show a
	 * little red dot and add some text to the tooltip.
	 */
	public bool OptionSelected = false;

	/*
	 * `image` is supposed to be either the bestiary filtering or sorting button, since the icon
	 * can be easily cut out of them from a specific location.
	 */
	public OptionPanelToggleButton(string texturePath, string name)
	{
		_iconPath = texturePath;
		_name = name;
		Width.Pixels = Height.Pixels = 22;
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		var pos = GetDimensions().Position();
		var filterIcon = Main.Assets.Request<Texture2D>(_iconPath).Value;

		// We only want the icon part of the texture without the part that usually has the text.
		sb.Draw(filterIcon, pos, new Rectangle(4, 4, 22, 22), Color.White);

		/*
		 * This is the same indicator used for trapped chests. It's about the right shape and
		 * size, so it's good enough.
		 */
		if (OptionSelected)
		{
			sb.Draw(TextureAssets.Wire.Value, pos + new Vector2(4),
				new Rectangle(4, 58, 8, 8), Color.White, 0f, new Vector2(4), 1, 0, 0);
		}

		if (IsMouseHovering)
		{
			var c = Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.RightClickToClear");
			Main.instance.MouseText(_name + (OptionSelected ? '\n' + c : ""));
		}
	}
}

/*
 * A page with a search bar, filters, and sorting options. It maintains an `IQueryable` element,
 * updating it whenever the search options change.
 */
public class UISearchPage<T> : UIElement, IFocusableSearchPage
{
	// Icon that provides help when hovered.
	private class HelpIcon : UIElement
	{
		private LocalizedText _text;
		public HelpIcon(LocalizedText text)
		{
			_text = text;

			Width.Pixels = Height.Pixels = 22;
			Append(new UIText("?", 0.8f){
				HAlign = 0.5f,
				VAlign = 0.5f,
				TextColor = new Color(0.7f, 0.7f, 0.7f)
			});
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			// Kind of a silly way to draw the text on top.
			base.DrawSelf(sb);

			if (IsMouseHovering)
			{
				UICommon.TooltipMouseText(_text.Value);
			}
		}
	}

	private IQueryable<T> _queryable;


	private UIOptionPanel<Predicate<T>> _filterPanel = new();
	private UIOptionPanel<Comparison<T>> _sortPanel = new();
	private UIQERSearchBar _searchBar = new();

	/*
	 * `squareSideLength` is the side length of the grid squares, and `padding` is the amount of
	 * padding between grid squares.
	 */
	public UISearchPage(IQueryable<T> queryElement, LocalizedText helpText)
	{
		const float BarHeight = 50;

		_queryable = queryElement;

		Width.Percent = 1;
		Height.Percent = 1;

		float offset = 0;

		var filters = queryElement.GetFilterGroups().ToList();
		if (filters.Count > 0)
		{
			var filterToggleButton = new OptionPanelToggleButton(
				"Images/UI/Bestiary/Button_Filtering",
				Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.FilterHover"));

			_filterPanel.Width.Percent = 1;
			_filterPanel.Height.Percent = 1;
			_filterPanel.OnValueChanged += f => {
				var preds = f.Value.ToList();
				filterToggleButton.OptionSelected = !f.IsDefaulted;
				_queryable.SetFilters(preds);
			};

			foreach (var f in filters) { _filterPanel.AddGroup(f); }
			_filterPanel.ResetWithEvent();

			filterToggleButton.OnLeftClick += (b, e) => UISystem.UI?.OpenPopup(_filterPanel);
			filterToggleButton.OnRightClick += (b, e) => _filterPanel.ResetWithEvent();

			offset += filterToggleButton.Width.Pixels + 10;
			Append(filterToggleButton);
		}

		var sorts = queryElement.GetSortGroups().ToList();
		if (sorts.Count > 0)
		{
			var sortToggleButton = new OptionPanelToggleButton(
				"Images/UI/Bestiary/Button_Sorting",
				Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.SortHover"));

			_sortPanel.Width.Percent = 1;
			_sortPanel.Height.Percent = 1;
			_sortPanel.OnValueChanged += f => {
				var comp = f.Value.FirstOrDefault();
				sortToggleButton.OptionSelected = !f.IsDefaulted;
				_queryable.SetSortComparison(comp);
			};

			foreach (var s in sorts) { _sortPanel.AddGroup(s); }
			_sortPanel.ResetWithEvent();

			sortToggleButton.Left.Pixels = offset;
			sortToggleButton.OnLeftClick += (b, e) => UISystem.UI?.OpenPopup(_sortPanel);
			sortToggleButton.OnRightClick += (b, e) => _sortPanel.ResetWithEvent();

			offset += sortToggleButton.Width.Pixels + 10;
			Append(sortToggleButton);
		}

		var helpIcon = new HelpIcon(helpText);
		helpIcon.HAlign = 1;

		// Make room for the filters on the left and the help icon on the right.
		_searchBar.Width = new StyleDimension(-offset - helpIcon.Width.Pixels - 10, 1);
		_searchBar.Left.Pixels = offset;
		_searchBar.OnContentsChanged += s => {
			_queryable.SetSearchText(s ?? "");
		};

		/*
		 * There aren't generic constructors, so there's no way to require that `queryElement` be
		 * a `UIElement`, so we have to do that check here.
		 */
		if (queryElement is UIElement e)
		{
			e.Width.Percent = 1;
			e.Height = new StyleDimension(-BarHeight, 1);
			e.VAlign = 1;

			Append(e);
		}

		Append(_searchBar);
		Append(helpIcon);
	}

	public void FocusSearchBar()
	{
		_searchBar.SetTakingInput(true);
	}

	public void ApplyDefaults()
	{
		_filterPanel.ResetWithEvent();
		_sortPanel.ResetWithEvent();
	}
}
