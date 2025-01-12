using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
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
			HAlign = 1
		};

		public RecipeTab(IRecipeHandler handler)
		{
			Handler = handler;
			RecipeList.SetScrollbar(Scrollbar);
		}
	}

	private List<Item> _allItems;
	private List<Item> _filteredItems;

	// Contains references to tabs in either _sourceTabs or _usageTabs.
	private List<RecipeTab> _activeTabs = new();
	private List<RecipeTab> _sourceTabs = new();
	private List<RecipeTab> _usageTabs = new();

	private UITabBar _tabBar = new();

	// Contains, as a child, the current recipe list tab being viewed.
	private UIElement _recipeListContainer = new();

	// Contains the recipe list scrollbar as a child.
	private UIElement _recipeScrollContainer = new();

	/*
	 * When an item panel is being hovered, this keeps track of it. This is needed so that we can
	 * have the panel do tooltip modifications.
	 */
	private UIItemPanel? _hoveredItemPanel = null;

	public override void OnInitialize()
	{
		_allItems = Enumerable.Range(0, ItemLoader.ItemCount)
			.Select(i => new Item(i))
			.Where(i => i.type != 0)
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

		var recipePanel = new UIPanel();
		recipePanel.Left.Percent = 0.04f;
		recipePanel.Width.Percent = 0.45f;
		recipePanel.Height.Percent = 0.8f;
		recipePanel.VAlign = 0.5f;

		const float BarHeight = 40;
		const float ScrollBarWidth = 30;

		var itemPanel = new UIPanel();
		itemPanel.Left.Percent = 0.51f;
		itemPanel.Width.Percent = 0.45f;
		itemPanel.Height.Percent = 0.8f;
		itemPanel.VAlign = 0.5f;

		var scroll = new UIScrollbar();
		scroll.Height = new StyleDimension(-BarHeight, 1);
		scroll.Width.Pixels = ScrollBarWidth;
		scroll.HAlign = 1;
		scroll.VAlign = 1;

		var list = new UIItemList();
		list.Scrollbar = scroll;
		list.Items = _filteredItems;
		list.Width = new StyleDimension(-ScrollBarWidth, 1);
		list.Height = new StyleDimension(-BarHeight, 1);
		list.VAlign = 1;

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

		_tabBar.OnTabSelected += ShowTab;

		var search = new UISearchBar(Language.GetText(""), 1);
		search.Width.Percent = 1;
		search.Height.Pixels = BarHeight;

		search.OnLeftClick += (evt, elem) => {
			if (elem is UISearchBar s)
			{
				s.ToggleTakingText();
			}
		};
		search.OnRightClick += (evt, elem) => {
			if (elem is UISearchBar s)
			{
				s.SetContents("");
				if (!s.IsWritingText)
				{
					s.ToggleTakingText();
				}
			}
		};
		search.OnContentsChanged += s => {
			var sNorm = s.ToLower();
			_filteredItems.Clear();
			_filteredItems.AddRange(_allItems.Where(i => i.Name.ToLower().Contains(sNorm)));
			list.Items = _filteredItems;
		};

		recipePanel.Append(_recipeListContainer);
		recipePanel.Append(_recipeScrollContainer);

		Append(_tabBar);
		itemPanel.Append(list);
		itemPanel.Append(scroll);
		itemPanel.Append(search);

		Append(recipePanel);
		Append(itemPanel);
	}

	public void AddSourceHandler(IRecipeHandler handler) => _sourceTabs.Add(new(handler));
	public void AddUsageHandler(IRecipeHandler handler) => _usageTabs.Add(new(handler));

	public void ShowSources(Item i) => TryShowRelevantTabs(_sourceTabs, i);
	public void ShowUses(Item i) => TryShowRelevantTabs(_usageTabs, i);

	public override void LeftClick(UIMouseEvent e)
	{
		if (e.Target is UIItemPanel p && p.DisplayedItem != null)
		{
			ShowSources(p.DisplayedItem);
		}
	}

	public override void RightClick(UIMouseEvent e)
	{
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
	private void TryShowRelevantTabs(List<RecipeTab> tabs, Item item)
	{
		// List of lists of things to display for each tab.
		var displayLists = tabs.Select(t => t.Handler.GetRecipeDisplays(item).ToList()).ToList();

		// No tab has anything to display; don't do anything else.
		if (displayLists.All(l => l.Count == 0)) { return; }

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

		ShowTab(0);
	}

	// Try to display the active tab with index `i`, if it exists.
	private void ShowTab(int i)
	{
		if (i < 0 || i >= _activeTabs.Count) { return; }

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
}
