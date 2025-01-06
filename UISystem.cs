using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

internal class UIQERState : UIState
{
	public override void OnInitialize()
	{
		var panel = new UIPanel();
		panel.Width.Percent = panel.Height.Percent = 0.8f;
		panel.HAlign = panel.VAlign = 0.5f;

		var scroll = new UIScrollbar();
		scroll.Height.Percent = 1;
		scroll.Width.Percent = 0.1f;
		scroll.HAlign = 1;

		var list = new UIItemList();
		list.Scrollbar = scroll;
		list.Items = Enumerable.Range(0, ItemLoader.ItemCount)
			.Select(i => new Item(i))
			.Where(i => i.type != 0)
			.ToList();
		list.Width.Percent = 0.95f;
		list.Height.Percent = 1;

		list.OnLeftClickItem += i => Main.NewText($"[i:{i.type}]");

		panel.Append(list);
		panel.Append(scroll);

		Append(panel);
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
