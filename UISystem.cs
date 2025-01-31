using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class UISystem : ModSystem
{
	private UIQERState _ui;

	public static ModKeybind OpenUIKey { get; private set; }
	public static ModKeybind HoverSourcesKey { get; private set; }
	public static ModKeybind HoverUsesKey { get; private set; }
	public static ModKeybind BackKey { get; private set; }

	public override void Load()
	{
		OpenUIKey = KeybindLoader.RegisterKeybind(Mod, "OpenUI", "OemTilde");
		HoverSourcesKey = KeybindLoader.RegisterKeybind(Mod, "HoverSources", "OemOpenBrackets");
		HoverUsesKey = KeybindLoader.RegisterKeybind(Mod, "HoverUses", "OemCloseBrackets");
		BackKey = KeybindLoader.RegisterKeybind(Mod, "Back", "Back");
	}

	public override void OnWorldLoad()
	{
		/*
		 * We want to reset the UI every time the world loads so it's not carrying over weird state
		 * across worlds. There's also potentially world-specific data that needs to be handled
		 * differently in each world.
		 */
		_ui = new();
	}

	public override void UpdateUI(GameTime t)
	{
		if (OpenUIKey.JustPressed)
		{
			if (Main.InGameUI.CurrentState == _ui)
			{
				IngameFancyUI.Close();
			}
			else
			{
				IngameFancyUI.OpenUIState(_ui);
			}
		}

		if (HoverSourcesKey.JustPressed && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			_ui.ShowSources(Main.HoverItem);
			IngameFancyUI.OpenUIState(_ui);
		}

		if (HoverUsesKey.JustPressed && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			_ui.ShowUses(Main.HoverItem);
			IngameFancyUI.OpenUIState(_ui);
		}
	}
}
