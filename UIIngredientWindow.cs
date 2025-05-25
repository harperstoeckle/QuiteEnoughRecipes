using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace QuiteEnoughRecipes;

public class UIIngredientWindow : UIWindow
{
	private const float TabHeight = 50;

	// This will be re-focused when the browser is opened.
	private IFocusableSearchPage? _pageToFocusOnOpen = null;

	public UIIngredientWindow()
	{
		// For now, we have a help button for both item and NPC search.
		AddHelp(Language.GetText("Mods.QuiteEnoughRecipes.UI.ItemSearchHelp"));
		AddHelp(Language.GetText("Mods.QuiteEnoughRecipes.UI.NPCSearchHelp"));

		var itemGrid = new UIQueryableIngredientGrid<ItemIngredient, UIItemPanel>();
		var itemSearchPage = new UISearchPage(itemGrid);

		var npcGrid = new UIQueryableIngredientGrid<NPCIngredient, UINPCPanel>();
		var npcSearchPage = new UISearchPage(npcGrid);

		var ingredientListContainer = new UIContainer(){
			Width = new(0, 1),
			Height = new(-TabHeight, 1),
			VAlign = 1,
		};

		var ingredientTabBar = new UITabBar<UIElement>(){
			Width = new(0, 1),
			Height = new(TabHeight, 0),
		};
		ingredientTabBar.Width = new(0, 1);
		ingredientTabBar.Height.Pixels = TabHeight;
		ingredientTabBar.AddTab(Language.GetText("Mods.QuiteEnoughRecipes.Tabs.ItemList"),
			new Item(ItemID.IronBar), itemSearchPage);
		ingredientTabBar.AddTab(Language.GetText("Mods.QuiteEnoughRecipes.Tabs.NPCList"),
			new Item(ItemID.Bunny), npcSearchPage);

		ingredientTabBar.OnTabSelected += page => {
			UIQERSearchBar.UnfocusAll();

			if (QERConfig.Instance.AutoFocusSearchBars && page is IFocusableSearchPage s)
			{
				if (UISystem.IsOpen()) { s.FocusSearchBar(); }
				_pageToFocusOnOpen = s;
			}

			ingredientListContainer.Open(page);
		};

		ingredientTabBar.OpenTabFor(itemSearchPage);

		Contents.Append(ingredientListContainer);
		Contents.Append(ingredientTabBar);
	}

	public override void OnOpen()
	{
		/*
		 * TODO: `OnOpen` is the wrong place to put this, since it won't be activated when the UI
		 * is reopened.
		 */
		if (QERConfig.Instance.AutoFocusSearchBars) { _pageToFocusOnOpen?.FocusSearchBar(); }
	}
}
