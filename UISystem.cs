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

internal class UIQERState : UIState
{
	private List<Item> _allItems;
	private List<Item> _filteredItems;

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

		list.OnLeftClickItem += i => Main.mouseItem = i;

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

		foreach (var recipe in Main.recipe)
		{
			if (recipe.createItem.type == ItemID.Zenith)
			{
				recipePanel.Append(new UIRecipePanel(recipe));
				break;
			}
		}

		itemPanel.Append(list);
		itemPanel.Append(scroll);
		itemPanel.Append(search);

		Append(recipePanel);
		Append(itemPanel);
	}
}

public class UISystem : ModSystem
{
	private UIQERState _ui = new();

	public static ModKeybind OpenUIKey { get; private set; }

	public override void Load()
	{
		OpenUIKey = KeybindLoader.RegisterKeybind(Mod, "OpenUI", "OemOpenBrackets");
	}

	public override void UpdateUI(GameTime t)
	{
		if (OpenUIKey.JustPressed)
		{
			IngameFancyUI.OpenUIState(_ui);
		}
	}
}
