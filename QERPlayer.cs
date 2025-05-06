using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria;

namespace QuiteEnoughRecipes;

public class QERPlayer : ModPlayer
{
	public static bool ShouldGoBackInHistory { get; private set; } = false;
	public static bool ShouldGoForwardInHistory { get; private set; } = false;

	public override void ProcessTriggers(TriggersSet ts)
	{
		ShouldGoBackInHistory = false;
		ShouldGoForwardInHistory = false;
		if (UISystem.BackKey?.JustPressed ?? false)
		{
			ShouldGoForwardInHistory = Main.keyState.PressingShift();
			ShouldGoBackInHistory = !ShouldGoForwardInHistory;
		}

		if (UISystem.OpenUIKey?.JustPressed ?? false)
		{
			UISystem.ToggleOpen();
		}

		if ((UISystem.HoverSourcesKey?.JustPressed ?? false) && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			UISystem.ShowSources(new ItemIngredient(Main.HoverItem));
			UISystem.Open();
		}

		if ((UISystem.HoverUsesKey?.JustPressed ?? false) && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			UISystem.ShowUses(new ItemIngredient(Main.HoverItem));
			UISystem.Open();
		}

		if (UISystem.ToggleFullscreenKey?.JustPressed ?? false)
		{
			UISystem.ToggleFullscreen();
		}
	}
}
