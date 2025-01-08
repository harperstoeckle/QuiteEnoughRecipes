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
	private List<Item> _allItems;
	private List<Item> _filteredItems;
	private UIList _recipeList = new();

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

		var recipePanel = new UIPanel();
		recipePanel.Left.Percent = 0.04f;
		recipePanel.Width.Percent = 0.45f;
		recipePanel.Height.Percent = 0.8f;
		recipePanel.VAlign = 0.5f;

		const float ItemPanelBarHeight = 30;
		const float ScrollBarWidth = 30;

		var itemPanel = new UIPanel();
		itemPanel.Left.Percent = 0.51f;
		itemPanel.Width.Percent = 0.45f;
		itemPanel.Height.Percent = 0.8f;
		itemPanel.VAlign = 0.5f;

		var scroll = new UIScrollbar();
		scroll.Height = new StyleDimension(-ItemPanelBarHeight, 1);
		scroll.Width.Pixels = ScrollBarWidth;
		scroll.HAlign = 1;
		scroll.VAlign = 1;

		var list = new UIItemList();
		list.Scrollbar = scroll;
		list.Items = _filteredItems;
		list.Width = new StyleDimension(-ScrollBarWidth, 1);
		list.Height = new StyleDimension(-ItemPanelBarHeight, 1);
		list.VAlign = 1;

		var recipeScroll = new UIScrollbar();
		recipeScroll.Height.Percent = 1;
		recipeScroll.Width = new StyleDimension(-ScrollBarWidth, 1);
		recipeScroll.HAlign = 1;

		_recipeList.Width = new StyleDimension(-ScrollBarWidth, 1);
		_recipeList.Height.Percent = 1;
		_recipeList.ListPadding = 15;
		_recipeList.SetScrollbar(recipeScroll);

		var search = new UISearchBar(Language.GetText(""), 1);
		search.Width.Percent = 1;
		search.Height.Pixels = ItemPanelBarHeight;

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
			_filteredItems.Clear();
			_filteredItems.AddRange(_allItems.Where(i => i.Name.ToLower().StartsWith(s)));
			list.Items = _filteredItems;
		};

		recipePanel.Append(_recipeList);
		recipePanel.Append(recipeScroll);

		itemPanel.Append(list);
		itemPanel.Append(scroll);
		itemPanel.Append(search);

		Append(recipePanel);
		Append(itemPanel);
	}

	// Open the recipe panel to a list of sources for an item.
	public void ShowSources(Item i)
	{
		_recipeList.Clear();

		foreach (var r in Main.recipe)
		{
			if (r.createItem.type == i.type)
			{
				_recipeList.Add(new UIRecipePanel(r));
			}
		}

		_recipeList.Activate();
	}

	// Open the recipe panel to a list of uses for an item.
	public void ShowUses(Item i)
	{
		_recipeList.Clear();

		foreach (var r in Main.recipe)
		{
			if (RecipeAcceptsItem(r, i))
			{
				_recipeList.Add(new UIRecipePanel(r));
			}
		}

		_recipeList.Activate();
	}

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

	private static bool RecipeAcceptsItem(Recipe r, Item i)
	{
		return r.requiredItem.Any(x => x.type == i.type)
			|| r.acceptedGroups.Any(
				g => RecipeGroup.recipeGroups.TryGetValue(g, out var rg) && rg.ContainsItem(i.type)
			);
	}
}
