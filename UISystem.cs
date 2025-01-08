using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class UISystem : ModSystem
{
	private UIQERState _ui = new();

	public static ModKeybind OpenUIKey { get; private set; }
	public static ModKeybind HoverSourcesKey { get; private set; }
	public static ModKeybind HoverUsesKey { get; private set; }

	public override void Load()
	{
		OpenUIKey = KeybindLoader.RegisterKeybind(Mod, "OpenUI", "OemTilde");
		HoverSourcesKey = KeybindLoader.RegisterKeybind(Mod, "HoverSources", "OemOpenBrackets");
		HoverUsesKey = KeybindLoader.RegisterKeybind(Mod, "HoverUses", "OemCloseBrackets");
	}

	public override void UpdateUI(GameTime t)
	{
		if (OpenUIKey.JustPressed)
		{
			IngameFancyUI.OpenUIState(_ui);
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
