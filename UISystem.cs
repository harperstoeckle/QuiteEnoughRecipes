using Microsoft.Xna.Framework;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class UISystem : ModSystem
{
	private UIQERState? _ui;

	public static ModKeybind? OpenUIKey { get; private set; }
	public static ModKeybind? HoverSourcesKey { get; private set; }
	public static ModKeybind? HoverUsesKey { get; private set; }
	public static ModKeybind? BackKey { get; private set; }

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

		// Loading items beforehand ensures that they *aren't* being loaded while scrolling.
		if (QERConfig.Instance.ShouldPreloadItems)
		{
			/*
			 * TODO: Since modded items are already loaded, should I only do this with vanilla
			 * items?
			 */
			for (int i = 0; i < ItemLoader.ItemCount; ++i)
			{
				QuiteEnoughRecipes.LoadItemAsync(i);
			}
		}
	}

	public override void UpdateUI(GameTime t)
	{
		if (_ui == null) { return; }

		if (OpenUIKey?.JustPressed ?? false)
		{
			if (_ui.IsOpen())
			{
				_ui.Close();
			}
			else
			{
				_ui.Open();
			}
		}

		if ((HoverSourcesKey?.JustPressed ?? false) && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			_ui.ShowSources(new ItemIngredient(Main.HoverItem));
			_ui.Open();
		}

		if ((HoverUsesKey?.JustPressed ?? false) && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			_ui.ShowUses(new ItemIngredient(Main.HoverItem));
			_ui.Open();
		}
	}
}
