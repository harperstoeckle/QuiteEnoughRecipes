using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.UI;
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

	// Enough to reconstruct (enough of) the recipe panel state.
	private struct HistoryEntry
	{
		// The tabs that were passed to `TryShowRelevantTabs`.
		public List<RecipeTab> Tabs;
		public Item ClickedItem;
		public int TabIndex;
		public float ScrollViewPosition;
	}

	private const float ScrollBarWidth = 30;

	/*
	 * The maximum amount of history entries that will actively be stored. Once the history stack
	 * reaches this size, old history entries will be removed from the bottom of the stack to make
	 * room for new ones.
	 *
	 * TODO: This should probably be a config option.
	 */
	private const int MaxHistorySize = 100;

	// Keeps track of the recipe pages that have been viewed, not including the current one.
	private List<HistoryEntry> _history = new();

	// Contains references to tabs in either _sourceTabs or _usageTabs.
	private List<RecipeTab> _activeTabs = new();
	private List<RecipeTab> _sourceTabs = new();
	private List<RecipeTab> _usageTabs = new();

	private UIItemList _itemList = new();

	/*
	 * This refers either to `_sourceTabs` or `_usageTabs`, and is used to keep track of tab
	 * history.
	 */
	private List<RecipeTab>? _currentTabSet = null;
	private Item? _clickedItem = null;

	// Index of currently active tab in `_activeTabs`.
	private int _tabIndex = 0;

	private UITabBar _tabBar = new();

	// Contains, as a child, the current recipe list tab being viewed.
	private UIElement _recipeListContainer = new();

	// Contains the recipe list scrollbar as a child.
	private UIElement _recipeScrollContainer = new();

	// Panel with the item list. This is needed so the filter panel can be added and removed.
	private UIPanel _itemListPanel = new();

	private UIIngredientSearchPage _itemSearchPage;

	/*
	 * When an item panel is being hovered, this keeps track of it. This is needed so that we can
	 * have the panel do tooltip modifications.
	 */
	private UIItemPanel? _hoveredItemPanel = null;

	// This will contain at most one of the options panels (sort, filter).
	private UIPopupContainer _optionPanelContainer = new();

	public UIQERState()
	{
		_itemSearchPage = new(_optionPanelContainer);

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

		AddInternalFilter(ItemID.StoneBlock, "Tiles", ItemPredicates.IsTile);
		AddInternalFilter(ItemID.Furnace, "CraftingStations", ItemPredicates.IsCraftingStation);
		AddInternalFilter(ItemID.SuspiciousLookingEye, "BossSummons", ItemPredicates.IsBossSummon);
		AddInternalFilter(ItemID.GoodieBag, "LootItems", ItemPredicates.IsLootItem);
		AddInternalFilter(ItemID.InfernoPotion, "Potions", ItemPredicates.IsPotion);
		AddInternalFilter(ItemID.Apple, "Food", ItemPredicates.IsFood);
		AddInternalFilter(ItemID.WoodFishingPole, "Fishing", ItemPredicates.IsFishing);
		AddInternalFilter(ItemID.RedDye, "Dye", ItemPredicates.IsDye);
		AddInternalFilter(ItemID.AnkletoftheWind, "Accessories", ItemPredicates.IsAccessory);
		AddInternalFilter(ItemID.ExoticEasternChewToy, "Pets", ItemPredicates.IsPet);
		AddInternalFilter(ItemID.SlimySaddle, "Mounts", ItemPredicates.IsMount);
		AddInternalFilter(ItemID.CreativeWings, "Wings", ItemPredicates.IsWings);
		AddInternalFilter(ItemID.GrapplingHook, "Hooks", ItemPredicates.IsHook);
		AddInternalFilter(ItemID.CopperPickaxe, "Tools", ItemPredicates.IsTool);
		AddInternalFilter(ItemID.CopperChainmail, "Armor", ItemPredicates.IsArmor);
		AddInternalFilter(ItemID.RedHat, "Vanity", ItemPredicates.IsVanity);

		AddInternalFilter(ItemID.CopperShortsword, "MeleeWeapons", ItemPredicates.IsMeleeWeapon);
		AddInternalFilter(ItemID.WoodenBow, "RangedWeapons", ItemPredicates.IsRangedWeapon);
		AddInternalFilter(ItemID.WandofSparking, "MagicWeapons", ItemPredicates.IsMagicWeapon);
		AddInternalFilter(ItemID.BabyBirdStaff, "SummonWeapons", ItemPredicates.IsSummonWeapon);

		AddInternalFilter(ItemID.FlareGun, "ClasslessWeapons", ItemPredicates.IsClasslessWeapon);

		// We only add the thrower class filter if there are mods that add throwing weapons.
		if (FindIconItemForDamageClass(DamageClass.Throwing) is Item iconItem)
		{
			/*
			 * Unlike the other modded damage classes, we only want to show throwing weapons that
			 * are *exactly* in the throwing class, since modded classes that just *derive* from
			 * throwing (like rogue) will also be shown below with the other modded classes.
			 */
			AddInternalFilter(iconItem.type, "ThrowingWeapons",
				i => i.DamageType == DamageClass.Throwing && ItemPredicates.IsWeapon(i));
		}

		/*
		 * Modded damage classes, grouped by name. An item will be considered to fit into a group if
		 * it counts as any of the damage classes in that group. Note that if two mods add damage
		 * classes with the same name, they will be grouped together even if they're completely
		 * unrelated.
		 */
		var moddedDamageClassSets =
			Enumerable.Range(0, DamageClassLoader.DamageClassCount)
			.Select(i => DamageClassLoader.GetDamageClass(i))
			.GroupBy(c => BaseDamageClassName(c.DisplayName.Value))
			.Select(g => g.ToList())
			.Where(g => g.All(d => !(d is VanillaDamageClass)))
			.OrderBy(g => g[0].Mod?.Name ?? "");

		foreach (var dcs in moddedDamageClassSets)
		{
			var icon = dcs.Select(d => FindIconItemForDamageClass(d))
				.FirstOrDefault(n => n != null, null);

			// This damage class has no items, so we can't really make a filter for it.
			if (icon == null) { continue; }

			// Adjust the name so instead of "rogue damage Weapons", we get "Rogue Weapons".
			var name = Language.GetText("Mods.QuiteEnoughRecipes.Filters.OtherWeapons")
				.Format(BaseDamageClassName(dcs[0].DisplayName.Value));
			AddFilter(icon, $"{name}",
				i => dcs.Any(dc => ItemPredicates.IsWeaponInDamageClass(i, dc)));
		}

		/*
		 * This should put the panel just to the left of the item list panel, which will keep it out
		 * of the way for the most part. Since it's not inside the bounds of the item list panel, it
		 * has to be appended directly to the main element.
		 */
		_optionPanelContainer.Width.Percent = 0.25f;
		_optionPanelContainer.Height.Percent = 0.5f;
		_optionPanelContainer.Top.Percent = 0.1f;
		_optionPanelContainer.Left.Percent = 0.26f;

		_itemSearchPage.AddSort(new Item(ItemID.AlphabetStatue1),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.ID"),
			(x, y) => x.type.CompareTo(y.type));
		_itemSearchPage.AddSort(new Item(ItemID.AlphabetStatueA),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.Alphabetical"),
			(x, y) => x.Name.CompareTo(y.Name));
		_itemSearchPage.AddSort(new Item(ItemID.StarStatue),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.Rarity"),
			(x, y) => x.rare.CompareTo(y.rare));
		_itemSearchPage.AddSort(new Item(ItemID.ChestStatue),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.Value"),
			(x, y) => x.value.CompareTo(y.value));

		InitRecipePanel();
		InitItemPanel();
		Append(_optionPanelContainer);
	}

	public override void Update(GameTime t)
	{
		base.Update(t);
		Main.LocalPlayer.mouseInterface = true;
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		// TODO: Is this actually the right place to handle input?
		if (UISystem.BackKey.JustPressed)
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
		_itemSearchPage.AddFilter(icon, hoverName, pred);
	}

	// If it exists, load the top of the history stack and pop it.
	public void TryPopHistory()
	{
		if (_history.Count == 0) { return; }

		var top = _history[_history.Count - 1];
		_history.RemoveAt(_history.Count - 1);

		/*
		 * It would be weird if the page showed something the first time, but didn't have anything
		 * to show this time, so we'll just assume it will always work.
		 */
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
			_optionPanelContainer.Close();
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
			_optionPanelContainer.Close();
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

	public void ModifyTooltips(Mod mod, Item item, List<TooltipLine> tooltips)
	{
		// Prevent weird situations where the wrong tooltip can be modified.
		if ((_hoveredItemPanel?.DisplayedItem?.type ?? 0) != item.type) { return; }

		_hoveredItemPanel.ModifyTooltips(mod, tooltips);
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
			_history.Add(historyEntry);
		}

		if (_history.Count > MaxHistorySize)
		{
			_history.RemoveRange(0, _history.Count - MaxHistorySize);
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
		_itemSearchPage.StopTakingInput();
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
		_itemListPanel.Left.Percent = 0.51f;
		_itemListPanel.Width.Percent = 0.45f;
		_itemListPanel.Height.Percent = 0.8f;
		_itemListPanel.VAlign = 0.5f;

		_itemListPanel.Append(_itemSearchPage);
		Append(_itemListPanel);
	}

	// Add a filter using a localization key.
	private void AddInternalFilter(int itemID, string key, Predicate<Item> pred)
	{
		AddFilter(new Item(itemID), Language.GetTextValue($"Mods.QuiteEnoughRecipes.Filters.{key}"),
			pred);
	}

	/*
	 * Tries to find a low-rarity item to use as an icon for a damage class filter. When applying
	 * the filter, any weapon that counts for this damage class will be shown, but for the purposes
	 * of the icon, we only want something whose damage class is *exactly* `d`, in the hope that it
	 * will be more representative of the class.
	 */
	private static Item? FindIconItemForDamageClass(DamageClass d)
	{
		return Enumerable.Range(0, ItemLoader.ItemCount)
			.Select(i => new Item(i))
			.Where(i => i.DamageType == d && ItemPredicates.IsWeapon(i))
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
