using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using Terraria.GameContent.Bestiary;
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
			var recipeDisplays = _handler.GetRecipes(ingredient, queryType)
				.Select(r => r.Element).ToList();
			if (recipeDisplays.Count == 0) { return false; }
			_recipeList.Clear();
			_recipeList.AddRange(recipeDisplays);
			return true;
		}
	}

	// Enough to reconstruct (enough of) the recipe panel state.
	private class HistoryEntry
	{
		public QueryType QueryType = QueryType.Sources;
		public IIngredient ClickedIngredient = new ItemIngredient(new(0));
		public UIRecipePage? RecipePage = null;
		public float ScrollViewPosition = 0;

		public HistoryEntry() {}
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

	// Keeps track of the recipe pages that have been viewed, including the current one.
	private List<HistoryEntry> _history = [new()];
	private int _historyIndex = 0;

	private List<UIRecipePage> _allTabs = new();

	private UITabBar<UIRecipePage> _recipeTabBar = new();

	/*
	 * When an item panel is being hovered, this keeps track of it. This is needed so that we can
	 * have the panel do tooltip modifications.
	 */
	private UIItemPanel? _hoveredItemPanel = null;

	// This will contain at most one of the options panels (sort, filter).
	private UIPopupContainer _optionPanelContainer = new();

	// This will be re-focused when the browser is opened.
	private IFocusableSearchPage? _pageToFocusOnOpen = null;

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

		var itemSearchPage = new UIIngredientSearchPage<ItemIngredient, UIItemPanel>(
			_optionPanelContainer, allItems,
			Language.GetText("Mods.QuiteEnoughRecipes.UI.ItemSearchHelp"));

		AddItemFilters(itemSearchPage);
		itemSearchPage.AddFilterGroup(MakeModFilterGroup(allItems));
		AddItemSorts(itemSearchPage);

		var allNPCs = Enumerable.Range(0, NPCLoader.NPCCount)
			.Where(n => Main.BestiaryDB.FindEntryByNPCID(n).Icon != null)
			.Select(n => new NPCIngredient(n))
			.OrderBy(n => ContentSamples.NpcBestiarySortingId[n.ID])
			.ToList();

		var npcSearchPage = new UIIngredientSearchPage<NPCIngredient, UINPCPanel>(
			_optionPanelContainer, allNPCs,
			Language.GetText("Mods.QuiteEnoughRecipes.UI.NPCSearchHelp"));

		AddNPCFilters(npcSearchPage);
		npcSearchPage.AddFilterGroup(MakeModFilterGroup(allNPCs));
		AddNPCSorts(npcSearchPage);

		AddHandler(new RecipeHandlers.Basic());
		AddHandler(new RecipeHandlers.CraftingStations());
		AddHandler(new RecipeHandlers.ShimmerTransmutations());
		AddHandler(new RecipeHandlers.NPCShops());
		AddHandler(new RecipeHandlers.ItemDrops());
		AddHandler(new RecipeHandlers.NPCDrops());
		AddHandler(new RecipeHandlers.GlobalDrops());

		var recipePanel = new UIPanel();
		recipePanel.Left.Percent = 0.04f;
		recipePanel.Width.Percent = 0.45f;
		recipePanel.Height.Percent = 0.8f;
		recipePanel.VAlign = 0.5f;

		var recipeContainer = new UIPopupContainer();
		recipeContainer.Width.Percent = 1;
		recipeContainer.Height.Percent = 1;

		recipePanel.Append(recipeContainer);

		_recipeTabBar.Width = new StyleDimension(-10, 0.45f);
		_recipeTabBar.Height.Pixels = TabHeight;
		_recipeTabBar.Left = new StyleDimension(5, 0.04f);
		_recipeTabBar.Top = new StyleDimension(-TabHeight, 0.1f);

		_recipeTabBar.OnTabSelected += page => {
			recipeContainer.Open(page);
			_history[_historyIndex].RecipePage = page;
		};

		/*
		 * This should put the panel just to the left of the item list panel, which will keep it out
		 * of the way for the most part. Since it's not inside the bounds of the item list panel, it
		 * has to be appended directly to the main element.
		 */
		_optionPanelContainer.Width.Percent = 0.25f;
		_optionPanelContainer.Height.Percent = 0.5f;
		_optionPanelContainer.Top.Percent = 0.1f;
		_optionPanelContainer.Left.Percent = 0.26f;

		var ingredientListPanel = new UIPanel();
		ingredientListPanel.Left.Percent = 0.51f;
		ingredientListPanel.Width.Percent = 0.45f;
		ingredientListPanel.Height.Percent = 0.8f;
		ingredientListPanel.VAlign = 0.5f;

		var ingredientListContainer = new UIPopupContainer();
		ingredientListContainer.Width.Percent = 1;
		ingredientListContainer.Height.Percent = 1;

		ingredientListPanel.Append(ingredientListContainer);

		var ingredientTabBar = new UITabBar<UIElement>();
		ingredientTabBar.Width = new StyleDimension(-10, 0.45f);
		ingredientTabBar.Height.Pixels = TabHeight;
		ingredientTabBar.Left = new StyleDimension(5, 0.51f);
		ingredientTabBar.Top = new StyleDimension(-TabHeight, 0.1f);
		ingredientTabBar.AddTab(Language.GetText("Mods.QuiteEnoughRecipes.Tabs.ItemList"),
			new Item(ItemID.IronBar), itemSearchPage);
		ingredientTabBar.AddTab(Language.GetText("Mods.QuiteEnoughRecipes.Tabs.NPCList"),
			new Item(ItemID.Bunny), npcSearchPage);

		ingredientTabBar.OnTabSelected += page => {
			UIQERSearchBar.UnfocusAll();

			if (QERConfig.Instance.AutoFocusSearchBars && page is IFocusableSearchPage s)
			{
				if (IsOpen()) { s.FocusSearchBar(); }
				_pageToFocusOnOpen = s;
			}

			_optionPanelContainer.Close();
			ingredientListContainer.Open(page);
		};

		ingredientTabBar.OpenTabFor(itemSearchPage);

		Append(ingredientListPanel);
		Append(ingredientTabBar);
		Append(recipePanel);
		Append(_recipeTabBar);
		Append(_optionPanelContainer);
	}

	public override void Update(GameTime t)
	{
		base.Update(t);
		Main.LocalPlayer.mouseInterface = true;
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		if (QERPlayer.ShouldGoForwardInHistory)
		{
			TryMoveInHistory(1);
		}
		else if (QERPlayer.ShouldGoBackInHistory)
		{
			TryMoveInHistory(-1);
		}
	}

	public bool IsOpen() => Main.InGameUI.CurrentState == this;

	/*
	 * `Open` and `Close` are the preferred ways to open and close the browser, since they handle
	 * things like auto-focusing the search bar.
	 */
	public void Open()
	{
		IngameFancyUI.OpenUIState(this);

		if (QERConfig.Instance.AutoFocusSearchBars)
		{
			_pageToFocusOnOpen?.FocusSearchBar();
		}
	}
	public void Close()
	{
		UIQERSearchBar.UnfocusAll();
		IngameFancyUI.Close();
	}

	public void AddHandler(IRecipeHandler handler) => _allTabs.Add(new(handler));

	public void ShowSources(IIngredient i) => TryPushPage(i, QueryType.Sources);
	public void ShowUses(IIngredient i) => TryPushPage(i, QueryType.Uses);

	/*
	 * Tries to load the history entry at `offset` from the current location. For example, if
	 * `offset` is -1, then it will go back to the previous entry, and if it is 1, then it will
	 * try to go forward.
	 */
	public void TryMoveInHistory(int offset)
	{
		int newIndex = _historyIndex + offset;
		if (newIndex < 0 || newIndex >= _history.Count) { return; }

		var entry = _history[newIndex];
		_historyIndex = newIndex;

		float savedScrollPos = entry.ScrollViewPosition;
		var savedPage = entry.RecipePage;

		/*
		 * It would be weird if the page showed something the first time, but didn't have anything
		 * to show this time, so we'll just assume it will always work.
		 */
		TryShowRelevantTabs(entry.ClickedIngredient, entry.QueryType);
		if (savedPage != null) { _recipeTabBar.OpenTabFor(savedPage); }
		if (entry.RecipePage != null) { entry.RecipePage.ScrollViewPosition = savedScrollPos; }
	}

	public override void LeftClick(UIMouseEvent e)
	{
		if (!(e.Target is UIQERSearchBar))
		{
			UIQERSearchBar.UnfocusAll();
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
			UIQERSearchBar.UnfocusAll();
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
		_hoveredItemPanel?.ModifyTooltips(mod, tooltips);
	}

	/*
	 * Try to show tabs with recipes for `ingredient`. If there are no recipes, the recipe panel
	 * will not be changed, and this function will return false. If this function ends up actually
	 * displaying recipes, then those changes will be reflected in `_history[_historyIndex]`.
	 */
	private bool TryShowRelevantTabs(IIngredient ingredient, QueryType queryType)
	{
		var pagesForIngredient = _allTabs.Where(t => t.ShowRecipes(ingredient, queryType)).ToList();

		// No tab has anything to display; don't do anything else.
		if (pagesForIngredient.Count == 0) { return false; }

		_history[_historyIndex].QueryType = queryType;
		_history[_historyIndex].ClickedIngredient = ingredient;

		_recipeTabBar.ClearTabs();
		foreach (var page in pagesForIngredient)
		{
			_recipeTabBar.AddTab(page.HoverName, page.TabItem, page);
		}

		_recipeTabBar.Activate();
		_recipeTabBar.OpenTabFor(pagesForIngredient[0]);

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
		var curHistory = _history[_historyIndex];

		if (curHistory.QueryType == queryType
			&& curHistory.ClickedIngredient.IsEquivalent(ingredient))
		{
			return;
		}

		/*
		 * If there's no page being displayed, then we're currently looking at a blank page, which
		 * we don't want to store as its own history entry. Instead, we keep working in the
		 * current history entry.
		 */
		if (curHistory.RecipePage == null)
		{
			TryShowRelevantTabs(ingredient, queryType);
			return;
		}

		++_historyIndex;
		bool pushExtraHistoryEntry = _historyIndex >= _history.Count;
		if (pushExtraHistoryEntry)
		{
			_history.Add(new());
		}

		if (TryShowRelevantTabs(ingredient, queryType))
		{
			_history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);

			// Maintain the same tab if it exists for the new item.
			_recipeTabBar.OpenTabFor(curHistory.RecipePage);
		}
		else if (pushExtraHistoryEntry)
		{
			// Nothing to show for this ingredient; undo.
			_history.RemoveAt(_history.Count - 1);
			--_historyIndex;
		}

		if (_history.Count > MaxHistorySize)
		{
			_history.RemoveRange(0, _history.Count - MaxHistorySize);
		}
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

	private static void AddItemFilters(UIIngredientSearchPage<ItemIngredient, UIItemPanel> page)
	{
		IEnumerable<(int, string, Predicate<ItemIngredient>)> filters = [
			(ItemID.StoneBlock, "Tiles", i => ItemPredicates.IsTile(i.Item)),
			(ItemID.Furnace, "CraftingStations", i => ItemPredicates.IsCraftingStation(i.Item)),
			(ItemID.SuspiciousLookingEye, "BossSummons", i => ItemPredicates.IsBossSummon(i.Item)),
			(ItemID.GoodieBag, "LootItems", i => ItemPredicates.IsLootItem(i.Item)),
			(ItemID.InfernoPotion, "Potions", i => ItemPredicates.IsPotion(i.Item)),
			(ItemID.Apple, "Food", i => ItemPredicates.IsFood(i.Item)),
			(ItemID.WoodFishingPole, "Fishing", i => ItemPredicates.IsFishing(i.Item)),
			(ItemID.RedDye, "Dye", i => ItemPredicates.IsDye(i.Item)),
			(ItemID.AnkletoftheWind, "Accessories", i => ItemPredicates.IsAccessory(i.Item)),
			(ItemID.ExoticEasternChewToy, "Pets", i => ItemPredicates.IsPet(i.Item)),
			(ItemID.SlimySaddle, "Mounts", i => ItemPredicates.IsMount(i.Item)),
			(ItemID.CreativeWings, "Wings", i => ItemPredicates.IsWings(i.Item)),
			(ItemID.GrapplingHook, "Hooks", i => ItemPredicates.IsHook(i.Item)),
			(ItemID.CopperPickaxe, "Tools", i => ItemPredicates.IsTool(i.Item)),
			(ItemID.CopperChainmail, "Armor", i => ItemPredicates.IsArmor(i.Item)),
			(ItemID.RedHat, "Vanity", i => ItemPredicates.IsVanity(i.Item)),
		];

		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.ItemFilters";
		page.AddFilterGroup(MakeOptionGroup(filters, keyParent));

		keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.WeaponFilters";
		IEnumerable<(int, string, Predicate<ItemIngredient>)> damageFilters = [
			(ItemID.CopperShortsword, "MeleeWeapons", i => ItemPredicates.IsMeleeWeapon(i.Item)),
			(ItemID.WoodenBow, "RangedWeapons", i => ItemPredicates.IsRangedWeapon(i.Item)),
			(ItemID.WandofSparking, "MagicWeapons", i => ItemPredicates.IsMagicWeapon(i.Item)),
			(ItemID.BabyBirdStaff, "SummonWeapons", i => ItemPredicates.IsSummonWeapon(i.Item)),
			(ItemID.FlareGun, "ClasslessWeapons", i => ItemPredicates.IsClasslessWeapon(i.Item))
		];

		var group = MakeOptionGroup(damageFilters, keyParent);

		// We only add the thrower class filter if there are mods that add throwing weapons.
		if (FindIconItemForDamageClass(DamageClass.Throwing) is Item iconItem)
		{
			/*
			 * Unlike the other modded damage classes, we only want to show throwing weapons that
			 * are *exactly* in the throwing class, since modded classes that just *derive* from
			 * throwing (like rogue) will also be shown below with the other modded classes.
			 */
			group.Options.Add(
				new(new UIItemIcon(iconItem, false), Language.GetText($"{keyParent}.ThrowingWeapons"),
					i => i.Item.DamageType == DamageClass.Throwing && ItemPredicates.IsWeapon(i.Item)));
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
				.FirstOrDefault(n => n != null);

			// This damage class has no items, so we can't really make a filter for it.
			if (icon == null) { continue; }

			// Adjust the name so instead of "rogue damage Weapons", we get "Rogue Weapons".
			var name = Language.GetText($"{keyParent}.OtherWeapons")
				.WithFormatArgs(BaseDamageClassName(dcs[0].DisplayName.Value));
			group.Options.Add(
				new(new UIItemIcon(icon, false), name,
					i => dcs.Any(dc => ItemPredicates.IsWeaponInDamageClass(i.Item, dc))));
		}

		page.AddFilterGroup(group);
	}

	private static void AddItemSorts(UIIngredientSearchPage<ItemIngredient, UIItemPanel> page)
	{
		IEnumerable<(int, string, Comparison<ItemIngredient>)> sorts = [
			(ItemID.AlphabetStatue1, "ID", (x, y) => x.Item.type.CompareTo(y.Item.type)),
			(ItemID.AlphabetStatueA, "Alphabetical", (x, y) => x.Name.CompareTo(y.Name)),
			(ItemID.StarStatue, "Rarity", (x, y) => x.Item.rare.CompareTo(y.Item.rare)),
			(ItemID.ChestStatue, "Value", (x, y) => x.Item.value.CompareTo(y.Item.value))
		];

		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.ItemSorts";
		page.AddSortGroup(MakeOptionGroup(sorts, keyParent));
	}

	private static void AddNPCFilters(UIIngredientSearchPage<NPCIngredient, UINPCPanel> page)
	{
		IEnumerable<(int, string, Predicate<NPCIngredient>)> filters = [
			(ItemID.SlimeCrown, "Bosses",
				n => TryGetBestiaryInfoElement<BossBestiaryInfoElement>(n.ID) != null),
			(ItemID.GoldCoin, "TownNPCs", n => TryGetNPC(n.ID)?.isLikeATownNPC ?? false)
		];

		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.NPCFilters";
		page.AddFilterGroup(MakeOptionGroup(filters, keyParent));
	}

	private static void AddNPCSorts(UIIngredientSearchPage<NPCIngredient, UINPCPanel> page)
	{
		IEnumerable<(int, string, Comparison<NPCIngredient>)> sorts = [
			(ItemID.AlphabetStatue1, "ID", (x, y) => x.ID.CompareTo(y.ID)),
			(ItemID.AlphabetStatueA, "Alphabetical", (x, y) => x.Name.CompareTo(y.Name)),
			(ItemID.StarStatue, "Rarity", (x, y) => {
				int xRare = TryGetNPC(x.ID)?.rarity ?? 0;
				int yRare = TryGetNPC(y.ID)?.rarity ?? 0;
				return xRare.CompareTo(yRare);
			})
		];

		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.NPCSorts";
		page.AddSortGroup(MakeOptionGroup(sorts, keyParent));
	}

	private static OptionGroup<T> MakeOptionGroup<T>(IEnumerable<(int, string, T)> opts,
		string keyParent)
	{
		var group = new OptionGroup<T>();
		if (Language.Exists($"{keyParent}.Name"))
		{
			group.Name = Language.GetText($"{keyParent}.Name");
		}

		foreach (var (id, key, v) in opts)
		{
			var icon = new UIItemIcon(new(id), false);
			group.Options.Add(new(icon, Language.GetText($"{keyParent}.{key}"), v));
		}

		return group;
	}

	// Make an option group for mods that are present in a master list of ingredients.
	private static OptionGroup<Predicate<T>> MakeModFilterGroup<T>(IEnumerable<T> ingredients)
		where T : IIngredient
	{
		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.Mods";
		var group = new OptionGroup<Predicate<T>>{
			Name = Language.GetText($"{keyParent}.Name")
		};

		var mods = ingredients
			.Select(i => i.Mod)
			.Where(m => m != null)
			.Select(m => m!)
			.Distinct()
			.OrderBy(m => m.DisplayNameClean);

		foreach (var mod in mods)
		{
			var name = Language.GetText($"{keyParent}.ModName").WithFormatArgs(mod.DisplayNameClean);
			var icon = mod.ModSourceBestiaryInfoElement.GetFilterImage();
			group.Options.Add(new(icon, name, i => i.Mod == mod));
		}

		return group;
	}

	private static T? TryGetBestiaryInfoElement<T>(int npcID)
	{
		return Main.BestiaryDB.FindEntryByNPCID(npcID).Info.OfType<T>().FirstOrDefault();
	}

	private static NPC? TryGetNPC(int npcID)
	{
		return ContentSamples.NpcsByNetId.TryGetValue(npcID, out var npc) ? npc : null;
	}
}
