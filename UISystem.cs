using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
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

		panel.Append(new UIItemPanel(new Item(ItemID.Zenith)));

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
