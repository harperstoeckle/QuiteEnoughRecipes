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
