using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class UIQERState : UIState
{
	// A tab that may or may not be displayed.
	private class RecipeTab
	{
		public IRecipeHandler Handler;
		public UIList RecipeList = new(){
			Width = new(0, 1),
			Height = new(0, 1),
			ListPadding = 30
		};

		// Each tab has its own associated scrollbar.
		public UIScrollbar Scrollbar = new(){
			Height = new(0, 1),
			HAlign = 0.5f
		};

		public RecipeTab(IRecipeHandler handler)
		{
			Handler = handler;
			RecipeList.SetScrollbar(Scrollbar);
		}
	}

	// Enough to reconstruct (enough of) the recipe panel state.
	private struct HistoryEntry
	{
		// The tabs that were passed to `TryShowRelevantTabs`.
		public List<RecipeTab> Tabs;
		public Item ClickedItem;
		public int TabIndex;
		public float ScrollViewPosition;
	}

	private class OptionPanelToggleButton : UIElement
	{
		private string _iconPath;
		private string _name;

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

			if (IsMouseHovering)
			{
				Main.instance.MouseText(_name);
			}
		}
	}

	private const float BarHeight = 50;
	private const float ScrollBarWidth = 30;

	// Keeps track of the recipe pages that have been viewed, not including the current one.
	private Stack<HistoryEntry> _history = new();

	private List<Item> _allItems;
	private List<Item> _filteredItems;

	// Contains references to tabs in either _sourceTabs or _usageTabs.
	private List<RecipeTab> _activeTabs = new();
	private List<RecipeTab> _sourceTabs = new();
	private List<RecipeTab> _usageTabs = new();

	private UIItemList _itemList;

	/*
	 * This refers either to `_sourceTabs` or `_usageTabs`, and is used to keep track of tab
	 * history.
	 */
	private List<RecipeTab>? _currentTabSet;
	private Item? _clickedItem;

	// Index of currently active tab in `_activeTabs`.
	private int _tabIndex = 0;

	private UITabBar _tabBar = new();

	// Contains, as a child, the current recipe list tab being viewed.
	private UIElement _recipeListContainer = new();

	// Contains the recipe list scrollbar as a child.
	private UIElement _recipeScrollContainer = new();

	// Panel with the item list. This is needed so the filter panel can be added and removed.
	private UIPanel _itemListPanel;

	/*
	 * When an item panel is being hovered, this keeps track of it. This is needed so that we can
	 * have the panel do tooltip modifications.
	 */
	private UIItemPanel? _hoveredItemPanel = null;

	// Gives us access to the active search bar so it can be canceled.
	private UIQERSearchBar? _activeSearchBar = null;
	private string? _searchText = null;

	// This will contain at most one of the options panels (sort, filter).
	private UIElement _optionPanelContainer = new();

	private UIOptionPanel<Predicate<Item>> _filterPanel = new();
	private Predicate<Item>? _activeFilter = null;

	private UIOptionPanel<Comparison<Item>> _sortPanel = new();
	private Comparison<Item>? _activeSortComparison = null;

	public override void OnInitialize()
	{
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

		AddSourceHandler(new RecipeHandlers.BasicSourceHandler());
		AddSourceHandler(new RecipeHandlers.ShimmerSourceHandler());
		AddSourceHandler(new RecipeHandlers.NPCShopSourceHandler());
		AddSourceHandler(new RecipeHandlers.ItemDropSourceHandler());
		AddSourceHandler(new RecipeHandlers.NPCDropSourceHandler());
		AddSourceHandler(new RecipeHandlers.GlobalLootSourceHandler());

		AddUsageHandler(new RecipeHandlers.BasicUsageHandler());
		AddUsageHandler(new RecipeHandlers.TileUsageHandler());
		AddUsageHandler(new RecipeHandlers.ShimmerUsageHandler());
		AddUsageHandler(new RecipeHandlers.ItemDropUsageHandler());

		AddInternalFilter(ItemID.Wood, "Blocks", ItemPredicates.IsBlock);
		AddInternalFilter(ItemID.SuspiciousLookingEye, "BossSummons", ItemPredicates.IsBossSummon);

		AddInternalFilter(ItemID.CopperShortsword, "MeleeWeapons", ItemPredicates.IsMeleeWeapon);
		AddInternalFilter(ItemID.WoodenBow, "RangedWeapons", ItemPredicates.IsRangedWeapon);
		AddInternalFilter(ItemID.WandofSparking, "MagicWeapons", ItemPredicates.IsMagicWeapon);
		AddInternalFilter(ItemID.BabyBirdStaff, "SummonWeapons", ItemPredicates.IsSummonWeapon);

		var moddedDamageClasses =
			Enumerable.Range(0, DamageClassLoader.DamageClassCount)
			.Select(i => DamageClassLoader.GetDamageClass(i))
			.Where(c => !(c is VanillaDamageClass))
			.ToList();

		foreach (var dc in moddedDamageClasses)
		{
			/*
			 * We only want modded damage classes that are "pure" in that they don't inherit other
			 * modded damage classes. For example, Calamity has a "true melee no speed" class that
			 * we don't want to also appear.
			 */
			if (moddedDamageClasses.Any(c => dc.GetEffectInheritance(c)))
			{
				continue;
			}

			var icon = FindIconItemForDamageClass(dc) ?? new Item(ItemID.Zenith);

			// This damage class has no items, so we can't really make a filter for it.
			if (icon == null) { continue; }

			// Adjust the name so instead of "rogue damage Weapons", we get "Rogue Weapons".
			var name = Language.GetText("Mods.QuiteEnoughRecipes.Filters.OtherWeapons")
				.Format(BaseDamageClassName(dc.DisplayName.Value));
			AddFilter(icon, $"{BaseDamageClassName(dc.DisplayName.Value)} Weapons",
				i => i.CountsAsClass(dc) && !ItemPredicates.IsTool(i));
		}

		/*
		 * This should put the panel just to the left of the item list panel, which will keep it out
		 * of the way for the most part. Since it's not inside the bounds of the item list panel, it
		 * has to be appended directly to the main element.
		 */
		_optionPanelContainer.Width.Percent = 0.25f;
		_optionPanelContainer.Height.Percent = 0.25f;
		_optionPanelContainer.Top.Percent = 0.1f;
		_optionPanelContainer.Left.Percent = 0.26f;

		/*
		 * If we don't do this, then `_optionPanelContainer` will absorb any mouse interactions even
		 * if there's no active option panel, which creates a sort of "dead zone" in the recipe
		 * panel. Note that this has to get set to false whenever an option panel is opened, so that
		 * the active option panel can actually be used.
		 */
		_optionPanelContainer.IgnoresMouseInteraction = true;

		_filterPanel.Width.Percent = 1;
		_filterPanel.Height.Percent = 1;
		_filterPanel.OnSelectionChanged += pred => {
			_activeFilter = pred;
			UpdateDisplayedItems();
		};

		_sortPanel.Width.Percent = 1;
		_sortPanel.Height.Percent = 1;
		_sortPanel.OnSelectionChanged += comp => {
			_activeSortComparison = comp;
			UpdateDisplayedItems();
		};

		_sortPanel.AddItemIconOption(new Item(ItemID.AlphabetStatue1),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.ID"),
			(x, y) => x.type.CompareTo(y.type));
		_sortPanel.AddItemIconOption(new Item(ItemID.AlphabetStatueA),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.Alphabetical"),
			(x, y) => x.Name.CompareTo(y.Name));
		_sortPanel.AddItemIconOption(new Item(ItemID.StarStatue),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.Rarity"),
			(x, y) => x.rare.CompareTo(y.rare));
		_sortPanel.AddItemIconOption(new Item(ItemID.ChestStatue),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.Value"),
			(x, y) => x.value.CompareTo(y.value));

		InitRecipePanel();
		InitItemPanel();
		Append(_optionPanelContainer);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		// TODO: Is this actually the right place to handle input?
		if (UISystem.BackKey.JustPressed && _activeSearchBar == null)
		{
			TryPopHistory();
		}
	}

	public void AddSourceHandler(IRecipeHandler handler) => _sourceTabs.Add(new(handler));
	public void AddUsageHandler(IRecipeHandler handler) => _usageTabs.Add(new(handler));

	public void ShowSources(Item i) => TryPushPage(_sourceTabs, i);
	public void ShowUses(Item i) => TryPushPage(_usageTabs, i);

	// This must be called before the filter panel is initialized.
	public void AddFilter(Item icon, string hoverName, Predicate<Item> pred)
	{
		_filterPanel.AddItemIconOption(icon, hoverName, pred);
	}

	// If it exists, load the top of the history stack and pop it.
	public void TryPopHistory()
	{
		if (_history.Count == 0) { return; }

		/*
		 * It would be weird if the page showed something the first time, but didn't have anything
		 * to show this time, so we'll just assume it will always work.
		 */
		var top = _history.Pop();
		TryShowRelevantTabs(top.Tabs, top.ClickedItem);
		SwitchToTab(top.TabIndex);
		_activeTabs[_tabIndex].Scrollbar.ViewPosition = top.ScrollViewPosition;
	}

	public override void LeftClick(UIMouseEvent e)
	{
		if (!(e.Target is UIQERSearchBar))
		{
			StopTakingInput();
		}

		/*
		 * Don't close the filter panel while we're in the middle of opening it, but close it with
		 * any other click.
		 */
		if (!(e.Target is OptionPanelToggleButton) && !_optionPanelContainer.IsMouseHovering)
		{
			CloseOptionPanel();
		}

		if (e.Target is UIItemPanel p && p.DisplayedItem != null)
		{
			ShowSources(p.DisplayedItem);
		}
	}

	public override void RightClick(UIMouseEvent e)
	{
		if (!(e.Target is UIQERSearchBar))
		{
			StopTakingInput();
		}

		if (!(e.Target is OptionPanelToggleButton) && !_optionPanelContainer.IsMouseHovering)
		{
			CloseOptionPanel();
		}

		if (e.Target is UIItemPanel p && p.DisplayedItem != null)
		{
			ShowUses(p.DisplayedItem);
		}
	}

	public override void MouseOver(UIMouseEvent e)
	{
		if (e.Target is UIItemPanel p) { _hoveredItemPanel = p; }
	}

	public override void MouseOut(UIMouseEvent e)
	{
		if (e.Target == _hoveredItemPanel) { _hoveredItemPanel = null; }
	}

	public void ModifyTooltips(Mod mod, List<TooltipLine> tooltips)
	{
		_hoveredItemPanel?.ModifyTooltips(mod, tooltips);
	}

	/*
	 * Try to show the subset of tabs in `tabs` that apply to the item `item`. If no tabs have any
	 * elements for the given item, the view is not changed.
	 */
	private bool TryShowRelevantTabs(List<RecipeTab> tabs, Item item)
	{
		// List of lists of things to display for each tab.
		var displayLists = tabs.Select(t => t.Handler.GetRecipeDisplays(item).ToList()).ToList();

		// No tab has anything to display; don't do anything else.
		if (displayLists.All(l => l.Count == 0)) { return false; }

		_currentTabSet = tabs;
		_clickedItem = item;

		// We only want to update and show tabs that are relevant (i.e., they have content).
		_activeTabs.Clear();
		for (int i = 0; i < tabs.Count; ++i)
		{
			if (displayLists[i].Count > 0)
			{
				tabs[i].RecipeList.Clear();
				tabs[i].RecipeList.AddRange(displayLists[i]);
				_activeTabs.Add(tabs[i]);
			}
		}

		_tabBar.ClearTabs();
		foreach (var tab in _activeTabs)
		{
			_tabBar.AddTab(tab.Handler.HoverName, tab.Handler.TabItem);
		}

		_tabBar.Activate();
		SwitchToTab(0);

		return true;
	}

	/*
	 * Try to switch the page. If there was something to show, push a history entry on the top of
	 * the history stack. If successful, the page will show results for item `item` with tabs taken
	 * from `tabs`. If the item and tab set are the same as the currently active one, then the page
	 * layout will be reset, but a new history entry will not be added.
	 */
	private void TryPushPage(List<RecipeTab> tabs, Item item)
	{
		/*
		 * If there is no tab set active, then we are still on the blank page, so there's no history
		 * item we can actually push on the stack.
		 */
		if (_currentTabSet == null || _currentTabSet == tabs && _clickedItem.type == item.type)
		{
			TryShowRelevantTabs(tabs, item);
			return;
		}

		var historyEntry = new HistoryEntry{
			Tabs = _currentTabSet,
			ClickedItem = _clickedItem,
			TabIndex = _tabIndex,
			ScrollViewPosition = _activeTabs[_tabIndex].Scrollbar.ViewPosition
		};

		if (TryShowRelevantTabs(tabs, item))
		{
			_history.Push(historyEntry);
		}
	}

	/*
	 * Display the content associated with tab `i`. This does not actually switch the tab in the tab
	 * bar.
	 */
	private void ShowTabContent(int i)
	{
		if (i < 0 || i >= _activeTabs.Count) { return; }

		_tabIndex = i;

		_recipeListContainer.RemoveAllChildren();
		_recipeListContainer.Append(_activeTabs[i].RecipeList);

		_recipeScrollContainer.RemoveAllChildren();
		_recipeScrollContainer.Append(_activeTabs[i].Scrollbar);

		/*
		 * Just try to activate these every time since they won't be activated on the initial
		 * initialization of the UI state.
		 */
		_activeTabs[i].RecipeList.Activate();
		_activeTabs[i].Scrollbar.Activate();

		Recalculate();
	}

	// Change the active tab to `i`.
	private void SwitchToTab(int i) => _tabBar.SwitchToTab(i);

	// If a search bar is focused, stop taking input from it.
	private void StopTakingInput()
	{
		_activeSearchBar?.SetTakingInput(false);
		_activeSearchBar = null;
	}

	// The left panel that displays recipes.
	private void InitRecipePanel()
	{
		var recipePanel = new UIPanel();
		recipePanel.Left.Percent = 0.04f;
		recipePanel.Width.Percent = 0.45f;
		recipePanel.Height.Percent = 0.8f;
		recipePanel.VAlign = 0.5f;

		_recipeScrollContainer.Height.Percent = 1;
		_recipeScrollContainer.Width.Pixels = ScrollBarWidth;
		_recipeScrollContainer.HAlign = 1;

		_recipeListContainer.Width = new StyleDimension(-ScrollBarWidth, 1);
		_recipeListContainer.Height.Percent = 1;

		const float TabHeight = 50;

		_tabBar.Width = new StyleDimension(-10, 0.45f);
		_tabBar.Height.Pixels = TabHeight;
		_tabBar.Left = new StyleDimension(5, 0.04f);
		_tabBar.Top = new StyleDimension(-TabHeight, 0.1f);

		_tabBar.OnTabSelected += ShowTabContent;

		recipePanel.Append(_recipeListContainer);
		recipePanel.Append(_recipeScrollContainer);

		Append(recipePanel);
		Append(_tabBar);
	}

	private void InitItemPanel()
	{
		_itemListPanel = new UIPanel();
		_itemListPanel.Left.Percent = 0.51f;
		_itemListPanel.Width.Percent = 0.45f;
		_itemListPanel.Height.Percent = 0.8f;
		_itemListPanel.VAlign = 0.5f;

		var scroll = new UIScrollbar();
		scroll.Height.Percent = 1;
		scroll.Width.Percent = 1;
		scroll.HAlign = 0.5f;

		var scrollContainer = new UIElement();
		scrollContainer.Width.Pixels = ScrollBarWidth;
		scrollContainer.Height = new StyleDimension(-BarHeight, 1);
		scrollContainer.HAlign = 1;
		scrollContainer.VAlign = 1;
		scrollContainer.Append(scroll);

		_itemList = new();
		_itemList.Scrollbar = scroll;
		_itemList.Items = _filteredItems;
		_itemList.Width = new StyleDimension(-ScrollBarWidth, 1);
		_itemList.Height = new StyleDimension(-BarHeight, 1);
		_itemList.VAlign = 1;

		var filterToggleButton = new OptionPanelToggleButton("Images/UI/Bestiary/Button_Filtering",
			Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.FilterHover"));
		filterToggleButton.OnLeftClick += (b, e) => ToggleOptionPanel(_filterPanel);

		float offset = filterToggleButton.Width.Pixels + 10;

		var sortToggleButton = new OptionPanelToggleButton("Images/UI/Bestiary/Button_Sorting",
			Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.SortHover"));
		sortToggleButton.Left.Pixels = offset;
		sortToggleButton.OnLeftClick += (b, e) => ToggleOptionPanel(_sortPanel);

		offset += sortToggleButton.Width.Pixels + 10;

		var search = new UIQERSearchBar();
		search.Width = new StyleDimension(-offset, 1);
		search.HAlign = 1;
		search.OnStartTakingInput += () => {
			_activeSearchBar = search;
		};
		search.OnEndTakingInput += () => {
			_activeSearchBar = null;
		};
		search.OnContentsChanged += s => {
			_searchText = s;
			UpdateDisplayedItems();
		};

		_itemListPanel.Append(_itemList);
		_itemListPanel.Append(scrollContainer);
		_itemListPanel.Append(filterToggleButton);
		_itemListPanel.Append(sortToggleButton);
		_itemListPanel.Append(search);

		Append(_itemListPanel);
	}

	// Update what items are being displayed based on the search bar and filters.
	private void UpdateDisplayedItems()
	{
		var sNorm = _searchText?.ToLower() ?? "";
		_filteredItems.Clear();
		_filteredItems.AddRange(
			_allItems.Where(i =>
				i.Name.ToLower().Contains(sNorm) && (_activeFilter?.Invoke(i) ?? true)));

		if (_activeSortComparison != null)
		{
			_filteredItems.Sort(_activeSortComparison);
		}

		_itemList.Items = _filteredItems;
	}

	// Add a filter using a localization key.
	private void AddInternalFilter(int itemID, string key, Predicate<Item> pred)
	{
		AddFilter(new Item(itemID), Language.GetTextValue($"Mods.QuiteEnoughRecipes.Filters.{key}"),
			pred);
	}

	// If option panel `panel` is already active, close it. If not, switch to it.
	private void ToggleOptionPanel(UIElement panel)
	{
		if (_optionPanelContainer.HasChild(panel))
		{
			CloseOptionPanel();
		}
		else
		{
			OpenOptionPanel(panel);
		}
	}

	private void OpenOptionPanel(UIElement e)
	{
		_optionPanelContainer.RemoveAllChildren();
		_optionPanelContainer.Append(e);
		e.Activate();
		e.Recalculate();
		_optionPanelContainer.IgnoresMouseInteraction = false;
	}

	private void CloseOptionPanel()
	{
		_optionPanelContainer.RemoveAllChildren();
		_optionPanelContainer.IgnoresMouseInteraction = true;
	}

	// Tries to find a low-rarity item to use as an icon for a damage class filter.
	private static Item? FindIconItemForDamageClass(DamageClass d)
	{
		return Enumerable.Range(0, ItemLoader.ItemCount)
			.Select(i => new Item(i))
			.Where(i => i.CountsAsClass(d) && !ItemPredicates.IsTool(i))
			.MinBy(i => i.rare);
	}

	/*
	 * Tries to turn something like "rogue damage" into "Rogue", i.e., a single capitalized word.
	 *
	 * TODO: Try to figure out how to make this work with all languages. I currently assume that the
	 * display name will end with the word "damage", but this clearly doesn't work for other
	 * languages. One possibility is to just cut the last word off, regardless of what it is. This
	 * doesn't work for languages where the "damage" equivalent is at the start of the name, though.
	 * Alternatively, maybe I could just determine the common suffix and prefix text from the
	 * vanilla display names for the current localization and strip it.
	 */
	private static string BaseDamageClassName(string name)
	{
		var ti = new CultureInfo("en-US", false).TextInfo;
		return ti.ToTitleCase(
			Regex.Replace(name, @"^\s*(.*?)\s*damage\s*$", @"$1", RegexOptions.IgnoreCase));
	}
}
