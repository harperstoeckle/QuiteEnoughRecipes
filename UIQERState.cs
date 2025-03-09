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
	// Inner part of recipe page; can be searched.
	private class UIRecipeList : UIElement, IQueryable<IRecipe>
	{
		private record RecipeEntry(IRecipe Recipe, UIElement Elem, List<IIngredient> Ingredients) {}

		private List<RecipeEntry> _entries = [];
		private UIList _recipeList = new(){
			Width = new(-ScrollBarWidth, 1),
			Height = new(0, 1),
			ListPadding = 30
		};

		public UIScrollbar Scrollbar { get; } = new(){
			Height = new(0, 1),
			HAlign = 1
		};

		public UIRecipeList()
		{
			_recipeList.SetScrollbar(Scrollbar);
			_recipeList.ManualSortMethod = l => {};

			Append(_recipeList);
			Append(Scrollbar);
		}

		public void SetSearchText(string text)
		{
			var query = SearchQuery.FromSearchText(text);

			var recipesToView = _entries
				.Where(e => e.Ingredients.Any(i => query.Matches(i)))
				.Select(e => e.Elem);

			_recipeList.Clear();

			foreach (var e in recipesToView)
			{
				_recipeList.Add(e);
			}
		}

		public void SetFilters(List<Predicate<IRecipe>> filters) {}
		public void SetSortComparison(Comparison<IRecipe>? comparison) {}

		public void SetRecipes(IEnumerable<IRecipe> recipes)
		{
			_entries.Clear();
			_entries.AddRange(
				recipes.Select(r => new RecipeEntry(r, r.Element, r.GetIngredients().ToList())));

			// Hack.
			SetSearchText("");
		}
	}

	private class UIRecipePage : UISearchPage<IRecipe>
	{
		private IRecipeHandler _handler;
		public float ScrollViewPosition
		{
			get => _list.Scrollbar.ViewPosition;
			set => _list.Scrollbar.ViewPosition = value;
		}

		private UIRecipeList _list;

		public LocalizedText HoverName => _handler.HoverName;
		public Item TabItem => _handler.TabItem;

		private UIRecipePage(IRecipeHandler handler, UIRecipeList list) :
			base(list, Language.GetText(""))
		{
			_list = list;
			_handler = handler;

			Width.Percent = 1;
			Height.Percent = 1;
		}

		public UIRecipePage(IRecipeHandler handler) : this(handler, new()) {}

		/*
		 * Attempts to show recipes for `ingredient`. If there are no recipes, this element remains
		 * unchanged and this function will return false.
		 */
		public bool ShowRecipes(IIngredient ingredient, QueryType queryType)
		{
			var recipes = _handler.GetRecipes(ingredient, queryType).ToList();
			if (recipes.Count == 0) { return false; }
			_list.SetRecipes(recipes);
			return true;
		}
	}

	private class UIPopupContainer : UIContainer
	{
		public override void OnOpen()
		{
			var dims = Parent?.GetInnerDimensions() ?? new CalculatedStyle();

			var mousePos = Main.MouseScreen - dims.Position();
			var popupSize = GetOuterDimensions().ToRectangle().Size();

			var pos = mousePos - new Vector2(popupSize.X - 10, 10);

			if (pos.X < 0)
			{
				pos.X = mousePos.X - 10;
			}
			if (pos.Y + popupSize.Y > dims.Height)
			{
				pos.Y = dims.Height - popupSize.Y;
			}

			Left.Pixels = pos.X;
			Top.Pixels = pos.Y;

			Recalculate();
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			base.DrawSelf(sb);
			if (IsOpen && !ContainsPoint(Main.MouseScreen)) { Close(); }
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

	// This will contain the current popup. For example, this is used for filters and sorts.
	private UIPopupContainer _popupContainer = new();

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
			.Select(i => new ItemIngredient(i))
			.ToList();

		var itemGrid = new UIQueryableIngredientGrid<ItemIngredient, UIItemPanel>(allItems);
		var itemSearchPage = new UISearchPage<ItemIngredient>(itemGrid,
			Language.GetText("Mods.QuiteEnoughRecipes.UI.ItemSearchHelp"));

		itemSearchPage.AddFilterGroup(IngredientRegistry.Instance.MakeFilterGroup<ItemIngredient>());
		itemSearchPage.AddFilterGroup(IngredientRegistry.Instance.MakeModFilterGroup<ItemIngredient>());
		itemSearchPage.AddSortGroup(IngredientRegistry.Instance.MakeSortGroup<ItemIngredient>());

		itemSearchPage.ApplyDefaults();

		var allNPCs = Enumerable.Range(0, NPCLoader.NPCCount)
			.Where(n => Main.BestiaryDB.FindEntryByNPCID(n).Icon != null)
			.Select(n => new NPCIngredient(n))
			.ToList();

		var npcGrid = new UIQueryableIngredientGrid<NPCIngredient, UINPCPanel>(allNPCs);
		var npcSearchPage = new UISearchPage<NPCIngredient>(npcGrid,
			Language.GetText("Mods.QuiteEnoughRecipes.UI.NPCSearchHelp"));

		npcSearchPage.AddFilterGroup(IngredientRegistry.Instance.MakeFilterGroup<NPCIngredient>());
		npcSearchPage.AddFilterGroup(IngredientRegistry.Instance.MakeModFilterGroup<NPCIngredient>());
		npcSearchPage.AddSortGroup(IngredientRegistry.Instance.MakeSortGroup<NPCIngredient>());

		npcSearchPage.ApplyDefaults();

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

		var recipeContainer = new UIContainer();
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
		_popupContainer.Width.Percent = 0.25f;
		_popupContainer.Height.Percent = 0.5f;

		var ingredientListPanel = new UIPanel();
		ingredientListPanel.Left.Percent = 0.51f;
		ingredientListPanel.Width.Percent = 0.45f;
		ingredientListPanel.Height.Percent = 0.8f;
		ingredientListPanel.VAlign = 0.5f;

		var ingredientListContainer = new UIContainer();
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

			ingredientListContainer.Open(page);
		};

		ingredientTabBar.OpenTabFor(itemSearchPage);

		Append(ingredientListPanel);
		Append(ingredientTabBar);
		Append(recipePanel);
		Append(_recipeTabBar);
		Append(_popupContainer);
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

	// Open `e` in a popup at the cursor.
	public void OpenPopup(UIElement e) => _popupContainer.Open(e);

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

		// Reset search bars and filters.
		foreach (var tab in _allTabs) { tab.ApplyDefaults(); }

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
			_historyIndex = _history.Count - 1;
		}
	}
}
