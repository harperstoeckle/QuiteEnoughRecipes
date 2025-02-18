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

// A page including a scrollable list of ingredients, a search bar, and filter options.
public class UIIngredientSearchPage : UIElement
{
	private struct SearchQuery
	{
		public string Name;
		public string? Mod;
		public string? Tooltip;

		public bool Matches(Item i)
		{
			if (Mod != null &&
				(i.ModItem == null || !NormalizeForSearch(RemoveWhitespace(i.ModItem.Mod.DisplayNameClean)).Contains(Mod)))
			{
				return false;
			}

			if (Tooltip != null)
			{
				int yoyoLogo = -1;
				int researchLine = -1;
				int numLines = 1;
				var tooltipNames = new string[30];
				var tooltipLines = new string[30];
				var prefixLines = new bool[30];
				var badPrefixLines = new bool[30];

				Main.MouseText_DrawItemTooltip_GetLinesInfo(i, ref yoyoLogo, ref researchLine, i.knockBack,
					ref numLines, tooltipLines, prefixLines, badPrefixLines, tooltipNames, out var p);

				var lines = ItemLoader.ModifyTooltips(i, ref numLines, tooltipNames, ref tooltipLines,
					ref prefixLines, ref badPrefixLines, ref yoyoLogo, out Color?[] o, p);

				/*
				 * We have to remove the item name line (which is the item name, so it's not "part
				 * of the description". We also have to remove any tooltip lines specific to this
				 * mod. Since QER tooltips depend on what slot is being hovered, this can create
				 * weird behavior where hovering over an item in the browser changes search results.
				 */
				var cleanLines = lines.Where(l => l.Name != "ItemName" && !(l.Mod is QuiteEnoughRecipes));

				// Needed since we can't capture `Tooltip`.
				var tooltip = Tooltip;
				if (!cleanLines.Any(l => NormalizeForSearch(l.Text).Contains(tooltip)))
				{
					return false;
				}
			}

			return i.Name.ToLower().Contains(Name);
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
		public HelpIcon()
		{
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
				UICommon.TooltipMouseText(
					Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.SearchHelp"));
			}
		}
	}

	private List<Item> _allItems;
	private List<Item> _filteredItems;

	private UIScrollableGrid<Item, UIItemPanel> _itemList = new();

	/*
	 * This is a reference to a popup container in the parent that will be used to display the
	 * filter and sort panels.
	 */
	private UIPopupContainer _optionPanelContainer;

	private OptionPanelToggleButton _filterToggleButton = new("Images/UI/Bestiary/Button_Filtering",
			Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.FilterHover"));
	private UIOptionPanel<Predicate<Item>> _filterPanel = new();
	private Predicate<Item>? _activeFilter = null;

	private OptionPanelToggleButton _sortToggleButton = new("Images/UI/Bestiary/Button_Sorting",
			Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.SortHover"));
	private UIOptionPanel<Comparison<Item>> _sortPanel = new();
	private Comparison<Item>? _activeSortComparison = null;

	private UIQERSearchBar _searchBar = new();
	private string? _searchText = null;

	public UIIngredientSearchPage(UIPopupContainer optionPanelContainer)
	{
		const float BarHeight = 50;
		const float ScrollBarWidth = 30;

		/*
		 * Our "master list" of items is sorted by creative order first and item ID second, so this
		 * is the order that will be used if no sort order is chosen.
		 */
		_allItems = Enumerable.Range(0, ItemLoader.ItemCount)
			.Select(i => new Item(i))
			.Where(i => i.type != 0)
			.OrderBy(i => {
				var itemGroup = ContentSamples.CreativeHelper.GetItemGroup(i, out int orderInGroup);
				return (itemGroup, orderInGroup, i.type);
			})
			.ToList();
		_filteredItems = new(_allItems);

		_optionPanelContainer = optionPanelContainer;

		Width.Percent = 1;
		Height.Percent = 1;

		_filterPanel.Width.Percent = 1;
		_filterPanel.Height.Percent = 1;
		_filterPanel.OnSelectionChanged += pred => {
			_filterToggleButton.OptionSelected = pred != null;
			_activeFilter = pred;
			UpdateDisplayedItems();
		};

		_sortPanel.Width.Percent = 1;
		_sortPanel.Height.Percent = 1;
		_sortPanel.OnSelectionChanged += comp => {
			_sortToggleButton.OptionSelected = comp != null;
			_activeSortComparison = comp;
			UpdateDisplayedItems();
		};

		var scroll = new UIScrollbar();
		scroll.Height = new StyleDimension(-BarHeight, 1);
		scroll.HAlign = 1;
		scroll.VAlign = 1;

		_itemList.Scrollbar = scroll;
		_itemList.Values = _filteredItems;
		_itemList.Width = new StyleDimension(-ScrollBarWidth, 1);
		_itemList.Height = new StyleDimension(-BarHeight, 1);
		_itemList.VAlign = 1;

		_filterToggleButton.OnLeftClick += (b, e) => _optionPanelContainer.Toggle(_filterPanel);
		_filterToggleButton.OnRightClick += (b, e) => _filterPanel.DisableAllOptions();

		float offset = _filterToggleButton.Width.Pixels + 10;

		_sortToggleButton.Left.Pixels = offset;
		_sortToggleButton.OnLeftClick += (b, e) => _optionPanelContainer.Toggle(_sortPanel);
		_sortToggleButton.OnRightClick += (b, e) => _sortPanel.DisableAllOptions();

		offset += _sortToggleButton.Width.Pixels + 10;

		var helpIcon = new HelpIcon();
		helpIcon.HAlign = 1;

		// Make room for the filters on the left and the help icon on the right.
		_searchBar.Width = new StyleDimension(-offset - helpIcon.Width.Pixels - 10, 1);
		_searchBar.Left.Pixels = offset;
		_searchBar.OnContentsChanged += s => {
			_searchText = s;
			UpdateDisplayedItems();
		};

		Append(_itemList);
		Append(scroll);
		Append(_filterToggleButton);
		Append(_sortToggleButton);
		Append(_searchBar);
		Append(helpIcon);

	}

	public void AddFilter(Item icon, string name, Predicate<Item> pred)
	{
		_filterPanel.AddItemIconOption(icon, name, pred);
	}

	public void AddSort(Item icon, string name, Comparison<Item> compare)
	{
		_sortPanel.AddItemIconOption(icon, name, compare);
	}

	// Unfocus the search bar.
	public void StopTakingInput()
	{
		_searchBar.SetTakingInput(false);
	}

	// Update what items are being displayed based on the search bar and filters.
	private void UpdateDisplayedItems()
	{
		var query = ParseSearchText(_searchText ?? "");

		_filteredItems.Clear();
		_filteredItems.AddRange(
			_allItems.Where(i => query.Matches(i) && (_activeFilter?.Invoke(i) ?? true)));

		if (_activeSortComparison != null)
		{
			_filteredItems.Sort(_activeSortComparison);
		}

		_itemList.Values = _filteredItems;
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
