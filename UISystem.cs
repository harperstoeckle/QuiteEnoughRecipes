using Microsoft.Xna.Framework;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class UISystem : ModSystem
{
	public static UIQERState? UI { get; private set; }

	public static ModKeybind? OpenUIKey { get; private set; }
	public static ModKeybind? HoverSourcesKey { get; private set; }
	public static ModKeybind? HoverUsesKey { get; private set; }
	public static ModKeybind? BackKey { get; private set; }

	public static bool ShouldGoBackInHistory { get; private set; } = false;
	public static bool ShouldGoForwardInHistory { get; private set; } = false;

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
		UI = new();

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
		/*
		 * This has to be handled here to work even when autopaused. This is (I think) the same set
		 * of conditions under which `ModPlayer::ProcessTriggers` is called.
		 */
		if (Main.hasFocus && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest
				&& !Main.blockInput)
		{
			HandleInput();
		}
	}

	private void HandleInput()
	{
		ShouldGoBackInHistory = false;
		ShouldGoForwardInHistory = false;

		if (UISystem.BackKey?.JustPressed ?? false)
		{
			ShouldGoForwardInHistory = Main.keyState.PressingShift();
			ShouldGoBackInHistory = !ShouldGoForwardInHistory;
		}

		if (UISystem.UI == null) { return; }

		if (UISystem.OpenUIKey?.JustPressed ?? false)
		{
			if (UISystem.UI.IsOpen())
			{
				UISystem.UI.Close();
			}
			else
			{
				UISystem.UI.Open();
			}
		}

		if ((UISystem.HoverSourcesKey?.JustPressed ?? false) && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			UISystem.UI.ShowSources(new ItemIngredient(Main.HoverItem));
			UISystem.UI.Open();
		}

		if ((UISystem.HoverUsesKey?.JustPressed ?? false) && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			UISystem.UI.ShowUses(new ItemIngredient(Main.HoverItem));
			UISystem.UI.Open();
		}
	}
}
