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
	private struct SearchQuery
	{
		public string Name;
		public string? Mod;
		public string? Tooltip;

		public bool Matches(T i)
		{
			if (!string.IsNullOrWhiteSpace(Name) &&
				(i.Name == null || !NormalizeForSearch(i.Name).Contains(Name)))
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(Mod) &&
				(i.Mod == null || !NormalizeForSearch(RemoveWhitespace(i.Mod.DisplayNameClean)).Contains(Mod)))
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(Tooltip))
			{
				var lines = i.GetTooltipLines();

				// Needed to be usable in a lambda.
				var tooltip = Tooltip;
				if (lines == null || !lines.Any(l => NormalizeForSearch(l).Contains(tooltip)))
				{
					return false;
				}
			}

			return true;
		}

		// Used to remove whitespace
		private static readonly Regex WhitespaceRegex = new(@"\s+");
		private string RemoveWhitespace(string s)
		{
			return WhitespaceRegex.Replace(s, "");
		}
	}

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
	private Predicate<T>? _activeFilter = null;

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
		_filterPanel.OnSelectionChanged += pred => {
			_filterToggleButton.OptionSelected = pred != null;
			_activeFilter = pred;
			UpdateDisplayedIngredients();
		};

		_sortPanel.Width.Percent = 1;
		_sortPanel.Height.Percent = 1;
		_sortPanel.OnSelectionChanged += comp => {
			_sortToggleButton.OptionSelected = comp != null;
			_activeSortComparison = comp;
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

	public void AddFilter(Item icon, string name, Predicate<T> pred)
	{
		_filterPanel.AddItemIconOption(icon, name, pred);
	}

	public void AddSort(Item icon, string name, Comparison<T> compare)
	{
		_sortPanel.AddItemIconOption(icon, name, compare);
	}

	// Update what ingredients are being displayed based on the search bar and filters.
	private void UpdateDisplayedIngredients()
	{
		var query = ParseSearchText(_searchText ?? "");

		_filteredIngredients.Clear();
		_filteredIngredients.AddRange(
			_allIngredients.Where(i => query.Matches(i) && (_activeFilter?.Invoke(i) ?? true)));

		if (_activeSortComparison != null)
		{
			_filteredIngredients.Sort(_activeSortComparison);
		}

		_ingredientList.Values = _filteredIngredients;
	}

	/*
	 * Put text in a standard form so that searching can be effectively done using
	 * `string.Contains`.
	 */
	private static string NormalizeForSearch(string s)
	{
		return s.Trim().ToLower();
	}

	private static SearchQuery ParseSearchText(string text)
	{
		var query = new SearchQuery();
		var parts = text.Split("#", 2);

		if (parts.Length >= 2)
		{
			query.Tooltip = NormalizeForSearch(parts[1]);
		}

		/*
		 * TODO: These could be made more efficient by not re-compiling the regexes every time, but
		 * it's probably fine since this only happens once when the search is changed.
		 *
		 * This will just use the first specified mod and ignore the rest.
		 */
		var modCaptures = Regex.Matches(parts[0], @"@(\S+)");
		if (modCaptures.Count >= 1)
		{
			query.Mod = NormalizeForSearch(modCaptures[0].Groups[1].Value);
		}

		/*
		 * We remove *any* @ from the name, even if it doesn't have any characters after it that
		 * might be part of a mod.
		 */
		query.Name = NormalizeForSearch(Regex.Replace(parts[0], @"@\S*\s*", ""));

		return query;
	}
}
