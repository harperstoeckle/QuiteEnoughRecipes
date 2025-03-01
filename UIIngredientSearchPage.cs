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
 * A page including a scrollable list of ingredients, a search bar, and filter options. This
 * contains a list of type `T`, which are displayed in elements of type `E` in a grid.
 */
public class UIIngredientSearchPage<T, E> : UIElement, IFocusableSearchPage
	where T : IIngredient
	where E : UIElement, IScrollableGridElement<T>, new()
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

	private List<T> _allIngredients;
	private List<T> _filteredIngredients;

	private UIScrollableGrid<T, E> _ingredientList = new();

	/*
	 * This is a reference to a popup container in the parent that will be used to display the
	 * filter and sort panels.
	 */
	private UIPopupContainer _optionPanelContainer;

	private OptionPanelToggleButton _filterToggleButton = new("Images/UI/Bestiary/Button_Filtering",
			Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.FilterHover"));
	private UIOptionPanel<Predicate<T>> _filterPanel = new();
	private List<Predicate<T>> _activeFilters = [];

	private OptionPanelToggleButton _sortToggleButton = new("Images/UI/Bestiary/Button_Sorting",
			Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.SortHover"));
	private UIOptionPanel<Comparison<T>> _sortPanel = new();
	private Comparison<T>? _activeSortComparison = null;

	private UIQERSearchBar _searchBar = new();
	private string? _searchText = null;

	/*
	 * `squareSideLength` is the side length of the grid squares, and `padding` is the amount of
	 * padding between grid squares.
	 */
	public UIIngredientSearchPage(UIPopupContainer optionPanelContainer, List<T> allIngredients,
		LocalizedText helpText)
	{
		const float BarHeight = 50;
		const float ScrollBarWidth = 30;

		_allIngredients = allIngredients;
		_filteredIngredients = new(_allIngredients);

		_optionPanelContainer = optionPanelContainer;

		Width.Percent = 1;
		Height.Percent = 1;

		_filterPanel.Width.Percent = 1;
		_filterPanel.Height.Percent = 1;
		_filterPanel.OnSelectionChanged += preds => {
			_filterToggleButton.OptionSelected = preds.Count != 0;
			_activeFilters.Clear();
			_activeFilters.AddRange(preds);
			UpdateDisplayedIngredients();
		};

		_sortPanel.Width.Percent = 1;
		_sortPanel.Height.Percent = 1;
		_sortPanel.OnSelectionChanged += comp => {
			_sortToggleButton.OptionSelected = comp.Count != 0;
			_activeSortComparison = comp.FirstOrDefault();
			UpdateDisplayedIngredients();
		};

		var scroll = new UIScrollbar();
		scroll.Height = new StyleDimension(-BarHeight, 1);
		scroll.HAlign = 1;
		scroll.VAlign = 1;

		_ingredientList.Scrollbar = scroll;
		_ingredientList.Values = _filteredIngredients;
		_ingredientList.Width = new StyleDimension(-ScrollBarWidth, 1);
		_ingredientList.Height = new StyleDimension(-BarHeight, 1);
		_ingredientList.VAlign = 1;

		_filterToggleButton.OnLeftClick += (b, e) => _optionPanelContainer.Toggle(_filterPanel);
		_filterToggleButton.OnRightClick += (b, e) => _filterPanel.DisableAllOptions();

		float offset = _filterToggleButton.Width.Pixels + 10;

		_sortToggleButton.Left.Pixels = offset;
		_sortToggleButton.OnLeftClick += (b, e) => _optionPanelContainer.Toggle(_sortPanel);
		_sortToggleButton.OnRightClick += (b, e) => _sortPanel.DisableAllOptions();

		offset += _sortToggleButton.Width.Pixels + 10;

		var helpIcon = new HelpIcon(helpText);
		helpIcon.HAlign = 1;

		// Make room for the filters on the left and the help icon on the right.
		_searchBar.Width = new StyleDimension(-offset - helpIcon.Width.Pixels - 10, 1);
		_searchBar.Left.Pixels = offset;
		_searchBar.OnContentsChanged += s => {
			_searchText = s;
			UpdateDisplayedIngredients();
		};

		Append(_ingredientList);
		Append(scroll);
		Append(_filterToggleButton);
		Append(_sortToggleButton);
		Append(_searchBar);
		Append(helpIcon);

	}

	public void FocusSearchBar()
	{
		_searchBar.SetTakingInput(true);
	}

	public void AddFilterGroup(in OptionGroup<Predicate<T>> g)
	{
		_filterPanel.AddGroup(g);
	}

	public void AddSortGroup(in OptionGroup<Comparison<T>> g)
	{
		_sortPanel.AddGroup(g);
	}

	// Update what ingredients are being displayed based on the search bar and filters.
	private void UpdateDisplayedIngredients()
	{
		var query = SearchQuery.FromSearchText(_searchText ?? "");

		_filteredIngredients.Clear();
		_filteredIngredients.AddRange(
			_allIngredients.Where(i => query.Matches(i) && (_activeFilters.All(f => f(i)))));

		if (_activeSortComparison != null)
		{
			_filteredIngredients.Sort(_activeSortComparison);
		}

		_ingredientList.Values = _filteredIngredients;
	}

}
