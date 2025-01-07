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

		var itemPanel = new UIPanel();
		itemPanel.Left.Percent = 0.51f;
		itemPanel.Width.Percent = 0.45f;
		itemPanel.Height.Percent = 0.8f;
		itemPanel.VAlign = 0.5f;

		var scroll = new UIScrollbar();
		scroll.Height.Percent = 0.9f;
		scroll.Width.Percent = 0.1f;
		scroll.HAlign = 1;
		scroll.VAlign = 1;

		var list = new UIItemList();
		list.Scrollbar = scroll;
		list.Items = _filteredItems;
		list.Width.Percent = 0.95f;
		list.Height.Percent = 0.9f;
		list.VAlign = 1;

		var recipeScroll = new UIScrollbar();
		recipeScroll.Height.Percent = 1;
		recipeScroll.Width.Percent = 0.1f;
		recipeScroll.HAlign = 1;

		_recipeList.Width.Percent = 0.95f;
		_recipeList.Height.Percent = 1;
		_recipeList.ListPadding = 15;
		_recipeList.SetScrollbar(recipeScroll);

		var search = new UISearchBar(Language.GetText(""), 1);
		search.Width.Percent = 1;
		search.Height.Percent = 0.1f;

		search.OnLeftClick += (evt, elem) => {
			if (elem is UISearchBar s)
			{
				s.ToggleTakingText();
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

	public override void LeftClick(UIMouseEvent e)
	{
		if (e.Target is UIItemPanel p && p.DisplayedItem != null)
		{
			_recipeList.Clear();

			foreach (var r in Main.recipe)
			{
				if (r.createItem.type == p.DisplayedItem.type)
				{
					_recipeList.Add(new UIRecipePanel(r));
				}
			}

			_recipeList.Activate();
		}
	}

	public override void RightClick(UIMouseEvent e)
	{
		if (e.Target is UIItemPanel p && p.DisplayedItem != null)
		{
			_recipeList.Clear();

			foreach (var r in Main.recipe)
			{
				if (r.requiredItem.Any(i => i.type == p.DisplayedItem.type))
				{
					_recipeList.Add(new UIRecipePanel(r));
				}
			}

			_recipeList.Activate();
		}
	}
}
