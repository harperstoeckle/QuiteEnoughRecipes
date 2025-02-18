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
	private class UIRecipePage : UIElement
	{
		private IRecipeHandler _handler;
		private UIList _recipeList = new(){
			Width = new(-ScrollBarWidth, 1),
			Height = new(0, 1),
			ListPadding = 30
		};

		// Each tab has its own associated scrollbar.
		private UIScrollbar _scrollbar = new(){
			Height = new(0, 1),
			HAlign = 1
		};

		public float ScrollViewPosition
		{
			get => _scrollbar.ViewPosition;
			set => _scrollbar.ViewPosition = value;
		}

		public LocalizedText HoverName => _handler.HoverName;
		public Item TabItem => _handler.TabItem;

		public UIRecipePage(IRecipeHandler handler)
		{
			_handler = handler;
			_recipeList.SetScrollbar(_scrollbar);

			Width.Percent = 1;
			Height.Percent = 1;

			Append(_recipeList);
			Append(_scrollbar);
		}

		/*
		 * Attempts to show recipes for `ingredient`. If there are no recipes, this element remains
		 * unchanged and this function will return false.
		 */
		public bool ShowRecipes(IIngredient ingredient, QueryType queryType)
		{
			var recipeDisplays = _handler.GetRecipeDisplays(ingredient, queryType).ToList();
			if (recipeDisplays.Count == 0) { return false; }
			_recipeList.Clear();
			_recipeList.AddRange(recipeDisplays);
			return true;
		}
	}

	// Enough to reconstruct (enough of) the recipe panel state.
	private struct HistoryEntry
	{
		public QueryType QueryType;
		public IIngredient ClickedIngredient;
		public UIRecipePage RecipePage;
		public float ScrollViewPosition;
	}

	private const float ScrollBarWidth = 30;
	private const float TabHeight = 50;

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

	private List<UIRecipePage> _allTabs = new();

	private QueryType _currentQueryType = QueryType.Sources;
	private IIngredient? _clickedIngredient = null;

	// Recipe page currently being viewed.
	private UIRecipePage? _recipePage = null;

	private UITabBar<UIRecipePage> _tabBar = new();

	// Panel with the item list. This is needed so the filter panel can be added and removed.
	private UIPanel _itemListPanel = new();

	private UIIngredientSearchPage<ItemIngredient, UIItemPanel> _itemSearchPage;

	/*
	 * When an item panel is being hovered, this keeps track of it. This is needed so that we can
	 * have the panel do tooltip modifications.
	 */
	private UIItemPanel? _hoveredItemPanel = null;

	// This will contain at most one of the options panels (sort, filter).
	private UIPopupContainer _optionPanelContainer = new();

	public UIQERState()
	{
		/*
		 * Our "master list" of items is sorted by creative order first and item ID second, so this
		 * is the order that will be used if no sort order is chosen.
		 */
		var allItems = Enumerable.Range(0, ItemLoader.ItemCount)
			.Select(i => new Item(i))
			.Where(i => i.type != 0)
			.OrderBy(i => {
				var itemGroup = ContentSamples.CreativeHelper.GetItemGroup(i, out int orderInGroup);
				return (itemGroup, orderInGroup, i.type);
			})
			.Select(i => new ItemIngredient(i))
			.ToList();

		_itemSearchPage = new(_optionPanelContainer, allItems);

		AddHandler(new RecipeHandlers.Basic());
		AddHandler(new RecipeHandlers.CraftingStations());
		AddHandler(new RecipeHandlers.ShimmerTransmutations());
		AddHandler(new RecipeHandlers.NPCShops());
		AddHandler(new RecipeHandlers.ItemDrops());
		AddHandler(new RecipeHandlers.NPCDrops());
		AddHandler(new RecipeHandlers.GlobalDrops());

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

		AddSort(new Item(ItemID.AlphabetStatue1),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.ID"),
			(x, y) => x.type.CompareTo(y.type));
		AddSort(new Item(ItemID.AlphabetStatueA),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.Alphabetical"),
			(x, y) => x.Name.CompareTo(y.Name));
		AddSort(new Item(ItemID.StarStatue),
			Language.GetTextValue("Mods.QuiteEnoughRecipes.Sorts.Rarity"),
			(x, y) => x.rare.CompareTo(y.rare));
		AddSort(new Item(ItemID.ChestStatue),
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

	public void AddHandler(IRecipeHandler handler) => _allTabs.Add(new(handler));

	public void ShowSources(IIngredient i) => TryPushPage(i, QueryType.Sources);
	public void ShowUses(IIngredient i) => TryPushPage(i, QueryType.Uses);

	// This must be called before the filter panel is initialized.
	public void AddFilter(Item icon, string hoverName, Predicate<Item> pred)
	{
		// TODO: Work with `ItemIngredient` predicates directly instead of converting them.
		_itemSearchPage.AddFilter(icon, hoverName, i => pred(i.Item));
	}

	public void AddSort(Item icon, string hoverName, Comparison<Item> compare)
	{
		_itemSearchPage.AddSort(icon, hoverName, (a, b) => compare(a.Item, b.Item));
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
		TryShowRelevantTabs(top.ClickedIngredient, top.QueryType);
		_tabBar.OpenTabFor(top.RecipePage);
		top.RecipePage.ScrollViewPosition = top.ScrollViewPosition;
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

		if (e.Target is IIngredientElement s && s.Ingredient != null)
		{
			ShowSources(s.Ingredient);
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

		if (e.Target is IIngredientElement s && s.Ingredient != null)
		{
			ShowUses(s.Ingredient);
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
	 * Try to show tabs with recipes for `ingredient`. If there are no recipes, the recipe panel
	 * will not be changed, and this function will return false.
	 */
	private bool TryShowRelevantTabs(IIngredient ingredient, QueryType queryType)
	{
		var pagesForIngredient = _allTabs.Where(t => t.ShowRecipes(ingredient, queryType)).ToList();

		// No tab has anything to display; don't do anything else.
		if (pagesForIngredient.Count == 0) { return false; }

		_currentQueryType = queryType;
		_clickedIngredient = ingredient;

		_tabBar.ClearTabs();
		foreach (var page in pagesForIngredient)
		{
			_tabBar.AddTab(page.HoverName, page.TabItem, page);
		}

		_tabBar.Activate();
		_tabBar.OpenTabFor(pagesForIngredient[0]);

		return true;
	}

	/*
	 * Try to switch the page. If there was something to show, push a history entry on the top of
	 * the history stack. If successful, the page will show results for ingredient `ingredient`. If
	 * `ingredient` and `queryType` are the same as what is currently being viewed, then no new
	 * history entry will be added.
	 */
	private void TryPushPage(IIngredient ingredient, QueryType queryType)
	{
		/*
		 * If there is no active recipe page, then we're not looking at anything, so there's nothing
		 * worth storing in history.
		 */
		if (_recipePage == null ||
			_currentQueryType == queryType && _clickedIngredient.IsEquivalent(ingredient))
		{
			TryShowRelevantTabs(ingredient, queryType);
			return;
		}

		var historyEntry = new HistoryEntry{
			QueryType = _currentQueryType,
			ClickedIngredient = _clickedIngredient,
			RecipePage = _recipePage,
			ScrollViewPosition = _recipePage.ScrollViewPosition
		};

		if (TryShowRelevantTabs(ingredient, queryType))
		{
			_history.Add(historyEntry);
		}

		if (_history.Count > MaxHistorySize)
		{
			_history.RemoveRange(0, _history.Count - MaxHistorySize);
		}
	}

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

		var recipeContainer = new UIPopupContainer();
		recipeContainer.Width.Percent = 1;
		recipeContainer.Height.Percent = 1;

		_tabBar.Width = new StyleDimension(-10, 0.45f);
		_tabBar.Height.Pixels = TabHeight;
		_tabBar.Left = new StyleDimension(5, 0.04f);
		_tabBar.Top = new StyleDimension(-TabHeight, 0.1f);

		_tabBar.OnTabSelected += page => {
			recipeContainer.Open(page);
			_recipePage = page;
		};

		recipePanel.Append(recipeContainer);

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
